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
        public Packet5ChatReceive(Client clientThread, BinaryReader stream, Direction direction)
        {
            this.client = clientThread;
            this.stream = stream;
            this.direction = direction;
        }

        public override Object onReceive()
        {
            byte context = stream.ReadByte();
            string world = stream.ReadStarString();
            uint clientID = stream.ReadUInt32BE();
            string name = stream.ReadStarString();
            string message = stream.ReadStarString();

            Client target = StarryboundServer.getClient(clientID);
            if (target != null)
            {
                target.playerData.serverName = name;
                string formatName = target.playerData.formatName;
                if (!String.IsNullOrEmpty(formatName))
                {
                    client.sendChatMessage((ChatReceiveContext)context, world, clientID, formatName, message);
                    return false;
                }
            }

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
