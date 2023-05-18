using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using RPTY.Interop.Definitions;

namespace RPTY.Interop
{
    internal class Pipe : IDisposable
    {
        private SafeFileHandle _write;
        private SafeFileHandle _read;

        public Pipe()
            : this(SecurityAttributes.Zero) { }

        public Pipe(SecurityAttributes securityAttributes)
        {
            if (!ConsoleApi.CreatePipe(out _read, out _write, ref securityAttributes, 0))
            {
                throw new InteropException("Failed to create pipe.",
                    Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error()));
            }
        }

        ~Pipe()
        {
            Dispose(false);
        }

        public SafeFileHandle Read => _read;

        public SafeFileHandle Write => _write;

        public void MakeReadNoninheritable(IntPtr processHandle)
        {
            MakeHandleNoninheritable(ref _read, processHandle);
        }

        public void MakeWriteNoninheritable(IntPtr processHandle)
        {
            MakeHandleNoninheritable(ref _write, processHandle);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            _read.Dispose();
            _write.Dispose();
        }

        private void MakeHandleNoninheritable(ref SafeFileHandle handler, IntPtr processHandle)
        {
            // Create noninheritable read handle and close the inheritable read handle.
            if (!ConsoleApi.DuplicateHandle(
                    processHandle,
                    handler.DangerousGetHandle(),
                    processHandle,
                    out var handleClone,
                    0,
                    false,
                    Constants.DUPLICATE_SAME_ACCESS))
            {
                throw InteropException.CreateWithInnerHResultException("Couldn't duplicate the handle.");
            }

            var toRelease = handler;
            handler = new SafeFileHandle(handleClone, true);
            toRelease.Dispose();
        }
    }
}
