namespace XMLDocNormalizer.Cli.Output
{
    /// <summary>
    /// Provides low-level console logging for the command-line interface.
    /// </summary>
    /// <remarks>
    /// This logger is responsible for operational console output such as
    /// informational messages, warnings, and inline progress updates.
    /// It is intentionally independent from findings reporters and result summaries.
    /// </remarks>
    internal static class ConsoleLogger
    {
        internal static readonly object SyncRoot = new();
        internal static RunProgress? CurrentProgress { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether verbose messages are enabled.
        /// </summary>
        /// <value>
        /// True if verbose messages are enabled; otherwise false.
        /// </value>
        public static bool VerboseEnabled { get; set; }


        /// <summary>
        /// Writes an informational message that is always shown.
        /// </summary>
        /// <param name="message">The message to write.</param>
        public static void Info(string message)
        {
            WithProgressPaused(() => Console.WriteLine(message));
        }

        /// <summary>
        /// Writes an informational message only if verbose output is enabled.
        /// </summary>
        /// <param name="message">The message to write.</param>
        public static void InfoVerbose(string message)
        {
            if (VerboseEnabled)
            {
                Info(message);
            }
        }

        /// <summary>
        /// Writes a warning message.
        /// </summary>
        /// <param name="message">The warning message to write.</param>
        public static void Warn(string message)
        {
            WriteColoredLine("[WARN] " + message, ConsoleColors.Warning);
        }

        /// <summary>
        /// Writes a warning message only if verbose output is enabled.
        /// </summary>
        /// <param name="message">The warning message to write.</param>
        public static void WarnVerbose(string message)
        {
            if (VerboseEnabled)
            {
                Warn(message);
            }
        }

        /// <summary>
        /// Writes an inline progress message that overwrites the current console line.
        /// </summary>
        /// <param name="message">The progress message to display.</param>
        /// <remarks>
        /// Updates the active run's stderr display; selection is independent of verbose logging.
        /// </remarks>
        public static void InfoProgress(string message)
        {
            CurrentProgress?.Activity(message);
        }

        /// <summary>
        /// Clears the current progress line before other console output.
        /// </summary>
        public static void EndProgress()
        {
            CurrentProgress?.ClearLine();
        }

        internal static void DrawProgress(TextWriter writer, string message, ref int lastLength)
        {
            int clearLength = Math.Max(lastLength - message.Length, 0);
            writer.Write("\r" + message + new string(' ', clearLength));
            lastLength = message.Length;
        }

        internal static void WithProgressPaused(Action output)
        {
            if (CurrentProgress is null) { output(); return; }
            lock (SyncRoot) { EndProgress(); output(); }
        }

        /// <summary>
        /// Writes a full line in the specified color.
        /// </summary>
        /// <param name="message">The message to write.</param>
        /// <param name="color">The color to use.</param>
        private static void WriteColoredLine(string message, ConsoleColor color)
        {
            WithProgressPaused(() => WriteColoredLineCore(message, color));
        }

        private static void WriteColoredLineCore(string message, ConsoleColor color)
        {

            if (Console.IsOutputRedirected)
            {
                Console.WriteLine(message);
                return;
            }

            ConsoleColor previousColor = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.WriteLine(message);
            Console.ForegroundColor = previousColor;
        }
    }
}
