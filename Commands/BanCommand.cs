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

using com.avilance.Starrybound.Packets;
using com.avilance.Starrybound.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace com.avilance.Starrybound.Commands
{
    class BanCommand : CommandBase
    {
        public BanCommand(ClientThread client)
        {
            this.name = "ban";
            this.HelpText = "<username> <length (mins)> <reason>; Bans the user from the server for the specified time (in minutes) and reason.";
            this.Permission = new List<string>();
            this.Permission.Add("admin.ban");

            this.client = client;
            this.player = client.playerData;
        }

        public override bool doProcess(string[] args)
        {
            if (!hasPermission()) { permissionError(); return false; }

            if (args.Length > 3) { showHelpText(); return false; }

            string player = args[0].Trim();
            string expiry = args[1].Trim();
            string reason = string.Join(" ", args.Skip(2).Take(args.Length - 2)).Trim();

            if (player == null || player.Length < 1 || expiry == null || expiry.Length < 1 || reason == null || reason.Length < 1) { showHelpText(); return false; }

            if (StarryboundServer.clients.ContainsKey(player))
            {
                ClientThread target = StarryboundServer.clients[player];

                string uuid = target.playerData.uuid;
                string name = target.playerData.name;
                string ip = target.playerData.ip;

                int timeNow = Utils.getTimestamp();

                try
                {
                    int bExpiry = int.Parse(expiry);

                    if (bExpiry != 0) bExpiry = timeNow + (bExpiry * 60);

                    Bans.addNewBan(name, uuid, ip, timeNow, this.player.name, bExpiry, reason);

                    target.banClient(reason);

                    return true;
                }
                catch (Exception e)
                {
                    this.client.sendCommandMessage("An exception occured while attempting to ban " + player);
                    StarryboundServer.logException("Error occured while banning player " + player + ": " + e.ToString());
                    return false;
                }
            }
            else
            {
                this.client.sendCommandMessage("Player '" + player + "' not found.");
                return false;
            }
        }
    }

    class BanReloadCommand : CommandBase
    {
        public BanReloadCommand(ClientThread client)
        {
            this.name = "banreload";
            this.HelpText = "Reloads the banned-players.txt file";
            this.Permission = new List<string>();
            this.Permission.Add("admin.ban.reload");

            this.client = client;
            this.player = client.playerData;
        }

        public override bool doProcess(string[] args)
        {
            if (!hasPermission()) { permissionError(); return false; }
            this.client.sendChatMessage("Attempting to reload all server bans from banned-players.txt");
            Bans.readBansFromFile();
            return true;
        }
    }
}
