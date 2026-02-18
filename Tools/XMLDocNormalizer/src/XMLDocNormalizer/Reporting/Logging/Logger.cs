using XMLDocNormalizer.Models;
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
            WriteColoredLine("[WARN] " + message, ConsoleColor.Yellow);
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
            if (!VerboseEnabled)
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
        private static void WriteColoredLine(string message, ConsoleColor color)
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

        /// <summary>
        /// Writes text to the console using the specified color.
        /// If the output is redirected (e.g., CI pipelines or file output),
        /// no color is applied to avoid ANSI escape pollution.
        /// </summary>
        /// <param name="text">Text to write.</param>
        /// <param name="color">Console color to use.</param>
        private static void WriteColored(string text, ConsoleColor color)
        {
            if (!SysConsole.IsOutputRedirected)
            {
                ConsoleColor previousColor = SysConsole.ForegroundColor;
                SysConsole.ForegroundColor = color;
                SysConsole.Write(text);
                SysConsole.ForegroundColor = previousColor;
            }
            else
            {
                SysConsole.Write(text);
            }
        }

        #region Result Evaluation and Reporting
        /// <summary>
        /// Reports the result of a check run to the console.
        /// Highlights the status: "Check succeeded" in green, "Check failed" in red,
        /// and optionally the number of findings if there are any.
        /// Handles progress cleanup and respects redirected output.
        /// </summary>
        /// <param name="result">The aggregated run result to report.</param>
        public static void ReportCheckRunResult(RunResult result)
        {
            EndProgress();

            if (result.FindingCount == 0)
            {
                WriteColored("Check succeeded", ConsoleColor.Green);
                SysConsole.WriteLine(": no documentation issues found.");
            }
            else
            {
                WriteColored("Check failed", ConsoleColor.Red);
                SysConsole.Write(": ");
                WriteColored(result.FindingCount.ToString(), ConsoleColor.Red);
                SysConsole.Write(" documentation issue(s) found");

                bool first = true;
                AppendStat("Errors", result.ErrorCount, ConsoleColor.Red);
                AppendStat("Warnings", result.WarningCount, ConsoleColor.Yellow);
                AppendStat("Suggestions", result.SuggestionCount, ConsoleColor.Blue);

                if (!first)
                {
                    SysConsole.Write(")");
                }

                SysConsole.WriteLine(".");

                // Appends a severity statistic to the output if the value is greater than zero.
                // Handles proper comma separation and colored value rendering.
                void AppendStat(string label, int value, ConsoleColor color)
                {
                    if (value <= 0)
                    {
                        return;
                    }

                    SysConsole.Write(first ? " (" : ", ");
                    WriteColored(label, color);
                    SysConsole.Write(": ");
                    WriteColored(value.ToString(), color);
                    first = false;
                }
            }
        }

        /// <summary>
        /// Reports the result of a fix run to the console.
        /// Highlights the number of changed files in green and findings in green (0) or red (>0).
        /// Only the numeric parts are colored, the rest of the text uses the default console color.
        /// Handles progress cleanup and respects redirected output.
        /// </summary>
        /// <param name="result">The aggregated run result to report.</param>
        public static void ReportFixRunResult(RunResult result)
        {
            EndProgress();

            SysConsole.Write("Done. Changed files: ");
            WriteColored(result.ChangedFiles.ToString(), ConsoleColor.Green);

            SysConsole.Write(". Findings: ");
            if (result.FindingCount == 0)
            {
                WriteColored(result.FindingCount.ToString(), ConsoleColor.Green);
            }
            else
            {
                WriteColored(result.FindingCount.ToString(), ConsoleColor.Red);
            }
            SysConsole.WriteLine(".");
        }
        #endregion
    }
}