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
using System.ComponentModel;

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

        public static string ReadConfigFile(string file)
        {
            return File.ReadAllText(file);
        }

        public static string GetMotd()
        {
            string returnString = StarryboundServer.motdData;

            returnString = returnString.Replace("%players%", StarryboundServer.clientCount.ToString());
            returnString = returnString.Replace("%versionNum%", StarryboundServer.VersionNum.ToString());

            return returnString;
        }

        public static string GetRules()
        {
            return StarryboundServer.rulesData;
        }

        public static void SetupConfig()
        {
            CreateIfNot(RulesPath, "1) Respect all players 2) No griefing/hacking 3) Have fun!");
            CreateIfNot(MotdPath, "This server is running Starrybound Server v%versionNum%. Type /help for a list of commands. There are currently %players% player(s) online.");

            StarryboundServer.motdData = ReadConfigFile(MotdPath);
            StarryboundServer.rulesData = ReadConfigFile(RulesPath);
            
            if (File.Exists(ConfigPath))
            {
                StarryboundServer.config = ConfigFile.Read(ConfigPath);
            }

            if (StarryboundServer.IsMono && StarryboundServer.config == null)
                StarryboundServer.config = new ConfigFile();

            StarryboundServer.config.Write(ConfigPath);

#if DEBUG
            StarryboundServer.config.logLevel = LogType.Debug;
            StarryboundServer.logDebug("SetupConfig", "This was compiled in DEBUG, forcing debug logging!");
#endif
        }
    }

    class ConfigFile
    {
        [Description("")]
        public short serverPort = 21024;
        public string proxyIP = "0.0.0.0";
        public short proxyPort = 21025;
        public string proxyPass = "";
        public int passwordRounds = 5000;

        public int maxClients = 25;

        public string logFile = "proxy.log";
        public LogType logLevel = LogType.Info;

        public bool allowSpaces = true;
        public bool allowSymbols = false;
        public string[] bannedUsernames = new string[] { "admin", "developer", "moderator", "owner" };

        public bool freeFuelForNewPlayers = true;
        public string[] starterItems = new string[] { "" };

        public bool spawnWorldProtection = false;
        public string buildErrorMessage = "You do not have permission to build on this server. You can apply for build rights on our forum.";

        public string[] sectors = new string[] { "alpha", "beta", "gamma", "delta", "sectorx" };

        public bool allowModdedClients = true;

        public bool enableGeoIP = false;
        public int maxFailedConnections = 3;

        public string[] projectileBlacklist = new string[] { "" };
        public string[] projectileBlacklistSpawn = new string[] { "" };
        public bool projectileSpawnListIsWhitelist = false;

        public int connectTimeout = 5;
        public int internalSocketTimeout = 5;
        public int clientSocketTimeout = 15;

        public bool enableCallback = true;
        
        public static ConfigFile Read(string path) {
            if (!File.Exists(path))
                return new ConfigFile();

            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                ConfigFile file = Read(fs);
                StarryboundServer.logInfo("Starrybound config loaded successfully.");
                return file;
            }
        }

        public static ConfigFile Read(Stream stream)
        {
            try
            {
                using (var sr = new StreamReader(stream))
                {
                    return JsonConvert.DeserializeObject<ConfigFile>(sr.ReadToEnd());
                }
            }
            catch (Exception) 
            {
                StarryboundServer.logException("Starrybound config is unreadable - Re-creating config with default values");
                return new ConfigFile(); 
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
