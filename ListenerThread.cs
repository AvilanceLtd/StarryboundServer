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
        public void run()
        {
            IPAddress localAdd = IPAddress.Parse(StarryboundServer.config.proxyIP);
            TcpListener serversocket = new TcpListener(localAdd, StarryboundServer.config.proxyPort);

            serversocket.Start();
            StarryboundServer.logInfo("Proxy server has been started on " + localAdd.ToString() + ":" + StarryboundServer.config.proxyPort);
            StarryboundServer.serverState = ServerState.Running;

            try
            {
                while (true)
                {
                    TcpClient clientSocket = serversocket.AcceptTcpClient();
                    new Thread(new ThreadStart(new Client(clientSocket).run)).Start();
                }
            }
            catch (Exception e)
            {
                StarryboundServer.logException("ListenerThread: " + e.ToString());
            }

            serversocket.Stop();
            StarryboundServer.logException("ListenerThread has failed - No new connections will be possible.");
            Console.ReadLine();
        }
    }
}
