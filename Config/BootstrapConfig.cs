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
using System.Threading;

namespace com.avilance.Starrybound
{
    class BootstrapConfig
    {
        internal static string BootstrapPath { get { return "bootstrap.config"; } }

        public static void SetupConfig()
        {
            if (File.Exists(BootstrapPath))
            {
                StarryboundServer.bootstrapConfig = BootstrapFile.Read(BootstrapPath);
                StarryboundServer.SavePath = StarryboundServer.bootstrapConfig.storageDirectory + Path.DirectorySeparatorChar + "starrybound";
            }
            else
            {
                Console.WriteLine("[FATAL ERROR] bootstrap.config file could not be detected!");
                Thread.Sleep(5000);
                Environment.Exit(7);
            }
            if (!Directory.Exists(StarryboundServer.SavePath))
            {
                Directory.CreateDirectory(StarryboundServer.SavePath);
            }
        }
    }

    class BootstrapFile
    {

        public string[] assetSources = new string[] { "../assets" };

        public string modSource = "../mods";

        public string storageDirectory = "..";

        public static BootstrapFile Read(string path)
        {
            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                BootstrapFile file = Read(fs);
                return file;
            }
        }

        public static BootstrapFile Read(Stream stream)
        {
            try
            {
                using (var sr = new StreamReader(stream))
                {
                    return JsonConvert.DeserializeObject<BootstrapFile>(sr.ReadToEnd());
                }
            }
            catch (Exception)
            {
                StarryboundServer.logFatal("bootstrap.config file is unreadable. The server start cannot continue.");
                Thread.Sleep(5000);
                Environment.Exit(6);
            }

            return null;
        }
    }
}
