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
    class SetHome : CommandBase
    {
        public SetHome(Client client)
        {
            this.name = "SetHome";
            this.HelpText = " <coords>: Allows you to set your home planet.";

            this.Permission = new List<string>();
            this.Permission.Add("client.sethome");

            this.client = client;
            this.player = client.playerData;
        }

        public override bool doProcess(string[] args)
        {
            if (!hasPermission()) { permissionError(); return false; }

            if (args.Length == 1)
            {
                string[] coords = args[0].Split(':');
                if (coords.Length == 6)
                {
                    string sector = coords[0].ToLower().Trim();
                    if (sector == "alpha" || sector == "beta" || sector == "gamma" || sector == "delta" || sector == "sectorx")
                    {
                        int sysX = int.Parse(coords[1]);
                        int sysY = int.Parse(coords[2]);
                        int sysZ = int.Parse(coords[3]);
                        int planet = int.Parse(coords[4]);
                        int satelite = int.Parse(coords[5]);
                        if (planet >= 0 && satelite >= 0)
                        {
                            Extensions.WorldCoordinate newHome = new Extensions.WorldCoordinate(sector, sysX, sysY, sysZ, planet, satelite);
                            if (!newHome.Equals(this.client.playerData.home))
                            {
                                this.client.playerData.home = newHome;
                                this.client.sendCommandMessage("Your home is now set to " + newHome.ToString());
                                return true;
                            }
                            else
                            {
                                this.client.sendCommandMessage("Your home is already set to this location.");
                                return false;
                            }
                        }
                        else
                        {
                            this.client.sendCommandMessage("Invalid planet/satellite. Must be positive or 0.");
                            return false;
                        }
                    }
                    else
                    {
                        this.client.sendCommandMessage("Invalid sector name. Must be either alpha, beta, gamma, delta or sectorx");
                        return false;
                    }
                }
                else
                {
                    this.client.sendCommandMessage("Invalid syntax. Coords must be in this format: sector:x:y:z:planet:satellite");
                    return false;
                }
            }
            else
            {
                this.client.sendCommandMessage("Invalid syntax. Use: /sethome <coords>");
                this.client.sendCommandMessage("You can find the coords of the current world using /find.");
                return false;
            }
        }
    }
}
