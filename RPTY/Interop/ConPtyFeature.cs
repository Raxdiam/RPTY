using RPTY.Interop.Definitions;

namespace RPTY.Interop
{
    internal static class ConPtyFeature
    {
        private static readonly object locker = new();
        private static bool? _isVirtualTerminalEnabled;
        private static bool? _isVirtualTerminalConsoleSequeceEnabled;

        public static bool IsVirtualTerminalEnabled
        {
            get
            {
                if (_isVirtualTerminalEnabled.HasValue)
                {
                    return _isVirtualTerminalEnabled.Value;
                }

                // You must be running Windows 1903 (build >= 10.0.18362.0) or later to run ConPTY terminal
                // System.Runtime.InteropServices.RuntimeInformation.OSDescription;
                _isVirtualTerminalEnabled = Environment.OSVersion.Platform == PlatformID.Win32NT
                    && Environment.OSVersion.Version >= new Version(6, 2, 9200);

                return (bool)_isVirtualTerminalEnabled;
            }
        }

        public static bool IsVirtualTerminalConsoleSequeceEnabled
        {
            get
            {
                if (_isVirtualTerminalConsoleSequeceEnabled.HasValue)
                {
                    return _isVirtualTerminalConsoleSequeceEnabled.Value;
                }

                TryEnableVirtualTerminalConsoleSequenceProcessing();
                return _isVirtualTerminalConsoleSequeceEnabled ?? false;
            }
        }

        public static void ThrowIfVirtualTerminalIsNotEnabled()
        {
            if (!IsVirtualTerminalEnabled)
            {
                throw new InteropException("A virtual terminal is not enabled, you must be running Windows 1903 (build >= 10.0.18362.0) or later.");
            }
        }

        public static void TryEnableVirtualTerminalConsoleSequenceProcessing()
        {
            if (_isVirtualTerminalConsoleSequeceEnabled.HasValue)
            {
                return;
            }

            lock (locker)
            {
                if (_isVirtualTerminalConsoleSequeceEnabled.HasValue)
                {
                    return;
                }

                try
                {
                    SetConsoleModeToVirtualTerminal();
                    _isVirtualTerminalConsoleSequeceEnabled = true;
                }
                catch
                {
                    _isVirtualTerminalConsoleSequeceEnabled = false;
                    throw;
                }
            }
        }

        private static void SetConsoleModeToVirtualTerminal()
        {
            var stdIn = ConsoleApi.GetStdHandle(StdHandle.InputHandle);
            if (!ConsoleApi.GetConsoleMode(stdIn, out var outConsoleMode))
            {
                throw InteropException.CreateWithInnerHResultException("Could not get console mode.");
            }

            outConsoleMode |= Constants.ENABLE_VIRTUAL_TERMINAL_PROCESSING | Constants.DISABLE_NEWLINE_AUTO_RETURN;
            if (!ConsoleApi.SetConsoleMode(stdIn, outConsoleMode))
            {
                throw InteropException.CreateWithInnerHResultException("Could not enable virtual terminal processing.");
            }
        }
    }
}
