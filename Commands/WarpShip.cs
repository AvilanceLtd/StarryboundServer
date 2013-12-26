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
using System.Linq;
using System.IO;
using System.Text;
using com.avilance.Starrybound.Util;
using com.avilance.Starrybound.Extensions;

namespace com.avilance.Starrybound.Commands
{
    class WarpShip : CommandBase
    {
        public WarpShip(ClientThread client)
        {
            this.name = "warpship";
            this.HelpText = " <name>: Teleports you to another player's ship.";

            this.client = client;
            this.player = client.playerData;
        }

        public override bool doProcess(string[] args)
        {
            if (!hasPermission()) { permissionError(); return false; }

            string player = string.Join(" ", args).Trim();

            string sector;
            int x;
            int y;
            int z;
            int planet;
            int satellite;

            if (player == null || player.Length < 1)
            {
                showHelpText();
                return false;
            }
            else
            {
                if (StarryboundServer.clients.ContainsKey(player))
                {
                    Player playerData = StarryboundServer.clients[player].playerData;
                    sector = playerData.sector;
                    x = playerData.x;
                    y = playerData.y;
                    z = playerData.z;
                    planet = playerData.planet;
                    satellite = playerData.satellite;
                    this.client.sendCommandMessage("Warping ship to " + player + " [" + sector + ":" + x + ":" + y + ":" + z + ":" + planet + ":" + satellite + "]");
                }
                else
                {
                    this.client.sendCommandMessage("Player '" + player + "' not found.");
                    return false;
                }
            }

            MemoryStream packetWarp = new MemoryStream();
            BinaryWriter packetWrite = new BinaryWriter(packetWarp);

            packetWrite.WriteBE((uint)WarpType.MoveShip);
            packetWrite.WriteStarString(sector);
            packetWrite.WriteBE(x);
            packetWrite.WriteBE(y);
            packetWrite.WriteBE(z);
            packetWrite.WriteBE(planet);
            packetWrite.WriteBE(satellite);
            packetWrite.WriteStarString("");
            client.sendServerPacket(Packet.WarpCommand, packetWarp.ToArray());
            return true;
        }
    }
}
