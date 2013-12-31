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
using com.avilance.Starrybound.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using com.avilance.Starrybound.Permissions;
using MaxMind;
using System.Net;

namespace com.avilance.Starrybound
{
    class StarryboundServer
    {
        public static string SavePath;
        public static BootstrapFile bootstrapConfig = new BootstrapFile();
        public static ConfigFile config = new ConfigFile();
        public static ServerFile serverConfig = new ServerFile();
        public static readonly Version VersionNum = Assembly.GetExecutingAssembly().GetName().Version;
        public static readonly int ProtocolVersion = 628;
        public static readonly string unmoddedClientDigest = "515E04DF26D1E9777FCFB32D7789DDAF6EF733EFC3BEB798454DAB3BE6668719";
        public static StarboundVersion starboundVersion = new StarboundVersion();
        public static GeoIPCountry Geo;
        public static int parentProcessId;

        public static string authCode;
        
        // Dictionary<string, ClientThread>
        // string           Username        Unique username for client, MUST be exactcase
        // ClientThread     ClientThread    Invidivual thread for client, used to access client specific functions
        private static Dictionary<string, Client> clients = new Dictionary<string, Client>();
        private static Dictionary<uint, Client> clientsById = new Dictionary<uint, Client>();
        public static int clientCount { get { return clients.Count; } set { return; } }

        public static Dictionary<string, Group> groups = new Dictionary<string, Group>();

        public static ServerThread sbServer;
        public static ListenerThread tcpListener;
        static Thread sbServerThread;
        static Thread listenerThread;
        static Thread monitorThread;

        public static string privatePassword;

        public static int failedConnections;
        public static ServerState serverState;
        public static int startTime;
        public static int restartTime = 0;
        private static bool shuttingDown = false;

        public static string defaultGroup = null;

        public static string motdData = "";
        public static string rulesData = "";

        public static List<byte[]> sectors = new List<byte[]>();
        public static WorldCoordinate spawnPlanet;

        public static bool IsMono
        {
            get { return (Type.GetType("Mono.Runtime") != null); }
        }

        static void ProcessExit(object sender, EventArgs e)
        {
            logDebug("ProcessExit", "Hello");
            doShutdown(true);
            logDebug("ProcessExit", "Goodbye");
        }

        static void UnhandledException(object sender, UnhandledExceptionEventArgs args)
        {
            logDebug("UnhandledException", "Hello");
            Exception e = (Exception)args.ExceptionObject;
            try
            {
                logFatal("Unhandled Exception Occurred: " + e.ToString());
                doShutdown(true);
                Environment.Exit(1);
            }
            catch (Exception ex) 
            {
                Console.WriteLine("[FATAL ERROR] Unhandled Exception Occurred: " + e.ToString());
                Console.WriteLine("[EXCEPTION] Unhandled Exception Handler Failed: " + ex.ToString());
            }
            logDebug("UnhandledException", "Goodbye");
        }

        internal static bool ConsoleCtrlCheck()
        {
            logDebug("ConsoleCtrlCheck", "Hello");
            doShutdown(true);
            logDebug("ConsoleCtrlCheck", "Goodbye");
            Environment.Exit(0);
            return true;
        }

