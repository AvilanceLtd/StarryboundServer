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
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using com.avilance.Starrybound.Util;
using com.avilance.Starrybound.Extensions;
using com.avilance.Starrybound.Packets;

namespace com.avilance.Starrybound
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable")]
    class ClientThread
    {
        public Player playerData = new Player();
        public string clientUUID { get { return playerData.UUID; } set { playerData.UUID = value; } }

        public bool kickException = false;

        public ClientState clientState = ClientState.PendingConnect;
        public string passwordSalt;

        TcpClient cSocket;
        BinaryReader cIn;
        BinaryWriter cOut;

        TcpClient sSocket;
        BinaryReader sIn;
        BinaryWriter sOut;

        public Int32 targetTimestamp = 0;

        public Boolean blockPackets = false;
        public Boolean connectionAlive { get { if (this.cSocket.Connected && this.sSocket.Connected) return true; else return false; } }

        public ClientThread(TcpClient aClient)
        {
            this.cSocket = aClient;
        }

        public void run()
        {
            try
            {
                this.cIn = new BinaryReader(this.cSocket.GetStream());
                this.cOut = new BinaryWriter(this.cSocket.GetStream());

                IPEndPoint ipep = (IPEndPoint)this.cSocket.Client.RemoteEndPoint;
                IPAddress ipa = ipep.Address;

                this.playerData.ip = ipep.Address;

                StarryboundServer.logInfo("Accepting new connection from " + ipa.ToString());

                this.clientUUID = StarryboundServer.uniqueID.ToString();
                StarryboundServer.uniqueID++;

                sSocket = new TcpClient();
                sSocket.Connect("127.0.0.1", 21024);

                this.sIn = new BinaryReader(this.sSocket.GetStream());
                this.sOut = new BinaryWriter(this.sSocket.GetStream());

                if (!sSocket.Connected)
                {
                    StarryboundServer.logWarn("[" + this.clientUUID + "]: Client unable to connect to parent server.");

                    MemoryStream packet = new MemoryStream();
                    BinaryWriter packetWrite = new BinaryWriter(packet);
                    packetWrite.WriteBE(StarryboundServer.ProtocolVersion);
                    this.sendClientPacket(Packet.ProtocolVersion, packet.ToArray());

                    Packet2ConnectResponse packet2 = new Packet2ConnectResponse(this, false, Util.Direction.Client);
                    packet2.prepare(false, 0, "Starrybound Server was unable to connect to the parent server.");
                    packet2.onSend();
                    return;
                }

                // Forwarding for data from SERVER (sIn) to CLIENT (cOut)
                new Thread(new ThreadStart(new ForwardThread(this, this.sIn, this.cOut, Direction.Server).run)).Start();

                // Forwarding for data from CLIENT (cIn) to SERVER (sOut)
                new Thread(new ThreadStart(new ForwardThread(this, this.cIn, this.sOut, Direction.Client).run)).Start();
            }
            catch (Exception e)
            {
                StarryboundServer.logException("Client (" + this.playerData.name + ") encountered an error: ");
                StarryboundServer.logException(e.ToString());
            }
        }

        public void sendClientPacket(Packet packetID, byte[] packetData)
        {
            this.cOut.WriteVarUInt32((uint)packetID);
            this.cOut.WriteVarInt32((int)packetData.Length);
            this.cOut.Write(packetData);
            this.cOut.Flush();
        }

        public void sendServerPacket(Packet packetID, byte[] packetData)
        {
            this.sOut.WriteVarUInt32((uint)packetID);
            this.sOut.WriteVarInt32((int)packetData.Length);
            this.sOut.Write(packetData);
            this.sOut.Flush();
        }

        public void connectionClosed()
        {
            if (StarryboundServer.clients.ContainsKey(this.playerData.name) && this.clientState != ClientState.Disposing)
            {
                StarryboundServer.clients.Remove(this.playerData.name);
                StarryboundServer.sendGlobalMessage(this.playerData.name + " has left the server.");
            }

            this.clientState = ClientState.Disposing;

            this.cSocket.Close();
            this.sSocket.Close();
        }

        public void banClient(string reason)
        {
            try
            {
                sendServerPacket(Packet.ClientDisconnect, new byte[1]); //This causes the server to gracefully save and remove the player, and close its connection, even if the client ignores ServerDisconnect.

                Packet11ChatSend packet = new Packet11ChatSend(this, false, Util.Direction.Client);
                packet.prepare(Util.ChatReceiveContext.Broadcast, "", 0, "server", "^#f75d5d;You have been banned from the server for " + reason + ".");
                packet.onSend();

                this.clientState = ClientState.Disposing;

                StarryboundServer.sendGlobalMessage("^#f75d5d;" + this.playerData.name + " has been banned from the server for " + reason + "!");
                StarryboundServer.clients.Remove(this.playerData.name);

                this.blockPackets = true;

                targetTimestamp = StarryboundServer.getTimestamp() + 7;

            }
            catch (Exception) { }
        }

        public void kickClient(string reason)
        {
            
            try
            {
                sendServerPacket(Packet.ClientDisconnect, new byte[1]); //This causes the server to gracefully save and remove the player, and close its connection, even if the client ignores ServerDisconnect.

                Packet11ChatSend packet = new Packet11ChatSend(this, false, Util.Direction.Client);
                packet.prepare(Util.ChatReceiveContext.Broadcast, "", 0, "server", "^#f75d5d;You have been kicked from the server by an administrator.");
                packet.onSend();

                this.clientState = ClientState.Disposing;

                StarryboundServer.sendGlobalMessage("^#f75d5d;" + this.playerData.name + " has been kicked from the server!");
                StarryboundServer.clients.Remove(this.playerData.name);

                this.blockPackets = true;

                targetTimestamp = StarryboundServer.getTimestamp() + 7;
            
            }
            catch (Exception) {}
        }

        public void connectionLost(Direction direction, string reason)
        {
            try
            {
                if (StarryboundServer.clients.ContainsKey(this.playerData.name) && this.clientState != ClientState.Disposing)
                {
                    StarryboundServer.clients.Remove(this.playerData.name);
                    StarryboundServer.sendGlobalMessage(this.playerData.name + " has left the server.");
                }

                this.clientState = ClientState.Disposing;

                sendServerPacket(Packet.ClientDisconnect, new byte[1]);
                connectionClosed();
            }
            catch (Exception) {}

            StarryboundServer.logError("- Connection from " + this.clientUUID + " dropped by " + direction.ToString() + " for " + reason);
        }
    }
}
