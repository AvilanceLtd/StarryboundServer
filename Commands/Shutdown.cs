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
    class Shutdown : CommandBase
    {
        public Shutdown(ClientThread client)
        {
            this.name = "shutdown";
            this.HelpText = ": Gracefully closes all connections";
            this.Permission = new List<string>();
            this.Permission.Add("admin.shutdown");

            this.client = client;
            this.player = client.playerData;
        }

        public override bool doProcess(string[] args)
        {
            if (!hasPermission()) { permissionError(); return false; }

            StarryboundServer.sendGlobalMessage("^#f75d5d;The server is now going down for a restart... We'll be back shortly.");

            StarryboundServer.allowNewClients = false;

            foreach (ClientThread client in StarryboundServer.clients.Values)
            {
                client.sendServerPacket(Packet.ClientDisconnect, new byte[1]); //This causes the server to gracefully save and remove the player, and close its connection, even if the client ignores ServerDisconnect.
                this.client.sendChatMessage("^#f75d5d;You have been disconnected.");
                client.kickTargetTimestamp = Utils.getTimestamp() + 7;
            }

            while (StarryboundServer.clients.Count > 0)
            {
                // Waiting
            }

            StarryboundServer.logInfo("All connections closed -- Shutting down gracefully.");

            System.Environment.Exit(0);

            return true;
        }
    }

    class Restart : CommandBase
    {
        public Restart(ClientThread client)
        {
            this.name = "restart";
            this.HelpText = "Initiate a restart of the server, 30 second delay.";
            this.Permission = new List<string>();
            this.Permission.Add("admin.restart");

            this.client = client;
            this.player = client.playerData;
        }

        public override bool doProcess(string[] args)
        {
            if (!hasPermission()) { permissionError(); return false; }

            if (StarryboundServer.serverState == ServerState.Restarting)
            {
                StarryboundServer.sendGlobalMessage("^#f75d5d;The server restart has been aborted by " + this.player.name);

                StarryboundServer.serverState = ServerState.Running;

                StarryboundServer.restartTime = 0;
            }
            else
            {
                StarryboundServer.sendGlobalMessage("^#f75d5d;The server will restart in 30 seconds. We will be back shortly.");

                StarryboundServer.serverState = ServerState.Restarting;

                StarryboundServer.restartTime = Utils.getTimestamp() + 30;
            }

            return true;
        }
    }
}
