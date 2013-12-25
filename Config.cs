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
    class Config {
        internal static string RulesPath { get { return Path.Combine(StarryboundServer.SavePath, "rules.txt"); } }
        internal static string MotdPath { get { return Path.Combine(StarryboundServer.SavePath, "motd.txt"); } }
        internal static string ConfigPath { get { return Path.Combine(StarryboundServer.SavePath, "config.json"); } }

        public static void CreateIfNot(string file, string data = "")
        {
            if (!File.Exists(file))
            {
                File.WriteAllText(file, data);
            }
        }

        public static void SetupConfig()
        {
            if (!Directory.Exists(StarryboundServer.SavePath))
            {
                Directory.CreateDirectory(StarryboundServer.SavePath);
            }

            CreateIfNot(RulesPath, "1) Respect all players 2) No griefing/hacking 3) Have fun!");
            CreateIfNot(MotdPath, "This server is running Starrybound Server.\nType /help for a list of commands.\nThere are currently %players% player(s) online.");
            if (File.Exists(ConfigPath))
            {
                StarryboundServer.config = ConfigFile.Read(ConfigPath);
            }
            StarryboundServer.config.Write(ConfigPath);
        }
    }

    class ConfigFile
    {
        public string serverAccount = "";
        public string serverPass = "";
        public short serverPort = 21024;

        public string proxyIP = "0.0.0.0";
        public short proxyPort = 21025;
        public string proxyPass = "";
        public int passwordRounds = 5000;

        public int maxClients = 40;

        public string logFile = "proxy.log";

        public LogType logLevel = LogType.Info;

        public string[] adminUUID = new string[] { "" };

        public bool allowSpaces = false;

        public bool allowSymbols = false;

        public string[] sectors = new string[] { "alpha", "beta", "gamma", "delta", "sectorx" };
        
        public static ConfigFile Read(string path) {
            if (!File.Exists(path))
                return new ConfigFile();

            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                return Read(fs);
            }
        }

        public static ConfigFile Read(Stream stream)
        {
            using (var sr = new StreamReader(stream))
            {
                return JsonConvert.DeserializeObject<ConfigFile>(sr.ReadToEnd()); 
            }
        }

        public void Write(string path)
        {
            using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Write))
            {
                Write(fs);
            }
        }

        public void Write(Stream stream)
        {
            var str = JsonConvert.SerializeObject(this, Formatting.Indented);
            using (var sw = new StreamWriter(stream))
            {
                sw.Write(str);
            }
        }
    }
}
