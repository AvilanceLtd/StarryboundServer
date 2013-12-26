using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace com.avilance.Starrybound.Permissions
{
    public class Group
    {
        public string name;
        public string nameColor;
        public string prefix;
        public bool isDefault = false;
        public Dictionary<string, bool> permissions;

        public Group(string name, string nameColor, string prefix, Dictionary<string, bool> permissions, bool isDefault = false)
        {
            this.name = name;
            this.nameColor = nameColor;
            this.prefix = prefix;
            this.permissions = permissions;
            this.isDefault = isDefault;
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
            }
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
                StarryboundServer.logError("Default user group flag (isDefault) is not set for any groups - Please set this in the groups.json!");
                StarryboundServer.serverState = Util.ServerState.Crashed;
                return;
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
            Group superAdmin = new Group("superadmin", "#9801ba", "[SA]", saPerms);
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
            Group admin = new Group("admin", "#ba0123", "[A]", aPerms);
            groups.Add(admin);

            Dictionary<string, bool> mPerms = new Dictionary<string, bool>();
            mPerms.Add("admin.kick", true);
            mPerms.Add("admin.mute", true);
            mPerms.Add("admin.ban", true);
            mPerms.Add("admin.build", true);
            mPerms.Add("admin.chat", true);
            mPerms.Add("client.*", true);
            mPerms.Add("chat.*", true);
            Group mod = new Group("moderator", "#ea6207", "[M]", mPerms);
            groups.Add(mod);

            Dictionary<string, bool> pPerms = new Dictionary<string, bool>();
            pPerms.Add("client.*", true);
            pPerms.Add("chat.*", true);
            Group player = new Group("player", null, null, mPerms);
            groups.Add(player);

            Dictionary<string, bool> gPerms = new Dictionary<string, bool>();
            gPerms.Add("client.*", true);
            gPerms.Add("chat.*", true);
            Group guest = new Group("guest", null, null, mPerms, true);
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
                StarryboundServer.logInfo("Starrybound config loaded successfully.");
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
