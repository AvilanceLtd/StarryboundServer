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
    class Packet7ClientConnect : PacketBase
    {
        public Packet7ClientConnect(ClientThread clientThread, Object stream, Direction direction)
        {
            this.mClient = clientThread;
            this.mStream = stream;
            this.mDirection = direction;
        }

        public override Object onReceive()
        {
            BinaryReader packetData = (BinaryReader)this.mStream;

            byte[] assetDigest = packetData.ReadStarByteArray();
            List<object> claim = packetData.ReadStarVariant();
            byte[] UUID = packetData.ReadStarUUID();
            string name = packetData.ReadStarString();
            string species = packetData.ReadStarString();
            byte[] shipWorld = packetData.ReadStarByteArray();
            string account = packetData.ReadStarString();

            // Identify player to server
            this.mClient.playerData.UUID = Utils.ByteArrayToString(UUID).ToLower();
            this.mClient.playerData.name = name;
            this.mClient.playerData.account = account;

            StarryboundServer.logDebug("[" + this.mClient.clientUUID + "][" + this.mDirection.ToString() + "] ClientConnect:[" + name + ":" + account + ":" + species + ":" + Utils.ByteArrayToString(UUID).ToLower() + "]");

            return null;
        }

        public override void onSend()
        {
            //This should never happen! We don't NEED to send it!
        }

        public override int getPacketID()
        {
            return 5;
        }
    }
}
