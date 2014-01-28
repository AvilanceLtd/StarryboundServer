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

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using com.avilance.Starrybound.Util;
using com.avilance.Starrybound.Extensions;
using System.Threading;

namespace com.avilance.Starrybound
{
    class ServerConfig
    {
        internal static string ConfigPath { get { return Path.Combine(StarryboundServer.bootstrapConfig.storageDirectory, "starbound.config"); } }

        public static void CreateIfNot(string file, string data = "")
        {
            if (!File.Exists(file))
            {
                File.WriteAllText(file, data);
            }
        }

        public static void SetupConfig()
        {
            try
            {
                if (File.Exists(ConfigPath))
                {
                    StarryboundServer.serverConfig = ServerFile.Read(ConfigPath);
                }
                StarryboundServer.serverConfig.gamePort = StarryboundServer.config.serverPort;
                StarryboundServer.privatePassword = Utils.GenerateSecureSalt();
                StarryboundServer.serverConfig.serverPasswords = new string[] { StarryboundServer.privatePassword };
                StarryboundServer.serverConfig.maxPlayers = StarryboundServer.config.maxClients + 10;
                StarryboundServer.serverConfig.bind = StarryboundServer.config.proxyIP;
                if (StarryboundServer.serverConfig.useDefaultWorldCoordinate)
                {
                    string[] spawnPlanet = StarryboundServer.serverConfig.defaultWorldCoordinate.Split(':');
                    if (spawnPlanet.Length == 5) StarryboundServer.spawnPlanet = new WorldCoordinate(spawnPlanet[0], Convert.ToInt32(spawnPlanet[1]), Convert.ToInt32(spawnPlanet[2]), Convert.ToInt32(spawnPlanet[3]), Convert.ToInt32(spawnPlanet[4]), 0);
                    else StarryboundServer.spawnPlanet = new WorldCoordinate(spawnPlanet[0], Convert.ToInt32(spawnPlanet[1]), Convert.ToInt32(spawnPlanet[2]), Convert.ToInt32(spawnPlanet[3]), Convert.ToInt32(spawnPlanet[4]), Convert.ToInt32(spawnPlanet[5]));
                }
                StarryboundServer.serverConfig.Write(ConfigPath);
            }
            catch(Exception e)
            {
                StarryboundServer.logFatal("Failed to parse starbound.config: " + e.ToString());
                Thread.Sleep(5000);
                Environment.Exit(8);
            }
        }

        public static void RemovePrivateConfig()
        {
            StarryboundServer.serverConfig.serverPasswords = new string[] { "" };
            StarryboundServer.serverConfig.gamePort = 21025;
            StarryboundServer.serverConfig.Write(ConfigPath);
        }
    }

    [DataContract]
    class ServerFile
    {
        [DataMember]
        public bool allowAdminCommands = true;
        [DataMember]
        public bool allowAdminCommandsFromAnyone = false;

        [DataMember]
        public bool attemptAuthentication = false;

        [DataMember]
        public int[] audioChannelSeparation = new int[] { -25, 25 };

        [DataMember]
        public int audioChannels = 2;

        [DataMember]
        public string authHostname = "auth.playstarbound.com";
        [DataMember]
        public int authPort = 21027;

        [DataMember]
        public int bcryptRounds = 5000;

        [DataMember]
        public string bind = "0.0.0.0";

        [DataMember]
        public bool checkAssetsDigest = false;

        [DataMember]
        public string claimFile = "indev.claim";

        [DataMember]
        public bool clearPlayerFiles = false;
        [DataMember]
        public bool clearUniverseFiles = false;

        [DataMember]
        public int controlPort = 21026;

        [DataMember(Name = "crafting.filterHaveMaterials")]
        public bool craftingfilterHaveMaterials = false;

        [DataMember]
        public string defaultWorldCoordinate = "";

        [DataMember]
        public bool fullscreen = false;
        [DataMember]
        public int[] fullscreenResoluton = new int[] { 1920, 1080 };

        [DataMember]
        public int gamePort = 21024;

        [DataMember]
        public int maxFrameskip = 5;

