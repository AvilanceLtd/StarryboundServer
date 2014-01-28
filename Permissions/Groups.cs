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
using System.Threading;

namespace com.avilance.Starrybound.Permissions
{
    public class Group
    {
        public string name;
        public string nameColor;
        public string prefix;
        public bool isDefault = false;
        public bool isStaff = false;
        public Dictionary<string, bool> permissions;

        public Group(string name, string nameColor, string prefix, Dictionary<string, bool> permissions, bool isDefault = false, bool isStaff = false)
        {
            this.name = name;
            this.nameColor = nameColor;
            this.prefix = prefix;
            this.permissions = permissions;
            this.isDefault = isDefault;
            this.isStaff = isStaff;
        }

        public bool hasPermission(string node)
        {
            string[] allNodes = node.Split('.');
            string rootNode = allNodes[0] + ".";
            string subNode = allNodes[1];

            foreach (KeyValuePair<string, bool> key in permissions)
            {
                if (key.Key.Equals("*")) return true;
                else if (key.Key.Equals(rootNode + "*"))
                {
                    if (key.Value) return true; else return false;
                }
                else if (key.Key.Equals(rootNode + subNode))
                {
                    if (key.Value) return true; else return false;
                }
            }

            return false;
        }

        public bool givePermission(string node)
        {
            if (!node.Contains(".")) return false;

            if (hasPermission(node)) return false;

            permissions.Add(node, true);
            return true;
        }
    }

    class Groups 
    {
        internal static string GroupsPath { get { return Path.Combine(StarryboundServer.SavePath, "groups.json"); } }

        public static void SetupGroups()
        {
            if (File.Exists(GroupsPath))
            {
                GroupFile.ProcessGroups(GroupFile.Read(GroupsPath));
                GroupFile.Write(GroupsPath);
            }
            else
            {
                GroupFile.Write(GroupsPath);
                GroupFile.ProcessGroups(GroupFile.Read(GroupsPath));
            }
        }

        public static void SaveGroups()
        {
            GroupFile.Write(GroupsPath);
        }
    }

    class GroupFile 
    {
        public static void ProcessGroups(List<Group> groupList)
        {
            string defaultGroup = null;

            foreach (Group group in groupList)
            {
                StarryboundServer.groups.Add(group.name, group);
                if (group.isDefault) defaultGroup = group.name;
            }

            if (String.IsNullOrWhiteSpace(defaultGroup))
            {
                StarryboundServer.logFatal("Default user group flag (isDefault) is not set for any groups - Please set this in the groups.json!");
                Thread.Sleep(5000);
                Environment.Exit(5);
            }

            StarryboundServer.defaultGroup = defaultGroup;

            StarryboundServer.logInfo("Loaded " + StarryboundServer.groups.Count + " group(s). Default group is " + defaultGroup);
        }

        public static object getGroup(string name)
        {
            try
            {
                return StarryboundServer.groups[name];
            }
            catch (Exception)
            {
                return null;
            }
        }

        static List<Group> DefaultGroups()
        {
            List<Group> groups = new List<Group>();

            Dictionary<string, bool> saPerms = new Dictionary<string,bool>();
            saPerms.Add("*", true);
            Group superAdmin = new Group("superadmin", "#9801ba", "[SA]", saPerms, false, true);
            groups.Add(superAdmin);

            Dictionary<string, bool> aPerms = new Dictionary<string, bool>();
            aPerms.Add("admin.kick", true);
            aPerms.Add("admin.mute", true);
            aPerms.Add("admin.ban", true);
            aPerms.Add("admin.unban", true);
            aPerms.Add("admin.build", true);
            aPerms.Add("admin.give", true);
            aPerms.Add("admin.broadcast", true);
            aPerms.Add("admin.chat", true);
            aPerms.Add("client.*", true);
            aPerms.Add("chat.*", true);
            aPerms.Add("world.*", true);
            Group admin = new Group("admin", "#ba0123", "[A]", aPerms, false, true);
            groups.Add(admin);

            Dictionary<string, bool> mPerms = new Dictionary<string, bool>();
            mPerms.Add("admin.kick", true);
            mPerms.Add("admin.mute", true);
            mPerms.Add("admin.ban", true);
            mPerms.Add("admin.build", true);
            mPerms.Add("admin.chat", true);
            mPerms.Add("client.*", true);
            mPerms.Add("chat.*", true);
            mPerms.Add("world.*", true);
            Group mod = new Group("moderator", "#ea6207", "[M]", mPerms, false, true);
            groups.Add(mod);

            Dictionary<string, bool> pPerms = new Dictionary<string, bool>();
            pPerms.Add("client.*", true);
            pPerms.Add("chat.*", true);
            pPerms.Add("world.build", true);
            Group player = new Group("player", null, null, pPerms);
            groups.Add(player);

            Dictionary<string, bool> gPerms = new Dictionary<string, bool>();
            gPerms.Add("client.*", true);
            gPerms.Add("chat.*", true);
            gPerms.Add("world.build", true);
            Group guest = new Group("guest", null, null, gPerms, true);
            groups.Add(guest);

            return groups;
        }

        public static List<Group> Read(string path)
        {
            if (!File.Exists(path))
            {
                return DefaultGroups();
            }

            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                List<Group> file = Read(fs);
                StarryboundServer.logInfo("Starrybound groups loaded successfully.");
                return file;
            }
        }

        public static List<Group> Read(Stream stream)
        {
            try
            {
                using (var sr = new StreamReader(stream))
                {
                    return JsonConvert.DeserializeObject<List<Group>>(sr.ReadToEnd());
                }
            }
            catch (Exception)
            {
                StarryboundServer.logException("Server player groups are unreadable - Re-creating config with default values");
                return DefaultGroups();
            }
        }

        public static void Write(string path)
        {
            using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Write))
            {
                Write(fs);
            }
        }

        public static void Write(Stream stream)
        {
            List<Group> groupList;
            if (StarryboundServer.groups.Count > 0)
            {
                groupList = new List<Group>();

                foreach (Group group in StarryboundServer.groups.Values)
                {
                    groupList.Add(group);
                }
            } else groupList = DefaultGroups();

            var str = JsonConvert.SerializeObject(groupList, Formatting.Indented);
            using (var sw = new StreamWriter(stream))
            {
                sw.Write(str);
            }
        }
    }
}
