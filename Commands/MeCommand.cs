using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace com.avilance.Starrybound.Commands
{
    class MeCommand : CommandBase
    {
        public MeCommand(ClientThread client)
        {
            this.name = "me";
<<<<<<< HEAD
            this.HelpText = " <message>: Sends an emote message.";
=======
            this.HelpText = "<message>; Sends a emote message.";
            this.Permission = new List<string>();
            this.Permission.Add("chat.emote");
>>>>>>> 05237e4719208a518f0474f7007c03d021d61d51

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
