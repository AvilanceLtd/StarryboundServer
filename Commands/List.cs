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
    class List : CommandBase
    {
        public List(Client client)
        {
            this.name = "players";
            this.HelpText = ": Lists all of the players connected to the server.";
            this.aliases = new string[] {"list","who"};

            this.client = client;
            this.player = client.playerData;
        }

        public override bool doProcess(string[] args)
        {
            string list = "";

            int noOfUsers = StarryboundServer.clientCount;
            int i = 0;

            foreach (Client user in StarryboundServer.getClients())
            {
                list = list + user.playerData.formatName+"^#5dc4f4;";
                if (i != noOfUsers - 1) list = list + ", ";
                i++;
            }
            this.client.sendChatMessage("^#5dc4f4;" + noOfUsers + "/" + StarryboundServer.config.maxClients + " player(s): " + list);
            return true;
        }
    }
}
