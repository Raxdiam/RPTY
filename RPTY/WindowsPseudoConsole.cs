using System.Text;
using System.Text.RegularExpressions;
using RPTY.Interop.Definitions;

namespace RPTY
{
    /// <summary>
    /// Pseudo Console (ConPTY)
    /// </summary>
    public class WindowsPseudoConsole : IDisposable
    {
        /// <summary>
        /// Occurs when console title received
        /// </summary>
        public event EventHandler<string> TitleReceived;

        /// <summary>
        /// Occurs each time console writes a line.
        /// </summary>
        public event EventHandler<string> OutputDataReceived;

        /// <summary>
        /// Occurs when the console exits.
        /// </summary>
        public event EventHandler<int> Exited;

        /// <summary>
        /// Working directory. Default: <see cref="Directory.GetCurrentDirectory()"/>
        /// </summary>
        public string WorkingDirectory { get; set; } = Directory.GetCurrentDirectory();

        /// <summary>
        /// Gets or sets the application or document to start.
        /// </summary>
        public string FileName { get; set; } = string.Empty;

        /// <summary>
        /// Arguments that pass to the console. Default: <see cref="string.Empty"/>
        /// </summary>
        public string Arguments { get; set; } = string.Empty;

        /// <summary>
        /// Filtering out ANSI escape sequences on <see cref="OutputDataReceived"/>. Default: <see langword="false"/>
        /// </summary>
        public bool FilterControlSequences { get; set; } = false;

        private Terminal _terminal;
        private Stream _inputStream;
        private bool _disposed;

        /// <summary>
        /// Pseudo Console (ConPTY)
        /// </summary>
        public WindowsPseudoConsole() { }

        /// <summary>
        /// Start pseudo console
        /// </summary>
        public virtual ProcessInfo Start(short width = 120, short height = 30)
        {
            if (WorkingDirectory == null) {
                throw new Exception("WorkingDirectory is not set");
            }

            var filePath = Path.Combine(WorkingDirectory, FileName);

            if (!File.Exists(filePath)) {
                throw new Exception($"File does not exist ({filePath})");
            }

            // Start pseudo console
            _terminal = new Terminal();
            var processInfo = _terminal.Start($"{filePath}{(string.IsNullOrEmpty(Arguments) ? string.Empty : $" {Arguments}")}", width, height);

            // Save the inputStream
            _inputStream = _terminal.Input;

            // Read pseudo console output in the background
            Task.Run(() => ReadConPtyOutput(_terminal.Output));

            // Wait the pseudo console exit in the background
            Task.Run(() => {
                _terminal.WaitForExit();

                // Call Exited event with exit code
                Exited?.Invoke(this, _terminal.TryGetExitCode(out var exitCode) ? (int)exitCode : -1);
            });

            return processInfo;
        }

        /// <summary>
        /// Write data to the console.
        /// </summary>
        /// <param name="data"></param>
        public void Write(char data) => Write(data.ToString());

        /// <summary>
        /// Write data to the console.
        /// </summary>
        /// <param name="data"></param>
        public void Write(char[] data) => Write(data.ToString());

        /// <summary>
        /// Write data to the console.
        /// </summary>
        /// <param name="data"></param>
        public void Write(string data)
        {
            var bytes = Encoding.UTF8.GetBytes(data);
            _inputStream.Write(bytes, 0, bytes.Length);
            _inputStream.Flush();
        }

        /// <summary>
        /// Write data to the console, followed by a break line character.
        /// </summary>
        /// <param name="data"></param>
        public void WriteLine(string data) => Write($"{data}\x0D");

        /// <summary>
        /// Write data to the console.
        /// </summary>
        /// <param name="data"></param>
        public Task WriteAsync(char data) => WriteAsync(data.ToString());

        /// <summary>
        /// Write data to the console.
        /// </summary>
        /// <param name="data"></param>
        public Task WriteAsync(char[] data) => WriteAsync(data.ToString());

        /// <summary>
        /// Write data to the console.
        /// </summary>
        /// <param name="data"></param>
        public async Task WriteAsync(string data)
        {
            var bytes = Encoding.UTF8.GetBytes(data);
            await _inputStream.WriteAsync(bytes, 0, bytes.Length);
            await _inputStream.FlushAsync();
        }

        /// <summary>
        /// Write data to the console, followed by a break line character.
        /// </summary>
        /// <param name="data"></param>
        public Task WriteLineAsync(string data) => WriteAsync($"{data}\x0D");

        private async Task ReadConPtyOutput(Stream output)
        {
            var titleInvoked = false;
            var title = string.Empty;
            const string cursorLow = "\x1B[?25l", cursorHigh = "\x1B[?25h";

            var regex = new Regex(@"\x1B(?:[@-Z\\-_]|\[[0-?]*[ -/]*[@-~])");

            try {
                using var reader = new StreamReader(output);
                var buffer = new char[1024];

                while (true) {
                    var readed = await reader.ReadAsync(buffer, 0, buffer.Length);

                    if (readed > 0) {
                        var outputData = new string(buffer.Take(readed).ToArray());

                        if (!titleInvoked) {
                            title += outputData;

                            var subs = title.Split(new[] { cursorLow, cursorHigh }, 2, StringSplitOptions.None);

                            if (subs.Length <= 1) {
                                continue;
                            }

                            titleInvoked = true;

                            title = regex.Replace(subs[0], string.Empty).TrimEnd('\x7');
                            title = title.StartsWith("0;") ? title.Substring(2) : title;

                            TitleReceived?.Invoke(this, title);

                            outputData = cursorLow + subs[1];
                        }

                        OutputDataReceived?.Invoke(this, FilterControlSequences ? regex.Replace(outputData, string.Empty) : outputData);
                    }

                    await Task.Delay(1).ConfigureAwait(false);
                }
            }
            catch (ObjectDisposedException) {
                // Disposed
            }
        }

        /// <summary>
        /// Release the resources
        /// </summary>
        /// <param name="disposing"></param>
        protected void Dispose(bool disposing)
        {
            if (_disposed) {
                return;
            }

            _disposed = true;

            if (disposing) { }

            _terminal.Dispose();
            _inputStream?.Dispose();
        }

        /// <summary>
        /// Release the resources
        /// </summary>
        ~WindowsPseudoConsole()
        {
            Dispose(disposing: false);
        }

        /// <summary>
        /// Release the resources
        /// </summary>
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
