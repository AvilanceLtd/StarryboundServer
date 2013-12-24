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

        public Packet11ChatSend(ClientThread clientThread, Object stream, Direction direction)
        {
            this.mClient = clientThread;
            this.mStream = stream;
            this.mDirection = direction;
        }

        /// <summary>
        /// Reads the data from the packet stream
        /// </summary>
        /// <returns>true to maintain packet and send to client, false to drop packet, -1 will boot the client</returns>
        public override Object onReceive()
        {
            BinaryReader packetData = (BinaryReader)this.mStream;

            string message = packetData.ReadStarString();
            byte context = packetData.ReadByte();

            if (message.StartsWith("/"))
            {
                string[] args = message.Remove(0, 1).Split(' ');

                string cmd = args[0].ToLower();

                args = message.Remove(0, cmd.Length + 1).Split(' ');

                switch (cmd) {
                    case "ban":
                        new BanCommand(this.mClient).doProcess(args);
                        break;

                    case "kick":
                        new Kick(this.mClient).doProcess(args);
                        break;

                    case "pvp":
                    case "w":
                        return true;

                    case "nick":
                        if (this.mClient.playerData.hasPermission("cmd.nick")) return true;
                        else
                        {
                            Packet11ChatSend packetE = new Packet11ChatSend(this.mClient, false, Util.Direction.Client);
                            packetE.prepare(Util.ChatReceiveContext.Whisper, "", 0, "server", "You do not have permission to use this command.");
                            packetE.onSend();
                        }
                        break;

                    case "who":
                    case "online":
                    case "players":
                        new Players(this.mClient).doProcess(args);
                        break;

                    case "broadcast":
                        new Broadcast(this.mClient).doProcess(args);
                        break;

                    case "ship":
                        new Ship(this.mClient).doProcess(args);
                        break;

                    case "planet":
                        new Planet(this.mClient).doProcess(args);
                        break;

                    case "home":
                        new Home(this.mClient).doProcess(args);
                        break;

                    case "mute":
                        new Mute(this.mClient).doProcess(args);
                        break;

                    case "uptime":
                        new Uptime(this.mClient).doProcess(args);
                        break;

                    case "shutdown":
                        new Shutdown(this.mClient).doProcess(args);
                        break;

                    case "build":
                        new Build(this.mClient).doProcess(args);
                        break;

                    default:
                        Packet11ChatSend packet = new Packet11ChatSend(this.mClient, false, Util.Direction.Client);
                        packet.prepare(Util.ChatReceiveContext.CommandResult, "", 0, "server", "Command " + cmd + " not found.");
                        packet.onSend();
                        break;
                }

                if (this.mClient.playerData.isMuted)
                {
                    Packet11ChatSend packet = new Packet11ChatSend(this.mClient, false, Util.Direction.Client);
                    packet.prepare(Util.ChatReceiveContext.CommandResult, "", 0, "server", "^#f75d5d;You try to speak, but nothing comes out... You have been muted.");
                    packet.onSend();

                    return false;
                }
                return false;
            }

            StarryboundServer.logInfo("[" + this.mClient.clientUUID + "][" + this.mDirection.ToString() + "] Chat: [" + context + "] Message: " + message);

            return true;
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
            this.mClient.sendClientPacket(Packet.ChatReceive, packet.ToArray());
        }

        public override int getPacketID()
        {
            return 11;
        }
    }
}
