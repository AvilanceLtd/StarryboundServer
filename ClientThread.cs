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
        public ClientState clientState = ClientState.PendingConnect;

        private TcpClient cSocket;
        private BinaryReader cIn;
        private BinaryWriter cOut;

        private TcpClient sSocket;
        private BinaryReader sIn;
        private BinaryWriter sOut;

        public int kickTargetTimestamp = 0;
        public bool connectionAlive { get { if (this.cSocket.Connected && this.sSocket.Connected && this.clientState != ClientState.Disposing) return true; else return false; } }

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

                this.playerData.ip = ipep.Address.ToString();

                StarryboundServer.logInfo("[" + playerData.client + "] Accepting new connection.");

                sSocket = new TcpClient();
                sSocket.Connect("127.0.0.1", 21024);

                this.sIn = new BinaryReader(this.sSocket.GetStream());
                this.sOut = new BinaryWriter(this.sSocket.GetStream());

                if (!sSocket.Connected)
                {
                    MemoryStream packet = new MemoryStream();
                    BinaryWriter packetWrite = new BinaryWriter(packet);
                    packetWrite.WriteBE(StarryboundServer.ProtocolVersion);
                    this.sendClientPacket(Packet.ProtocolVersion, packet.ToArray());

                    Packet2ConnectResponse packet2 = new Packet2ConnectResponse(this, false, Util.Direction.Client);
                    packet2.prepare("Starrybound Server was unable to connect to the parent server.");
                    packet2.onSend();

                    this.forceDisconnect("Unable to connect to parent server.");
                    return;
                }

                // Forwarding for data from SERVER (sIn) to CLIENT (cOut)
                new Thread(new ThreadStart(new ForwardThread(this, this.sIn, this.cOut, Direction.Server).run)).Start();

                // Forwarding for data from CLIENT (cIn) to SERVER (sOut)
                new Thread(new ThreadStart(new ForwardThread(this, this.cIn, this.sOut, Direction.Client).run)).Start();
            }
            catch (Exception e)
            {
                this.forceDisconnect("ClientThread Error: " + e.ToString());
            }
        }

        public void sendClientPacket(Packet packetID, byte[] packetData)
        {
            try
            {
                this.cOut.WriteVarUInt32((uint)packetID);
                this.cOut.WriteVarInt32((int)packetData.Length);
                this.cOut.Write(packetData);
                this.cOut.Flush();
            }
            catch (Exception e)
            {
                this.errorDisconnect(Direction.Client, "Failed to send packet: " + e.Message);
            }
        }

        public void sendServerPacket(Packet packetID, byte[] packetData)
        {
            try
            {
                this.sOut.WriteVarUInt32((uint)packetID);
                this.sOut.WriteVarInt32((int)packetData.Length);
                this.sOut.Write(packetData);
                this.sOut.Flush();
            }
            catch (Exception e)
            {
                this.errorDisconnect(Direction.Server, "Failed to send packet: " + e.Message);
            }
        }

        public void sendCommandMessage(string message)
        {
            sendChatMessage(ChatReceiveContext.CommandResult, "", message);
        }

        public void sendChatMessage(string message)
        {
            sendChatMessage("", message);
        }

        public void sendChatMessage(string name, string message)
        {
            sendChatMessage(ChatReceiveContext.Broadcast, "", message);
        }

        public void sendChatMessage(ChatReceiveContext context, string name, string message)
        {
            if (clientState != ClientState.Connected) return;
            Packet11ChatSend packet = new Packet11ChatSend(this, false, Util.Direction.Client);
            packet.prepare(context, "", 0, name, message);
            packet.onSend();
        }

        public void banClient(string reason)
        {
            sendServerPacket(Packet.ClientDisconnect, new byte[1]); //This causes the server to gracefully save and remove the player, and close its connection, even if the client ignores ServerDisconnect.
            sendChatMessage("^#f75d5d;You have been banned from the server for " + reason + ".");
            StarryboundServer.sendGlobalMessage("^#f75d5d;" + this.playerData.name + " has been banned from the server for " + reason + "!");
            kickTargetTimestamp = Utils.getTimestamp() + 7;
        }

        public void kickClient(string reason)
        {
            sendServerPacket(Packet.ClientDisconnect, new byte[1]); //This causes the server to gracefully save and remove the player, and close its connection, even if the client ignores ServerDisconnect.
            sendChatMessage("^#f75d5d;You have been kicked from the server by an administrator.");
            StarryboundServer.sendGlobalMessage("^#f75d5d;" + this.playerData.name + " has been kicked from the server!");
            kickTargetTimestamp = Utils.getTimestamp() + 7;
        }

        private void doDisconnect(bool log)
        {
            if (this.playerData.name != null)
            {
                if (StarryboundServer.clients.ContainsKey(this.playerData.name))
                {
                    StarryboundServer.clients.Remove(this.playerData.name);
                    StarryboundServer.sendGlobalMessage(this.playerData.name + " has left the server.");
                    if(!log)
                        StarryboundServer.logInfo("[" + playerData.client + "] has left the server.");
                }
            }
            if (this.clientState != ClientState.Disposing)
            {
                this.clientState = ClientState.Disposing;
                try
                {
                    this.cSocket.Close();
                    this.sSocket.Close();
                }
                catch (Exception) { }
            }
        }

        public void forceDisconnect(string reason)
        {
            if (this.clientState != ClientState.Disposing)
                StarryboundServer.logWarn("[" + playerData.client + "] Dropped for " + reason);
            doDisconnect(true);
        }

        public void errorDisconnect(Direction direction, string reason)
        {
            if (this.clientState != ClientState.Disposing)
                StarryboundServer.logError("[" + playerData.client + "] Dropped by parent " + direction.ToString() + " for " + reason);
            doDisconnect(true);
        }


        public void forceDisconnect()
        {
            doDisconnect(false);
        }
    }
}
