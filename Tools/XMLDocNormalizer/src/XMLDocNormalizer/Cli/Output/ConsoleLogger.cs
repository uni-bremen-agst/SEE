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
        /// <summary>
        /// Gets or sets a value indicating whether verbose messages and progress updates are enabled.
        /// </summary>
        public static bool VerboseEnabled { get; set; }

        /// <summary>
        /// Tracks the length of the last progress message so the current console line
        /// can be overwritten cleanly.
        /// </summary>
        private static int lastProgressLength = 0;

        /// <summary>
        /// Writes an informational message that is always shown.
        /// </summary>
        /// <param name="message">The message to write.</param>
        public static void Info(string message)
        {
            EndProgress();
            Console.WriteLine(message);
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
        /// Progress output is only shown in verbose mode and only when output is not redirected.
        /// </remarks>
        public static void InfoProgress(string message)
        {
            if (!VerboseEnabled || Console.IsOutputRedirected)
            {
                return;
            }

            int clearLength = Math.Max(lastProgressLength - message.Length, 0);
            Console.Write("\r" + message + new string(' ', clearLength));
            lastProgressLength = message.Length;
        }

        /// <summary>
        /// Completes the current progress line by moving the cursor to the next line.
        /// </summary>
        public static void EndProgress()
        {
            if (lastProgressLength > 0)
            {
                Console.WriteLine();
                lastProgressLength = 0;
            }
        }

        /// <summary>
        /// Writes a full line in the specified color.
        /// </summary>
        /// <param name="message">The message to write.</param>
        /// <param name="color">The color to use.</param>
        private static void WriteColoredLine(string message, ConsoleColor color)
        {
            EndProgress();

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