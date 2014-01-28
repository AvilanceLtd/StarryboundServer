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
    class WhosThere : CommandBase
    {
        public WhosThere(Client client)
        {
            this.name = "whosthere";
            this.HelpText = ": shows a list of all players in this world.";
            
            this.Permission = new List<string>();
            this.Permission.Add("client.whosthere");

            this.player = client.playerData;
            this.client = client;
        }

        public override bool doProcess(string[] args)
        {
            if (!hasPermission()) { permissionError(); return false; }

            if (this.client.playerData.loc == null)
            {
                this.client.sendCommandMessage("Unable to find your exact location at this time.");
                return false;
            }

            string list = "";
            foreach (Client otherClient in StarryboundServer.getClients())
            {
                PlayerData otherPlayer = otherClient.playerData;
                if (otherPlayer.loc == null) continue;
                else if (this.player.isInSameWorldAs(otherPlayer) && this.player.name != otherPlayer.name)
                {
                    list += otherPlayer.name + ", ";
                }
            }
            if (list.Length != 0)
            {
                this.client.sendChatMessage("^#5dc4f4;Players in this world: " + list.Substring(0, list.Length -2));
            }
            else
            {
                this.client.sendChatMessage("^#5dc4f4;There are no other players in this world.");
            }
            return true;
        }
    }
}