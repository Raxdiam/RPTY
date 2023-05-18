using System.Runtime.InteropServices;
using RPTY.Interop.Definitions;

namespace RPTY.Interop
{
    internal class Process : IDisposable
    {
        private bool _disposed = false;

        public Process(StartInfoExtended startupInfo, ProcessInfo processInfo)
        {
            StartupInfo = startupInfo;
            ProcessInfo = processInfo;
        }

        ~Process()
        {
            Dispose(false);
        }

        public StartInfoExtended StartupInfo { get; }

        public ProcessInfo ProcessInfo { get; }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            // Free the attribute list
            if (StartupInfo.lpAttributeList != IntPtr.Zero)
            {
                ProcessApi.DeleteProcThreadAttributeList(StartupInfo.lpAttributeList);
                Marshal.FreeHGlobal(StartupInfo.lpAttributeList);
            }

            // Close process and thread handles
            if (ProcessInfo.hProcess != IntPtr.Zero)
            {
                ConsoleApi.CloseHandle(ProcessInfo.hProcess);
            }
            if (ProcessInfo.hThread != IntPtr.Zero)
            {
                ConsoleApi.CloseHandle(ProcessInfo.hThread);
            }

            _disposed = true;
        }
    }
}
