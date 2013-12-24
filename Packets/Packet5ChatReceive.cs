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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using com.avilance.Starrybound.Extensions;
using com.avilance.Starrybound.Util;

namespace com.avilance.Starrybound.Packets
{
    class Packet5ChatReceive : PacketBase
    {
        public Packet5ChatReceive(ClientThread clientThread, Object stream, Direction direction)
        {
            this.mClient = clientThread;
            this.mStream = stream;
            this.mDirection = direction;
        }

        public override Object onReceive()
        {
            BinaryReader packetData = (BinaryReader)this.mStream;

            byte context = packetData.ReadByte();
            string world = packetData.ReadStarString();
            uint clientID = packetData.ReadUInt32BE();
            string name = packetData.ReadStarString();
            string message = packetData.ReadStarString();

            StarryboundServer.logDebug("[" + this.mClient.clientUUID + "][" + this.mDirection.ToString() + "] Chat: [" + context + ":" + world + "] [" + clientID + "] " + name + ": " + message);

            return null;
        }

        public override void onSend()
        {
            //Should this even happen either?
        }

        public override int getPacketID()
        {
            return 5;
        }
    }
}
