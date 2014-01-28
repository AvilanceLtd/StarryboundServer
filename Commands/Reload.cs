using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace com.avilance.Starrybound.Commands
{
    class Reload : CommandBase
    {
        public Reload(Client client) {
            this.name = "reload";
            this.HelpText = "Allows reloading of the configuration files";
            this.Permission = new List<string>();
            this.Permission.Add("admin.reload");

            this.client = client;
            this.player = client.playerData;
        }

        public override bool doProcess(string[] args)
        {
            if (!hasPermission()) { permissionError(); return false; }

            if (args.Length < 1)
            {
                this.client.sendCommandMessage("Invalid syntax. Use /reload help for instructions.");
                return false;
            }

            string command = args[0].Trim().ToLower();

            switch (command)
            {
                case "help":
                    this.client.sendChatMessage("^#5dc4f4;Reload command help:");
                    this.client.sendChatMessage("^#5dc4f4;/reload all - reloads all configuration files.");
                    this.client.sendChatMessage("^#5dc4f4;/reload motd - reloads the MOTD file.");
                    this.client.sendChatMessage("^#5dc4f4;/reload rules - reloads the rules file.");
                    this.client.sendChatMessage("^#5dc4f4;/reload config - reloads the configuration file.");
                    break;

                case "all":
                    this.client.sendCommandMessage("Reloading all configuration files, this may take a moment...");
                    if (this.reloadAll()) this.client.sendCommandMessage("Reload successful.");
                    else this.client.sendCommandMessage("^#f75d5d;Reload has failed, a file is missing or the config.json is corrupt. Reload failed with errors.");
                    break;

                case "motd":
                    this.client.sendCommandMessage("Reloading message of the day, this may take a moment...");
                    if (Config.ReloadMOTD()) this.client.sendCommandMessage("Reload successful.");
                    else this.client.sendCommandMessage("^#f75d5d;Reload has failed, the motd.txt file is missing. Reload failed with errors.");
                    break;

                case "rules":
                    this.client.sendCommandMessage("Reloading rules, this may take a moment...");
                    if (Config.ReloadRules()) this.client.sendCommandMessage("Reload successful.");
                    else this.client.sendCommandMessage("^#f75d5d;Reload has failed, the rules.txt file is missing. Reload failed with errors.");
                    break;

                case "config":
                    this.client.sendCommandMessage("Reloading config, this may take a moment...");
                    if (Config.ReloadConfig()) this.client.sendCommandMessage("Reload successful.");
                    else this.client.sendCommandMessage("^#f75d5d;Reload has failed, the config.json file is missing or corrupt. Reload failed with errors.");
                    break;

                case "bans":
                    this.client.sendCommandMessage("Reloading all bans from file, this may take a moment...");
                    reloadBans();
                    break;

                default:
                    this.client.sendCommandMessage("Invalid syntax. Use /reload help for instructions.");
                    break;
            }

            return true;
        }

        private void reloadBans()
        {
            Bans.allBans = new Dictionary<int, Ban>();
            Bans.ProcessBans();
            this.client.sendChatMessage(Bans.allBans.Count + " ban(s) have been loaded.");
        }

        private bool reloadAll()
        {
            this.reloadBans();
            if (!Config.ReloadMOTD() || !Config.ReloadRules() || !Config.ReloadConfig()) return false;
            else return true;
        }
    }
}
