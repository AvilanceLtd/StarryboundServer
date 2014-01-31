/* 
 * Starrybound Server
 * Copyright 2013, Avilance Ltd
 * Created by Zidonuke (zidonuke@gmail.com) and Crashdoom (crashdoom@avilance.com)
 * 
 * This file is a part of Starrybound Server.
 * Starrybound Server is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 * Starrybound Server is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
 * You should have received a copy of the GNU General Public License along with Starrybound Server. If not, see http://www.gnu.org/licenses/.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace com.avilance.Starrybound.Util
{
    public enum Packet
    {
        ProtocolVersion = 0, //Done
        ConnectResponse = 1, //Done
        ServerDisconnect = 2, //Done
        HandshakeChallenge = 3, //Done
        ChatReceive = 4, //Done
        UniverseTimeUpdate = 5, //Not Needed
        ClientConnect = 6, //Done
        ClientDisconnect = 7, //Done
        HandshakeResponse = 8, //Done
        WarpCommand = 9, //Done
        ChatSend = 10, //Done
        ClientContextUpdate = 11, //In Progress - NetStateDelta
        WorldStart = 12, //Done
        WorldStop = 13, //Done
        TileArrayUpdate = 14,
        /*
         * VLQI X
         * VLQI T
         * VLQU SizeX
         * VLQU SizeY
         * {
         * Star::NetTile
         * }
         */
        TileUpdate = 15,
        /*
         * int X
         * int Y
         * Star::NetTile
         */
        TileLiquidUpdate = 16,
        /*
         * VLQI X
         * VLQI Y
         * uchar
         * uchar
         */
        TileDamageUpdate = 17,
        /*
         * int X
         * int Y
         * uchar
         * Star::TileDamageStatus
         * {
         * float
         * float
         * float
         * float
         * float
         * }
         */
        TileModificationFailure = 18,
        /*
         * VLQU Size
         * {
         * ???
         * }
         */
        GiveItem = 19, //Done
        SwapInContainerResult = 20,
        EnvironmentUpdate = 21,
        /*
         * ByteArray
         * ByteArray
         */
        EntityInteractResult = 22,
        /*
         * uint ClientId
         * int EntityId
         * Star::Variant
         */
        ModifyTileList = 23,
        /*
         * VLQU Size
         * {
         * ???
         * }
         * bool
         */
        DamageTile = 24,
        /*
         * int X
         * int Y
         * uchar
         * [float, float]
         * uchar
         * float
         */
        DamageTileGroup = 25,
        /*
         * VLQU
         * {
         * int X
         * int Y
         * ???
         * }
         * uchar
         * [float, float]
         * uchar
         * float
         */
        RequestDrop = 26,
        /*
         * VLQI SlotId?
         */
        SpawnEntity = 27,
        /*
         * uchar Type
         * ByteArray loadArray
         */
        EntityInteract = 28,
        /*
         * int EntityId
         * float, float
         * float, float
         */
        ConnectWire = 29,
        /*
         * VLQI
         * VLQI
         * VLQI
         * VLQI
         * VLQI
         * VLQI
         * VLQI
         * VLQI
         */
        DisconnectAllWires = 30,
        /*
         * VLQI
         * VLQI
         * VLQI
         * VLQI
         */
        OpenContainer = 31,
        CloseContainer = 32,
        SwapInContainer = 33,
        ItemApplyInContainer = 34,
        StartCraftingInContainer = 35,
        StopCraftingInContainer = 36,
        BurnContainer = 37,
        ClearContainer = 38,
        WorldClientStateUpdate = 39,  //In Progress - NetStateDelta
        EntityCreate = 40,
        /*
         * uchar Type
         * ByteArray loadArray
         * VLQI EntityId
         */
        EntityUpdate = 41,
        /*
         * VLQI EntityId
         * ByteArray loadDeltaArray
         */
        EntityDestroy = 42,
        /*
         * VLQI EntityId
         * bool
         */
        DamageNotification = 43,
        /*
         * Star::DamageNotification
         * [
         * ]
         */
        StatusEffectRequest = 44,
        /*
         * Star::StatusEffectRequest
         */
        UpdateWorldProperties = 45,
        /*
         * VLQU
         * {
         * string
         * Star::Variant
         * }
         */
        Heartbeat = 46, //Not Needed
    }

    public enum ProtectionTypes
    {
        Public = 0,
        Whitelist = 1,
        Private = 2,
    }

    public enum PlanetAccess
    {
        Banned = -1,
        ReadOnly = 0,
        Builder = 1,
        Moderator = 2,
        Owner = 3,
    }

    public enum Direction
    {
        Server = 0,
        Client = 1,
    }

    public enum LogType
    {
        FileOnly = 0,
        Debug = 1,
        Info = 2,
        Warn = 3,
        Error = 4,
        Exception = 5,
        Fatal = 6,
    }

    public enum ChatReceiveContext
    {
        Broadcast = 1, //Yellow, Universe
        Channel = 0, //Green, Planet
        Whisper = 2, //Light Pink
        CommandResult = 3, //Grey
        White = 4, //White, Not Offical, just defaults to white if not the above.
        //Anything else is White
    }

    public enum ChatSendContext
    {
        Universe = 0,
        Planet = 1,
    }

    public enum ClientState
    {
        PendingConnect = 0,
        PendingAuthentication = 1,
        PendingConnectResponse = 2,
        Connected = 3,
        Disposing = 4,
    }

    public enum ServerState
    {
        Starting = 0,
        ListenerReady = 1,
        StarboundReady = 2,
        Running = 3,
        Crashed = 4,
        Shutdown = 5,
        GracefulShutdown = 6,
    }

    public enum WarpType
    {
        MoveShip = 1,
        WarpToOwnShip = 2,
        WarpToPlayerShip = 3,
        WarpToOrbitedPlanet = 4,
        WarpToHomePlanet = 5,
    }

    public enum EntityType
    {
        EOF = -1, //Not actually valid
        Player = 0,
        Monster = 1,
        Object = 2,
        ItemDrop = 3,
        Projectile = 4,
        Plant = 5,
        PlantDrop = 6,
        Effect = 7,
    }
    /*
     * string ProjectileKey
     * Variant ProjectileParams
     * Star::StatusEffectSource
     * [
     * VLQU (size)
     * {
     * string
     * Variant
     * float
     * }
     * ]
     * float x2 (MovementController)
     * float x2 (MovementController)
     * VLQI (Source Entity)
     * bool (Source Entity)
     * float (???)
     * float x2 (???)
     * Star::EntityDamageTeam
     * [
     * uchar
     * uchar
     * ]
     */
    /*
                                         * UUID
                                         * Star::HumanoidIdentity
                                         * [
                                         * string
                                         * string
                                         * uchar
                                         * string x12
                                         * float
                                         * float
                                         * float
                                         * float
                                         * { ??? (4)
                                         * uchar
                                         * }
                                         * ]
                                         * Star::StatusEntityParameters
                                         * [
                                         * bool
                                         * float x16
                                         * string
                                         * string
                                         * ]
                                         * Star::Status
                                         * [
                                         * float x2 x5
                                         * bool
                                         * float x3
                                         * Star::StringList?? x2
                                         * [
                                         * VLQU
                                         * {
                                         * string
                                         * }
                                         * ]
                                         * ]
                                         * string
                                         * double
                                         * Star::PlayerInventory
                                         * [
                                         * StarByteArray
                                         * [
                                         * ulong
                                         * Star::ItemBag x3
                                         * [
                                         * VLQU
                                         * {
                                         * Star::ItemDatabase::readItem []
                                         * }
                                         * ]
                                         * VLQU (size)
                                         * {
                                         * Star::ItemDatabase::readItem []
                                         * }
                                         * VLQU (size)
                                         * {
                                         * Star::ItemDatabase::readItem []
                                         * }
                                         * VLQU (size)
                                         * {
                                         * Star::ItemDatabase::readItem []
                                         * }
                                         * Star::ItemDatabase::readItem
                                         * [
                                         * Star::ItemDescriptor
                                         * [
                                         * string
                                         * VLQS
                                         * Variant
                                         * ]
                                         * ]
                                         * uchar
                                         * VLQS
                                         * uchar
                                         * VLQS
                                         * ]
                                         * ]
                                         */

}
