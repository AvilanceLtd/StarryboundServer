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
using com.avilance.Starrybound.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using com.avilance.Starrybound.Commands;

namespace com.avilance.Starrybound.Packets
{
    class Packet11ChatSend : PacketBase
    {
        Dictionary<string, object> tmpArray = new Dictionary<string, object>();

        public Packet11ChatSend(Client clientThread, BinaryReader stream, Direction direction)
        {
            this.client = clientThread;
            this.stream = stream;
            this.direction = direction;
        }

        public Packet11ChatSend(Client clientThread, Direction direction)
        {
            this.client = clientThread;
            this.direction = direction;
        }

        /// <summary>
        /// Reads the data from the packet stream
        /// </summary>
        /// <returns>true to maintain packet and send to client, false to drop packet, -1 will boot the client</returns>
        public override Object onReceive()
        {
            BinaryReader packetData = (BinaryReader)this.stream;

            string message = packetData.ReadStarString();
            byte context = packetData.ReadByte();

            #region Command Processor
            if (message.StartsWith("#"))
            {
                StarryboundServer.logInfo("[Admin Chat] [" + this.client.playerData.name + "]: " + message);

                bool aChat = new AdminChat(this.client).doProcess(new string[] { message.Remove(0, 1) });

                return false;
            }
            else if (message.StartsWith("/"))
            {
                try
                {
                    StarryboundServer.logInfo("[Command] [" + this.client.playerData.name + "]: " + message);
                    string[] args = message.Remove(0, 1).Split(' ');
                    string cmd = args[0].ToLower();

                    args = parseArgs(message.Remove(0, cmd.Length + 1));

                    switch (cmd)
                    {
                        case "ban":
                            new BanC(this.client).doProcess(args);
                            break;

                        case "unban":
                            new UnbanCommand(this.client).doProcess(args);
                            break;

                        case "reload":
                            new Reload(this.client).doProcess(args);
                            break;

                        case "kick":
                            new Kick(this.client).doProcess(args);
                            break;

                        case "fuel":
                            new Fuel(this.client).doProcess(args);
                            break;

                        case "starteritems":
                            new StarterItems(this.client).doProcess(args);
                            break;

                        case "admin":
                            new AdminChat(this.client).doProcess(args);
                            break;

                        case "rules":
                            new Rules(this.client).doProcess(args);
                            break;

                        case "version":
                            new VersionC(this.client).doProcess(args);
                            break;

                        case "me":
                            new Me(this.client).doProcess(args);
                            break;

                        case "pvp":
                        case "w":
                            return true;

                        case "nick":
                            if (this.client.playerData.hasPermission("cmd.nick")) return true;
                            else
                            {
                                this.client.sendChatMessage(ChatReceiveContext.Whisper, "", "You do not have permission to use this command.");
                            }
                            break;

                        case "who":
                        case "online":
                        case "players":
                            new List(this.client).doProcess(args);
                            break;

                        case "whosthere":
                            new WhosThere(this.client).doProcess(args);
                            break;

                        case "broadcast":
                            new Broadcast(this.client).doProcess(args);
                            break;

                        case "ship":
                            new Ship(this.client).doProcess(args);
                            break;

                        case "planet":
                            new Planet(this.client).doProcess(args);
                            break;

                        case "home":
                            new Home(this.client).doProcess(args);
                            break;

                        case "item":
                        case "give":
                            new Item(this.client).doProcess(args);
                            break;

                        case "mute":
                            new Mute(this.client).doProcess(args);
                            break;

                        case "uptime":
                            new Uptime(this.client).doProcess(args);
                            break;

                        case "shutdown":
                            new Shutdown(this.client).doProcess(args);
                            break;

                        case "restart":
                            new Restart(this.client).doProcess(args);
                            break;

                        case "build":
                            new Build(this.client).doProcess(args);
                            break;

                        case "where":
                        case "find":
                            new Find(this.client).doProcess(args);
                            break;

                        case "warpship":
                            new WarpShip(this.client).doProcess(args);
                            break;

                        case "spawn":
                            new Spawn(this.client).doProcess(args);
                            break;

                        case "group":
                            new GroupC(this.client).doProcess(args);
                            break;

                            /*
                        case "sethome":
                            new SetHome(this.client).doProcess(args);
                            break;
                             */

                        case "shipaccess":
                            new ShipAccess(this.client).doProcess(args);
                            break;

                        case "help":
                        case "commands":
                        case "commandlist":
                        case "?":
                            new Help(this.client).doProcess(args);
                            break;

                        case "auth":
                            if (String.IsNullOrWhiteSpace(StarryboundServer.authCode)) goto default;
                            else new Auth(this.client).doProcess(args);
                            break;

                        default:
                            this.client.sendCommandMessage("Command " + cmd + " not found.");
                            break;
                    }
                    return false;
                }
                catch (Exception e)
                {
                    this.client.sendCommandMessage("Command failed: " + e.Message);
                    Console.WriteLine(e.ToString());
                }
            }
            #endregion

            if (this.client.playerData.isMuted)
            {
                this.client.sendCommandMessage("^#f75d5d;You try to speak, but nothing comes out... You have been muted.");
                return false;
            }

            StarryboundServer.logInfo("[" + ((ChatSendContext)context).ToString() + "] [" + this.client.playerData.name + "]: " + message);
            return true;
        }

