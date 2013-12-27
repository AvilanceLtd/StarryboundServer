using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace com.avilance.Starrybound.Commands
{
    class WhosThereCommand : CommandBase
    {
        public WhosThereCommand(ClientThread client)
        {
            this.name = "whosthere";
            this.HelpText = ": shows a list of all players in this world.";
            
            this.Permission = new List<string>();
            this.Permission.Add("world.listplayers");

            this.player = client.playerData;
            this.client = client;
        }

        public override bool doProcess(string[] args)
        {
            string list = "";
            foreach (ClientThread otherClient in StarryboundServer.clients.Values)
            {
                Player otherPlayer = otherClient.playerData;
                if (this.player.isInSameWorldAs(otherPlayer) && this.player.name != otherPlayer.name)
                {
                    list += otherPlayer.name + ", ";
                }
            }
            if (list.Length != 0)
            {
                this.client.sendChatMessage("^#5dc4f4;Players in this world: " + list.Substring(0, list.Length -2));
            }
            else
            {
                this.client.sendChatMessage("^#5dc4f4;There are no other players in this world.");
            }
            return true;
        }
    }
}