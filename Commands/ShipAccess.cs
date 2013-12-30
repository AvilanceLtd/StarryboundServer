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
    class ShipAccess : CommandBase
    {
        public ShipAccess(Client client)
        {
            this.name = "shipaccess";
            this.HelpText = ": Allows you to control the access of other players to your ship. Use /shipaccess help for instructions.";

            this.Permission = new List<string>();
            this.Permission.Add("client.shipaccess");

            this.client = client;
            this.player = client.playerData;
        }

        public override bool doProcess(string[] args)
        {
            if (!hasPermission()) { permissionError(); return false; }

            if (args.Length > 0)
            {
                string command = args[0].ToLower().Trim();
                if (command == "status")
                {
                    this.client.sendCommandMessage("Ship access status:");
                    this.client.sendCommandMessage("Access type: " + (this.player.privateShip ? "Private" : "Public"));
                    if (this.player.shipWhitelist != null && this.player.shipWhitelist.Count > 0)
                    {
                        string whitelist = "Whitelist: ";
                        foreach (string name in this.player.shipWhitelist)
                        {
                            if (!String.IsNullOrEmpty(name)) // Who knows...
                            {
                                whitelist += name + ", ";
                            }
                        }
                        this.client.sendCommandMessage(whitelist.Substring(0, whitelist.Length - 2));
                    }
                    else
                    {
                        this.client.sendCommandMessage("Whitelist: Empty.");
                    }

                    if (this.player.shipBlacklist != null && this.player.shipBlacklist.Count > 0)
                    {
                        string blacklist = "Blacklist: ";
                        foreach (string name in this.player.shipBlacklist)
                        {
                            if (!String.IsNullOrEmpty(name)) // Who knows...
                            {
                                blacklist += name + ", ";
                            }
                        }
                        this.client.sendCommandMessage(blacklist.Substring(0, blacklist.Length - 2));
                    }
                    else
                    {
                        this.client.sendCommandMessage("Blacklist: Empty.");
                    }

                    return true;
                }
                else if (command == "public")
                {
                    this.client.playerData.privateShip = false;
                    this.client.sendCommandMessage("Your ship access is now set to public. Anyone who can reach it can now access it.");
                    return true;
                }
                else if (command == "private")
                {
                    this.client.playerData.privateShip = true;
                    this.client.sendCommandMessage("Your ship access is now set to private. Only players in your ship's whitelist can access it.");
                    return true;
                }
                else if (command == "allow")
                {
                    if (args.Length == 2)
                    {
                        string targetName = args[1];
                        if (!this.client.playerData.shipWhitelist.Contains(targetName))
                        {
                            this.player.shipWhitelist.Add(targetName);
                            this.client.sendCommandMessage("Player " + targetName + " has been added to your ship's whitelist.");
                            return true;
                        }
                        else
                        {
                            this.client.sendCommandMessage("Player " + targetName + " is already in your ship's whitelist.");
                            return false;
                        }
                    }
                    else
                    {
                        this.client.sendCommandMessage("Invalid syntax. Type /shipaccess help for instructions.");
                        return false;
                    }
                }
                else if (command == "disallow")
                {
                    if (args.Length == 2)
                    {
                        string targetName = args[1];
                        if (this.client.playerData.shipWhitelist.Contains(targetName))
                        {
                            this.player.shipWhitelist.Remove(targetName);
                            this.client.sendCommandMessage("Player " + targetName + " has been removed from your ship's whitelist.");
                            return true;
                        }
                        else
                        {
                            this.client.sendCommandMessage("Player " + targetName + " is not in your ship's whitelist.");
                            return false;
                        }
                    }
                    else
                    {
                        this.client.sendCommandMessage("Invalid syntax. Type /shipaccess help for instructions.");
                        return false;
                    }
                }
                else if (command == "block")
                {
                    if (args.Length == 2)
                    {
                        string targetName = args[1];
                        if (!this.client.playerData.shipBlacklist.Contains(targetName))
                        {
                            this.player.shipBlacklist.Add(targetName);
                            this.client.sendCommandMessage("Player " + targetName + " has been added to your ship's blacklist, and can no longer access it.");
                            return true;
                        }
                        else
                        {
                            this.client.sendCommandMessage("Player " + targetName + " is already in your ship's blacklist.");
                            return false;
                        }
                    }
                    else
                    {
                        this.client.sendCommandMessage("Invalid syntax. Type /shipaccess help for instructions.");
                        return false;
                    }
                }
                else if (command == "unblock")
                {
                    if (args.Length == 2)
                    {
                        string targetName = args[1];
                        if (this.client.playerData.shipBlacklist.Contains(targetName))
                        {
                            this.player.shipBlacklist.Remove(targetName);
                            this.client.sendCommandMessage("Player " + targetName + " has been removed from your ship's blacklist.");
                            return true;
                        }
                        else
                        {
                            this.client.sendCommandMessage("Player " + targetName + " is not in your ship's blacklist.");
                            return false;
                        }
                    }
                    else
                    {
                        this.client.sendCommandMessage("Invalid syntax. Type /shipaccess help for instructions.");
                        return false;
                    }
                }
                else if (command == "help")
                {
                    this.client.sendCommandMessage("Ship access command help:");
                    this.client.sendCommandMessage("/shipaccess status - shows your ship access status.");
                    this.client.sendCommandMessage("/shipaccess public - sets your ship access to public, allowing everyone who can reach it to access it.");
                    this.client.sendCommandMessage("/shipaccess private - sets your ship access to private, allowing only players in the ship's whitelist to access it.");
                    this.client.sendCommandMessage("/shipaccess allow <player name> - adds <player name> to your ship's whitelist.");
                    this.client.sendCommandMessage("/shipaccess disallow <player name> - removes <player name> from your ship's whitelist.");
                    this.client.sendCommandMessage("/shipaccess block <player name> - adds <player name> to your ship's blacklist.");
                    this.client.sendCommandMessage("/shipaccess unblock <player name> - removes <player name> from your ship's blacklist.");
                    return true;
                }
                else
                {
                    this.client.sendCommandMessage("Invalid syntax. Type /shipaccess help for instructions.");
                    return false;
                }
            }
            else
            {
                this.client.sendCommandMessage("Invalid syntax. Type /shipaccess help for instructions.");
                return false;
            }
        }
    }
}
