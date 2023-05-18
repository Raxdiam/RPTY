using Microsoft.Win32.SafeHandles;
using RPTY.Interop;
using RPTY.Interop.Definitions;

namespace RPTY
{
    /// <summary>
    /// Terminal
    /// </summary>
    internal class Terminal : IDisposable
    {
        private Pipe _input;
        private Pipe _output;
        private PseudoConsole _console;
        private Process _process;
        private bool _disposed;

        /// <summary>
        /// Terminal
        /// </summary>
        public Terminal()
        {
            ConPtyFeature.ThrowIfVirtualTerminalIsNotEnabled();

            if (ConsoleApi.GetConsoleWindow() != IntPtr.Zero)
            {
                ConPtyFeature.TryEnableVirtualTerminalConsoleSequenceProcessing();
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        ~Terminal()
        {
            Dispose(false);
        }

        /// <summary>
        /// Input Stream
        /// </summary>
        public FileStream Input { get; private set; }

        /// <summary>
        /// Output Stream
        /// </summary>
        public FileStream Output { get; private set; }

        /// <summary>
        /// Starts the console
        /// </summary>
        /// <param name="shellCommand"></param>
        /// <param name="consoleWidth"></param>
        /// <param name="consoleHeight"></param>
        /// <returns></returns>
        public ProcessInfo Start(string shellCommand, short consoleWidth, short consoleHeight)
        {
            _input = new Pipe();
            _output = new Pipe();

            _console = PseudoConsole.Create(_input.Read, _output.Write, consoleWidth, consoleHeight);
            _process = ProcessFactory.Start(shellCommand, PseudoConsole.PseudoConsoleThreadAttribute, _console.Handle);

            Input = new FileStream(_input.Write, FileAccess.Write);
            Output = new FileStream(_output.Read, FileAccess.Read);

            return _process.ProcessInfo;
        }

        /// <summary>
        /// Immediately stops the associated console.
        /// </summary>
        public void Kill()
        {
            _console?.Dispose();
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public WaitHandle BuildWaitHandler()
        {
            return new AutoResetEvent(false)
            {
                SafeWaitHandle = new SafeWaitHandle(_process.ProcessInfo.hProcess, ownsHandle: false)
            };
        }

        /// <summary>
        /// Instructs the console to wait indefinitely for the associated process to exit.
        /// </summary>
        public void WaitForExit()
        {
            BuildWaitHandler().WaitOne(Timeout.Infinite);
        }

        /// <summary>
        /// Try get ExitCode
        /// </summary>
        /// <param name="exitCode"></param>
        /// <returns></returns>
        public bool TryGetExitCode(out uint exitCode)
        {
            return ProcessApi.GetExitCodeProcess(_process.ProcessInfo.hProcess, out exitCode);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _process?.Dispose();
            _console?.Dispose();

            if (disposing)
            {
                Input?.Dispose();
                Output?.Dispose();
            }

            _input?.Dispose();
            _output?.Dispose();
        }
    }
}
