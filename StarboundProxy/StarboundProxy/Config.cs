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

using com.avilance.Starrybound.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace com.avilance.Starrybound
{
    class Config
    {
        public string serverAccount = "";
        public string serverPass = "";

        public readonly string proxyIP = "0.0.0.0";
        public readonly short proxyPort = 21025;
        public readonly string proxyPass = "Testing";
        public readonly int passwordRounds = 5000;

        public readonly int maxClients = 40;

        public readonly string logFile = "proxy.log";

        public readonly LogType logLevel = LogType.Debug;
        public readonly bool debug = true;

        public readonly string[] adminUUID = new string[] { "be17d6d1257ea51ecb920ecc8d0c3bff", "49be2a484fb0cebc4ec427095cb0dc6b" };

        public readonly bool allowSpaces = false;

        public readonly bool allowSymbols = false;

        public static Config Read(string path) {
            if (!File.Exists(path))
                return new Config();

            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                return Read(fs);
            }
        }

        public static Config Read(Stream stream)
        {
            using (var sr = new StreamReader(stream))
            {
                return JsonConvert.DeserializeObject<Config>(sr.ReadToEnd()); 
            }
        }
    }
}
