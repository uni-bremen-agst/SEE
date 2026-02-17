using SysConsole = System.Console;

namespace XMLDocNormalizer.Reporting.Logging
{
    /// <summary>
    /// Simple logger for XMLDocNormalizer.
    /// Supports verbose logging, warnings, and inline progress updates,
    /// and colored status output.
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
            SysConsole.WriteLine(message);
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
                Info(message);
            }
        }

        /// <summary>
        /// Logs a warning message.
        /// Ensures that any active progress line is completed before writing.
        /// </summary>
        /// <param name="message">The warning message to log.</param>
        public static void Warn(string message)
        {
            WriteColored("[WARN] " + message, ConsoleColor.Yellow);
        }

        /// <summary>
        /// Logs a warning message only in verbose mode.
        /// </summary>
        /// <param name="message">Warning message.</param>
        public static void WarnVerbose(string message)
        {
            if (VerboseEnabled)
            {
                Warn(message);
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
                return;
            }
            if (!SysConsole.IsOutputRedirected)
            {
                // Clear remaining chars from previous progress
                int clear = Math.Max(lastProgressLength - message.Length, 0);
                SysConsole.Write("\r" + message + new string(' ', clear));

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
                SysConsole.WriteLine();
                lastProgressLength = 0;
            }
        }

        /// <summary>
        /// Writes a message in a specified color, only if output is not redirected.
        /// Falls back to normal Console.WriteLine if output is redirected.
        /// Ensures any active progress line is properly ended.
        /// </summary>
        /// <param name="message">The message to write.</param>
        /// <param name="color">The console color to use.</param>
        private static void WriteColored(string message, ConsoleColor color)
        {
            EndProgress();

            if (!SysConsole.IsOutputRedirected)
            {
                SysConsole.ForegroundColor = color;
                SysConsole.WriteLine(message);
                SysConsole.ResetColor();
            }
            else
            {
                SysConsole.WriteLine(message);
            }
        }
    }

}