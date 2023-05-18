using Microsoft.Win32.SafeHandles;
using RPTY.Interop.Definitions;

namespace RPTY.Interop
{
    internal class PseudoConsole : IDisposable
    {
        public static readonly IntPtr PseudoConsoleThreadAttribute
            = (IntPtr)Constants.PROC_THREAD_ATTRIBUTE_PSEUDOCONSOLE;

        private PseudoConsole(IntPtr handle)
        {
            Handle = handle;
        }

        ~PseudoConsole()
        {
            Dispose(false);
        }

        public IntPtr Handle { get; }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            ConPtyApi.ClosePseudoConsole(Handle);
        }

        public static PseudoConsole Create(SafeFileHandle inputReadSide, SafeFileHandle outputWriteSide, short width, short height)
        {
            var createResult = ConPtyApi.CreatePseudoConsole(
                new Coordinates { X = width, Y = height },
                inputReadSide, outputWriteSide,
                0, out var hPc);

            if (createResult != 0)
            {
                throw InteropException.CreateWithInnerHResultException($"Could not create pseudo console. Error Code: {createResult}");
            }

            return new PseudoConsole(hPc);
        }
    }
}
