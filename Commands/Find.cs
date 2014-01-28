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
using System.Linq;
using System.Text;

namespace com.avilance.Starrybound.Commands
{
    class Find : CommandBase
    {
        public Find(Client client)
        {
            this.name = "find";
            this.HelpText = " <player (optional)>; Find your world co-ordinates or those of a specified player.";
            this.aliases = new string[] { "where" };

            this.Permission = new List<string>();
            this.Permission.Add("client.find");

            this.client = client;
            this.player = client.playerData;
        }

        public override bool doProcess(string[] args)
        {
            if (!hasPermission()) { permissionError(); return false; }

            string player = string.Join(" ", args).Trim();

            if (player == null || player.Length < 1)
            {
                if (this.client.playerData.loc == null)
                {
                    this.client.sendCommandMessage("Unable to find your exact location at this time.");
                    return false;
                }
                this.client.sendCommandMessage("You are located [" + this.player.loc.ToString() + "]");
                return true;
            }
            else
            {
                Client target = StarryboundServer.getClient(player);
                if (target != null)
                {
                    PlayerData playerData = target.playerData;
                    if (playerData.loc == null)
                    {
                        this.client.sendCommandMessage("Unable to find an exact location for " + player + ".");
                        return false;
                    }
                    this.client.sendCommandMessage(player + " located at [" + playerData.loc.ToString() + "]" + (playerData.inPlayerShip ? "in a ship." : ""));
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
}
