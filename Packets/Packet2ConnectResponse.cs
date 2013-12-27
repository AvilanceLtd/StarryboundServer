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

namespace com.avilance.Starrybound.Packets
{
    class Packet2ConnectResponse : PacketBase
    {
        string rejectReason;

        public Packet2ConnectResponse(Client clientThread, BinaryReader stream, Direction direction)
        {
            this.client = clientThread;
            this.stream = stream;
            this.direction = direction;
        }

        public Packet2ConnectResponse(Client clientThread, Direction direction, string rejectReason)
        {
            this.client = clientThread;
            this.direction = direction;
            this.rejectReason = rejectReason;
        }

        public override object onReceive()
        {
            BinaryReader packetData = (BinaryReader)this.stream;

            bool success = packetData.ReadBoolean();
            uint clientID = packetData.ReadVarUInt32();
            string rejectReason = packetData.ReadStarString();

            this.client.playerData.id = clientID;
            PlayerData player = this.client.playerData;

            if(!success)
            {
                this.client.rejectPreConnected("Rejected by parent server: " + rejectReason);
                return true;
            }

            StarryboundServer.clients.Add(player.name, this.client);
            StarryboundServer.sendGlobalMessage(player.name + " has joined the server!");
            this.client.state = ClientState.Connected;
            StarryboundServer.logInfo("[" + this.client.playerData.client + "][" + this.client.playerData.id + "] joined with UUID " + player.uuid);
            return true;
        }

        public override void onSend()
        {
            MemoryStream packet = new MemoryStream();
            BinaryWriter packetWrite = new BinaryWriter(packet);

            packetWrite.Write(false);
            packetWrite.WriteVarUInt32(0);
            packetWrite.WriteStarString(rejectReason);

            this.client.sendClientPacket(Packet.ConnectResponse, packet.ToArray());
        }

        public override int getPacketID()
        {
            return 2;
        }
    }
}
