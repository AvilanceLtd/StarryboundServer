using com.avilance.Starrybound.Extensions;
using com.avilance.Starrybound.Util;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace com.avilance.Starrybound.Permissions
{
    [JsonObject(MemberSerialization.OptIn)]
    class Planet
    {
        public string loc;
        public int accessType = (int)ProtectionTypes.Public;

        public string[] owner = new string[2];

        public Dictionary<string, PlanetAccess> players = new Dictionary<string, PlanetAccess>();
        public List<string> banlist = new List<string>();

        /*
         * PlanetAccess.Owner       - Full access to planet
         * PlanetAccess.Moderator   - Ability to kick/ban players from planet
         * PlanetAccess.Builder     - Read/write access to planet
         * PlanetAccess.ReadOnly    - Read-only access to planet
         * PlanetAccess.Banned      - Explicitly no access to planet
         */
        public PlanetAccess canAccess(string uuid)
        {
            if (this.owner[0] == uuid) return PlanetAccess.Owner;
            else if (this.banlist.Contains(uuid)) return PlanetAccess.Banned;
            else if (this.players.ContainsKey(uuid)) return this.players[uuid];
            else if (this.accessType == (int)ProtectionTypes.Private) return PlanetAccess.Banned;
            else return PlanetAccess.ReadOnly;
        }

        [JsonConstructor]
        public Planet(string loc, int accessType, string[] owner, Dictionary<string, PlanetAccess> players, List<string> banlist)
        {
            this.loc = loc;
            this.accessType = accessType;
            this.owner = owner;
            this.players = players;
            this.banlist = banlist;
        }

        public Planet(Client client)
        {
            client.sendChatMessage("^#00aeff;The planet has been claimed - You can use /manage help to find a list of planetary management commands.");

            this.owner[0] = client.playerData.uuid;
            this.owner[1] = client.playerData.name;

            this.loc = client.playerData.loc.ToString();

            client.playerData.claimedPlanet = this.loc;

            StarryboundServer.planets.protectedPlanets.Add(this.loc, this);
            StarryboundServer.planets.planetOwners.Add(client.playerData.uuid, this.loc);

            StarryboundServer.logInfo("Planet protection created by " + client.playerData.name + " at " + this.loc.ToString() + " - Now " + StarryboundServer.planets.protectedPlanets.Count + " planet(s) protected.");

            Planets.SavePlanets();
        }

        public void setAccess(Client client, PlanetAccess access)
        {
            client.sendChatMessage("^#00aeff;Your access on " + this.loc.ToString() + " has been updated to " + access.ToString());

            if (players.ContainsKey(client.playerData.uuid)) players.Remove(client.playerData.uuid);

            players.Add(client.playerData.uuid, access);

            Planets.SavePlanets();
        }

        public bool removeAccess(Client client)
        {
            if (!players.ContainsKey(client.playerData.uuid)) return false;

            client.sendChatMessage("^#00aeff;Your access on " + this.loc.ToString() + " has been revoked.");

            players.Remove(client.playerData.uuid);

            Planets.SavePlanets();

            return true;
        }

        public void addBan(Client client)
        {
            string uuid = client.playerData.uuid;

            if (!banlist.Contains(uuid))
            {
                banlist.Add(uuid);
                Planets.SavePlanets();
            }

            if (client.playerData.loc.ToString() == this.loc && !client.playerData.inPlayerShip)
            {
                client.sendChatMessage("^#f75d5d;Error: You have been banned from this planet by the owner.");
                kickPlayer(client);
            }
        }

        public void kickPlayer(Client client)
        {
            client.playerData.inPlayerShip = true;

            MemoryStream packetWarp = new MemoryStream();
            BinaryWriter packetWrite = new BinaryWriter(packetWarp);
            packetWrite.WriteBE((uint)WarpType.WarpToOwnShip);
            packetWrite.Write(new WorldCoordinate());
            packetWrite.WriteStarString("");
            client.sendServerPacket(Packet.WarpCommand, packetWarp.ToArray());
        }

        public bool removeBan(Client client)
        {
            string uuid = client.playerData.uuid;

            if (banlist.Contains(uuid))
            {
                banlist.Remove(uuid);
                Planets.SavePlanets();
                return true;
            } else return false;
        }

        public void deletePlanet()
        {
            StarryboundServer.planets.protectedPlanets.Remove(loc);
            StarryboundServer.planets.planetOwners.Remove(this.owner[0]);
            Planets.SavePlanets();
        }
    }

    class Planets
    {
        public Dictionary<string, Planet> protectedPlanets = new Dictionary<string, Planet>();
        public Dictionary<string, string> planetOwners = new Dictionary<string, string>(); // Dictionary<UUID, WorldCoordinate>

        internal static string PlanetsPath { get { return Path.Combine(StarryboundServer.SavePath, "planets.json"); } }

        public static void SetupPlanets()
        {
            if (File.Exists(PlanetsPath))
            {
                PlanetsFile.ProcessPlanets(PlanetsFile.Read(PlanetsPath));
            }
            else
            {
                PlanetsFile.Write(PlanetsPath);
                PlanetsFile.ProcessPlanets(PlanetsFile.Read(PlanetsPath));
            }
        }

        public static void SavePlanets()
        {
            PlanetsFile.Write(PlanetsPath);
        }
    }

    class PlanetsFile
    {
        public static void ProcessPlanets(List<Planet> planetList)
        {
            foreach (Planet planet in planetList)
            {
                StarryboundServer.planets.protectedPlanets.Add(planet.loc, planet);
                StarryboundServer.planets.planetOwners.Add(planet.owner[0], planet.loc);
            }

            StarryboundServer.logInfo(StarryboundServer.planets.protectedPlanets.Count + " planet(s) loaded.");
        }

        public static List<Planet> Read(string path)
        {
            if (!File.Exists(path))
            {
                return new List<Planet>();
            }

            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                List<Planet> file = Read(fs);
                StarryboundServer.logInfo("Starrybound planets loaded successfully.");
                return file;
            }
        }

        public static List<Planet> Read(Stream stream)
        {
            try
            {
                using (var sr = new StreamReader(stream))
                {
                    return JsonConvert.DeserializeObject<List<Planet>>(sr.ReadToEnd());
                }
            }
            catch (Exception e)
            {
                StarryboundServer.logException("Planet files are unreadable - User planets will not be protected: " + e.ToString());
                return new List<Planet>();
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
            List<Planet> planetList = new List<Planet>();
            if (StarryboundServer.planets.protectedPlanets.Count > 0)
            {
                foreach (Planet planet in StarryboundServer.planets.protectedPlanets.Values)
                {
                    planetList.Add(planet);
                }
            }

            var str = JsonConvert.SerializeObject(planetList, Formatting.Indented);
            using (var sw = new StreamWriter(stream))
            {
                sw.Write(str);
            }
        }
    }
}
