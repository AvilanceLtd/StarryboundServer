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
        public Find(ClientThread client)
        {
            this.name = "find";
            this.HelpText = "<player (optional)>; Find your world co-ordinates or those of a specified player.";

            this.client = client;
            this.player = client.playerData;
        }

        public override bool doProcess(string[] args)
        {
            string player = string.Join(" ", args).Trim();

            if (player == null || player.Length < 1)
            {
                this.client.sendCommandMessage("You are located [" + this.player.sector + ":" + this.player.x + ":" + this.player.y + ":" + this.player.z + ":" + this.player.planet + ":" + this.player.satellite + "]");
                return true;
            }
            else
            {
                if (StarryboundServer.clients.ContainsKey(player))
                {
                    Player playerData = StarryboundServer.clients[player].playerData;
                    this.client.sendCommandMessage(player + " located [" + playerData.sector + ":" + playerData.x + ":" + playerData.y + ":" + playerData.z + ":" + playerData.planet + ":" + playerData.satellite + "]");
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
