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
        ProtocolVersion = 1,
        ConnectResponse = 2,
        ServerDisconnect = 3,
        HandshakeChallenge = 4,
        ChatReceive = 5,
        UniverseTimeUpdate = 6,
        ClientConnect = 7,
        ClientDisconnect = 8,
        HandshakeResponse = 9,
        WarpCommand = 10,
        ChatSend = 11,
        ClientContextUpdate = 12,
        WorldStart = 13,
        WorldStop = 14,
        TileArrayUpdate = 15,
        TileUpdate = 16,
        TileLiquidUpdate = 17,
        TileDamageUpdate = 18,
        TileModificationFailure = 19,
        GiveItem = 20,
        WeatherUpdate = 21,
        SwapInContainerResult = 22,
        SkyUpdate = 23,
        EntityInteractResult = 24,
        ModifyTileList = 25,
        DamageTile = 26,
        DamageTileGroup = 27,
        RequestDrop = 28,
        SpawnEntity = 29,
        EntityInteract = 30,
        ConnectWire = 31,
        DisconnectAllWires = 32,
        OpenContainer = 33,
        CloseContainer = 34,
        SwapInContainer = 35,
        ItemApplyInContainer = 36,
        StartCraftingInContainer = 37,
        StopCraftingInContainer = 38,
        BurnContainer = 39,
        ClearContainer = 40,
        WorldClientStateUpdate = 41,
        EntityCreate = 42,
        EntityUpdate = 43,
        EntityDestroy = 44,
        DamageNotification = 45,
        StatusEffectRequest = 46,
        UpdateWorldProperties = 47,
        Heartbeat = 48,
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
        StartingProxy = 1,
        Running = 2,
        Restarting = 3,
        Crashed = 4,
    }

    public enum WarpType
    {
        MoveShip = 1,
        WarpToOwnShip = 2,
        WarpToPlayerShip = 3,
        WarpToOrbitedPlanet = 4,
        WarpToHomePlanet = 5,
    }
}
