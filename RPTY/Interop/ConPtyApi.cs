using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using RPTY.Interop.Definitions;

namespace RPTY.Interop
{
    internal static class ConPtyApi
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern int CreatePseudoConsole(
            Coordinates size,
            SafeFileHandle hInput,
            SafeFileHandle hOutput,
            uint dwFlags,
            out IntPtr phPc);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern int ResizePseudoConsole(IntPtr hPc, Coordinates size);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern int ClosePseudoConsole(IntPtr hPc);
    }
}