        static void Main(string[] args)
        {
#if DEBUG
            config.logLevel = LogType.Debug;
#endif
            startTime = Utils.getTimestamp();
            serverState = ServerState.Starting;
            Console.Title = "Loading... Starrybound Server (" + VersionNum + ") (" + ProtocolVersion + ")";

            try
            {
                int processId = Convert.ToInt32(File.ReadAllText("starbound_server.pid"));
                Process proc = Process.GetProcessById(processId);
                proc.Kill();
                File.Delete("starbound_server.pid");
            }
            catch (Exception) { }

            monitorThread = new Thread(new ThreadStart(crashMonitor));
            monitorThread.Start();

            if (IsMono)
                Environment.CurrentDirectory = Path.GetDirectoryName(typeof(StarryboundServer).Assembly.Location);

            AppDomain.CurrentDomain.ProcessExit += new EventHandler(ProcessExit);
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(UnhandledException);

            if (!IsMono)
                NativeMethods.SetConsoleCtrlHandler(new NativeMethods.HandlerRoutine(ConsoleCtrlCheck), true);

            BootstrapConfig.SetupConfig();

            writeLog("", LogType.FileOnly);
            writeLog("-- Log Start: " + DateTime.Now + " --", LogType.FileOnly);

            logInfo("##############################################");
            logInfo("####   Avilance Ltd. Starrybound Server   ####");
            logInfo("####   Copyright (c) Avilance Ltd. 2013   ####");
            logInfo("####       Licensed under the GPLv3       ####");
            logInfo("##############################################");
            logInfo("Version: " + VersionNum + " (" + ProtocolVersion + ")");
            logInfo("Loading Starrybound Server...");

            Config.SetupConfig();
            ServerConfig.SetupConfig();
            Groups.SetupGroups();
            Users.SetupUsers();
#if !DEBUG
            if (config.logLevel == LogType.Debug)
            {
                logWarn("The logLevel in your config is currently set to DEBUG. This **WILL** flood your console and log file, if you do not want this please edit your config logLevel to INFO");
                logWarn("Launch will proceed in 5 seconds.");
                System.Threading.Thread.Sleep(5000);
            }
#endif
#if !NOSERVER
            if(config.proxyPort == config.serverPort)
            {
                logFatal("You cannot have the serverPort and proxyPort on the same port!");
                Thread.Sleep(5000);
                Environment.Exit(3);
            }
#endif
            var geoippath = Path.Combine(SavePath, "GeoIP.dat");
            if (config.enableGeoIP && File.Exists(geoippath))
                Geo = new GeoIPCountry(geoippath);

            foreach(string sector in config.sectors)
            {
                byte[] sectorBytes = Encoding.UTF8.GetBytes(sector);
                byte[] buffer = new byte[sectorBytes.Length + 1];
                buffer[0] = (byte)sectorBytes.Length;
                Buffer.BlockCopy(sectorBytes, 0, buffer, 1, sectorBytes.Length);
                sectors.Add(sectorBytes);
            }
            Bans.ProcessBans();

            logInfo("Starrybound Server initialization complete.");

            tcpListener = new ListenerThread();
            listenerThread = new Thread(new ThreadStart(tcpListener.run));
            listenerThread.Start();
            while (serverState != ServerState.ListenerReady) { }
            if ((int)serverState > 3) return;

            Console.Title = "Starting... Starrybound Server (" + VersionNum + ") (" + ProtocolVersion + ")";
#if !NOSERVER
            logInfo("Starting parent Starbound server - This may take a few moments...");
            sbServer = new ServerThread();
            sbServerThread = new Thread(new ThreadStart(sbServer.run));
            sbServerThread.Start();
            while (serverState != ServerState.StarboundReady) { }
            if ((int)serverState > 3) return;
#endif
            logInfo("Parent Starbound server is ready. Starrybound Server now accepting connections.");
            serverState = ServerState.Running;

            //Keep this last!
            if (config.enableCallback)
                runCallback();
        }

