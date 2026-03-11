using XMLDocNormalizer.Models;
using XMLDocNormalizer.Models.Dto;
using XMLDocNormalizer.Models.Keys;
using XMLDocNormalizer.Utils;

namespace XMLDocNormalizer.Cli.Output
{
    /// <summary>
    /// Writes human-readable run summaries to the console.
    /// </summary>
    /// <remarks>
    /// This component is responsible for rendering the final aggregated
    /// <see cref="RunResult"/> for check and fix runs.
    /// It is intentionally separate from operational logging and from
    /// machine-readable findings reporters such as JSON or SARIF.
    /// </remarks>
    internal static class RunResultConsoleReporter
    {
        /// <summary>
        /// Writes the final result of a check run to the console.
        /// </summary>
        /// <param name="result">The aggregated run result.</param>
        /// <param name="options">The tool options.</param>
        public static void ReportCheckRunResult(RunResult result, ToolOptions options)
        {
            ConsoleLogger.EndProgress();

            RunMetricsDto metrics = RunMetricsCalculator.From(result);

            if (result.FindingCount == 0)
            {
                WriteColored("Check succeeded", ConsoleColors.Success);
                Console.WriteLine(": no documentation issues found.");
                return;
            }

            WriteColored("Check failed", ConsoleColors.Error);
            Console.Write(": ");
            WriteColored(result.FindingCount.ToString(), ConsoleColors.Error);
            Console.Write(" documentation issue(s) found");
            AppendStats(result);
            Console.WriteLine(".");

            PrintSlocMetricsLine(metrics);

            if (options.Verbose)
            {
                PrintTotalsAndCoverageLines(metrics);
                PrintSmellCountLines(result);
            }
        }

        /// <summary>
        /// Writes the final result of a fix run to the console.
        /// </summary>
        /// <param name="result">The aggregated run result.</param>
        /// <param name="options">The tool options.</param>
        public static void ReportFixRunResult(RunResult result, ToolOptions options)
        {
            ConsoleLogger.EndProgress();

            RunMetricsDto metrics = RunMetricsCalculator.From(result);

            Console.Write("Done. Changed files: ");
            WriteColored(result.ChangedFiles.ToString(), ConsoleColors.Success);

            Console.Write(". Findings: ");
            ConsoleColor findingsColor = result.FindingCount == 0 ? ConsoleColors.Success : ConsoleColors.Error;
            WriteColored(result.FindingCount.ToString(), findingsColor);
            Console.Write(" documentation issue(s)");
            AppendStats(result);
            Console.WriteLine(".");

            PrintSlocMetricsLine(metrics);

            if (options.Verbose)
            {
                PrintTotalsAndCoverageLines(metrics);
                PrintSmellCountLines(result);
            }
        }

        /// <summary>
        /// Writes a line containing SLOC and density metrics per KLOC.
        /// </summary>
        /// <param name="metrics">The precomputed run metrics snapshot.</param>
        private static void PrintSlocMetricsLine(RunMetricsDto metrics)
        {
            if (metrics.Sloc <= 0)
            {
                return;
            }

            Console.Write("SLOC: ");
            WriteColored(metrics.Sloc.ToString("N0"), ConsoleColors.Info);

            Console.Write(" | Findings/KLOC: ");
            ConsoleColor findingsColor = metrics.FindingCount == 0 ? ConsoleColors.Success : ConsoleColors.Error;
            WriteColored(metrics.FindingsPerKLoc.ToString("0.00"), findingsColor);

            if (metrics.ErrorCount > 0)
            {
                Console.Write(" | Errors/KLOC: ");
                WriteColored(metrics.ErrorsPerKLoc.ToString("0.00"), ConsoleColors.Error);
            }

            if (metrics.WarningCount > 0)
            {
                Console.Write(" | Warnings/KLOC: ");
                WriteColored(metrics.WarningsPerKLoc.ToString("0.00"), ConsoleColors.Warning);
            }

            if (metrics.SuggestionCount > 0)
            {
                Console.Write(" | Suggestions/KLOC: ");
                WriteColored(metrics.SuggestionsPerKLoc.ToString("0.00"), ConsoleColors.Suggestion);
            }

            Console.WriteLine();
        }

