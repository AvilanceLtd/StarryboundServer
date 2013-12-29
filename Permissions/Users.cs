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
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace com.avilance.Starrybound.Permissions
{
    public class User
    {
        public string name;
        public string uuid;
        public string lastIp;
        public string groupName;

        public bool isMuted = false;
        public bool canBuild = true;
        public bool freeFuel = false;
        public bool receivedStarterKit = false;

        public int lastOnline = 0;

        public User(string name, string uuid, string lastIp, string groupName, bool isMuted, bool canBuild, int lastOnline, bool freeFuel, bool starterItems)
        {
            this.name = name;
            this.uuid = uuid;
            this.lastIp = lastIp;
            this.groupName = groupName;
            this.isMuted = isMuted;
            this.canBuild = canBuild;
            this.lastOnline = lastOnline;
            this.freeFuel = freeFuel;
            this.receivedStarterKit = starterItems;
        }

        public Group getGroup()
        {
            try
            {
                return StarryboundServer.groups[groupName];
            }
            catch (Exception)
            {
                groupName = StarryboundServer.defaultGroup;
                return StarryboundServer.groups[groupName];
            }
        }
    }

    class Users
    {
        internal static string UsersPath { get { return Path.Combine(StarryboundServer.SavePath, "players"); } }

        public static void SetupUsers()
        {
            if (!Directory.Exists(UsersPath))
            {
                Directory.CreateDirectory(UsersPath);
                GenerateSAKey();
            }
            else if (!Directory.EnumerateFileSystemEntries(UsersPath).Any()) GenerateSAKey();
            else if (File.Exists(Path.Combine(StarryboundServer.SavePath, "authcode.txt"))) GenerateSAKey();
        }

        public static void GenerateSAKey()
        {
            if (!File.Exists(Path.Combine(StarryboundServer.SavePath, "authcode.txt")))
            {
                var r = new Random((int)DateTime.Now.ToBinary());
                StarryboundServer.authCode = r.Next(100000, 10000000).ToString();

                using (var tw = new StreamWriter(Path.Combine(StarryboundServer.SavePath, "authcode.txt")))
                {
                    tw.WriteLine(StarryboundServer.authCode);
                }
            }
            else
            {
                using (var tr = new StreamReader(Path.Combine(StarryboundServer.SavePath, "authcode.txt")))
                {
                    StarryboundServer.authCode = tr.ReadLine();
                }
            }

            StarryboundServer.logWarn("***********************************************************************************************");
            StarryboundServer.logWarn("Important notice: To become SuperAdmin, you need to join the game and type /auth " + StarryboundServer.authCode);
            StarryboundServer.logWarn("This token will display until disabled by verification and is usable by any player.");
            StarryboundServer.logWarn("***********************************************************************************************");
        }

        public static User GetUser(string name, string uuid, string ip)
        {
            if (File.Exists(Path.Combine(UsersPath, name.ToLower() + ".json")))
            {
                try
                {
                    User user = Read(Path.Combine(UsersPath, name.ToLower() + ".json"), new string[] { name, uuid });
                    return user;
                }
                catch (Exception)
                {
                    StarryboundServer.logError("Player data for user " + name.ToLower() + " with UUID " + uuid + " is corrupt. Re-generating user file");

                    User user = new User(name, uuid, ip, StarryboundServer.defaultGroup, false, true, 0, true, true);
                    Write(Path.Combine(UsersPath, name.ToLower() + ".json"), user);

                    return user;
                }
            }
            else
            {
                User user = new User(name, uuid, ip, StarryboundServer.defaultGroup, false, true, 0, false, false);
                Write(Path.Combine(UsersPath, name.ToLower() + ".json"), user);

                return user;
            }
        }

        public static void SaveUser(PlayerData player)
        {
            try
            {
                User user = new User(player.name, player.uuid, player.ip, player.group.name, player.isMuted, player.canBuild, Utils.getTimestamp(), player.freeFuel, player.receivedStarterKit);
                Write(Path.Combine(UsersPath, player.name.ToLower() + ".json"), user);
            }
            catch (Exception e)
            {
                StarryboundServer.logException("Unable to save player data file for " + player.name + ": " + e.StackTrace);
            }
        }

        static User Read(string path, string[] data)
        {

            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                User file = Read(fs, data);
                StarryboundServer.logInfo("Loaded persistant user storage for " + file.name);
                return file;
            }
        }

        static User Read(Stream stream, string[] data)
        {
            try
            {
                using (var sr = new StreamReader(stream))
                {
                    return JsonConvert.DeserializeObject<User>(sr.ReadToEnd());
                }
            }
            catch (Exception)
            {
                StarryboundServer.logException("Persistant user storage for " + data[0] + " is corrupt - Creating with default values");
                return new User(data[0], data[1], data[2], StarryboundServer.defaultGroup, false, true, Utils.getTimestamp(), false, false);
            }
        }

        static void Write(string path, User user)
        {
            using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Write))
            {
                Write(fs, user);
            }
        }

        static void Write(Stream stream, User user)
        {
            var str = JsonConvert.SerializeObject(user, Formatting.Indented);
            using (var sw = new StreamWriter(stream))
            {
                sw.Write(str);
            }
        }
    }
}
