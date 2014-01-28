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
    class Help : CommandBase
    {
        CommandBase[] commands;
                                     
        public Help(Client client)
        {
            this.name = "Help";
            this.HelpText = ": Provides help for using commands.";
            this.aliases = new string[] {"?", "commands", "commandlist"};

            this.client = client;
            this.player = client.playerData;
            commands =  new CommandBase[] {
                new BanC(client),
                new Reload(client),
                new Broadcast(client), 
                new Build(client), 
                new Find(client),
                new GroupC(client),
                new UnbanCommand(client),
                new Home(client), 
                new Item(client),
                new Kick(client), 
                new List(client), 
                new Me(client), 
                new Mute(client), 
                new Planet(client), 
                new Rules(client),
                new Ship(client), 
                new ShipAccess(client),
                new Shutdown(client), 
                new Spawn(client),
                new StarterItems(client),
                new Uptime(client),
                new VersionC(client),
                new WarpShip(client),
                new WhosThere(client),
            };
        }

        public override bool doProcess(string[] args)
        {
            if (args.Length == 1)
            {
                string commandToFind = args[0];
                foreach (CommandBase command in commands)
                {
                    if (command.name.ToLower().Equals(commandToFind.ToLower()))
                    {
                        bool hasPermission = true;
                        if (command.Permission != null && command.Permission.Count > 0)
                        {
                            foreach (string permission in command.Permission)
                            {
                                if (!this.player.hasPermission(permission))
                                {
                                    hasPermission = false;
                                }
                            }
                        }
                        if (hasPermission)
                        {
                            this.client.sendCommandMessage("/" + command.name + command.HelpText);
                            if (command.aliases != null && command.aliases.Length > 0)
                            {
                                string aliasesMessage = "Aliases: ";
                                for (int i = 0; i < command.aliases.Length; i++)
                                {
                                    aliasesMessage += "/" + command.aliases[i] + " ";
                                }
                                this.client.sendCommandMessage(aliasesMessage);
                            }
                            return true;
                        }
                        else
                        {
                            this.client.sendChatMessage(Util.ChatReceiveContext.CommandResult, "", "You do not have permission to view this command.");
                            return true;
                        }
                    }
                }
                this.client.sendChatMessage("Command "+commandToFind+" not found.");
                return true;
            }
            else
            {
                this.client.sendChatMessage("^#5dc4f4;Command list:");
                StringBuilder sb = new StringBuilder();
                foreach (CommandBase command in commands)
                {
                    bool hasPermission = true;
                    if (command.Permission != null)
                    {
                        foreach (string permission in command.Permission)
                        {
                            if (!this.player.hasPermission(permission))
                            {
                                hasPermission = false;
                            }
                        }
                    }
                    if (hasPermission)
                    {
                        if (sb.Length + command.name.Length < 58)
                        {
                            sb.Append("/").Append(command.name).Append(", ");
                        }
                        else
                        {
                            this.client.sendChatMessage("^#5dc4f4;" + sb.ToString());
                            sb.Clear();
                            sb.Append("/").Append(command.name).Append(", ");
                        }
                    }
                }
                this.client.sendChatMessage("^#5dc4f4;" + sb.Remove(sb.Length - 2, 2).ToString());
                this.client.sendChatMessage("^#5dc4f4;Use /help <command> for help with a specific command.");
                return true;
            }
        }
    }
}