        private static void crashMonitor()
        {
            int lastCount = -1;
            while (true)
            {
                if (lastCount != clientCount && serverState == ServerState.Running)
                    Console.Title = serverConfig.serverName + " (" + clientCount + "/" + config.maxClients + ") - Starrybound Server (" + VersionNum + ") (" + ProtocolVersion + ")"; 

                if (restartTime != 0)
                {
                    int remaining = restartTime - Utils.getTimestamp();
                    if (remaining < 0) 
                        doRestart();
                    else if ((remaining % 5 == 0 || remaining < 5) && remaining != 0)
                    {
                        logWarn("The server will restart in " + remaining + " seconds.");
                        sendGlobalMessage("^#f75d5d;The server will restart in " + remaining + " seconds.");
                    }
                }

                if (serverState == ServerState.Crashed)
                {
                    logFatal("The server has encountered a fatal error and cannot continue. Restarting in 5 seconds.");
                    Thread.Sleep(5000);
                    doRestart();
                    return;
                }
                else if(serverState == ServerState.Shutdown)
                {
                    logFatal("OS, or Unhandled Exception requested immediate shutdown.");
                    doShutdown(true);
                    Environment.Exit(0);
                }
                else if(serverState == ServerState.GracefulShutdown)
                {
                    logWarn("User or Console requested graceful shutdown.");
                    doShutdown(false);
                    Environment.Exit(0);
                }

                if (restartTime != 0)
                    Thread.Sleep(1000);
                else
                    Thread.Sleep(2000);
            }
        }

        private static void doRestart()
        {
            doShutdown(false);
            if (IsMono)
            {
                logWarn("Auto Restarter doesn't support Mono. Exiting.");
                Environment.Exit(0);
            }
            logInfo("Now restarting...");
            Thread.Sleep(2500);
            if (serverState == ServerState.Shutdown || serverState == ServerState.GracefulShutdown)
            {
                logWarn("Something requested shutdown while restarting. Exiting.");
                Environment.Exit(1);
            }
            Process.Start(Assembly.GetEntryAssembly().Location);
            Environment.Exit(0);
        }

        private static void doShutdown(bool quick)
        {
            if (shuttingDown) return;
            shuttingDown = true;
            Console.Title = "Shutting Down... Starrybound Server (" + VersionNum + ") (" + ProtocolVersion + ")";
            logInfo("Starting graceful shutdown...");
            try
            {
                try { Geo.Dispose(); }
                catch (Exception) { }

                try
                {
                    if (!quick)
                    {
                        logDebug("Shutdown", "Disconnect all clients.");
                        foreach (Client client in clients.Values.ToList())
                        {
                            client.delayDisconnect("You have been disconnected. The server is being shut down.");
                        }

                        logInfo("Waiting 10 seconds for all clients to leave.");
                        int startWait = Utils.getTimestamp();
                        while (clientCount > 0)
                        {
                            if (Utils.getTimestamp() > startWait + 10)
                            {
                                logDebug("Shutdown", "Forcefully disconnecting all clients. Timeout exceeded.");
                                foreach (Client client in clients.Values.ToList())
                                {
                                    client.closeConnection();
                                }
                                break;
                            }
                            Thread.Sleep(500);
                        }
                    }
                    else
                    {
                        logDebug("Shutdown", "Quick, Disconnect all clients.");
                        foreach (Client client in clients.Values.ToList())
                        {
                            client.closeConnection();
                        }
                    }
                }
                catch (Exception) { }

                logDebug("Shutdown", "Requesting starbound_server close.");

                try { sbServer.process.CloseMainWindow(); }
                catch (Exception) { }

                logDebug("Shutdown", "Waiting for starbound_server to close.");

                Thread.Sleep(2000);

                logDebug("Shutdown", "Aborting server thread.");

                try { sbServerThread.Abort(); }
                catch (Exception) { }

                logDebug("Shutdown", "Killing starbound_server process.");

                try
                {
                    Process proc = Process.GetProcessById(parentProcessId);
                    proc.Kill();
                    File.Delete("starbound_server.pid");
                }
                catch (Exception) { }

                logDebug("Shutdown", "Aborting TCP listener.");
                try 
                {
                    lock (tcpListener.tcpSocket)
                    {
                        tcpListener.tcpSocket.Stop();
                        listenerThread.Abort();
                    }
                }
                catch (Exception) { }

                logInfo("Graceful shutdown complete.");
            }
            catch(Exception e)
            {
                try
                {
                    logException("Graceful shutdown failed: " + e.ToString());
                    Process proc = Process.GetProcessById(parentProcessId);
                    proc.Kill();
                    File.Delete("starbound_server.pid");
                }
                catch (Exception) { }
            }
            logDebug("Shutdown", "Goodbye.");
        }

