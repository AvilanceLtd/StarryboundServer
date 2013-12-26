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
using com.avilance.Starrybound.Util;

namespace com.avilance.Starrybound.Commands
{
    abstract class CommandBase
    {
        public string name { get; set; }
        public string HelpText = "No help available.";
        public string[] aliases { get; set; }
        public List<string> Permission;

        public Player player;
        public ClientThread client;

        public abstract bool doProcess(string[] args);

        public bool hasPermission(bool elevated = false)
        {
            if (Permission == null || Permission.Count < 1)
                return true;

            foreach (var node in Permission)
            {
                string checkNode = node;
                if (checkNode.StartsWith("e:"))
                {
                    checkNode = checkNode.Substring(3, checkNode.Length - 2);

                    if (player.hasPermission(checkNode))
                        return true;
                }
                else 
                {
                    if (!elevated)
                    {
                        if (player.hasPermission(checkNode))
                            return true;
                    }
                }
            }

            return false;
        }

        public void permissionError(int errorCode = 1)
        {
            string message = "An unknown error occurred.";

            switch (errorCode)
            {
                case 1:
                    message = "You do not have permission to use this command.";
                    break;

                case 2:
                    message = "You do not have permission to target this player.";
                    break;
            }

            this.client.sendChatMessage(ChatReceiveContext.CommandResult, "", message);
        }

        public void showHelpText()
        {
            this.client.sendCommandMessage("/" + this.name + this.HelpText);
        }
    }
}
