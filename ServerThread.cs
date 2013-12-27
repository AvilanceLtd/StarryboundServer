using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
                ProcessStartInfo startInfo = new ProcessStartInfo("starbound_server.exe")
                {
                    WindowStyle = ProcessWindowStyle.Hidden,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };

                process = Process.Start(startInfo);

                process.OutputDataReceived += (sender, e) => parseOutput(e.Data);

                process.BeginOutputReadLine();
                process.WaitForExit();

                Thread.Sleep(5000);

            }
            catch (Exception e)
            {
                StarryboundServer.logException("Unable to start starbound_server.exe, is this file in the same directory? " + e.ToString());
                StarryboundServer.serverState = Util.ServerState.Crashed;
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
                        StarryboundServer.logFatal("Press any key to continue...");
                        Console.ReadKey(true);
                        Environment.Exit(0);
                    }
                }

                if (consoleLine.Contains("TcpServer will close, listener thread caught exception"))
                {
                    StarryboundServer.logFatal("Starbound TcpServer has closed, no new clients will be accepted - Forcing a restart in 30 seconds.");
                    StarryboundServer.sendGlobalMessage("ATTENTION: The server will be restarted in 30 seconds.");
                    StarryboundServer.restartTime = Utils.getTimestamp() + 30;

                    StarryboundServer.serverState = Util.ServerState.Restarting;
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
