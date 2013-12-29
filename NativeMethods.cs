using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Diagnostics;
using System.Text;
using System.Runtime.InteropServices;

namespace com.avilance.Starrybound
{
    internal static class NativeMethods
    {
        internal static bool ConsoleCtrlCheck()
        {
            StarryboundServer.doShutdown(true);
            return true;
        }

        #region unmanaged
        // Declare the SetConsoleCtrlHandler function
        // as external and receiving a delegate.

        [DllImport("Kernel32")]
        internal static extern bool SetConsoleCtrlHandler(HandlerRoutine Handler, bool Add);

        // A delegate type to be used as the handler routine
        // for SetConsoleCtrlHandler.
        internal delegate bool HandlerRoutine();

        #endregion
    }
}
