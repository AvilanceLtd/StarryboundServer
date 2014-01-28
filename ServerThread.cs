using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading;
using com.avilance.Starrybound.Util;

namespace com.avilance.Starrybound
{
    class ServerThread
    {

        public Process process;
        string[] filterConsole = new string[] { "Slow asset", " does not have a ", "Debug: Correcting path from ", "closing Unknown address type", "Warn: Missing", "Failed to place a dungeon", "Generating a dungeon", "Failed to place dungeon object", "Info:  <", "Sending Handshake Challenge", " accept from ", " Connection received from: ", " UniverseServer: client connection made from " };

        bool parseError = false;

        public void run()
        {
            var executableName = "starbound_server" + (StarryboundServer.IsMono ? "" : ".exe");
            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo(executableName)
                {
                    WindowStyle = ProcessWindowStyle.Hidden,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };
                process = Process.Start(startInfo);
                StarryboundServer.parentProcessId = process.Id;
                File.WriteAllText("starbound_server.pid", process.Id.ToString());
                process.OutputDataReceived += (sender, e) => parseOutput(e.Data);
                process.ErrorDataReceived += (sender, e) => logStarboundError("ErrorDataReceived from starbound_server.exe: " + e.Data);
                process.BeginOutputReadLine();
                process.WaitForExit();
                StarryboundServer.serverState = ServerState.Crashed;
            }
            catch (ThreadAbortException) { }
            catch (Exception e)
            {
                StarryboundServer.logException("Unable to start starbound_server.exe, is this file in the same directory? " + e.ToString());
                StarryboundServer.serverState = ServerState.Crashed;
            }
        }

        void parseOutput(string consoleLine)
        {
            try
            {
                foreach (string line in filterConsole)
                {
                    if (consoleLine.Contains(line)) return;
                }

                if (consoleLine.StartsWith("Error: ") || consoleLine.StartsWith("AssetException:"))
                {
                    this.parseError = true;
                }
                else if ((consoleLine.StartsWith("Warn:") || consoleLine.StartsWith("Info:") || consoleLine.StartsWith("Debug:")) && this.parseError)
                {
                    logStarboundError(" ");
                    this.parseError = false;
                }
                else if (String.IsNullOrWhiteSpace(consoleLine) && this.parseError)
                {
                    logStarboundError(" ");
                    this.parseError = false;
                    return;
                }

                if (consoleLine.StartsWith("Warn: Perf: "))
                {
                    string[] perf = consoleLine.Remove(0, 12).Split(' ');
                    string function = perf[0];
                    float millis = Convert.ToSingle(perf[2]);
                    if (millis > 5000)
                    {
                        StarryboundServer.logWarn("Parent Server [" + function + "] lagged for " + (millis / 1000) + " seconds");
                    }
                    return;
                }
                else if (consoleLine.Contains("Info: Server version"))
                {
                    string[] versionString = consoleLine.Split('\'');
                    string versionName = versionString[1];
                    int protocolVersion = int.Parse(versionString[3]);
                    StarryboundServer.starboundVersion.Protocol = protocolVersion;
                    StarryboundServer.starboundVersion.Name = versionName;
                    if (protocolVersion != StarryboundServer.ProtocolVersion)
                    {
                        StarryboundServer.logFatal("Detected protcol version [" + protocolVersion + "] != [" + StarryboundServer.ProtocolVersion + "] to expected protocol version!");
                        Thread.Sleep(5000);
                        Environment.Exit(4);
                    }
                    StarryboundServer.logInfo("Parent Server Version: [" + versionName + "] Protocol: [" + protocolVersion + "]");
                    return;
                }
                else if (consoleLine.Contains("TcpServer will close, listener thread caught exception"))
                {
                    StarryboundServer.logFatal("Parent Server TcpServer listener thread caught exception, Forcing a restart.");
                    StarryboundServer.serverState = ServerState.Crashed;
                }
                else if (consoleLine.Contains("TcpServer listening on: "))
                {
                    StarryboundServer.serverState = ServerState.StarboundReady;
                    ServerConfig.RemovePrivateConfig();
                }
                else if (consoleLine.StartsWith("Info: Kicking client "))
                {
                    string[] kick = consoleLine.Remove(0, 21).Split(' ');
                    string user = kick[0];
                    string id = kick[1];
                    string ip = kick[2];
                    StarryboundServer.logWarn("Parent Server disconnected " + user + " " + ip + " for inactivity.");
                    return;
                }

                if (!this.parseError) Console.WriteLine("[STAR] " + consoleLine);
                else logStarboundError(consoleLine);
            }
            catch (Exception) { }
        }

        void logStarboundError(string errStr)
        {
            using (StreamWriter w = File.AppendText(Path.Combine(StarryboundServer.SavePath, "server-errors.log")))
            {
                w.WriteLine(errStr);
            }
        }
    }
}
