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
    abstract class CommandBase
    {
        public string name { get; set; }
        public string HelpText = "No help available.";
        public List<string> Permission;

        public Player player;
        public ClientThread client;

        public abstract bool doProcess(string[] args);

        public bool hasPermission()
        {
            if (Permission == null || Permission.Count < 1)
                return true;

            foreach (var node in Permission)
            {
                if (player.hasPermission(node))
                    return true;
            }

            return false;
        }

        public void permissionError()
        {
            Packet11ChatSend packet = new Packet11ChatSend(this.client, false, Util.Direction.Client);
            packet.prepare(Util.ChatReceiveContext.Whisper, "", 0, "server", "You do not have permission to use this command.");
            packet.onSend();
        }

        public void showHelpText()
        {
            Packet11ChatSend packet = new Packet11ChatSend(this.client, false, Util.Direction.Client);
            packet.prepare(Util.ChatReceiveContext.CommandResult, "", 0, "server", "/" + this.name + ": " + this.HelpText);
            packet.onSend();
        }
    }
}