        /// <summary>
        /// Writes totals and coverage ratios derived from the run result.
        /// </summary>
        /// <param name="metrics">The precomputed run metrics snapshot.</param>
        private static void PrintTotalsAndCoverageLines(RunMetricsDto metrics)
        {
            if (metrics.Sloc <= 0)
            {
                return;
            }

            if (metrics.Totals.Count > 0)
            {
                Console.Write("Totals: ");
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

                Console.WriteLine();
            }

            if (metrics.Coverage.Count > 0)
            {
                Console.Write("Coverage: ");
                bool firstCoverage = true;

                WriteCoverageIfPresent(metrics, ref firstCoverage, "Param missing", CoverageKeys.ParamMissingTagRate);
                WriteCoverageIfPresent(metrics, ref firstCoverage, "Param empty", CoverageKeys.ParamEmptyDescriptionRate);
                WriteCoverageIfPresent(metrics, ref firstCoverage, "TypeParam missing", CoverageKeys.TypeParamMissingTagRate);
                WriteCoverageIfPresent(metrics, ref firstCoverage, "TypeParam empty", CoverageKeys.TypeParamEmptyDescriptionRate);
                WriteCoverageIfPresent(metrics, ref firstCoverage, "Returns missing", CoverageKeys.ReturnsMissingRate);
                WriteCoverageIfPresent(metrics, ref firstCoverage, "Namespace central missing", CoverageKeys.NamespaceCentralDocMissingRate);

                Console.WriteLine();
            }
        }

        /// <summary>
        /// Writes the most frequent smell counts to the console.
        /// </summary>
        /// <param name="result">The aggregated run result.</param>
        private static void PrintSmellCountLines(RunResult result)
        {
            const int maxEntries = 10;

            if (result.SmellCounts.Count == 0)
            {
                return;
            }

            List<KeyValuePair<string, int>> smells =
                new List<KeyValuePair<string, int>>(result.SmellCounts);

            smells.Sort(static (left, right) =>
            {
                return right.Value.CompareTo(left.Value);
            });

            Console.WriteLine("Top smell counts:");

            int printed = 0;

            foreach (KeyValuePair<string, int> pair in smells)
            {
                if (printed >= maxEntries)
                {
                    break;
                }

                Console.Write("  ");
                Console.Write(pair.Key);
                Console.Write(": ");
                Console.WriteLine(pair.Value.ToString("N0"));

                printed++;
            }

            int remaining = smells.Count - printed;

            if (remaining > 0)
            {
                Console.Write("  ... and ");
                Console.Write(remaining);
                Console.WriteLine(" more smell types");
            }
        }

        /// <summary>
        /// Appends severity counters in parentheses to the current line.
        /// </summary>
        /// <param name="result">The aggregated run result.</param>
        private static void AppendStats(RunResult result)
        {
            bool first = true;

            AppendStat("Errors", result.ErrorCount, ConsoleColors.Error);
            AppendStat("Warnings", result.WarningCount, ConsoleColors.Warning);
            AppendStat("Suggestions", result.SuggestionCount, ConsoleColors.Suggestion);

            if (!first)
            {
                Console.Write(")");
            }

            void AppendStat(string label, int value, ConsoleColor color)
            {
                if (value <= 0)
                {
                    return;
                }

                Console.Write(first ? " (" : ", ");
                Console.Write(label);
                Console.Write(": ");
                WriteColored(value.ToString(), color);
                first = false;
            }
        }

        /// <summary>
        /// Writes a totals entry if the corresponding value exists and is greater than zero.
        /// </summary>
        /// <param name="metrics">The run metrics snapshot.</param>
        /// <param name="first">Whether this is the first entry on the line.</param>
        /// <param name="label">The label to print.</param>
        /// <param name="key">The totals key.</param>
        private static void WriteTotalIfPresent(
            RunMetricsDto metrics,
            ref bool first,
            string label,
            string key)
        {
            if (!metrics.Totals.TryGetValue(key, out int value) || value <= 0)
            {
                return;
            }

            if (!first)
            {
                Console.Write(" | ");
            }

            Console.Write(label);
            Console.Write(": ");
            Console.Write(value.ToString("N0"));

            first = false;
        }

        /// <summary>
        /// Writes a coverage entry if the corresponding value exists.
        /// </summary>
        /// <param name="metrics">The run metrics snapshot.</param>
        /// <param name="first">Whether this is the first entry on the line.</param>
        /// <param name="label">The label to print.</param>
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
                Console.Write(" | ");
            }

            Console.Write(label);
            Console.Write(": ");
            Console.Write((value * 100.0).ToString("0.00"));
            Console.Write("%");

            first = false;
        }

        /// <summary>
        /// Writes text in a given color unless output is redirected.
        /// </summary>
        /// <param name="text">The text to write.</param>
        /// <param name="color">The color to use.</param>
        private static void WriteColored(string text, ConsoleColor color)
        {
            if (Console.IsOutputRedirected)
            {
                Console.Write(text);
                return;
            }

            ConsoleColor previousColor = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.Write(text);
            Console.ForegroundColor = previousColor;
        }
    }
}