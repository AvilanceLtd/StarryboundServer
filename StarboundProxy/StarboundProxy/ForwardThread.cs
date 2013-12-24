/* 
 * Starrybound Server
 * Copyright 2013, Avilance Ltd
 * Created by Zidonuke (zidonuke@gmail.com) and Crashdoom (crashdoom@avilance.com)
 * 
 * This file is a part of Starrybound Server.
 * Starrybound Server is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 * Starrybound Server is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
 * You should have received a copy of the GNU General Public License along with Starrybound Server. If not, see http://www.gnu.org/licenses/.
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using com.avilance.Starrybound.Util;
using com.avilance.Starrybound.Extensions;
using Ionic.Zlib;
using com.avilance.Starrybound.Packets;

namespace com.avilance.Starrybound
{
    class ForwardThread
    {
        BinaryReader mInput;
        BinaryWriter mOutput;
        ClientThread mParent;
        Direction mDirection;

        public ForwardThread(ClientThread aParent, BinaryReader aInput, BinaryWriter aOutput, Direction aDirection) {
            this.mParent = aParent;
            this.mInput = aInput;
            this.mOutput = aOutput;
            this.mDirection = aDirection;
        }

        public void run()
        {
            try
            {
                for (;;)
                {
                    if (this.mParent.blockPackets)
                    {
                        if (this.mParent.targetTimestamp < StarryboundServer.getTimestamp())
                        {
                            this.mParent.connectionClosed();
                            return;
                        }
                        continue;
                    }

                    if (!this.mParent.connectionAlive) this.mParent.connectionLost(mDirection, "connection no longer alive");

                    //Packet ID and Vaildity Check.
                    uint temp = this.mInput.ReadVarUInt32();
                    if (temp < 1 || temp > 48)
                    {
                        StarryboundServer.logException("[" + this.mParent.clientUUID + "][" + this.mDirection.ToString() + "] ERROR: Invalid Packet ID: [" + temp + "]");
                        this.mParent.connectionLost(this.mDirection, "invalid packet ID");
                        return;
                    }
                    Packet packetID = (Packet)temp;
                    string packetName = temp.ToString();

                    //Packet Size and Compression Check.
                    bool compressed = false;
                    int packetSize = this.mInput.ReadVarInt32();
                    if (packetSize < 0)
                    {
                        packetSize = -packetSize;
                        compressed = true;
                    }

                    //Create buffer for forwarding
                    byte[] dataBuffer = this.mInput.ReadFully(packetSize);

                    //Do decompression
                    MemoryStream ms = new MemoryStream();
                    if (compressed)
                    {
                        ZlibStream compressedStream = new ZlibStream(new MemoryStream(dataBuffer), CompressionMode.Decompress);
                        byte[] buffer = new byte[32768];
                        for (;;)
                        {
                            int read = compressedStream.Read(buffer, 0, buffer.Length);
                            if (read <= 0)
                                break;
                            ms.Write(buffer, 0, read);
                        }
                        ms.Seek(0, SeekOrigin.Begin);
                    }
                    else
                    {
                        ms = new MemoryStream(dataBuffer);
                    }

                    //Create packet parser
                    BinaryReader packetData = new BinaryReader(ms);

                    //Return data for packet processor
                    object returnData = true;

                    //Process packet
                    if (packetID == Packet.ChatSend && mDirection == Direction.Client)
                    {
                        returnData = new Packet11ChatSend(this.mParent, packetData, this.mDirection).onReceive();
                    }
                    else if (packetID == Packet.ChatReceive && mDirection == Direction.Server)
                    {
                        returnData = new Packet5ChatReceive(this.mParent, packetData, this.mDirection).onReceive();
                    }
                    else if (packetID == Packet.ProtocolVersion && mDirection == Direction.Server)
                    {
                        uint protocolVersion = packetData.ReadUInt32BE();
                        if(protocolVersion != StarryboundServer.ProtocolVersion)
                        {
                            StarryboundServer.logFatal("[" + this.mParent.clientUUID + "][" + this.mDirection.ToString() + "] ProtocolVersion:[" + protocolVersion + "] Cannot handle this protocol version.");
                            MemoryStream packet = new MemoryStream();
                            BinaryWriter packetWrite = new BinaryWriter(packet);
                            packetWrite.WriteBE(protocolVersion);
                            this.mParent.sendClientPacket(Packet.ProtocolVersion, packet.ToArray());
                            
                            Packet2ConnectResponse packet2 = new Packet2ConnectResponse(this.mParent, false, Util.Direction.Client);
                            packet2.prepare(false, 0, "Starrybound Server was unable to handle the parent server protocol version.");
                            packet2.onSend();
                            returnData = false;
                            this.mParent.connectionClosed();
                        }
                        StarryboundServer.logDebug("[" + this.mParent.clientUUID + "][" + this.mDirection.ToString() + "] ProtocolVersion:[" + protocolVersion + "]");
                    }
                    else if (packetID == Packet.ClientConnect && mDirection == Direction.Client)
                    {
                        this.mParent.clientState = ClientState.PendingAuthentication;
                        returnData = new Packet7ClientConnect(this.mParent, packetData, this.mDirection).onReceive();
                        MemoryStream packet = new MemoryStream();
                        BinaryWriter packetWrite = new BinaryWriter(packet);

                        this.mParent.passwordSalt = Utils.GenerateSecureSalt();
                        packetWrite.WriteStarString("");
                        packetWrite.WriteStarString(this.mParent.passwordSalt);
                        packetWrite.WriteBE(StarryboundServer.config.passwordRounds);
                        this.mParent.sendClientPacket(Packet.HandshakeChallenge, packet.ToArray());
                    }
                    else if (packetID == Packet.HandshakeChallenge && mDirection == Direction.Server)
                    {
                        string claimMessage = packetData.ReadString();
                        string passwordSalt = packetData.ReadStarString();
                        int passwordRounds = packetData.ReadInt32BE();

                        MemoryStream packet = new MemoryStream();
                        BinaryWriter packetWrite = new BinaryWriter(packet);
                        string passwordHash = Utils.StarHashPassword(StarryboundServer.config.serverPass, StarryboundServer.config.serverAccount + passwordSalt, passwordRounds);
                        packetWrite.WriteStarString("");
                        packetWrite.WriteStarString(passwordHash);
                        this.mParent.sendServerPacket(Packet.HandshakeResponse, packet.ToArray());

                        returnData = false;
                    }
                    else if (packetID == Packet.HandshakeResponse && mDirection == Direction.Client)
                    {
                        string claimResponse = packetData.ReadStarString();
                        string passwordHash = packetData.ReadStarString();

                        string verifyHash = Utils.StarHashPassword(StarryboundServer.config.proxyPass, this.mParent.playerData.account + this.mParent.passwordSalt, StarryboundServer.config.passwordRounds);
                        if(passwordHash != verifyHash)
                        {
                            StarryboundServer.logWarn("[" + this.mParent.clientUUID + "][" + this.mDirection.ToString() + "] Could not authenticate with password.");
                            Packet2ConnectResponse packet = new Packet2ConnectResponse(this.mParent, false, Util.Direction.Client);
                            packet.prepare(false, 0, "Your password was incorrect.");
                            packet.onSend();
                            this.mParent.connectionClosed();
                        }

                        this.mParent.clientState = ClientState.PendingConnectResponse;
                        returnData = false;
                    }
                    else if (packetID == Packet.ConnectResponse && mDirection == Direction.Server)
                    {
                        while (this.mParent.clientState != ClientState.PendingConnectResponse) { } //TODO: needs timeout
                        returnData = new Packet2ConnectResponse(this.mParent, packetData, this.mDirection).onReceive();
                    }
                    else if (packetID == Packet.WorldStart && mDirection == Direction.Server)
                    {
                        byte[] data1 = packetData.ReadStarByteArray();
                        byte[] data2 = packetData.ReadStarByteArray();
                        byte[] data3 = packetData.ReadStarByteArray();
                        byte[] data4 = packetData.ReadStarByteArray();
                        float spawnX = packetData.ReadSingleBE();
                        float spawnY = packetData.ReadSingleBE();
                        uint mapParamsSize = packetData.ReadVarUInt32();
                        List<string> mapParamsKeys = new List<string>();
                        List<object> mapParamsValues = new List<object>();
                        for (int i = 0; i < mapParamsSize; i++)
                        {
                            mapParamsKeys.Add(packetData.ReadStarString());
                            mapParamsValues.Add(packetData.ReadStarVariant());
                        }
                        uint uint1 = packetData.ReadUInt32BE();
                        bool bool1 = packetData.ReadBoolean();

                        if (packetData.BaseStream.Position != ms.Length)
                            StarryboundServer.logException("[" + this.mParent.clientUUID + "][" + this.mDirection.ToString() + "] WorldStart failed parse (" + packetData.BaseStream.Position + " != " + ms.Length + ")");
                        else
                            StarryboundServer.logDebug("[" + this.mParent.clientUUID + "][" + this.mDirection.ToString() + "] WorldStart:[" + spawnX + ":" + spawnY + ":" + uint1 + ":" + bool1.ToString() + ":" + string.Join(":", mapParamsKeys.ToArray()) + ":" + Encoding.ASCII.GetString(data1) + ":" + Encoding.ASCII.GetString(data2) + ":" + Encoding.ASCII.GetString(data3) + ":" + Encoding.ASCII.GetString(data4) + "]");
                    }
                    else if (packetID == Packet.WorldStop && mDirection == Direction.Server)
                    {
                        string status = packetData.ReadStarString();
                        StarryboundServer.logDebug("[" + this.mParent.clientUUID + "][" + this.mDirection.ToString() + "] WorldStop:[" + status + "]");
                    }
                    else if (packetID == Packet.WarpCommand && mDirection == Direction.Client)
                    {
                        uint warp = packetData.ReadUInt32BE();
                        string sector = packetData.ReadStarString();
                        int x = packetData.ReadInt32BE();
                        int y = packetData.ReadInt32BE();
                        int z = packetData.ReadInt32BE();
                        int planet = packetData.ReadInt32BE();
                        int satellite = packetData.ReadInt32BE();
                        string player = packetData.ReadStarString();
                        StarryboundServer.logInfo(packetID + ":" + Utils.ByteArrayToString(ms.ToArray()));
                        StarryboundServer.logDebug("[" + this.mParent.clientUUID + "][" + this.mDirection.ToString() + "] WarpCommand:[" + warp + "][" + sector + ":" + x + ":" + y + ":" + z + ":" + planet + ":" + satellite + "][" + player + "]");
                    }
                    else if (packetID == Packet.TileDamageUpdate)
                    {

                    }
                    else if (packetID == Packet.TileUpdate)
                    {

                    }
                    else if (packetID == Packet.TileLiquidUpdate)
                    {

                    }
                    else if (packetID == Packet.TileArrayUpdate)
                    {

                    }
                    else if (packetID == Packet.TileDamageUpdate)
                    {

                    }
                    else if(packetID == Packet.DamageTile)
                    {
                        if (!this.mParent.playerData.canBuild) continue;
                    }
                    else if (packetID == Packet.DamageTileGroup)
                    {
                        if (!this.mParent.playerData.canBuild) continue;
                    }
                    else if (packetID == Packet.WorldClientStateUpdate && mDirection == Direction.Client)
                    {
                        byte[] byteArray = packetData.ReadStarByteArray();
                        if (byteArray.Length > 0)
                            StarryboundServer.logDebug("[" + this.mParent.clientUUID + "][" + this.mDirection.ToString() + "] WorldClientStateUpdate:[" + Encoding.ASCII.GetString(byteArray) + "]");
                    }
                    else if(packetID == Packet.ClientContextUpdate && mDirection == Direction.Client)
                    {
                        byte[] byteArray = packetData.ReadStarByteArray();
                        StarryboundServer.logDebug("[" + this.mParent.clientUUID + "][" + this.mDirection.ToString() + "] Dumping packet [" + packetName + ":" + packetID + "] (" + ms.Length + ") " + Encoding.ASCII.GetString(ms.ToArray()));
                    }
                    else if(packetID == Packet.ClientContextUpdate && mDirection == Direction.Server)
                    {
                        //StarryboundServer.logDebug("[" + this.mParent.clientUUID + "][" + this.mDirection.ToString() + "] Dumping packet [" + packetName + ":" + packetID + "] (" + ms.Length + ") bytes: " + Utils.ByteArrayToString(ms.ToArray()));
                        //StarryboundServer.logDebug("[" + Encoding.ASCII.GetString(ms.ToArray()) + "]");
                    }
                    else if (packetID == Packet.Heartbeat || packetID == Packet.UniverseTimeUpdate || packetID == Packet.SkyUpdate) //Time updates and heartbeats can gtfo.
                    {

                    }
                    else if(packetID == Packet.EntityCreate && mDirection == Direction.Client)
                    {
                        StarryboundServer.logDebug("[" + this.mParent.clientUUID + "][" + this.mDirection.ToString() + "] Dumping packet [" + packetName + ":" + packetID + "] (" + ms.Length + ") " + Encoding.ASCII.GetString(ms.ToArray()));
                    }
                    else if (packetID == Packet.EntityUpdate && mDirection == Direction.Client)
                    {
                        StarryboundServer.logDebug("[" + this.mParent.clientUUID + "][" + this.mDirection.ToString() + "] Dumping packet [" + packetName + ":" + packetID + "] (" + ms.Length + ") bytes: " + Utils.ByteArrayToString(dataBuffer));
                    }
                    else if (StarryboundServer.config.debug)
                    {
                        StarryboundServer.logDebug("[" + this.mParent.clientUUID + "][" + this.mDirection.ToString() + "] Dumping packet [" + packetName + ":" + packetID + "] (" + ms.Length + ") bytes: " + Utils.ByteArrayToString(ms.ToArray()));
                    }

                    if (returnData is Boolean)
                    {
                        if ((Boolean)returnData == false) continue;
                    }
                    else if (returnData is int)
                    {
                        if ((int)returnData == -1)
                        {
                            throw new Exception("Command processor requested to drop client " + this.mParent.playerData.name);
                        }
                    }

                    this.mOutput.WriteVarUInt32((uint)packetID);
                    if (compressed)
                    {
                        this.mOutput.WriteVarInt32(-packetSize);
                        this.mOutput.Write(dataBuffer, 0, packetSize);
                    }
                    else
                    {
                        this.mOutput.WriteVarInt32(packetSize);
                        this.mOutput.Write(dataBuffer, 0, packetSize);
                    }
                    this.mOutput.Flush();

                    if(packetID == Packet.ServerDisconnect && mDirection == Direction.Server)
                    {
                        this.mParent.clientState = ClientState.Disposing;
                        StarryboundServer.sendGlobalMessage(this.mParent.playerData.name + "has left the server.");
                        StarryboundServer.clients.Remove(this.mParent.playerData.name);
                        this.mParent.connectionClosed();
                    }
                }
            }
            catch (Exception e)
            {
                StarryboundServer.logException("[" + this.mParent.clientUUID + "][" + this.mDirection + "]" + e.ToString());
                this.mParent.connectionLost(this.mDirection, "Exception: " + e.Message);
            }
        }
    }
}
