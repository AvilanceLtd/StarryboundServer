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
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using com.avilance.Starrybound.Util;

namespace com.avilance.Starrybound
{
    class ListenerThread
    {
        public TcpListener tcpSocket;

        public void run()
        {
            try
            {
                IPAddress localAdd = IPAddress.Parse(StarryboundServer.config.proxyIP);
                tcpSocket = new TcpListener(localAdd, StarryboundServer.config.proxyPort);
                tcpSocket.Start();

                StarryboundServer.logInfo("Proxy server has been started on " + localAdd.ToString() + ":" + StarryboundServer.config.proxyPort);
                StarryboundServer.serverState = ServerState.ListenerReady;

                try
                {
                    while (true)
                    {
                        TcpClient clientSocket = tcpSocket.AcceptTcpClient();
                        clientSocket.ReceiveTimeout = StarryboundServer.config.clientSocketTimeout;
                        clientSocket.SendTimeout = StarryboundServer.config.internalSocketTimeout;
                        new Thread(new ThreadStart(new Client(clientSocket).run)).Start();
                    }
                }
                catch (ThreadAbortException) { }
                catch (Exception e)
                {
                    if ((int)StarryboundServer.serverState > 3) return;
                    StarryboundServer.logException("ListenerThread Exception: " + e.ToString());
                }

                tcpSocket.Stop();
                StarryboundServer.logFatal("ListenerThread has failed - No new connections will be possible.");
                StarryboundServer.serverState = ServerState.Crashed;
            }
            catch (ThreadAbortException) { }
            catch(SocketException e)
            {
                StarryboundServer.logFatal("TcpListener has failed to start: " + e.Message);
                StarryboundServer.serverState = ServerState.Crashed;
            }
        }
    }
}
