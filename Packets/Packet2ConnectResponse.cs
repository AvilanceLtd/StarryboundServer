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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using com.avilance.Starrybound.Extensions;
using System.Text.RegularExpressions;

namespace com.avilance.Starrybound.Packets
{
    class Packet2ConnectResponse : PacketBase
    {
        Dictionary<string, object> tmpArray = new Dictionary<string, object>();

        public Packet2ConnectResponse(ClientThread clientThread, Object stream, Direction direction)
        {
            this.mClient = clientThread;
            this.mStream = stream;
            this.mDirection = direction;
        }

        public override object onReceive()
        {
            BinaryReader packetData = (BinaryReader)this.mStream;

            bool success = packetData.ReadBoolean();
            uint clientID = packetData.ReadVarUInt32();
            string rejectReason = packetData.ReadStarString();

            this.mClient.playerData.id = clientID;
            Player player = this.mClient.playerData;

            if(!success)
            {
                this.mClient.forceDisconnect("Rejected by parent server: " + rejectReason);
                return true;
            }

            string[] reasonExpiry = Bans.checkForBan(new string[] { player.name, player.uuid, player.ip.ToString() });

            if (reasonExpiry.Length == 2)
            {
                tmpArray.Add("success", false);
                tmpArray.Add("clientID", clientID);
                tmpArray.Add("rejectReason", "You are " + ((reasonExpiry[1] == "0") ? "permanently" : "temporarily") + " banned from this server.\nReason: " + reasonExpiry[0]);
                this.onSend();
                this.mClient.forceDisconnect(((reasonExpiry[1] == "0") ? "Permanently" : "Temporarily") + " banned. (Name: " + player.name + "; UUID: " + player.uuid + ") - Ban Expires: " + reasonExpiry[1]);
                return false;
            }

            if (StarryboundServer.clients.ContainsKey(player.name))
            {
                tmpArray.Add("success", false);
                tmpArray.Add("clientID", clientID);
                tmpArray.Add("rejectReason", "This username is already in use.");
                this.onSend();
                this.mClient.forceDisconnect("Attempted to use a username already in use! (Name: " + player.name + "; UUID: " + player.uuid + ")");
                return false;
            }

            if (StarryboundServer.config.maxClients <= StarryboundServer.clientCount)
            {
                tmpArray.Add("success", false);
                tmpArray.Add("clientID", clientID);
                tmpArray.Add("rejectReason", "The server is full. Please try again later.");
                this.onSend();
                this.mClient.forceDisconnect("Server is full.");
                return false;
            }

            if (!StarryboundServer.config.allowSpaces)
            {
                if (this.mClient.playerData.name.Contains(" ") || String.IsNullOrWhiteSpace(this.mClient.playerData.name))
                {
                    tmpArray.Add("success", false);
                    tmpArray.Add("clientID", clientID);
                    tmpArray.Add("rejectReason", "You may not have spaces in your name on this server.");
                    this.onSend();
                    this.mClient.forceDisconnect("Username contained a space.");
                    return false;
                }
            }

            if (!StarryboundServer.config.allowSymbols)
            {
                Regex r = new Regex("^[a-zA-Z0-9_]*$");
                if (!r.IsMatch(this.mClient.playerData.name))
                {
                    tmpArray.Add("success", false);
                    tmpArray.Add("clientID", clientID);
                    tmpArray.Add("rejectReason", "You may not have special characters in your name on this server.");
                    this.onSend();
                    this.mClient.forceDisconnect("Username contained a symbol.");
                    return false;
                }
            }

            StarryboundServer.clients.Add(player.name, this.mClient);
            StarryboundServer.sendGlobalMessage(player.name + " has joined the server!");
            this.mClient.clientState = ClientState.Connected;
            StarryboundServer.logInfo("[" + this.mClient.playerData.client + "][" + this.mClient.playerData.id + "] joined with UUID " + player.uuid);
            return true;
        }

        public void prepare(string rejectReason)
        {
            tmpArray.Add("rejectReason", rejectReason);
        }

        public override void onSend()
        {
            MemoryStream packet = new MemoryStream();
            BinaryWriter packetWrite = new BinaryWriter(packet);

            packetWrite.Write(false);
            packetWrite.WriteVarUInt32(0);
            packetWrite.WriteStarString((string)tmpArray["rejectReason"]);

            this.mClient.sendClientPacket(Packet.ConnectResponse, packet.ToArray());
        }

        public override int getPacketID()
        {
            return 2;
        }
    }
}
