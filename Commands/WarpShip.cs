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
            this.HelpText = "Woot";

            this.client = client;
            this.player = client.playerData;
        }

        public override bool doProcess(string[] args)
        {
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
