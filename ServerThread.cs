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
        string[] filterConsole = new string[] { "Slow asset", "does not have a", "Perf: ", "closing Unknown address type", "Warn: Missing", "Failed to place a dungeon", "Generating a dungeon", "Failed to place dungeon object", "Info:  <" };

        public void run()
        {
            try
            {
                int processId = Convert.ToInt32(File.ReadAllText("starbound_server.pid"));
                Process proc = Process.GetProcessById(processId);
                proc.Kill();
                File.Delete("starbound_server.pid");
            }
            catch (Exception) { }

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

                if (consoleLine.Contains("Info: Server version"))
                {
                    string[] versionString = consoleLine.Split('\'');
                    string versionName = versionString[1];
                    int protocolVersion = int.Parse(versionString[3]);
                    int versionMinor = int.Parse(versionString[5]);
                    StarryboundServer.starboundVersion.Protocol = protocolVersion;
                    StarryboundServer.starboundVersion.Minor = versionMinor;
                    StarryboundServer.starboundVersion.Name = versionName;
                    if(protocolVersion != StarryboundServer.ProtocolVersion)
                    {
                        StarryboundServer.logFatal("Detected protcol version [" + protocolVersion + "] != [" + StarryboundServer.ProtocolVersion + "] to expected protocol version!");
                        Thread.Sleep(5000);
                        Environment.Exit(4);
                    }
                }

                if (consoleLine.Contains("TcpServer will close, listener thread caught exception"))
                {
                    StarryboundServer.logFatal("Starbound TcpServer has closed, no new clients will be accepted - Forcing a restart in 30 seconds.");
                    StarryboundServer.sendGlobalMessage("ATTENTION: The server will be restarted in 30 seconds.");
                    StarryboundServer.restartTime = Utils.getTimestamp() + 30;

                    StarryboundServer.serverState = Util.ServerState.ShuttingDown;
                }

                if (consoleLine.Contains("TcpServer listening on: "))
                {
                    StarryboundServer.serverState = Util.ServerState.StartingProxy;
                }

                if (consoleLine.Contains("Info: Client "))
                {
                    if (consoleLine.Contains(" connected"))
                    {

                    }
                }

                Console.WriteLine("[STAR] " + consoleLine);
            }
            catch (Exception) { }
        }
    }
}