        [DataMember]
        public int maxPlayers = 40;

        [DataMember]
        public bool maximized = true;

        [DataMember]
        public int[] maximizedResolution = new int[] { 1920, 1017 };

        [DataMember]
        public int musicVol = 100;

        [DataMember]
        public string passwordHash = "";

        [DataMember]
        public int pixelRadioIdx = 3;

        [DataMember]
        public int renderPreSleepRemainder = 4;
        [DataMember]
        public bool renderPriority = true;
        [DataMember]
        public bool renderSleep = true;

        [DataMember]
        public string rootKey = "MIICCgKCAgEAuuxHOxa47eCix12TeI1KiDuSvu6Yculxl3yOzXDGG3OfS3A1sUioUy5wM8YZwoI0jpiVxsyZgsFZtzbO948H/v47I6YXwfGJ0ciw0RrHfxgPpeoTEEBOckWYJAFYWmF0xh5tN7RxMkVwFcGSFImgsA83h4xNvC9m+eiLs741sCfs36qD5Ka0ApI2RzeruKbGDZ/lBy/E/3HfLWOituTu37WZEbkFSruc0zu6aNAeEJB7vV4pun4BaEX7MtMzIokvGfzxRYJNlp6+T7McMAcNHQShkWx7cVd8TPzEUe7oafmrw0EM77Ja5PIil0w0Zr3Z1ITKI+G1zGSeAvjdO6N4hMUEthcT5H7YDuVZVb/pSPGAojIjh7lhoFZBI+2k9mOFMV+D6ysWbfScfaczRW9W6Gs5Kt/Gqa+iLGR6P4Xa9pOfjVz/mIW1HzYjPcjXqhC4rsiur1wlkqljowcK5dyufb79eeULUgq9j7g2lBDkEuvzm83plrwvSKKRFToB8D4nFW01c+HAbpNZdEW1r+J+NdV8Meoo4sB9+8wEfnQadS/eGEqNwh6CPVIXTubpzqgXiLCowFjRQ0O9m5GjrsnC8vGbNZOxvxp/gM6uhoYEVm2QvZPFRjsBiOEWi6/Z5YXs3fa2528JPBe/bGOb+QA8NUiyIFfPl/c4muFoR81yixkCAwEAAQ==";

        [DataMember]
        public int sampleRate = 44100;

        [DataMember]
        public string serverName = "A Starrybound Server";

        [DataMember]
        public string[] serverPasswords = new string[] { "" }; // #TODO: Random server password on every server start

        [DataMember]
        public int sfxVol = 100;

        [DataMember]
        public bool speechBubbles = true;

        [DataMember]
        public int tileDamageLimit = 49;

        [DataMember(Name = "title.connectionString")]
        public string titleConnectionString = "playsb.avilance.com";

        [DataMember]
        public bool upnpPortForwarding = true;

        [DataMember]
        public bool useDefaultWorldCoordinate = false;

        [DataMember]
        public string username = "";

        [DataMember]
        public bool vsync = true;

        [DataMember]
        public bool waitForUpdate = true;

        [DataMember]
        public string windowTitle = "Starbound - Beta";

        [DataMember]
        public int[] windowedResolution = new int[] { 894, 744 };

        [DataMember]
        public int zoomLevel = 3;

        public static ServerFile Read(string path)
        {
            if (!File.Exists(path))
                return new ServerFile();

            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                ServerFile file = Read(fs);
                StarryboundServer.logInfo("Starbound server config loaded successfully.");
                return file;
            }
        }

        public static ServerFile Read(Stream stream)
        {
            try
            {
                DataContractJsonSerializer str = new DataContractJsonSerializer(StarryboundServer.serverConfig.GetType());
                return str.ReadObject(stream) as ServerFile;
            }
            catch (Exception) 
            {
                StarryboundServer.logException("Starbound server config is unreadable - Re-creating config with default values");
                return new ServerFile(); 
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
            DataContractJsonSerializer str = new DataContractJsonSerializer(this.GetType());
            str.WriteObject(stream, this);
        }
    }
}
