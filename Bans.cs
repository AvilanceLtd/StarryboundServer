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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace com.avilance.Starrybound
{
    public class Ban
    {
        int banID;
        int timeBanned;
        string admin;
        int expiry;
        string reason;
        private string username;
        private string uuid;
        private string ipaddress;

        public Ban(int banID, string username, string uuid, string ipaddress, int timeBanned, string admin, int expiry, string reason)
        {
            this.banID = banID;
            this.username = username;
            this.uuid = uuid;
            this.ipaddress = ipaddress;
            this.timeBanned = timeBanned;
            this.admin = admin;
            this.expiry = expiry;
            this.reason = reason;
        }

        public string getReason() { return this.reason; }

        public string getExpiry() { return this.expiry.ToString(); }

        public bool hasExpired() 
        {
            if (this.expiry == 0) return false;
            else if (Utils.getTimestamp() > this.expiry) return true; 
            else return false; 
        }

        public bool doesMatch(string[] needle) 
        {
            if (username.Equals(needle[0]) && !String.IsNullOrWhiteSpace(username)) return true; 
            else if (uuid.Equals(needle[1]) && !String.IsNullOrWhiteSpace(uuid)) return true;
            else if (ipaddress.Equals(needle[2]) && !String.IsNullOrWhiteSpace(ipaddress)) return true;
            else return false; 
        }

        public void remove()
        {
            Bans.allBans.Remove(banID);
            Bans.removeBan(banID);
        }
    }

    public static class Bans
    {
        public static Dictionary<int, Ban> allBans = new Dictionary<int, Ban>();

        static int nextBanID = 1;

        /// <summary>
        /// Checks to see if the data matches the ban record
        /// </summary>
        /// <param name="args">Username, UUID, IP Address</param>
        /// <returns>empty array if no match, or a string array with reason and expiry</returns>
        public static string[] checkForBan(string[] args)
        {
            foreach (Ban banData in allBans.Values)
            {
                if (banData.hasExpired()) banData.remove();
                if (banData.doesMatch(args)) return new string[] { banData.getReason(), banData.getExpiry() };
            }

            return new string[] { };
        }

        public static void removeBan(int banID)
        {
            try
            {

                List<string> allLines = File.ReadLines(Path.Combine(StarryboundServer.SavePath, "banned-players.txt")).ToList();
                string[] saveLines = new string[allLines.Count];

                for (var i = 0; i < allLines.Count; i++) 
                {
                    string line = allLines[i];
                    if (!line.StartsWith(banID.ToString())) saveLines[i] = line;
                }

                File.WriteAllLines(Path.Combine(StarryboundServer.SavePath, "banned-players.txt"), saveLines);
            }
            catch (Exception e)
            {
                StarryboundServer.logException("Exception while trying to remove ban from banned-players.txt - " + e.StackTrace);
            }
        }

        public static bool addNewBan(string username, string uuid, string ipaddress, int timeBanned, string admin, int expiry, string reason)
        {
            string[] args = new string[8];

            args[0] = nextBanID.ToString();
            args[1] = username;
            args[2] = uuid;
            args[3] = ipaddress;
            args[4] = timeBanned.ToString();
            args[5] = admin;
            args[6] = expiry.ToString();
            args[7] = reason;

            try
            {
                FileStream fs = new FileStream(Path.Combine(StarryboundServer.SavePath, "banned-players.txt"), FileMode.Append, FileAccess.Write);
                StreamWriter sw = new StreamWriter(fs);
                sw.WriteLine(String.Join("|", args));
                sw.Flush();
                sw.Close();

                Ban ban = new Ban(nextBanID, username, uuid, ipaddress, timeBanned, admin, expiry, reason);

                allBans.Add(nextBanID, ban);

                nextBanID++;
            }
            catch (Exception e) {
                StarryboundServer.logException("Unable to write ban to banned-players.txt: " + e.Message);
                return false; 
            }

            return true;
        }

        public static void readBansFromFile()
        {
            try
            {
                StreamReader reader = File.OpenText(Path.Combine(StarryboundServer.SavePath, "banned-players.txt"));
                string line;
                int banCount = 0;
                while ((line = reader.ReadLine()) != null)
                {
                    string[] args = line.Split('|');
                    /*
                     * Ban format:
                     * int          ID
                     * string       Username
                     * string       UUID
                     * string       IP Address
                     * timestamp    UnixTimestamp (Time of Addition)
                     * string       Admin
                     * timestamp    Expiry
                     * string       Reason
                     * 
                     * Example:
                     * 2|User|133bfef193364620513ea1980ba39dc3|127.0.0.1|1387767903|Crashdoom|0|Griefing the spawn
                     * 
                     */

                    if (args.Length != 8) continue;

                    try
                    {
                        int banID = int.Parse(args[0]);
                        string username = args[1];
                        string uuid = args[2];
                        string ipaddress = args[3];
                        int timeBanned = int.Parse(args[4]);
                        string admin = args[5];
                        int expiry = int.Parse(args[6]);
                        string reason = args[7];

                        Ban ban = new Ban(banID, username, uuid, ipaddress, timeBanned, admin, expiry, reason);

                        allBans.Add(banID, ban);

                        nextBanID = banID + 1;

                        banCount++;
                    }
                    catch (Exception) { banCount--; StarryboundServer.logWarn("Invalid ban detected in banned-players.txt"); }
                }

                reader.Close();

                int removedCount = 0;
                foreach (Ban ban in allBans.Values)
                {
                    if (ban.hasExpired()) { ban.remove(); removedCount++; }
                }

                StarryboundServer.logInfo(banCount + " ban(s) have been loaded from file. " + removedCount + " ban(s) have expired and been removed.");
            }
            catch (Exception e)
            {
                StarryboundServer.logWarn("Unable to read bans from banned-players.txt: " + e.Message);
            }
        }
    }
}
