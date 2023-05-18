using RPTY.Interop;
using RPTY.Interop.Definitions;

namespace RPTY
{
    /// <summary>
    /// Native Console
    /// </summary>
    internal class NativeConsole : IDisposable
    {
        private nint _handle;
        private bool _isDisposed;
        private Pipe _stdOut, _stdErr, _stdIn;

        /// <summary>
        /// Native Console
        /// </summary>
        /// <param name="hidden"></param>
        public NativeConsole(bool hidden = true)
        {
            Initialise(hidden);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        ~NativeConsole()
        {
            Dispose(false);
        }

        /// <summary>
        /// StdOut
        /// </summary>
        public FileStream Output { get; private set; }

        /// <summary>
        /// StdErr
        /// </summary>
        public FileStream Error { get; private set; }

        /// <summary>
        /// StdIn
        /// </summary>
        public FileStream Input { get; private set; }

        /// <summary>
        /// Send CtrlEvent to the console
        /// </summary>
        /// <param name="ctrlEvent"></param>
        public static void SendCtrlEvent(CtrlEvent ctrlEvent)
        {
            ConsoleApi.GenerateConsoleCtrlEvent(ctrlEvent, 0);
        }

        /// <summary>
        /// Register OnClose Action
        /// </summary>
        /// <param name="action"></param>
        public static void RegisterOnCloseAction(Action action)
        {
            RegisterCtrlEventFunction((ctrlEvent) =>
            {
                if (ctrlEvent == CtrlEvent.CtrlClose)
                {
                    action();
                }

                return false;
            });
        }

        /// <summary>
        /// Register Ctrl Event Function
        /// </summary>
        /// <param name="function"></param>
        public static void RegisterCtrlEventFunction(CtrlEventDelegate function)
        {
            ConsoleApi.SetConsoleCtrlHandler(function, true);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;

            if (disposing)
            {
                Input.Dispose();
                Output.Dispose();
                Error.Dispose();
            }

            ConsoleApi.FreeConsole();
            ReleaseUnmanagedResources();
        }

        private void ReleaseUnmanagedResources()
        {
            _stdIn.Dispose();
            _stdOut.Dispose();
            _stdErr.Dispose();
        }

        private void Initialise(bool hidden)
        {
            if (!ConsoleApi.AllocConsole())
            {
                throw InteropException.CreateWithInnerHResultException("Could not allocate console. You may need to FreeConsole first.");
            }

            _handle = ConsoleApi.GetConsoleWindow();

            if (_handle != nint.Zero)
            {
                ConsoleApi.ShowWindow(_handle, hidden ? ShowState.SwHide : ShowState.SwShowDefault);
            }

            RegisterOnCloseAction(ReleaseUnmanagedResources);

            CreateStdOutPipe();
            CreateStdErrPipe();
            CreateStdInPipe();
        }

        private void CreateStdOutPipe()
        {
            _stdOut = new Pipe();
            if (!ConsoleApi.SetStdHandle(StdHandle.OutputHandle, _stdOut.Write.DangerousGetHandle()))
            {
                throw InteropException.CreateWithInnerHResultException("Could not redirect STDOUT.");
            }
            Output = new FileStream(_stdOut.Read, FileAccess.Read);
        }

        private void CreateStdErrPipe()
        {
            _stdErr = new Pipe();
            if (!ConsoleApi.SetStdHandle(StdHandle.ErrorHandle, _stdErr.Write.DangerousGetHandle()))
            {
                throw InteropException.CreateWithInnerHResultException("Could not redirect STDERR.");
            }
            Error = new FileStream(_stdErr.Read, FileAccess.Read);
        }

        private void CreateStdInPipe()
        {
            _stdIn = new Pipe();
            if (!ConsoleApi.SetStdHandle(StdHandle.InputHandle, _stdIn.Read.DangerousGetHandle()))
            {
                throw InteropException.CreateWithInnerHResultException("Could not redirect STDIN.");
            }
            Input = new FileStream(_stdIn.Write, FileAccess.Write);
        }
    }
}
