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
            this.client.playerData.uuid = Utils.ByteArrayToString(Utils.HashUUID(UUID)).ToLower();
            this.client.playerData.name = name;
            this.client.playerData.account = account;

            User userPData = Users.GetUser(name, this.client.playerData.uuid, this.client.playerData.ip);
            if (StarryboundServer.config.maxClients <= StarryboundServer.clientCount)
            {
                if (!userPData.getGroup().hasPermission("admin.chat") || StarryboundServer.clientCount == (StarryboundServer.serverConfig.maxPlayers - 1))
                {
                    this.client.rejectPreConnected("The server is full. Please try again later.");
                    return false;
                }
            }

            string[] reasonExpiry = Bans.checkForBan(new string[] { name, this.client.playerData.uuid, this.client.playerData.ip });
            if (reasonExpiry.Length == 2 && !userPData.getGroup().hasPermission("admin.bypassban"))
            {
                this.client.rejectPreConnected("You are " + ((reasonExpiry[1] == "0") ? "permanently" : "temporarily") + " banned from this server.\nReason: " + reasonExpiry[0]);
                return false;
            }

            string sAssetDigest = Utils.ByteArrayToString(assetDigest);
            StarryboundServer.logDebug("AssetDigest", "[" + this.client.playerData.client + "] [" + sAssetDigest + "]");
            if (!StarryboundServer.config.allowModdedClients)
            {
                if (sAssetDigest != StarryboundServer.unmoddedClientDigest)
                {
                    this.client.rejectPreConnected("Modded client detected: You cannot modify or add asset files or mods. Please delete your entire Starbound folder and reinstall Starbound to join.");
                    return false;
                }
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

            if (!userPData.getGroup().hasPermission("admin.bypassban"))
            {
                foreach (string bannedUnamePhrase in StarryboundServer.config.bannedUsernames)
                {
                    if (this.client.playerData.name.ToLower().Contains(bannedUnamePhrase.ToLower()))
                    {
                        this.client.rejectPreConnected("Your name contains a phrase that is banned on this server. (" + bannedUnamePhrase + ")");
                        return false;
                    }
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
                pData.receivedStarterKit = userPData.receivedStarterKit;
                pData.privateShip = userPData.privateShip;
                pData.shipBlacklist = userPData.shipBlacklist;
                pData.shipWhitelist = userPData.shipWhitelist;

                if (userPData.uuid != pData.uuid && userPData.groupName != StarryboundServer.defaultGroup)
                {
                    this.client.rejectPreConnected("Connection Failed: You cannot use \"" + pData.name + "\" on this server.");
                    return false;
                }
            }
            catch (Exception)
            {
                this.client.rejectPreConnected("Connection Failed: A internal server error occurred (2)");
                return false;
            }

            foreach (Client checkClient in StarryboundServer.getClients())
            {
                if (checkClient.playerData.name.ToLower() == name.ToLower())
                {
                    if (userPData.groupName != StarryboundServer.defaultGroup)
                    {
                        checkClient.delayDisconnect("Someone else is attempting to connect with your name. Disconnecting.");
                        this.client.rejectPreConnected("We have disconnected the old player on the server. Please try again in 15 seconds.");
                    }
                    else
                        this.client.rejectPreConnected("Someone is already logged in with this name.");
                }
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
