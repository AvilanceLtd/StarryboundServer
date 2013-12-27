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

using com.avilance.Starrybound.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using com.avilance.Starrybound.Extensions;
using com.avilance.Starrybound.Permissions;

namespace com.avilance.Starrybound.Packets
{
    class Packet7ClientConnect : PacketBase
    {
        public Packet7ClientConnect(ClientThread clientThread, Object stream, Direction direction)
        {
            this.mClient = clientThread;
            this.mStream = stream;
            this.mDirection = direction;
        }

        public override Object onReceive()
        {
            BinaryReader packetData = (BinaryReader)this.mStream;

            byte[] assetDigest = packetData.ReadStarByteArray();
            List<object> claim = packetData.ReadStarVariant();
            byte[] UUID = packetData.ReadStarUUID();
            string name = packetData.ReadStarString();
            string species = packetData.ReadStarString();
            byte[] shipWorld = packetData.ReadStarByteArray();
            string account = packetData.ReadStarString();

            // Identify player to server
            this.mClient.playerData.uuid = Utils.ByteArrayToString(UUID).ToLower();
            this.mClient.playerData.name = name;
            this.mClient.playerData.account = account;
            this.mClient.playerData.lastPlayerShip = this.mClient.playerData.inPlayerShip = name;

            User userPData;
            userPData = Users.GetUser(name, Utils.ByteArrayToString(UUID).ToLower());

            string[] reasonExpiry = Bans.checkForBan(new string[] { name, this.mClient.playerData.uuid, this.mClient.playerData.ip });

            if (reasonExpiry.Length == 2)
            {
                this.mClient.rejectPreConnected("You are " + ((reasonExpiry[1] == "0") ? "permanently" : "temporarily") + " banned from this server.\nReason: " + reasonExpiry[0]);
                return false;
            }

            if (StarryboundServer.clients.ContainsKey(name))
            {
                this.mClient.rejectPreConnected("This username is already in use.");
                return false;
            }

            if (StarryboundServer.config.maxClients <= StarryboundServer.clientCount)
            {
                if (!userPData.getGroup().hasPermission("admin.chat") || StarryboundServer.clientCount == (StarryboundServer.serverConfig.maxPlayers - 1))
                {
                    this.mClient.rejectPreConnected("The server is full. Please try again later.");
                    return false;
                }
            }

            if (!StarryboundServer.config.allowSpaces)
            {
                if (this.mClient.playerData.name.Contains(" ") || String.IsNullOrWhiteSpace(this.mClient.playerData.name))
                {
                    this.mClient.rejectPreConnected("You may not have spaces in your name on this server.");
                    return false;
                }
            }

            if (!StarryboundServer.config.allowSymbols)
            {
                Regex r = new Regex("^[a-zA-Z0-9_]*$");
                if (!r.IsMatch(this.mClient.playerData.name))
                {
                    this.mClient.rejectPreConnected("You may not have special characters in your name on this server.");
                    return false;
                }
            }

            com.avilance.Starrybound.Permissions.Group userGroup;
            try
            {
                Player pData = this.mClient.playerData;

                pData.isMuted = userPData.isMuted;
                pData.canBuild = userPData.canBuild;
                pData.lastOnline = userPData.lastOnline;
                pData.group = userPData.getGroup();

                if (userPData.name != pData.name)
                {
                    this.mClient.rejectPreConnected("Your character data is corrupt. Unable to connect to server.");
                    return false;
                }
            }
            catch (Exception)
            {
                this.mClient.rejectPreConnected("The server was unable to accept your connection at this time.\nPlease try again later.");
                return false;
            }

            return null;
        }

        public override void onSend()
        {
            //This should never happen! We don't NEED to send it!
        }

        public override int getPacketID()
        {
            return 5;
        }
    }
}
