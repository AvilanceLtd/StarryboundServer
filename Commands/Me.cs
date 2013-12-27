using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace com.avilance.Starrybound.Commands
{
    class Me : CommandBase
    {
        public Me(Client client)
        {
            this.name = "me";
            this.HelpText = " <message>: Sends an emote message.";
            this.Permission = new List<string>();
            this.Permission.Add("chat.emote");

            this.client = client;
            this.player = client.playerData;
        }

        public override bool doProcess(string[] args)
        {
            //if (!hasPermission()) { permissionError(); return false; }

            string message = string.Join(" ", args).Trim();

            if (message == null || message.Length < 1) { showHelpText(); return false; }

            StarryboundServer.sendGlobalMessage(this.player.name + " " + message, "#f49413");
            return true;
        }
    }
}