        // Test with http://gskinner.com/RegExr/
        // Current expression courtesy of http://stackoverflow.com/a/20303808
        /// <summary>
        /// The regular expression used when splitting arguments.
        /// Compiled as a static variable to speed up argument checking
        /// </summary>
        static System.Text.RegularExpressions.Regex argRegex = new System.Text.RegularExpressions.Regex(
            @"(?:^[ \t]*((?>[^ \t""\r\n]+|""[^""]+(?:""|$))+)|(?!^)[ \t]+((?>[^ \t""\\\r\n]+|(?<!\\)(?:\\\\)*""[^""\\\r\n]*(?:\\.[^""\\\r\n]*)*""{1,2}|(?:\\(?:\\\\)*"")+|\\+(?!""))+)|([^ \t\r\n]))", 
            System.Text.RegularExpressions.RegexOptions.Compiled);
        
        /// <summary>
        /// Parses the args into an array, taking quotes and brackets into account
        /// </summary>
        /// <param name="args">The string to parse</param>
        /// <returns></returns>
        private static string[] parseArgs(string args)
        {
            List<string> parsed;
            System.Text.RegularExpressions.MatchCollection matches;

            parsed = new List<string>();

            matches = argRegex.Matches(args);

            // Loop through matches (there should only ever be one if we're successful);
            foreach (System.Text.RegularExpressions.Match match in matches)
            {
                foreach (System.Text.RegularExpressions.Capture capture in match.Captures)
                {
                    string val;

                    // The value of the capture is our value
                    val = capture.Value;
                    // Trim out any trailing spaces
                    val = val.Trim();
                    // Trim out any characters used for sectioning off the argument
                    val = val.Trim('"', '(', ')', '\'');

                    parsed.Add(val);
                }
            }

            return parsed.ToArray();
        }

        public void prepare(ChatReceiveContext context, string message)
        {
            tmpArray.Add("context", context);
            tmpArray.Add("world", "");
            tmpArray.Add("entityID", 0);
            tmpArray.Add("name", "");
            tmpArray.Add("message", message);
        }

        public void prepare(ChatReceiveContext context, string name, string message)
        {
            tmpArray.Add("context", context);
            tmpArray.Add("world", "");
            tmpArray.Add("entityID", 0);
            tmpArray.Add("name", name);
            tmpArray.Add("message", message);
        }

        public void prepare(ChatReceiveContext context, string world, uint entityID, string name, string message)
        {
            tmpArray.Add("context", context);
            tmpArray.Add("world", world);
            tmpArray.Add("entityID", entityID);
            tmpArray.Add("name", name);
            tmpArray.Add("message", message);
        }

        public override void onSend()
        {
            if (tmpArray.Count < 5) return;

            MemoryStream packet = new MemoryStream();
            BinaryWriter packetWrite = new BinaryWriter(packet);

            ChatReceiveContext sContext = (ChatReceiveContext)tmpArray["context"];
            string sWorld = (string)tmpArray["world"];
            uint sClientID = (uint)tmpArray["entityID"]; // Player entity ID
            string sName = (string)tmpArray["name"]; // Name
            string sMessage = (string)tmpArray["message"]; // Message

            packetWrite.Write((byte)sContext);
            packetWrite.WriteStarString(sWorld);
            packetWrite.WriteBE(sClientID);
            packetWrite.WriteStarString(sName);
            packetWrite.WriteStarString(sMessage);
            this.client.sendClientPacket(Packet.ChatReceive, packet.ToArray());
        }

        public override int getPacketID()
        {
            return 11;
        }
    }
}
