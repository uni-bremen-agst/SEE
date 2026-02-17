namespace XMLDocNormalizer.Reporting.Logging
{
    /// <summary>
    /// Simple logger for XMLDocNormalizer.
    /// Supports verbose logging, warnings, and inline progress updates.
    /// </summary>
    internal static class Logger
    {
        /// <summary>
        /// If true, verbose and progress messages are shown.
        /// </summary>
        public static bool VerboseEnabled { get; set; }

        /// <summary>
        /// Tracks the length of the last progress message.
        /// Used to properly clear/overwrite the line in the console.
        /// </summary>
        private static int lastProgressLength = 0;

        /// <summary>
        /// Logs a normal info message that is always shown, independent of verbose mode.
        /// </summary>
        /// <param name="message">Message to display.</param>
        public static void Info(string message)
        {
            EndProgress();
            System.Console.WriteLine(message);
        }

        /// <summary>
        /// Logs a normal info message if verbose logging is enabled.
        /// Ensures that any active progress line is completed before writing.
        /// </summary>
        /// <param name="message">The message to log.</param>
        public static void InfoVerbose(string message)
        {
            if (VerboseEnabled)
            {
                EndProgress();
                System.Console.WriteLine(message);
            }
        }

        /// <summary>
        /// Logs a warning message.
        /// Ensures that any active progress line is completed before writing.
        /// </summary>
        /// <param name="message">The warning message to log.</param>
        public static void Warn(string message)
        {
            EndProgress();
            System.Console.ForegroundColor = ConsoleColor.Yellow;
            System.Console.WriteLine("[WARN] " + message);
            System.Console.ResetColor();
        }

        /// <summary>
        /// Logs a warning message only in verbose mode.
        /// </summary>
        /// <param name="message">Warning message.</param>
        public static void WarnVerbose(string message)
        {
            if (VerboseEnabled)
            {
                EndProgress();
                System.Console.ForegroundColor = ConsoleColor.Yellow;
                System.Console.WriteLine("[WARN] " + message);
                System.Console.ResetColor();
            }
        }

        /// <summary>
        /// Logs a progress message inline (overwrites the current console line).
        /// Subsequent calls will overwrite this line until <see cref="EndProgress"/> is called.
        /// </summary>
        /// <param name="message">The progress message to display.</param>
        internal static void InfoProgress(string message)
        {
            if (VerboseEnabled)
            {
                // Clear remaining chars from previous progress
                int clear = Math.Max(lastProgressLength - message.Length, 0);
                System.Console.Write("\r" + message + new string(' ', clear));

                // Store current message length for next overwrite
                lastProgressLength = message.Length;
            }
        }

        /// <summary>
        /// Ends the current progress message by writing a new line to the console. 
        /// This should be called after a series of progress updates to move to the next line in the console output.
        /// </summary>
        private static void EndProgress()
        {
            if (lastProgressLength > 0)
            {
                System.Console.WriteLine();
                lastProgressLength = 0;
            }
        }
    }

}