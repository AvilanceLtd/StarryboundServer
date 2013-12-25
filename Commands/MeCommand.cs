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
            this.HelpText = "<message>; Sends a emote message.";

            this.client = client;
            this.player = client.playerData;
        }

        public override bool doProcess(string[] args)
        {
            string message = string.Join(" ", args).Trim();

            if (message == null || message.Length < 1) { showHelpText(); return false; }

            StarryboundServer.sendGlobalMessage(this.player.name + " " + message);
            return true;
        }
    }
}