        public static void logDebug(string source, string message) { writeLog("[" + source + "]:" + message, LogType.Debug); }

        public static void logInfo(string message) { writeLog(message, LogType.Info); }

        public static void logWarn(string message) { writeLog(message, LogType.Warn); }

        public static void logError(string message) { writeLog(message, LogType.Error); }

        public static void logException(string message) { writeLog(message, LogType.Exception); }

        public static void logFatal(string message) { writeLog(message, LogType.Fatal); }

        public static void writeLog(string message, LogType logType)
        {
            if ((int)config.logLevel > (int)logType && logType != LogType.FileOnly) return;

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
                    message = "[EXCEPTION] " + message;
                    break;

                case LogType.Fatal:
                    message = "[FATAL ERROR] " + message;
                    break;
            }

            try
            {
                using (StreamWriter w = File.AppendText(Path.Combine(SavePath, "log.txt")))
                {
                    w.WriteLine(message);
                }                
            }
            catch(Exception e) 
            {
                if (config.logLevel == LogType.Debug) Console.WriteLine("[DEBUG] Logger Exception: " + e.ToString());
            }

            if ((int)logType >= (int)config.logLevel) Console.WriteLine(message);
        }

        public struct StarboundVersion
        {
            public int Protocol;
            public int Minor;
            public string Name;
        }

        public static void sendGlobalMessage(string message) 
        {
            sendGlobalMessage(message, "#5dc4f4");
        }

        public static void sendGlobalMessage(string message, string color)
        {
            Task sendMessages = Task.Factory.StartNew(() =>
            {
                var buffer = clients.Values.ToList();
                foreach (Client client in buffer)
                {
                    client.sendChatMessage("^" + color + ";" + message);
                }
            });
        }

        public static List<Client> getClients()
        {
            List<Client> result = new List<Client>();
            foreach(Client client in clients.Values.ToList())
            {
                result.Add(client);
            }
            return result;
        }

        public static Client getClient(string name)
        {
            Client result;
            if (clients.TryGetValue(name, out result))
                return result;
            else
                return null;
        }

        public static Client getClient(uint id)
        {
            Client result;
            if (clientsById.TryGetValue(id, out result))
                return result;
            else
                return null;
        }

        public static void addClient(Client client)
        {
            clients.Add(client.playerData.name, client);
            clientsById.Add(client.playerData.id, client);
        }

        public static void removeClient(Client client)
        {
            clients.Remove(client.playerData.name);
            clientsById.Remove(client.playerData.id);
        }

        private static void runCallback()
        {
            while (true)
            {
                logInfo("Sending callback data to master server.");
                try
                {
                    string json = "json={\"version\":\"" + VersionNum + "\"," +
                                  "\"protocol\":\"" + ProtocolVersion + "\"," +
                                  "\"mono\":\"" + IsMono + "\"," +
                                  "\"proxyPort\":\"" + config.proxyPort + "\"," +
                                  "\"maxSlots\":\"" + config.maxClients + "\"," +
                                  "\"clientCount\":\"" + clientCount + "\"}";
                    byte[] buffer = Encoding.UTF8.GetBytes(json);

                    WebRequest request = WebRequest.Create("http://callback.avilance.com/");
                    request.ContentType = "application/x-www-form-urlencoded";
                    request.Method = "POST";
                    request.ContentLength = buffer.Length;
                    Stream streamWriter = request.GetRequestStream();
                    streamWriter.Write(buffer, 0, buffer.Length);
                    streamWriter.Close();
                }
                catch (Exception e)
                {
                    logDebug("Callback", e.ToString());
                }
                Thread.Sleep(1000 * 60 * 15);
            }
        }
    }
}
