using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;

namespace com.avilance.Starrybound
{
    class ServerThread
    {

        public Process process;
        string[] filterConsole = new string[] { "Slow asset", "does not have a", "Perf: ", "closing Unknown address type", "Warn: Missing", "Failed to place a dungeon", "Generating a dungeon" };

        public void run()
        {
            try
            {
                Console.Write("!!!!!!!!!!!!!!!!!! ServerThread");

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
            foreach (string line in filterConsole)
            {
                if (consoleLine.Contains(line)) return;
            }

            if (consoleLine.Contains("Info: Server version"))
            {
                // Grab server version
            }

            if (consoleLine.Contains("TcpServer will close, listener thread caught exception"))
            {
                StarryboundServer.logFatal("Starbound TcpServer has closed, no new clients will be accepted - Forcing a restart in 30 seconds.");
                StarryboundServer.sendGlobalMessage("ATTENTION: The server will be restarted in 30 seconds.");
                StarryboundServer.restartTime = StarryboundServer.getTimestamp() + 30;

                StarryboundServer.serverState = Util.ServerState.Restarting;
            }

            if (consoleLine.Contains("Done scanning for router for portforwarding"))
            {
                StarryboundServer.serverState = Util.ServerState.StartingProxy;
                return;
            }

            Console.WriteLine("[Debug Console Output] " + consoleLine);
        }
    }
}
