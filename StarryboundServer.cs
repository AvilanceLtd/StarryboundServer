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


using com.avilance.Starrybound.Packets;
using com.avilance.Starrybound.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace com.avilance.Starrybound
{
    class StarryboundServer
    {
        public static Config config = new Config();
        public static readonly Version VersionNum = Assembly.GetExecutingAssembly().GetName().Version;
        public static readonly int ProtocolVersion = 628;
        
        // Dictionary<string, ClientThread>
        // string           Username        Unique username for client, MUST be lowercase
        // ClientThread     ClientThread    Invidivual thread for client, used to access client specific functions
        public static Dictionary<string, ClientThread> clients = new Dictionary<string, ClientThread>();
        public static int clientCount { get { return clients.Count; } set { return; } }

        public static int uniqueID = 1;

        public static ServerThread sbServer;

        static Thread listenerThread;
        static Thread monitorThread;
        static Thread sbServerThread;

        public static bool allowNewClients = true;

        public static ServerState serverState;

        public static int startTime;
        public static int restartTime = 0;

        private static void ProcessExit(object sender, EventArgs e)
        {
            try
            {
                Process[] proc = Process.GetProcessesByName("starbound_server");
                proc[0].Kill();
            }
            catch (Exception) { }

            if (listenerThread != null) listenerThread.Abort();

            try { sbServer.process.CloseMainWindow(); }
            catch (Exception) { }

            sbServerThread.Abort();
        }

        static void Main(string[] args)
        {
            try
            {
	            Process [] proc = Process.GetProcessesByName("starbound_server");
	            proc[0].Kill();
            }
            catch (Exception) { }

            startTime = getTimestamp();

            serverState = ServerState.Starting;

            AppDomain.CurrentDomain.ProcessExit += new EventHandler(ProcessExit);

            monitorThread = new Thread(new ThreadStart(StarryboundServer.crashMonitor));
            monitorThread.Start();

            writeLog("", LogType.FileOnly);
            writeLog("-- Log Start: " + DateTime.Now + " --", LogType.FileOnly);

            logInfo("##############################################");
            logInfo("####   Avilance Ltd. StarryBound Server   ####");
            logInfo("####   Copyright (c) Avilance Ltd. 2013   ####");
            logInfo("####       Licensed under the GPLv3       ####");
            logInfo("##############################################");
            logInfo("Version: " + VersionNum);

            if (config.logLevel == LogType.Debug)
            {
                logWarn("The logLevel in your config is currently set to DEBUG. This **WILL** flood your console and log file, if you do not want this please edit your config logLevel to INFO");
                logWarn("Launch will proceed in 5 seconds.");
                System.Threading.Thread.Sleep(5000);
            } else if (config.debug) {
                logWarn("The debug flag in your config is currently set to TRUE. This **WILL** flood your log file with development data, please only use this flag if you require assistance.");
                logWarn("Launch will proceed in 5 seconds.");
                System.Threading.Thread.Sleep(5000);
            }

            Bans.readBansFromFile();

            sbServer = new ServerThread();
            sbServerThread = new Thread(new ThreadStart(sbServer.run));
            sbServerThread.Start();

            logInfo("Starting Starbound Server - This may take a few moments...");
            while (serverState != ServerState.StartingProxy) { if (serverState == ServerState.Crashed) return; }

            logInfo("Starbound server is ready. Starting proxy wrapper.");

            ListenerThread listener = new ListenerThread();

            listenerThread = new Thread(new ThreadStart(listener.run));
            listenerThread.Start();
        }

        public static void crashMonitor()
        {
            while (true)
            {
                if (restartTime != 0)
                {
                    if (restartTime < getTimestamp()) doRestart();
                    break;
                }

                if (serverState == ServerState.Crashed)
                {
                    logFatal("The server has encountered a fatal error and cannot continue. Restarting in 10 seconds.");
                    System.Threading.Thread.Sleep(10000);
                    doRestart();
                    break;
                }
            }
        }

        public static void doRestart()
        {
            serverState = ServerState.Restarting;

            foreach (ClientThread client in clients.Values)
            {
                client.sendServerPacket(Packet.ClientDisconnect, new byte[1]);
                client.sendClientPacket(Packet.ServerDisconnect, new byte[1]);
                client.connectionClosed();
            }

            if (listenerThread != null) listenerThread.Abort();

            try { sbServer.process.CloseMainWindow(); }
            catch (Exception) { }

            System.Threading.Thread.Sleep(500);

            sbServerThread.Abort();

            System.Threading.Thread.Sleep(3000);

            Process.Start(Environment.CurrentDirectory + "\\StarryboundServer.exe");
            Environment.Exit(1);
        }

        public static void logDebug(string message) { writeLog(message, LogType.Debug); }

        public static void logInfo(string message) { writeLog(message, LogType.Info); }

        public static void logWarn(string message) { writeLog(message, LogType.Warn); }

        public static void logError(string message) { writeLog(message, LogType.Error); }

        public static void logException(string message) { writeLog(message, LogType.Exception); }

        public static void logFatal(string message) { writeLog(message, LogType.Fatal); }

        public static void writeLog(string message, LogType logType)
        {
            if ((int)config.logLevel > (int)logType && config.debug == false && logType != LogType.FileOnly) return;

            switch (logType)
            {
                case LogType.Debug:
                    message = "[DEBUG] " + message;
                    break;

                case LogType.Info:
                    message = "[INFO] " + message;
                    break;

                case LogType.Warn:
                    message = "[WARN] " + message;
                    break;

                case LogType.Error:
                    message = "[ERROR] " + message;
                    break;

                case LogType.Exception:
                    message = "[Exception] Please report this to the developers: " + message;
                    break;

                case LogType.Fatal:
                    message = "[FATAL ERROR!] " + message;
                    break;
            }

            try
            {
                if ((int)logType >= (int)config.logLevel) Console.WriteLine(message);

                using (StreamWriter w = File.AppendText("log.txt"))
                {
                    w.WriteLine(message);
                }                
            }
            catch(Exception e)
            {
                Console.WriteLine("EXCEPTION IN LOGGER: " + e.ToString());
            }
        }

        public static Int32 getTimestamp()
        {
            Int32 unixTimeStamp;
            DateTime currentTime = DateTime.Now;
            DateTime zuluTime = currentTime.ToUniversalTime();
            DateTime unixEpoch = new DateTime(1970, 1, 1);
            unixTimeStamp = (Int32)(zuluTime.Subtract(unixEpoch)).TotalSeconds;
            return unixTimeStamp;
        }

        public static void sendGlobalMessage (string message) 
        {
            foreach (ClientThread client in clients.Values)
            {
                if (client.clientState != ClientState.Connected) continue;

                Packet11ChatSend packet = new Packet11ChatSend(client, false, Util.Direction.Client);
                packet.prepare(Util.ChatReceiveContext.CommandResult, "", 0, "server", "^#5dc4f4;" + message);
                packet.onSend();
            }
        }
    }
}
