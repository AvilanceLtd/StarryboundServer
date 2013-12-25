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
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace com.avilance.Starrybound
{
    public class Player
    {
        public string name;
        public string account;
        public string ip;
        public uint id;
        public string uuid;

        public string client { get { if (String.IsNullOrEmpty(name)) return ip; else return name; } }

        public bool isMuted = false;
        public bool canBuild = true;

        public bool hasPermission(string node)
        {
            if (StarryboundServer.config.adminUUID.Contains(uuid)) return true;
            else return false;
        }
    }
}
