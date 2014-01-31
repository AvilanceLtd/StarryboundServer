﻿/* 
 * Starrybound Server
 * Copyright 2013, Avilance Ltd
 * Created by Zidonuke (zidonuke@gmail.com) and Crashdoom (crashdoom@avilance.com)
 * 
 * This file is a part of Starrybound Server.
 * Starrybound Server is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 * Starrybound Server is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
 * You should have received a copy of the GNU General Public License along with Starrybound Server. If not, see http://www.gnu.org/licenses/.
*/

using com.avilance.Starrybound.Permissions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using com.avilance.Starrybound.Extensions;
using com.avilance.Starrybound.Util;

namespace com.avilance.Starrybound
{
    public class PlayerData
    {
        public string name;
        public string account;
        public string ip;
        public uint id;
        public string uuid;
        public string serverName;

        public bool sentMotd = false;
        public bool freeFuel = false;
        public bool receivedStarterKit = false;

        public Group group;

        public WorldCoordinate loc;
        public WorldCoordinate home;

        public int lastOnline = 0;

        public bool inPlayerShip = true;

        public string client { get { if (String.IsNullOrEmpty(name)) return ip; else return name; } }

        public bool isMuted = false;
        public bool canBuild = true;

        public string claimedPlanet;

        public bool privateShip = false;
        public List<string> shipWhitelist = new List<string>();
        public List<string> shipBlacklist = new List<string>();

        public string formatName 
        {
            get 
            { 
                string prefix = group.prefix;
                string color = group.nameColor;

                if (prefix == null) prefix = "";
                if (color == null) color = "";

                return ((prefix != "") ? prefix + " " : "") + ((color != "") ? "^" + color + ";" : "") + this.name; 
            }
            set { return; }
        }

        public string format
        {
            get
            {
                string prefix = group.prefix;
                string color = group.nameColor;

                if (prefix == null) prefix = "";
                if (color == null) color = "";

                return ((prefix != "") ? prefix + " " : "") + ((color != "") ? "^" + color + ";" : "");
            }
            set { return; }
        }

        public bool hasPermission(string node)
        {
            if (this.group.hasPermission(node)) return true;
            else return false;
        }

        public bool isInSameWorldAs(PlayerData otherPlayer)
        {
            return loc.Equals(otherPlayer.loc);
        }

        public bool canIBuild()
        {
            if (StarryboundServer.serverConfig.useDefaultWorldCoordinate && StarryboundServer.config.spawnWorldProtection)
            {
                if (loc != null)
                {
                    if ((StarryboundServer.spawnPlanet.Equals(loc)) && !group.hasPermission("admin.spawnbuild") && !inPlayerShip)
                        return false;
                }
                else
                    return false;
            }

            if (StarryboundServer.planets.protectedPlanets.ContainsKey(loc.ToString()) && !inPlayerShip)
            {
                Planet planetInfo = StarryboundServer.planets.protectedPlanets[loc.ToString()];
                PlanetAccess access = planetInfo.canAccess(uuid);

                if (access == PlanetAccess.ReadOnly && planetInfo.accessType != (int)ProtectionTypes.Public)
                {
                    return false;
                }
            }
            
            if (!hasPermission("world.build")) return false;
            
            if (!canBuild) return false;

            return true;
        }

        public bool canAccessShip(PlayerData otherPlayer)
        {
            if (this.hasPermission("admin.ignoreshipaccess")) return true; // Admins can bypass access control
            if (otherPlayer.shipBlacklist.Contains(this.name)) return false; // Player is in blacklist

            if (otherPlayer.privateShip) // Ship is on private mode
            {
                if (otherPlayer.shipWhitelist.Contains(this.name)) return true; // Player is in whitelist
                else return false; // Player is NOT in whitelist
            }
            
            else return true; // Ship is on public mode
        }
    }
}
