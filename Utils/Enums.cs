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
	CelestialResponse = 6,
        ClientConnect = 7, //Done
        ClientDisconnect = 8, //Done
        HandshakeResponse = 9, //Done
        WarpCommand = 10, //Done
        ChatSend = 11, //Done
	CelestialRequest = 12,
        ClientContextUpdate = 13, //In Progress - NetStateDelta
        WorldStart = 14, //Done
        WorldStop = 15, //Done
        TileArrayUpdate = 16,
        /*
         * VLQI X
         * VLQI T
         * VLQU SizeX
         * VLQU SizeY
         * {
         * Star::NetTile
         * }
         */
        TileUpdate = 17,
        /*
         * int X
         * int Y
         * Star::NetTile
         */
        TileLiquidUpdate = 18,
        /*
         * VLQI X
         * VLQI Y
         * uchar
         * uchar
         */
        TileDamageUpdate = 19,
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
        TileModificationFailure = 20,
        /*
         * VLQU Size
         * {
         * ???
         * }
         */
        GiveItem = 21, //Done
        SwapInContainerResult = 22,
        EnvironmentUpdate = 23,
        /*
         * ByteArray
         * ByteArray
         */
        EntityInteractResult = 24,
        /*
         * uint ClientId
         * int EntityId
         * Star::Variant
         */
        ModifyTileList = 25,
        /*
         * VLQU Size
         * {
         * ???
         * }
         * bool
         */
        DamageTile = 26,
        /*
         * int X
         * int Y
         * uchar
         * [float, float]
         * uchar
         * float
         */
        DamageTileGroup = 27,
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
        RequestDrop = 28,
        /*
         * VLQI SlotId?
         */
        SpawnEntity = 29,
        /*
         * uchar Type
         * ByteArray loadArray
         */
        EntityInteract = 30,
        /*
         * int EntityId
         * float, float
         * float, float
         */
        ConnectWire = 31,
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
        DisconnectAllWires = 32,
        /*
         * VLQI
         * VLQI
         * VLQI
         * VLQI
         */
        OpenContainer = 33,
        CloseContainer = 34,
        SwapInContainer = 35,
        ItemApplyInContainer = 36,
        StartCraftingInContainer = 37,
        StopCraftingInContainer = 38,
        BurnContainer = 39,
        ClearContainer = 40,
        WorldClientStateUpdate = 41,  //In Progress - NetStateDelta
        EntityCreate = 42,
        /*
         * uchar Type
         * ByteArray loadArray
         * VLQI EntityId
         */
        EntityUpdate = 43,
        /*
         * VLQI EntityId
         * ByteArray loadDeltaArray
         */
        EntityDestroy = 44,
        /*
         * VLQI EntityId
         * bool
         */
        DamageNotification = 45,
        /*
         * Star::DamageNotification
         * [
         * ]
         */
        StatusEffectRequest = 46,
        /*
         * Star::StatusEffectRequest
         */
        UpdateWorldProperties = 47,
        /*
         * VLQU
         * {
         * string
         * Star::Variant
         * }
         */
        Heartbeat = 48, //Not Needed
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
