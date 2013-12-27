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
                StarryboundServer.logFatal("Bootstrap.config file could not be detected!");
                StarryboundServer.logFatal("Press any key to continue...");
                Console.ReadKey(true);
                Environment.Exit(0);
            }
        }
    }

    class BootstrapFile
    {
        public string[] assetSources;

        public string modSource;

        public string storageDirectory;

        public static BootstrapFile Read(string path)
        {
            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                BootstrapFile file = Read(fs);
                StarryboundServer.logInfo("Bootstrap config loaded successfully.");
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
                StarryboundServer.logFatal("Bootstrap.config file is unreadable. The server start cannot continue.");
                StarryboundServer.logFatal("Press any key to continue...");
                Console.ReadKey(true);
                Environment.Exit(0);
            }

            return null;
        }
    }
}
