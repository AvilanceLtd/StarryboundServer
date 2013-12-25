using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using com.avilance.Starrybound.Extensions;
using com.avilance.Starrybound.Util;

namespace com.avilance.Starrybound.Commands
{
    class Item : CommandBase
    {
        public Item(ClientThread client)
        {
            this.name = "item, /give";
            this.HelpText = "<item> <amount>; Allows you to give items to yourself.";

            this.client = client;
            this.player = client.playerData;
        }

        public override bool doProcess(string[] args)
        {
            if (args.Length < 2) { showHelpText(); return false; }

            string item = args[1];
            uint count = Convert.ToUInt32(args[2]);
            if (String.IsNullOrEmpty(item) || count < 1) { showHelpText(); return false; }

            MemoryStream packet = new MemoryStream();
            BinaryWriter packetWrite = new BinaryWriter(packet);

            packetWrite.WriteStarString(item);
            packetWrite.WriteVarUInt32(count);
            packetWrite.Write((byte)0); //0 length Star::Variant
            client.sendClientPacket(Packet.GiveItem, packet.ToArray());
            client.sendCommandMessage("Gave you " + count + " " + item);

            return true;
        }
    }
}
