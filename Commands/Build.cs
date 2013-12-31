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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace com.avilance.Starrybound.Commands
{
    class Build : CommandBase
    {
        public Build(Client client)
        {
            this.name = "build";
            this.HelpText = " <username>: Allows you to grant/revoke a player's building rights, this command is toggled.";
            this.Permission = new List<string>();
            this.Permission.Add("admin.build");

            this.client = client;
            this.player = client.playerData;
        }

        public override bool doProcess(string[] args)
        {
            if (!hasPermission()) { permissionError(); return false; }

            string player = string.Join(" ", args).Trim();

            if (player == null || player.Length < 1) { showHelpText(); return false; }

            Client target = StarryboundServer.getClient(player);
            if (target != null)
            {
                PlayerData pData = target.playerData;

                pData.canBuild = !pData.canBuild;

                if (!pData.canBuild)
                {
                    StarryboundServer.sendGlobalMessage("^#f75d5d;" + pData.name + " has had their build rights revoked!");
                }
                else
                {
                    StarryboundServer.sendGlobalMessage("^#6cdb67;" + pData.name + " has been granted build rights on the server.");
                }
                return true;
            }
            else
            {
                this.client.sendCommandMessage("Player '" + player + "' not found.");
                return false;
            }
        }
    }
}
