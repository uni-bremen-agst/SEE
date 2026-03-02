using System.Drawing;
using XMLDocNormalizer.Models;
using XMLDocNormalizer.Models.Dto;
using XMLDocNormalizer.Models.Keys;
using XMLDocNormalizer.Utils;
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
        /// 
        /// </summary>
        private static ConsoleColor errorColor = ConsoleColor.Red;

        /// <summary>
        /// 
        /// </summary>
        private static ConsoleColor warningColor = ConsoleColor.DarkYellow;

        /// <summary>
        /// 
        /// </summary>
        private static ConsoleColor suggestionColor = ConsoleColor.Yellow;

        /// <summary>
        /// 
        /// </summary>
        private static ConsoleColor infoColor = ConsoleColor.Blue;

        /// <summary>
        /// 
        /// </summary>
        private static ConsoleColor successColor = ConsoleColor.Green;

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
            WriteColoredLine("[WARN] " + message, warningColor);
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

        /// <summary>
        /// Writes a second line containing SLOC and per-KLOC density metrics.
        /// </summary>
        /// <param name="result">
        /// The aggregated run result.
        /// </param>
        /// <remarks>
        /// - Findings/KLOC is always shown.
        /// - Error/Warning/Suggestion densities are only shown if their absolute counts are greater than zero.
        /// </remarks>
        private static void PrintSlocMetricsLine(RunResult result)
        {
            if (result.Sloc <= 0)
            {
                return;
            }

            RunMetricsDto metrics = RunMetricsCalculator.From(result);

            double findingsPerKLoc = metrics.FindingsPerKLoc;
            double errorsPerKLoc = metrics.ErrorsPerKLoc;
            double warningsPerKLoc = metrics.WarningsPerKLoc;
            double suggestionsPerKLoc = metrics.SuggestionsPerKLoc;

            SysConsole.Write("SLOC: ");
            WriteColored(result.Sloc.ToString("N0"), infoColor);

            SysConsole.Write(" | Findings/KLOC: ");
            ConsoleColor findingsColor = result.FindingCount == 0
                ? successColor
                : errorColor;
            WriteColored(findingsPerKLoc.ToString("0.00"), findingsColor);

            if (result.ErrorCount > 0)
            {
                SysConsole.Write(" | Errors/KLOC: ");
                WriteColored(errorsPerKLoc.ToString("0.00"), errorColor);
            }

            if (result.WarningCount > 0)
            {
                SysConsole.Write(" | Warnings/KLOC: ");
                WriteColored(warningsPerKLoc.ToString("0.00"), warningColor);
            }

            if (result.SuggestionCount > 0)
            {
                SysConsole.Write(" | Suggestions/KLOC: ");
                WriteColored(suggestionsPerKLoc.ToString("0.00"), suggestionColor);
            }

            SysConsole.WriteLine();
        }

        /// <summary>
        /// Writes additional lines containing collected totals (denominators) and derived coverage ratios.
        /// </summary>
        /// <param name="result">The aggregated run result.</param>
        /// <remarks>
        /// The method uses <see cref="RunMetricsCalculator"/> to obtain a reporting snapshot.
        /// Coverage ratios are printed as percentages and only included if the corresponding values exist.
        /// Totals are printed as absolute counts and only included if at least one total is present.
        /// </remarks>
        private static void PrintTotalsAndCoverageLines(RunResult result)
        {
            if (result.Sloc <= 0)
            {
                return;
            }

            RunMetricsDto metrics = RunMetricsCalculator.From(result);

            if (metrics.Totals.Count > 0)
            {
                SysConsole.Write("Totals: ");

                bool firstTotal = true;

                WriteTotalIfPresent(metrics, ref firstTotal, "Namespaces", StatisticsKeys.UniqueNamespacesTotal);
                WriteTotalIfPresent(metrics, ref firstTotal, "Classes", StatisticsKeys.ClassDeclarationsTotal);
                WriteTotalIfPresent(metrics, ref firstTotal, "Structs", StatisticsKeys.StructDeclarationsTotal);
                WriteTotalIfPresent(metrics, ref firstTotal, "Interfaces", StatisticsKeys.InterfaceDeclarationsTotal);
                WriteTotalIfPresent(metrics, ref firstTotal, "Enums", StatisticsKeys.EnumDeclarationsTotal);
                WriteTotalIfPresent(metrics, ref firstTotal, "EnumMembers", StatisticsKeys.EnumMembersTotal);
                WriteTotalIfPresent(metrics, ref firstTotal, "Delegates", StatisticsKeys.DelegateDeclarationsTotal);

                WriteTotalIfPresent(metrics, ref firstTotal, "Methods", StatisticsKeys.MethodsTotal);
                WriteTotalIfPresent(metrics, ref firstTotal, "Ctors", StatisticsKeys.ConstructorsTotal);
                WriteTotalIfPresent(metrics, ref firstTotal, "Properties", StatisticsKeys.PropertiesTotal);
                WriteTotalIfPresent(metrics, ref firstTotal, "Indexers", StatisticsKeys.IndexersTotal);
                WriteTotalIfPresent(metrics, ref firstTotal, "Events", StatisticsKeys.EventsTotal);
                WriteTotalIfPresent(metrics, ref firstTotal, "Fields", StatisticsKeys.FieldsTotal);
                WriteTotalIfPresent(metrics, ref firstTotal, "Operators", StatisticsKeys.OperatorsTotal);
                WriteTotalIfPresent(metrics, ref firstTotal, "Conversions", StatisticsKeys.ConversionsTotal);

                WriteTotalIfPresent(metrics, ref firstTotal, "Params", StatisticsKeys.ParametersTotal);
                WriteTotalIfPresent(metrics, ref firstTotal, "TypeParams", StatisticsKeys.TypeParametersTotal);
                WriteTotalIfPresent(metrics, ref firstTotal, "Returnable", StatisticsKeys.ReturnsRequiredTotal);

                SysConsole.WriteLine();
            }

            if (metrics.Coverage.Count > 0)
            {
                SysConsole.Write("Coverage: ");

                bool firstCoverage = true;

                WriteCoverageIfPresent(metrics, ref firstCoverage, "Param missing", CoverageKeys.ParamMissingTagRate);
                WriteCoverageIfPresent(metrics, ref firstCoverage, "Param empty", CoverageKeys.ParamEmptyDescriptionRate);
                WriteCoverageIfPresent(metrics, ref firstCoverage, "TypeParam missing", CoverageKeys.TypeParamMissingTagRate);
                WriteCoverageIfPresent(metrics, ref firstCoverage, "TypeParam empty", CoverageKeys.TypeParamEmptyDescriptionRate);
                WriteCoverageIfPresent(metrics, ref firstCoverage, "Returns missing", CoverageKeys.ReturnsMissingRate);
                WriteCoverageIfPresent(metrics, ref firstCoverage, "Namespace central missing", CoverageKeys.NamespaceCentralDocMissingRate);

                SysConsole.WriteLine();
            }
        }

        /// <summary>
        /// Writes a single total entry if the corresponding key exists and the value is greater than zero.
        /// </summary>
        /// <param name="metrics">The run metrics snapshot.</param>
        /// <param name="first">Whether this is the first entry on the line.</param>
        /// <param name="label">The human-readable label to print.</param>
        /// <param name="key">The totals key.</param>
        private static void WriteTotalIfPresent(
            RunMetricsDto metrics,
            ref bool first,
            string label,
            string key)
        {
            if (!metrics.Totals.TryGetValue(key, out int value))
            {
                return;
            }

            if (value <= 0)
            {
                return;
            }

            if (!first)
            {
                SysConsole.Write(" | ");
            }

            SysConsole.Write(label);
            SysConsole.Write(": ");
            SysConsole.Write(value.ToString("N0"));

            first = false;
        }

        /// <summary>
        /// Writes a single coverage entry if the corresponding key exists.
        /// </summary>
        /// <param name="metrics">The run metrics snapshot.</param>
        /// <param name="first">Whether this is the first entry on the line.</param>
        /// <param name="label">The human-readable label to print.</param>
        /// <param name="key">The coverage key.</param>
        private static void WriteCoverageIfPresent(
            RunMetricsDto metrics,
            ref bool first,
            string label,
            string key)
        {
            if (!metrics.Coverage.TryGetValue(key, out double value))
            {
                return;
            }

            if (!first)
            {
                SysConsole.Write(" | ");
            }

            SysConsole.Write(label);
            SysConsole.Write(": ");
            SysConsole.Write((value * 100.0).ToString("0.00"));
            SysConsole.Write("%");

            first = false;
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
                WriteColored("Check succeeded", successColor);
                SysConsole.WriteLine(": no documentation issues found.");
            }
            else
            {
                WriteColored("Check failed", errorColor);
                SysConsole.Write(": ");
                WriteColored(result.FindingCount.ToString(), errorColor);
                SysConsole.Write(" documentation issue(s) found");
                AppendStats(result);
                SysConsole.WriteLine(".");
                PrintSlocMetricsLine(result);
                PrintTotalsAndCoverageLines(result);
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
            WriteColored(result.ChangedFiles.ToString(), successColor);

            SysConsole.Write(". Findings: ");
            ConsoleColor totalColor = result.FindingCount == 0
                ? successColor
                : errorColor;
            WriteColored(result.FindingCount.ToString(), totalColor);
            SysConsole.Write(" documentation issue(s)");
            AppendStats(result);
            SysConsole.WriteLine(".");
            PrintSlocMetricsLine(result);
            PrintTotalsAndCoverageLines(result);
        }

        /// <summary>
        /// Appends detailed statistics (Errors, Warnings, Suggestions) to the current console output
        /// using a consistent colored format.
        /// </summary>
        /// <param name="result">
        /// The <see cref="RunResult"/> containing the statistic counters to print.
        /// </param>
        /// <remarks>
        /// Only values greater than zero are written.  
        /// The statistics are appended in parentheses and separated by commas, e.g.:
        /// (Errors: 2, Warnings: 1).
        /// If all counters are zero, nothing is appended.
        /// </remarks>
        private static void AppendStats(RunResult result)
        {
            bool first = true;

            AppendStat("Errors", result.ErrorCount, errorColor);
            AppendStat("Warnings", result.WarningCount, warningColor);
            AppendStat("Suggestions", result.SuggestionCount, suggestionColor);

            if (!first)
            {
                SysConsole.Write(")");
            }

            // Appends a severity statistic to the output if the value is greater than zero.
            // Handles proper comma separation and colored value rendering.
            void AppendStat(string label, int value, ConsoleColor color)
            {
                if (value <= 0)
                {
                    return;
                }

                SysConsole.Write(first ? " (" : ", ");
                SysConsole.Write(label);
                SysConsole.Write(": ");
                WriteColored(value.ToString(), color);
                first = false;
            }
        }

        #endregion
    }
}