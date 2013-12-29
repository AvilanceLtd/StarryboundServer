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
        public static StarboundVersion starboundVersion = new StarboundVersion();
        public static GeoIPCountry Geo;
        public static int parentProcessId;
        
        // Dictionary<string, ClientThread>
        // string           Username        Unique username for client, MUST be lowercase
        // ClientThread     ClientThread    Invidivual thread for client, used to access client specific functions
        public static Dictionary<string, Client> clients = new Dictionary<string, Client>();
        public static Dictionary<uint, Client> clientsById = new Dictionary<uint, Client>();
        public static int clientCount { get { return clients.Count; } set { return; } }

        public static Dictionary<string, Group> groups = new Dictionary<string, Group>();

        public static ServerThread sbServer;
        static Thread sbServerThread;
        static Thread listenerThread;
        static Thread monitorThread;

        public static string privatePassword;

        public static int failedConnections;
        public static ServerState serverState;

        public static int startTime;
        public static int restartTime = 0;

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
            doShutdown(true);
        }

        static void UnhandledException(object sender, UnhandledExceptionEventArgs args)
        {
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
        }

        static void Main(string[] args)
        {
#if DEBUG
            config.logLevel = LogType.Debug;
#endif
            startTime = Utils.getTimestamp();
            serverState = ServerState.Starting;
            Console.Title = "Loading... Starrybound Server (" + VersionNum + ") (" + ProtocolVersion + ")";

            if (IsMono)
                Environment.CurrentDirectory = Path.GetDirectoryName(typeof(StarryboundServer).Assembly.Location);

            AppDomain.CurrentDomain.ProcessExit += new EventHandler(ProcessExit);
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(UnhandledException);

            if (!IsMono)
                NativeMethods.SetConsoleCtrlHandler(new NativeMethods.HandlerRoutine(NativeMethods.ConsoleCtrlCheck), true);

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
                sectors.Add(Encoding.UTF8.GetBytes(sector));
            }
            Bans.ProcessBans();

            logInfo("Starrybound Server initialization complete.");

            monitorThread = new Thread(new ThreadStart(crashMonitor));
            monitorThread.Start();

            listenerThread = new Thread(new ThreadStart(new ListenerThread().run));
            listenerThread.Start();
#if !NOSERVER
            sbServer = new ServerThread();
            sbServerThread = new Thread(new ThreadStart(sbServer.run));
            sbServerThread.Start();

            Console.Title = "Starting... Starrybound Server (" + VersionNum + ") (" + ProtocolVersion + ")";
            logInfo("Starting parent Starbound server - This may take a few moments...");
            while (serverState != ServerState.Ready) 
            {
                if (serverState == ServerState.Crashed)
                {
                    try
                    {
                        Process proc = Process.GetProcessById(parentProcessId);
                        proc.Kill();
                        File.Delete("starbound_server.pid");
                    }
                    catch (Exception) { }
                    logFatal("Parent Starbound Server failed to start!");
                    Thread.Sleep(5000);
                    Environment.Exit(2);
                }
            }
#endif
            logInfo("Parent Starbound server is ready. Starrybound Server now accepting connections.");
            serverState = ServerState.Running;
        }

        public static void crashMonitor()
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
                        sendGlobalMessage("^#f75d5d;The server will restart in " + remaining + " seconds.");
                }

                if (serverState == ServerState.Crashed)
                {
                    logFatal("The server has encountered a fatal error and cannot continue. Restarting in 10 seconds.");
                    Thread.Sleep(10000);
                    doRestart();
                    break;
                }

                if (restartTime != 0)
                    Thread.Sleep(1000);
                else
                    Thread.Sleep(3000);
            }
        }

        public static void doRestart()
        {
            doShutdown(false);
            logInfo("Now restarting...");
            Process.Start(AppDomain.CurrentDomain.FriendlyName);
            Environment.Exit(0);
        }

        public static void doShutdown(bool quick)
        {
            if (serverState == ServerState.DoShutdown) return;
            serverState = ServerState.DoShutdown;
            Console.Title = "Shutting Down... Starrybound Server (" + VersionNum + ") (" + ProtocolVersion + ")";
            logInfo("Starting graceful shutdown...");
            try
            {
                if (Geo != null)
                    Geo.Dispose();

                if (listenerThread != null) 
                    listenerThread.Abort();

                if (clients != null)
                {

                    if (!quick)
                    {
                        lock (clients)
                        {
                            foreach (Client client in clients.Values.ToList())
                            {
                                client.delayDisconnect("^#f75d5d;You have been disconnected.");
                            }
                        }

                        int startWait = Utils.getTimestamp();
                        while (clientCount > 0)
                        {
                            if (Utils.getTimestamp() > startWait + 7) break;
                            Thread.Sleep(500);
                        }
                    }
                    else
                    {
                        lock (clients)
                        {
                            foreach (Client client in clients.Values.ToList())
                            {
                                client.closeConnection();
                            }
                        }
                    }
                }

                try { sbServer.process.CloseMainWindow(); }
                catch (Exception) { }

                Thread.Sleep(500);

                try { sbServerThread.Abort(); }
                catch (Exception) { }

                try
                {
                    Process proc = Process.GetProcessById(parentProcessId);
                    proc.Kill();
                    File.Delete("starbound_server.pid");
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

        public static void sendGlobalMessage (string message) 
        {
            var buffer = clients.Values.ToList();
            foreach (Client client in buffer)
            {
                client.sendChatMessage("^#5dc4f4;" + message);
            }
        }

        public static void sendGlobalMessage(string message, string color)
        {
            var buffer = clients.Values.ToList();
            foreach (Client client in buffer)
            {
                client.sendChatMessage("^" + color + ";" + message);
            }
        }
    }
}
