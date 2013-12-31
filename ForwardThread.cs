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
using System.Threading;

namespace com.avilance.Starrybound
{
    class ForwardThread
    {
        BinaryReader incoming;
        BinaryWriter outgoing;
        Client client;
        Direction direction;
        private string passwordSalt;

        public ForwardThread(Client aClient, BinaryReader aInput, BinaryWriter aOutput, Direction aDirection) {
            this.client = aClient;
            this.incoming = aInput;
            this.outgoing = aOutput;
            this.direction = aDirection;
        }

        public void run()
        {
            try
            {
                for (;;)
                {
                    if (!this.client.connectionAlive)
                    {
                        this.client.forceDisconnect(direction, "Connection no longer alive");
                        return;
                    }


                    if (this.client.kickTargetTimestamp != 0)
                    {
                        if (this.client.kickTargetTimestamp < Utils.getTimestamp())
                        {
                            this.client.closeConnection();
                            return;
                        }
                        continue;
                    }

                    #region Process Packet
                    //Packet ID and Vaildity Check.
                    uint temp = this.incoming.ReadVarUInt32();
                    if (temp < 1 || temp > 48)
                    {
                        this.client.forceDisconnect(direction, "Sent invalid packet ID [" + temp + "].");
                        return;
                    }
                    Packet packetID = (Packet)temp;

                    //Packet Size and Compression Check.
                    bool compressed = false;
                    int packetSize = this.incoming.ReadVarInt32();
                    if (packetSize < 0)
                    {
                        packetSize = -packetSize;
                        compressed = true;
                    }

                    //Create buffer for forwarding
                    byte[] dataBuffer = this.incoming.ReadFully(packetSize);

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

                    if (packetID != Packet.Heartbeat && packetID != Packet.UniverseTimeUpdate)
                    {
                        if (direction == Direction.Client)
                        #region Handle Client Packets
                        {
                            #region Protocol State Security
                            ClientState curState = this.client.state;
                            if (curState != ClientState.Connected)
                            {
                                if (curState == ClientState.PendingConnect && packetID != Packet.ClientConnect)
                                {
                                    this.client.rejectPreConnected("Violated PendingConnect protocol state with " + packetID);
                                    return;
                                }
                                else if (curState == ClientState.PendingAuthentication && packetID != Packet.HandshakeResponse)
                                {
                                    this.client.rejectPreConnected("Violated PendingAuthentication protocol state with " + packetID);
                                    return;
                                }
                                else if (curState == ClientState.PendingConnectResponse)
                                {
                                    int startTime = Utils.getTimestamp();
                                    while (true)
                                    {
                                        if (this.client.state == ClientState.Connected) break;
                                        if (Utils.getTimestamp() > startTime + StarryboundServer.config.connectTimeout)
                                        {
                                            this.client.rejectPreConnected("Connection Failed: Server did not respond in time.");
                                            return;
                                        }
                                    }
                                }
                            }
                            #endregion

                            if (packetID == Packet.ChatSend)
                            {
                                returnData = new Packet11ChatSend(this.client, packetData, this.direction).onReceive();
                            }
                            else if (packetID == Packet.ClientConnect)
                            {
                                this.client.state = ClientState.PendingAuthentication;
                                returnData = new Packet7ClientConnect(this.client, packetData, this.direction).onReceive();
                                MemoryStream packet = new MemoryStream();
                                BinaryWriter packetWrite = new BinaryWriter(packet);

                                passwordSalt = Utils.GenerateSecureSalt();
                                packetWrite.WriteStarString("");
                                packetWrite.WriteStarString(passwordSalt);
                                packetWrite.WriteBE(StarryboundServer.config.passwordRounds);
                                this.client.sendClientPacket(Packet.HandshakeChallenge, packet.ToArray());
                            }
                            else if (packetID == Packet.HandshakeResponse)
                            {
                                string claimResponse = packetData.ReadStarString();
                                string passwordHash = packetData.ReadStarString();

                                string verifyHash = Utils.StarHashPassword(StarryboundServer.config.proxyPass, this.client.playerData.account + passwordSalt, StarryboundServer.config.passwordRounds);
                                if (passwordHash != verifyHash)
                                {
                                    this.client.rejectPreConnected("Your password was incorrect.");
                                    return;
                                }

                                this.client.state = ClientState.PendingConnectResponse;
                                returnData = false;
                            }
                            else if (packetID == Packet.WarpCommand)
                            {
                                WarpType cmd = (WarpType)packetData.ReadUInt32BE();
                                WorldCoordinate coord = packetData.ReadStarWorldCoordinate();
                                string player = packetData.ReadStarString();
                                if (cmd == WarpType.WarpToPlayerShip)
                                {
                                    Client target = StarryboundServer.getClient(player);
                                    if (target != null)
                                    {
                                        if (!this.client.playerData.canAccessShip(target.playerData))
                                        {
                                            this.client.sendChatMessage("^#5dc4f4;You cannot access this player's ship due to their ship access settings.");
                                            StarryboundServer.logDebug("ShipAccess", "Preventing " + this.client.playerData.name + " from accessing " + target.playerData.name + "'s ship.");
                                            MemoryStream packetWarp = new MemoryStream();
                                            BinaryWriter packetWrite = new BinaryWriter(packetWarp);
                                            packetWrite.WriteBE((uint)WarpType.WarpToOwnShip);
                                            packetWrite.Write(new WorldCoordinate());
                                            packetWrite.WriteStarString("");
                                            client.sendServerPacket(Packet.WarpCommand, packetWarp.ToArray());
                                            returnData = false;
                                        }
                                    }
                                }
                                StarryboundServer.logDebug("WarpCommand", "[" + this.client.playerData.client + "][" + cmd + "]" + (coord != null ? "[" + coord.ToString() + "]" : "") + "[" + player + "]");
                            }
                            else if (packetID == Packet.ModifyTileList || packetID == Packet.DamageTileGroup || packetID == Packet.DamageTile || packetID == Packet.ConnectWire || packetID == Packet.DisconnectAllWires)
                            {
                                if(!this.client.playerData.canIBuild()) returnData = false;
                            }
                            else if (packetID == Packet.EntityCreate)
                            {
                                while(true)
                                {
                                    EntityType type = (EntityType)packetData.Read();
                                    if(type == EntityType.EOF) break;
                                    byte[] entityData = packetData.ReadStarByteArray();
                                    int entityId = packetData.ReadVarInt32();
                                    if(type == EntityType.Projectile)
                                    {
                                        BinaryReader entity = new BinaryReader(new MemoryStream(entityData));
                                        string projectileKey = entity.ReadStarString();
                                        object projParams = entity.ReadStarVariant();
                                        if (StarryboundServer.config.projectileBlacklist.Contains(projectileKey))
                                        {
                                            MemoryStream packet = new MemoryStream();
                                            BinaryWriter packetWrite = new BinaryWriter(packet);
                                            packetWrite.WriteVarInt32(entityId);
                                            packetWrite.Write(false);
                                            this.client.sendClientPacket(Packet.EntityDestroy, packet.ToArray());
                                            returnData = false;
                                        }
                                        if (StarryboundServer.serverConfig.useDefaultWorldCoordinate && StarryboundServer.config.spawnWorldProtection)
                                        {
                                            if (this.client.playerData.loc != null)
                                            {
                                                if (StarryboundServer.config.projectileBlacklistSpawn.Contains(projectileKey) && StarryboundServer.spawnPlanet.Equals(this.client.playerData.loc) && !this.client.playerData.group.hasPermission("admin.spawnbuild") && !this.client.playerData.inPlayerShip)
                                                {
                                                    MemoryStream packet = new MemoryStream();
                                                    BinaryWriter packetWrite = new BinaryWriter(packet);
                                                    packetWrite.WriteVarInt32(entityId);
                                                    packetWrite.Write(false);
                                                    this.client.sendClientPacket(Packet.EntityDestroy, packet.ToArray());
                                                    returnData = false;
                                                }
                                            }
                                            else
                                            {
                                                MemoryStream packet = new MemoryStream();
                                                BinaryWriter packetWrite = new BinaryWriter(packet);
                                                packetWrite.WriteVarInt32(entityId);
                                                packetWrite.Write(false);
                                                this.client.sendClientPacket(Packet.EntityDestroy, packet.ToArray());
                                                returnData = false;
                                            }
                                        }
                                    }
                                    else if (type == EntityType.Object || type == EntityType.Plant || type == EntityType.PlantDrop || type == EntityType.Monster)
                                    {
                                        if (!this.client.playerData.canIBuild())
                                        {
                                            MemoryStream packet = new MemoryStream();
                                            BinaryWriter packetWrite = new BinaryWriter(packet);
                                            packetWrite.WriteVarInt32(entityId);
                                            packetWrite.Write(false);
                                            this.client.sendClientPacket(Packet.EntityDestroy, packet.ToArray());
                                            returnData = false;
                                        }
                                    }
                                }
                            }
                            else if (packetID == Packet.SpawnEntity)
                            {
                                while(true)
                                {
                                    EntityType type = (EntityType)packetData.Read();
                                    if (type == EntityType.EOF) break;
                                    byte[] entityData = packetData.ReadStarByteArray();
                                    if (type == EntityType.Projectile)
                                    {
                                        BinaryReader entity = new BinaryReader(new MemoryStream(entityData));
                                        string projectileKey = entity.ReadStarString();
                                        object projParams = entity.ReadStarVariant();
                                        if (StarryboundServer.config.projectileBlacklist.Contains(projectileKey))
                                        {
                                            returnData = false;
                                        }
                                        if (StarryboundServer.serverConfig.useDefaultWorldCoordinate && StarryboundServer.config.spawnWorldProtection)
                                        {
                                            if (this.client.playerData.loc != null)
                                            {
                                                if (StarryboundServer.config.projectileBlacklistSpawn.Contains(projectileKey) ^ StarryboundServer.config.projectileSpawnListIsWhitelist)
                                                {
                                                    if (StarryboundServer.spawnPlanet.Equals(this.client.playerData.loc) && !this.client.playerData.group.hasPermission("admin.spawnbuild") && !this.client.playerData.inPlayerShip)
                                                    {
                                                        returnData = false;
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                returnData = false;
                                            }
                                        }
                                    }
                                    else if (type == EntityType.Object || type == EntityType.Plant || type == EntityType.PlantDrop || type == EntityType.Monster)
                                    {
                                        if (!this.client.playerData.canIBuild()) returnData = false;
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
                                returnData = new Packet5ChatReceive(this.client, packetData, this.direction).onReceive();
                            }
                            else if (packetID == Packet.ProtocolVersion)
                            {
                                uint protocolVersion = packetData.ReadUInt32BE();
                                if (protocolVersion != StarryboundServer.ProtocolVersion)
                                {
                                    MemoryStream packet = new MemoryStream();
                                    BinaryWriter packetWrite = new BinaryWriter(packet);
                                    packetWrite.WriteBE(protocolVersion);
                                    this.client.sendClientPacket(Packet.ProtocolVersion, packet.ToArray());

                                    this.client.rejectPreConnected("Connection Failed: Unable to handle parent server protocol version.");
                                    return;
                                }
                            }
                            else if (packetID == Packet.HandshakeChallenge)
                            {
                                string claimMessage = packetData.ReadString();
                                string passwordSalt = packetData.ReadStarString();
                                int passwordRounds = packetData.ReadInt32BE();

                                MemoryStream packet = new MemoryStream();
                                BinaryWriter packetWrite = new BinaryWriter(packet);
                                string passwordHash = Utils.StarHashPassword(StarryboundServer.privatePassword, passwordSalt, passwordRounds);
                                packetWrite.WriteStarString("");
                                packetWrite.WriteStarString(passwordHash);
                                this.client.sendServerPacket(Packet.HandshakeResponse, packet.ToArray());

                                returnData = false;
                            }
                            else if (packetID == Packet.ConnectResponse)
                            {
                                int startTime = Utils.getTimestamp();
                                while (true) 
                                {
                                    if (this.client.state == ClientState.PendingConnectResponse) break;
                                    if (Utils.getTimestamp() > startTime + StarryboundServer.config.connectTimeout)
                                    {
                                        this.client.rejectPreConnected("Connection Failed: Client did not respond with handshake.");
                                        return;
                                    }
                                }
                                returnData = new Packet2ConnectResponse(this.client, packetData, this.direction).onReceive();
                            }
                            else if (packetID == Packet.WorldStart)
                            {
                                if (!this.client.playerData.sentMotd)
                                {
                                    this.client.sendChatMessage(Config.GetMotd());

                                    if (!this.client.playerData.group.hasPermission("world.build"))
                                        this.client.sendChatMessage("^#f75d5d;" + StarryboundServer.config.buildErrorMessage);

                                    this.client.playerData.sentMotd = true;
                                }

                                byte[] planet = packetData.ReadStarByteArray();
                                byte[] worldStructure = packetData.ReadStarByteArray();
                                byte[] sky = packetData.ReadStarByteArray();
                                byte[] serverWeather = packetData.ReadStarByteArray();
                                float spawnX = packetData.ReadSingleBE();
                                float spawnY = packetData.ReadSingleBE();
                                uint mapParamsSize = packetData.ReadVarUInt32();
                                Dictionary<string, object> mapParams = new Dictionary<string, object>();
                                int isPlayerShip = 0;
                                for (int i = 0; i < mapParamsSize; i++)
                                {
                                    string key = packetData.ReadStarString();
                                    var value = packetData.ReadStarVariant();
                                    mapParams.Add(key, value);
                                    if(key == "fuel.level")
                                    {
                                        isPlayerShip++;
                                    }
                                    else if(key == "fuel.max")
                                    {
                                        isPlayerShip++;
                                    }
                                }
                                this.client.playerData.inPlayerShip = (isPlayerShip == 2);
                                uint clientID = packetData.ReadUInt32BE();
                                bool bool1 = packetData.ReadBoolean();
                                WorldCoordinate coords = Utils.findGlobalCoords(sky);
                                if (coords != null)
                                {
                                    this.client.playerData.loc = coords;
                                    StarryboundServer.logDebug("WorldStart", "[" + this.client.playerData.client + "][" + bool1 + ":" + clientID + "] CurLoc:[" + this.client.playerData.loc.ToString() + "][" + this.client.playerData.inPlayerShip + "]");
                                }
                                else
                                    StarryboundServer.logDebug("WorldStart", "[" + this.client.playerData.client + "][" + bool1 + ":" + clientID + "] InPlayerShip:[" + this.client.playerData.inPlayerShip + "]");
                            }
                            else if (packetID == Packet.WorldStop)
                            {
                                string status = packetData.ReadStarString();
                            }
                            else if (packetID == Packet.GiveItem)
                            {
                                string name = packetData.ReadStarString();
                                uint count = packetData.ReadVarUInt32();
                                var itemDesc = packetData.ReadStarVariant();
                            }
                            else if (packetID == Packet.EnvironmentUpdate)
                            {
                                byte[] sky = packetData.ReadStarByteArray();
                                byte[] serverWeather = packetData.ReadStarByteArray();
                                if (this.client.playerData.loc == null)
                                {
                                    WorldCoordinate coords = Utils.findGlobalCoords(sky);
                                    if (coords != null)
                                    {
                                        this.client.playerData.loc = coords;
                                        StarryboundServer.logDebug("EnvUpdate", "[" + this.client.playerData.client + "] CurLoc:[" + this.client.playerData.loc.ToString() + "]");
                                    }
                                }
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
                                            if (dataBufferLength == 2)
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
                                                                    log.TryGetValue("loc", out this.client.playerData.loc);
                                                                    if (!log.TryGetValue("home", out this.client.playerData.home))
                                                                        this.client.playerData.home = this.client.playerData.loc;
                                                                    StarryboundServer.logDebug("ClientContext", "[" + this.client.playerData.client + "] CurLoc:[" + this.client.playerData.loc.ToString() + "][" + this.client.playerData.inPlayerShip + "]");
                                                                    StarryboundServer.logDebug("ClientContext", "[" + this.client.playerData.client + "] CurHome:[" + this.client.playerData.home.ToString() + "]");
                                                                }
                                                            }
                                                        }
                                                    }
                                                    else if (dataType == 14)
                                                    {
                                                        Dictionary<string, WorldCoordinate> log = dataReader.ReadStarCelestialLog();
                                                        log.TryGetValue("loc", out this.client.playerData.loc);
                                                        if (!log.TryGetValue("home", out this.client.playerData.home))
                                                            this.client.playerData.home = this.client.playerData.loc;
                                                        StarryboundServer.logDebug("ClientContext", "[" + this.client.playerData.client + "] CurLoc:[" + this.client.playerData.loc.ToString() + "][" + this.client.playerData.inPlayerShip + "]");
                                                        StarryboundServer.logDebug("ClientContext", "[" + this.client.playerData.client + "] CurHome:[" + this.client.playerData.home.ToString() + "]");
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                                catch (Exception e)
                                {
                                    StarryboundServer.logDebug("ClientContext", "[" + this.client.playerData.client + "] Failed to parse ClientContextUpdate from Server: " + e.ToString());
                                }
                            }
                            else if (packetID == Packet.EntityCreate)
                            {
                                MemoryStream sendStream = new MemoryStream();
                                BinaryWriter sendWriter = new BinaryWriter(sendStream);
                                bool test = true;
                                while (true)
                                {
                                    EntityType type = (EntityType)packetData.Read();
                                    if (type == EntityType.EOF) break;
                                    byte[] entityData = packetData.ReadStarByteArray();
                                    int entityId = packetData.ReadVarInt32();
                                    if (type == EntityType.Player)
                                    {
                                        byte[] buffer = new byte[16];
                                        Buffer.BlockCopy(entityData, 0, buffer, 0, 16);
                                        buffer = Utils.HashUUID(buffer);
                                        Buffer.BlockCopy(buffer, 0, entityData, 0, 16);
                                        returnData = test = false;
                                    }
                                    sendWriter.Write((byte)type);
                                    sendWriter.WriteVarUInt64((ulong)entityData.Length);
                                    sendWriter.Write(entityData);
                                    sendWriter.WriteVarInt32(entityId);
                                }
                                if(test == false)
                                {
                                    this.outgoing.WriteVarUInt32((uint)packetID);
                                    this.outgoing.WriteVarInt32((int)sendStream.Length);
                                    this.outgoing.Write(sendStream.ToArray());
                                    this.outgoing.Flush();
                                }
                            }
                        }
                        #endregion
                    }

                    //Check return data
                    if (returnData is Boolean)
                    {
                        if ((Boolean)returnData == false) continue;
                    }
                    else if (returnData is int)
                    {
                        if ((int)returnData == -1)
                        {
                            this.client.forceDisconnect(direction, "Command processor requested to drop client");
                            return;
                        }
                    }

                    #region Forward Packet
                    //Write data to dest
                    this.outgoing.WriteVarUInt32((uint)packetID);
                    if (compressed)
                    {
                        this.outgoing.WriteVarInt32(-packetSize);
                        this.outgoing.Write(dataBuffer, 0, packetSize);
                    }
                    else
                    {
                        this.outgoing.WriteVarInt32(packetSize);
                        this.outgoing.Write(dataBuffer, 0, packetSize);
                    }
                    this.outgoing.Flush();
                    #endregion

                    //If disconnect was forwarded to client, lets disconnect.
                    if(packetID == Packet.ServerDisconnect && direction == Direction.Server)
                    {
                        this.client.closeConnection();
                    }
                }
            }
            catch (ThreadAbortException) { }
            catch (EndOfStreamException) 
            {
                this.client.forceDisconnect(direction, "End of stream");
            }
            catch (Exception e)
            {
                if(e.InnerException != null)
                {
                    if(e.InnerException is System.Net.Sockets.SocketException)
                    {
                        this.client.forceDisconnect(direction, e.InnerException.Message);
                        return;
                    }

                }
                this.client.forceDisconnect(direction, "ForwardThread Exception: " + e.ToString());
            }
        }
    }
}
