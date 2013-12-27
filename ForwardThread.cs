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
        private string passwordSalt;

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
                    if (!this.mParent.connectionAlive)
                    {
                        this.mParent.forceDisconnect("Connection Lost");
                        return;
                    }


                    if (this.mParent.kickTargetTimestamp != 0)
                    {
                        if (this.mParent.kickTargetTimestamp < Utils.getTimestamp())
                        {
                            this.mParent.forceDisconnect("Kicked from server");
                            return;
                        }
                        continue;
                    }

                    #region Process Packet
                    //Packet ID and Vaildity Check.
                    uint temp = this.mInput.ReadVarUInt32();
                    if (temp < 1 || temp > 48)
                    {
                        this.mParent.errorDisconnect(mDirection, "Sent invalid packet ID [" + temp + "].");
                        return;
                    }
                    Packet packetID = (Packet)temp;

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
                    #endregion

                    //Return data for packet processor
                    object returnData = true;

                    if (mDirection == Direction.Client)
                    #region Handle Client Packets
                    {
                        #region Protocol State Security
                        ClientState curState = this.mParent.clientState;
                        if(curState != ClientState.Connected)
                        {
                            if(curState == ClientState.PendingConnect && packetID != Packet.ClientConnect)
                            {
                                this.mParent.forceDisconnect("Violated PendingConnect protocol state with " + packetID);
                            }
                            else if(curState == ClientState.PendingAuthentication && packetID != Packet.HandshakeResponse)
                            {
                                this.mParent.forceDisconnect("Violated PendingAuthentication protocol state with " + packetID);
                            }
                            else if(curState == ClientState.PendingConnectResponse)
                            {
                                this.mParent.forceDisconnect("Violated PendingConnectResponse protocol state with " + packetID);
                            }
                        }
                        #endregion

                        if (packetID == Packet.ChatSend)
                        {
                            returnData = new Packet11ChatSend(this.mParent, packetData, this.mDirection).onReceive();
                        }
                        else if (packetID == Packet.ClientConnect)
                        {
                            this.mParent.clientState = ClientState.PendingAuthentication;
                            returnData = new Packet7ClientConnect(this.mParent, packetData, this.mDirection).onReceive();
                            MemoryStream packet = new MemoryStream();
                            BinaryWriter packetWrite = new BinaryWriter(packet);

                            passwordSalt = Utils.GenerateSecureSalt();
                            packetWrite.WriteStarString("");
                            packetWrite.WriteStarString(passwordSalt);
                            packetWrite.WriteBE(StarryboundServer.config.passwordRounds);
                            this.mParent.sendClientPacket(Packet.HandshakeChallenge, packet.ToArray());
                        }
                        else if (packetID == Packet.HandshakeResponse)
                        {
                            string claimResponse = packetData.ReadStarString();
                            string passwordHash = packetData.ReadStarString();

                            string verifyHash = Utils.StarHashPassword(StarryboundServer.config.proxyPass, this.mParent.playerData.account + passwordSalt, StarryboundServer.config.passwordRounds);
                            if (passwordHash != verifyHash)
                            {
                                this.mParent.rejectPreConnected("Your password was incorrect.");
                            }

                            this.mParent.clientState = ClientState.PendingConnectResponse;
                            returnData = false;
                        }
                        else if (packetID == Packet.WarpCommand)
                        {
                            uint warp = packetData.ReadUInt32BE();
                            WorldCoordinate coord = packetData.ReadStarWorldCoordinate();
                            string player = packetData.ReadStarString();
                            WarpType cmd = (WarpType)warp;
                            if(cmd == WarpType.WarpToHomePlanet)
                            {
                                this.mParent.playerData.inPlayerShip = false;
                            }
                            else if(cmd == WarpType.WarpToOrbitedPlanet)
                            {
                                this.mParent.playerData.inPlayerShip = false;
                            }
                            else if (cmd == WarpType.WarpToOwnShip)
                            {
                                this.mParent.playerData.inPlayerShip = true;
                            }
                            else if(cmd == WarpType.WarpToPlayerShip)
                            {
                                this.mParent.playerData.inPlayerShip = true;
                            }

                            StarryboundServer.logDebug("WarpCommand", "[" + this.mParent.playerData.client + "][" + warp + "]" + (coord != null ? "[" + coord.ToString() + "]" : "") + "[" + player + "]");
                        }
                        else if (packetID == Packet.DamageTile)
                        {
                            if (!this.mParent.playerData.canBuild) continue;
                            if (this.mParent.playerData.loc != null)
                            {
                                string planetCheck = this.mParent.playerData.loc.ToString();
                                string spawnPlanet = StarryboundServer.serverConfig.defaultWorldCoordinate;

                                if (StarryboundServer.serverConfig.defaultWorldCoordinate.Split(':').Length == 5) spawnPlanet = spawnPlanet + ":0";

                                if ((planetCheck == spawnPlanet) && !this.mParent.playerData.group.hasPermission("admin.spawnbuild") && !this.mParent.playerData.inPlayerShip)
                                {
                                    continue;
                                }
                            }
                        }
                        else if (packetID == Packet.DamageTileGroup)
                        {
                            if (!this.mParent.playerData.canBuild) continue;
                            if (this.mParent.playerData.loc != null)
                            {
                                string planetCheck = this.mParent.playerData.loc.ToString();
                                string spawnPlanet = StarryboundServer.serverConfig.defaultWorldCoordinate;

                                if (StarryboundServer.serverConfig.defaultWorldCoordinate.Split(':').Length == 5) spawnPlanet = spawnPlanet + ":0";

                                if ((planetCheck == spawnPlanet) && !this.mParent.playerData.group.hasPermission("admin.spawnbuild") && !this.mParent.playerData.inPlayerShip)
                                {
                                    continue;
                                }
                            }
                        }
                        else if (packetID == Packet.ModifyTileList)
                        {
                            if (!this.mParent.playerData.canBuild) continue;
                            if (this.mParent.playerData.loc != null)
                            {
                                string planetCheck = this.mParent.playerData.loc.ToString();
                                string spawnPlanet = StarryboundServer.serverConfig.defaultWorldCoordinate;

                                if (StarryboundServer.serverConfig.defaultWorldCoordinate.Split(':').Length == 5) spawnPlanet = spawnPlanet + ":0";

                                if ((planetCheck == spawnPlanet) && !this.mParent.playerData.group.hasPermission("admin.spawnbuild") && !this.mParent.playerData.inPlayerShip)
                                {
                                    continue;
                                }
                            }
                        }
                    }
                    #endregion
                    else
                    #region Handle Server Packets
                    {
                        if (packetID == Packet.ChatReceive)
                        {
                            returnData = new Packet5ChatReceive(this.mParent, packetData, this.mDirection).onReceive();
                        }
                        else if (packetID == Packet.ProtocolVersion)
                        {
                            uint protocolVersion = packetData.ReadUInt32BE();
                            if (protocolVersion != StarryboundServer.ProtocolVersion)
                            {
                                MemoryStream packet = new MemoryStream();
                                BinaryWriter packetWrite = new BinaryWriter(packet);
                                packetWrite.WriteBE(protocolVersion);
                                this.mParent.sendClientPacket(Packet.ProtocolVersion, packet.ToArray());

                                this.mParent.rejectPreConnected("Starrybound Server was unable to handle the parent server protocol version.");
                                returnData = false;
                            }
                        }
                        else if (packetID == Packet.HandshakeChallenge)
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
                        else if (packetID == Packet.ConnectResponse)
                        {
                            while (this.mParent.clientState != ClientState.PendingConnectResponse) { } //TODO: needs timeout
                            returnData = new Packet2ConnectResponse(this.mParent, packetData, this.mDirection).onReceive();
                        }
                        else if (packetID == Packet.WorldStart)
                        {
                            if (!this.mParent.playerData.sentMotd)
                            {
                                this.mParent.sendChatMessage(Config.GetMotd());
                                this.mParent.playerData.sentMotd = true;
                            }

                            byte[] planet = packetData.ReadStarByteArray();
                            byte[] worldStructure = packetData.ReadStarByteArray();
                            byte[] sky = packetData.ReadStarByteArray();
                            byte[] serverWeather = packetData.ReadStarByteArray();
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
                            uint clientID = packetData.ReadUInt32BE();
                            bool bool1 = packetData.ReadBoolean();
                            WorldCoordinate coords = Utils.findGlobalCoords(sky);
                            if (coords != null)
                            {
                                this.mParent.playerData.loc = coords;
                                StarryboundServer.logDebug("WorldStart", "[" + this.mParent.playerData.client + "][" + bool1 + ":" + clientID + "] CurLoc:[" + this.mParent.playerData.loc.ToString() + "][" + this.mParent.playerData.inPlayerShip + "]");
                            }   
                        }
                        else if (packetID == Packet.WorldStop)
                        {
                            string status = packetData.ReadStarString();
                        }
                        else if (packetID == Packet.GiveItem)
                        {
                            string name = packetData.ReadStarString();
                            uint count = packetData.ReadVarUInt32();
                            List<object> itemDesc = packetData.ReadStarVariant();
                        }
                        else if (packetID == Packet.EnvironmentUpdate)
                        {
                            byte[] sky = packetData.ReadStarByteArray();
                            byte[] serverWeather = packetData.ReadStarByteArray();
                            /*WorldCoordinate coords = Utils.findGlobalCoords(sky);
                            if (coords != null)
                            {
                                this.mParent.playerData.loc = coords;
                                StarryboundServer.logDebug("EnvUpdate", "[" + this.mParent.playerData.client + "] CurLoc:[" + this.mParent.playerData.loc.ToString() + "]");
                            }*/
                        }
                        else if (packetID == Packet.ClientContextUpdate)
                        {
                            try
                            {
                                byte[] clientContextData = packetData.ReadStarByteArray();
                                if (clientContextData.Length != 0)
                                {
                                    BinaryReader clientContextReader = new BinaryReader(new MemoryStream(clientContextData));
                                    byte[] data = clientContextReader.ReadStarByteArray();
                                    if (data.Length > 8) //Should at least be more than 8 bytes for it to contain the data we want.
                                    {
                                        BinaryReader dataReader = new BinaryReader(new MemoryStream(data));
                                        byte dataBufferLength = dataReader.ReadByte();
                                        if(dataBufferLength == 2)
                                        {
                                            byte arrayLength = dataReader.ReadByte();
                                            if (arrayLength == 2 || arrayLength == 4) //Only observed these being used so far for what we want.
                                            {
                                                byte dataType = dataReader.ReadByte(); //04 = String, 0E = CelestialLog
                                                if (dataType == 4)
                                                {
                                                    string string1 = dataReader.ReadStarString();
                                                    if (dataReader.BaseStream.Position != dataReader.BaseStream.Length)
                                                    {
                                                        if (string1 == "null")
                                                        {
                                                            byte[] worldHeader = dataReader.ReadStarByteArray(); //0008020A000C
                                                            byte[] worldData = dataReader.ReadStarByteArray();
                                                            byte typeByte = dataReader.ReadByte(); //0E = CelestialLog
                                                            if (typeByte == 14)
                                                            {
                                                                Dictionary<string, WorldCoordinate> log = dataReader.ReadStarCelestialLog();
                                                                log.TryGetValue("loc", out this.mParent.playerData.loc);
                                                                if(!log.TryGetValue("home", out this.mParent.playerData.home))
                                                                    this.mParent.playerData.home = this.mParent.playerData.loc;
                                                                StarryboundServer.logDebug("ClientContext", "[" + this.mParent.playerData.client + "] CurLoc:[" + this.mParent.playerData.loc.ToString() + "][" + this.mParent.playerData.inPlayerShip + "]");
                                                                StarryboundServer.logDebug("ClientContext", "[" + this.mParent.playerData.client + "] CurHome:[" + this.mParent.playerData.home.ToString() + "]");
                                                            }
                                                        }
                                                    }
                                                }
                                                else if (dataType == 14)
                                                {
                                                    Dictionary<string, WorldCoordinate> log = dataReader.ReadStarCelestialLog();
                                                    log.TryGetValue("loc", out this.mParent.playerData.loc);
                                                    if (!log.TryGetValue("home", out this.mParent.playerData.home))
                                                        this.mParent.playerData.home = this.mParent.playerData.loc;
                                                    StarryboundServer.logDebug("ClientContext", "[" + this.mParent.playerData.client + "] CurLoc:[" + this.mParent.playerData.loc.ToString() + "][" + this.mParent.playerData.inPlayerShip + "]");
                                                    StarryboundServer.logDebug("ClientContext", "[" + this.mParent.playerData.client + "] CurHome:[" + this.mParent.playerData.home.ToString() + "]");
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            catch(Exception e)
                            {
                                StarryboundServer.logException("[" + this.mParent.playerData.client + "] Failed to parse ClientContextUpdate from Server: " + e.Message);
                            }
                        }
                    }
                    #endregion

                    #if DEBUG
                    if(packetID != Packet.Heartbeat)
                    {
                        //if (ms.Position != ms.Length)
                            //StarryboundServer.logDebug("ForwardThread", "[" + this.mParent.playerData.client + "] [" + this.mDirection.ToString() + "][" + packetID + "] failed parse (" + ms.Position + " != " + ms.Length + ")");
                        //StarryboundServer.logDebug("ForwardThread", "[" + this.mParent.playerData.client + "] [" + this.mDirection.ToString() + "][" + packetID + "] Dumping " + ms.Length + " bytes: " + Utils.ByteArrayToString(ms.ToArray()));
                    }
                    #endif

                    //Check return data
                    if (returnData is Boolean)
                    {
                        if ((Boolean)returnData == false) continue;
                    }
                    else if (returnData is int)
                    {
                        if ((int)returnData == -1)
                        {
                            this.mParent.forceDisconnect("Command processor requested to drop client");
                        }
                    }

                    #region Forward Packet
                    //Write data to dest
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
                    #endregion

                    //If disconnect was forwarded to client, lets disconnect.
                    if(packetID == Packet.ServerDisconnect && mDirection == Direction.Server)
                    {
                        this.mParent.forceDisconnect();
                    }
                }
            }
            catch (EndOfStreamException) 
            {
                this.mParent.forceDisconnect();
            }
            catch (Exception e)
            {
                this.mParent.errorDisconnect(mDirection, "ForwardThread Exception: " + e.ToString());
            }
        }
    }
}
