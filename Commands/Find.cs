using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace com.avilance.Starrybound.Commands
{
    class Find : CommandBase
    {
        public Find(ClientThread client)
        {
            this.name = "find";
            this.HelpText = "Woot";

            this.client = client;
            this.player = client.playerData;
        }

        public override bool doProcess(string[] args)
        {
            string player = string.Join(" ", args).Trim();

            if (player == null || player.Length < 1)
            {
                this.client.sendCommandMessage("You are located [" + this.player.sector + ":" + this.player.x + ":" + this.player.y + ":" + this.player.z + ":" + this.player.planet + ":" + this.player.satellite + "]");
                return true;
            }
            else
            {
                if (StarryboundServer.clients.ContainsKey(player))
                {
                    Player playerData = StarryboundServer.clients[player].playerData;
                    this.client.sendCommandMessage(player + " located [" + playerData.sector + ":" + playerData.x + ":" + playerData.y + ":" + playerData.z + ":" + playerData.planet + ":" + playerData.satellite + "]");
                    return true;
                }
                else
                {
                    this.client.sendCommandMessage("Player '" + player + "' not found.");
                    return false;
                }
            }
        }
    }
}
