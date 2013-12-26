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

        public string groupName;

        public bool isMuted = false;
        public bool canBuild = true;

        public int lastOnline = 0;

        public User(string name, string uuid, string groupName, bool isMuted, bool canBuild, int lastOnline)
        {
            this.name = name;
            this.uuid = uuid;
            this.groupName = groupName;
            this.isMuted = isMuted;
            this.canBuild = canBuild;
            this.lastOnline = lastOnline;
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
            if (!Directory.Exists(UsersPath)) Directory.CreateDirectory(UsersPath);
        }

        public static User GetUser(string name, string uuid)
        {
            if (File.Exists(Path.Combine(UsersPath, uuid + ".json")))
            {
                return Read(Path.Combine(UsersPath, uuid + ".json"), new string[] { name, uuid });
            }
            else
            {
                User user = new User(name, uuid, StarryboundServer.defaultGroup, false, true, 0);
                Write(Path.Combine(UsersPath, uuid + ".json"), user);

                return user;
            }
        }

        public static void SaveUser(Player player)
        {
            try
            {
                User user = new User(player.name, player.uuid, player.group.name, player.isMuted, player.canBuild, Utils.getTimestamp());
                Write(Path.Combine(UsersPath, player.uuid + ".json"), user);
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
                return new User(data[0], data[1], StarryboundServer.defaultGroup, false, true, Utils.getTimestamp());
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
