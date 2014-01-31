using com.avilance.Starrybound.Extensions;
using com.avilance.Starrybound.Permissions;
using com.avilance.Starrybound.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace com.avilance.Starrybound.Commands
{
    class ClaimCommand : CommandBase
    {
        public ClaimCommand(Client client)
        {
            this.name = "claim";
            this.HelpText = "Claim a planet as your own world to protect it, restrict access and assign build rights";

            this.Permission = new List<string>();
            this.Permission.Add("world.claim");

            this.client = client;
            this.player = client.playerData;
        }

        public override bool doProcess(string[] args)
        {
            if (!hasPermission()) { permissionError(); return false; }

            WorldCoordinate loc = this.client.playerData.loc;

            if (client.playerData.loc == null)
            {
                client.sendChatMessage("^#f75d5d;Error: Your location could not be detected at this time.");
                return false;
            }

            if (client.playerData.inPlayerShip)
            {
                client.sendChatMessage("^#f75d5d;Error: You cannot claim a player ship. Please use /shipaccess for ship protection.");
                return false;
            }

            if (client.playerData.loc.Equals(StarryboundServer.spawnPlanet))
            {
                client.sendChatMessage("^#f75d5d;Error: You cannot claim the spawn planet.");
                return false;
            }

            if (client.playerData.claimedPlanet != null)
            {
                if (!client.playerData.claimedPlanet.Equals(new WorldCoordinate()))
                {
                    if (client.playerData.claimedPlanet != loc.ToString())
                    {
                        client.sendChatMessage("^#f75d5d;Error: You can only claim ONE planet at a time.");
                        return false;
                    }
                }
            }

            if (StarryboundServer.planets.protectedPlanets.ContainsKey(loc.ToString()))
            {
                if (this.client.playerData.uuid == StarryboundServer.planets.protectedPlanets[loc.ToString()].owner[0])
                {
                    this.client.sendCommandMessage("Your claim on this planet has been released. You may now claim a new planet.");
                    StarryboundServer.planets.protectedPlanets[loc.ToString()].deletePlanet();
                    this.client.playerData.claimedPlanet = null;
                    return false;
                }

                this.client.sendCommandMessage("Sorry. This planet has already been claimed by " + StarryboundServer.planets.protectedPlanets[loc.ToString()].owner[1]);
                return false;
            }

            new Permissions.Planet(this.client);
            return true;
        }
    }

    class ManageCommand : CommandBase
    {
        public ManageCommand(Client client)
        {
            this.name = "manage";
            this.HelpText = "Planet management for protected planets; See /manage help for details.";

            this.Permission = new List<string>();
            this.Permission.Add("world.claim");

            this.client = client;
            this.player = client.playerData;
        }

        public override bool doProcess(string[] args)
        {
            if (!hasPermission()) { permissionError(); return false; }

            if (args.Length < 1)
            {
                this.client.sendCommandMessage("Invalid syntax. Use /manage help for instructions.");
                return false;
            }

            string command = args[0].Trim().ToLower();

            Permissions.Planet ownedPlanet = null;
            Client target;
            string playerName;

            PlanetAccess accessLvl = PlanetAccess.Owner;

            if (command != "help" && this.client.playerData.claimedPlanet == null)
            {
                if (command == "kick" && this.client.playerData.group.hasPermission("admin.kick"))
                {
                    playerName = args[1].Trim();

                    target = StarryboundServer.getClient(playerName);
                    if (target != null)
                    {
                        target.sendChatMessage("^#f75d5d;Error: You have been kicked from this planet by the server staff.");

                        target.playerData.inPlayerShip = true;

                        MemoryStream packetWarp = new MemoryStream();
                        BinaryWriter packetWrite = new BinaryWriter(packetWarp);
                        packetWrite.WriteBE((uint)WarpType.WarpToOwnShip);
                        packetWrite.Write(new WorldCoordinate());
                        packetWrite.WriteStarString("");
                        target.sendServerPacket(Packet.WarpCommand, packetWarp.ToArray());

                        this.client.sendCommandMessage("Player " + playerName + " has been banned from the planet.");
                        return true;
                    }
                    else
                    {
                        this.client.sendCommandMessage("Player '" + playerName + "' not found.");
                        return false;
                    }
                }
                else
                {
                    try
                    {
                        Permissions.Planet modPlanet = StarryboundServer.planets.protectedPlanets[this.client.playerData.loc.ToString()];

                        if (modPlanet.canAccess(this.client.playerData.uuid) == PlanetAccess.Moderator)
                        {
                            ownedPlanet = StarryboundServer.planets.protectedPlanets[this.client.playerData.loc.ToString()];
                            accessLvl = PlanetAccess.Moderator;
                        }
                        else
                        {
                            this.client.sendCommandMessage("You do not have a claimed planet and are not a moderator of this planet.");
                            return false;
                        }
                    }
                    catch (Exception)
                    {
                        this.client.sendCommandMessage("You do not have a claimed planet.");
                        return false;
                    }
                }
            }
            else if (command != "help")
            {
                try
                {
                    ownedPlanet = StarryboundServer.planets.protectedPlanets[this.client.playerData.claimedPlanet];
                }
                catch (Exception e)
                {
                    this.client.sendCommandMessage("An internal error occurred while trying to find your claimed planet. Please contact an administrator.");
                    StarryboundServer.logException("Exception occured while retreiving planet protection information: " + e.ToString());
                    StarryboundServer.logInfo("There are " + StarryboundServer.planets.protectedPlanets.Count + " planet(s) loaded.");
                    return false;
                }
            }

            switch (command)
            {
                case "private":
                    if (accessLvl != PlanetAccess.Owner)
                    {
                        this.accessDenied();
                        return false;
                    }

                    ownedPlanet.accessType = (int)Util.ProtectionTypes.Private;
                    foreach (Client otherClient in StarryboundServer.getClients())
                    {
                        PlayerData otherPlayer = otherClient.playerData;
                        if (otherPlayer.loc == null) continue;
                        else if (ownedPlanet.loc.Equals(otherPlayer.loc))
                        {
                            if (ownedPlanet.canAccess(otherPlayer.uuid) == Util.PlanetAccess.Banned)
                            {
                                otherClient.sendChatMessage("^#f75d5d;Error: This planet has been changed to private and you do not have permission to access this planet.");
                                MemoryStream packetWarp = new MemoryStream();
                                BinaryWriter packetWrite = new BinaryWriter(packetWarp);
                                packetWrite.WriteBE((uint)WarpType.WarpToOwnShip);
                                packetWrite.Write(new WorldCoordinate());
                                packetWrite.WriteStarString("");
                                otherClient.sendServerPacket(Packet.WarpCommand, packetWarp.ToArray());
                            }
                        }
                    }

                    this.client.sendCommandMessage("Your planet protection has been set to PRIVATE.");

                    return true;

                case "whitelist":
                    if (accessLvl != PlanetAccess.Owner)
                    {
                        this.accessDenied();
                        return false;
                    }

                    ownedPlanet.accessType = (int)Util.ProtectionTypes.Whitelist;

                    this.client.sendCommandMessage("Your planet protection has been set to WHITELIST - Only people on the user list are able to build.");
                    return true;

                case "public":
                    if (accessLvl != PlanetAccess.Owner)
                    {
                        this.accessDenied();
                        return false;
                    }

                    ownedPlanet.accessType = (int)Util.ProtectionTypes.Public;
                    return true;

                case "set":
                    if (args.Length <= 2)
                    {
                        this.commandError();
                        return false;
                    }

                    if (accessLvl != PlanetAccess.Owner)
                    {
                        this.accessDenied();
                        return false;
                    }

                    playerName = args[1].Trim();
                    string accessLevel = args[2].Trim();

                    PlanetAccess access = PlanetAccess.Banned;

                    switch (accessLevel)
                    {
                        case "readonly":
                            access = PlanetAccess.ReadOnly;
                            break;

                        case "builder":
                            access = PlanetAccess.Builder;
                            break;

                        case "moderator":
                            access = PlanetAccess.Moderator;
                            break;
                    }

                    if (access == PlanetAccess.Banned) return false;

                    target = StarryboundServer.getClient(playerName);
                    if (target != null)
                    {
                        ownedPlanet.setAccess(target, access);
                        this.client.sendCommandMessage("Player " + playerName + " has been added to level " + access + ".");
                        return true;
                    } 
                    else 
                    {
                        this.client.sendCommandMessage("Player '" + playerName + "' not found.");
                        return false;
                    }

                case "remove":
                    if (args.Length <= 1)
                    {
                        this.commandError();
                        return false;
                    }

                    if (accessLvl != PlanetAccess.Owner)
                    {
                        this.accessDenied();
                        return false;
                    }

                    playerName = args[1].Trim();

                    target = StarryboundServer.getClient(playerName);
                    if (target != null)
                    {
                        ownedPlanet.removeAccess(target);
                        this.client.sendCommandMessage("Player " + playerName + " has been removed from planet access list.");
                        return true;
                    }
                    else
                    {
                        this.client.sendCommandMessage("Player '" + playerName + "' not found.");
                        return false;
                    }

                case "ban":
                    if (args.Length <= 1)
                    {
                        this.commandError();
                        return false;
                    }

                    playerName = args[1].Trim();

                    target = StarryboundServer.getClient(playerName);
                    if (target != null)
                    {
                        ownedPlanet.addBan(target);
                        this.client.sendCommandMessage("Player " + playerName + " has been banned from the planet.");
                        return true;
                    }
                    else
                    {
                        this.client.sendCommandMessage("Player '" + playerName + "' not found.");
                        return false;
                    }

                case "kick":
                    if (args.Length <= 1)
                    {
                        this.commandError();
                        return false;
                    }

                    playerName = args[1].Trim();

                    target = StarryboundServer.getClient(playerName);
                    if (target != null)
                    {
                        target.sendChatMessage("^#f75d5d;Error: You have been kicked from this planet by the owner.");
                        ownedPlanet.kickPlayer(target);
                        this.client.sendCommandMessage("Player " + playerName + " has been banned from the planet.");
                        return true;
                    }
                    else
                    {
                        this.client.sendCommandMessage("Player '" + playerName + "' not found.");
                        return false;
                    }

                case "unban":
                    if (args.Length <= 1)
                    {
                        this.commandError();
                        return false;
                    }

                    if (accessLvl != PlanetAccess.Owner)
                    {
                        this.accessDenied();
                        return false;
                    }

                    playerName = args[1].Trim();

                    target = StarryboundServer.getClient(playerName);
                    if (target != null)
                    {
                        if (ownedPlanet.removeBan(target))
                        {
                            this.client.sendCommandMessage("Player " + playerName + " has unbanned from the planet.");
                            return true;
                        }
                        else
                        {
                            this.client.sendCommandMessage("Player '" + playerName + "' is not banned from the planet.");
                            return true;
                        }
                    }
                    else
                    {
                        this.client.sendCommandMessage("Player '" + playerName + "' not found.");
                        return false;
                    }

                case "help":
                    this.client.sendChatMessage("^#5dc4f4;Planet management command help:");
                    this.client.sendChatMessage("^#5dc4f4;/manage private* - sets the planet access to private (listed users only)");
                    this.client.sendChatMessage("^#5dc4f4;/manage whitelist* - sets the planet access to whitelist");
                    this.client.sendChatMessage("^#5dc4f4;/manage public* - sets the planet access to public (free-build)");
                    this.client.sendChatMessage("^#5dc4f4;/manage set <player> <readonly/builder/moderator>* - gives a user a set planet rank.");
                    this.client.sendChatMessage("^#5dc4f4;/manage remove <player>* - removes a player from the planet access list");
                    this.client.sendChatMessage("^#5dc4f4;/manage ban <player> - bans a player from the planet");
                    this.client.sendChatMessage("^#5dc4f4;/manage kick <player> - kicks a player from the planet");
                    this.client.sendChatMessage("^#5dc4f4;/manage unban <player>* - unbans a player from the planet");
                    this.client.sendChatMessage("^#5dc4f4;* Can only be used by the planet owner");
                    return true;

                default:
                    this.commandError();
                    return true;
            }
        }

        public void commandError()
        {
            this.client.sendCommandMessage("Invalid syntax. Use /manage help for instructions.");
        }

        public void accessDenied()
        {
            this.client.sendCommandMessage("^#f75d5d;Error: Access Denied - You do not have permission to use this command on this planet.");
        }
    }
}
