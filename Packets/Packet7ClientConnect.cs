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
        public Packet7ClientConnect(Client clientThread, BinaryReader stream, Direction direction)
        {
            this.client = clientThread;
            this.stream = stream;
            this.direction = direction;
        }

        public override Object onReceive()
        {
            byte[] assetDigest = stream.ReadStarByteArray();

            var claim = stream.ReadStarVariant();
            byte[] UUID = stream.ReadStarUUID();
            string name = stream.ReadStarString();
            string species = stream.ReadStarString();
            byte[] shipWorld = stream.ReadStarByteArray();
            string account = stream.ReadStarString();

            // Identify player to server
            this.client.playerData.uuid = Utils.ByteArrayToString(UUID).ToLower();
            this.client.playerData.name = name;
            this.client.playerData.account = account;

            User userPData = Users.GetUser(name, this.client.playerData.uuid);
            if (StarryboundServer.config.maxClients <= StarryboundServer.clientCount)
            {
                if (!userPData.getGroup().hasPermission("admin.chat") || StarryboundServer.clientCount == (StarryboundServer.serverConfig.maxPlayers - 1))
                {
                    this.client.rejectPreConnected("The server is full. Please try again later.");
                    return false;
                }
            }

            string[] reasonExpiry = Bans.checkForBan(new string[] { name, this.client.playerData.uuid, this.client.playerData.ip });
            if (reasonExpiry.Length == 2)
            {
                this.client.rejectPreConnected("You are " + ((reasonExpiry[1] == "0") ? "permanently" : "temporarily") + " banned from this server.\nReason: " + reasonExpiry[0]);
                return false;
            }

            string sAssetDigest = Utils.ByteArrayToString(assetDigest);
            StarryboundServer.logDebug("AssetDigest", "[" + this.client.playerData.client + "] [" + sAssetDigest + "]");
            if (StarryboundServer.config.useAssetDigest)
            {
                if (sAssetDigest != StarryboundServer.config.assetDigest)
                {
                    this.client.rejectPreConnected("Modded client detected: You cannot modify or add asset files or mods. Please delete your entire Starbound folder and reinstall Starbound to join.");
                    return false;
                }
            }

            if (StarryboundServer.clients.ContainsKey(name))
            {
                this.client.rejectPreConnected("This username is already in use.");
                return false;
            }

            if (String.IsNullOrWhiteSpace(this.client.playerData.name))
            {
                this.client.rejectPreConnected("You may not have a blank name.");
                return false;
            }

            if (!StarryboundServer.config.allowSpaces)
            {
                if (this.client.playerData.name.Contains(" "))
                {
                    this.client.rejectPreConnected("You may not have spaces in your name on this server.");
                    return false;
                }
            }

            if (!StarryboundServer.config.allowSymbols)
            {
                Regex r = new Regex("^[a-zA-Z0-9_\\- ]*$");
                if (!r.IsMatch(this.client.playerData.name))
                {
                    this.client.rejectPreConnected("You may not have special characters in your name on this server.");
                    return false;
                }
            }

            if(!String.IsNullOrEmpty(account))
            {
                this.client.rejectPreConnected("You need clear the server account field of all text.");
                return false;
            }

            try
            {
                PlayerData pData = this.client.playerData;

                pData.isMuted = userPData.isMuted;
                pData.canBuild = userPData.canBuild;
                pData.lastOnline = userPData.lastOnline;
                pData.group = userPData.getGroup();
                pData.freeFuel = userPData.freeFuel;

                if (userPData.name != pData.name)
                {
                    this.client.rejectPreConnected("Connection Failed: Your server side user data is corrupt.");
                    return false;
                }
            }
            catch (Exception)
            {
                this.client.rejectPreConnected("Connection Failed: A internal server error occurred (2)");
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
