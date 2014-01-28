using System;
using System.IO;
using System.Linq;
using com.avilance.Starrybound.Extensions;
using com.avilance.Starrybound.Util;
using com.avilance.Starrybound.Permissions;

namespace com.avilance.Starrybound.Commands
{
    class StarterItems : CommandBase
    {
        public StarterItems(Client client)
        {
            this.name = "starteritems";
            this.HelpText = ": Gives you a kit of items for beginners as specified by the server.";

            this.client = client;
            this.player = client.playerData;
        }

        public override bool doProcess(string[] args)
        {
            if (player.receivedStarterKit)
            {
                client.sendCommandMessage("You have already received your starting items.");
                return false;
            }

            if (StarryboundServer.config.starterItems == null || StarryboundServer.config.starterItems.Length <= 0)
            {
                client.sendCommandMessage("Sorry! This server does not provide any starting items.");
                return false;
            }

            int awardedItems = 0;

            foreach (string item in StarryboundServer.config.starterItems)
            {
                if (String.IsNullOrEmpty(item) || String.IsNullOrWhiteSpace(item))
                {
                    continue;
                }
                string name;
                uint amount;
                if (item.Contains('*'))
                {
                    name = item.Split('*')[0];
                    amount = uint.Parse(item.Split('*')[1]);
                    if (amount <= 0) { continue; }
                }
                else
                {
                    name = item;
                    amount = 1;
                }

                MemoryStream packet = new MemoryStream();
                BinaryWriter packetWrite = new BinaryWriter(packet);

                packetWrite.WriteStarString(name);
                packetWrite.WriteVarUInt32(amount+1);
                packetWrite.Write((byte)0);
                client.sendClientPacket(Packet.GiveItem, packet.ToArray());

                awardedItems++;
            }

            if (awardedItems == 0)
            {
                client.sendCommandMessage("Sorry! This server does not provide any starting items.");
                return false;
            }

            client.sendCommandMessage("You have been given a few starting items to help you on your journey. Good luck!");
            this.client.playerData.receivedStarterKit = true;
            Users.SaveUser(this.client.playerData);
            
            return true;
        }
    }
}
