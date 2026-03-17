using XMLDocNormalizer.Cli;
using XMLDocNormalizer.Configuration;
using XMLDocNormalizer.Models;
using XMLDocNormalizer.Models.DTO;
using XMLDocNormalizer.Reporting.Json;
using XMLDocNormalizer.Utils;

namespace XMLDocNormalizer.Execution
{
    /// <summary>
    /// Executes all exception analysis modes and produces a comparison report.
    /// </summary>
    internal static class ExceptionAnalysisModeComparisonRunner
    {
        /// <summary>
        /// Executes all exception analysis modes and writes a comparison report.
        /// </summary>
        /// <param name="options">The base tool options.</param>
        /// <returns>The comparison report DTO.</returns>
        public static ExceptionAnalysisModeComparisonReportDto Run(ToolOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);

            ExceptionComparisonExecutionResult executionResult =
                ToolRunner.RunComparison(options);

            RunMetricsDto sharedMetricsSource =
                RunMetricsCalculator.From(executionResult.SharedBaselineResult);

            Dictionary<string, int> sharedFindingCounts =
                CreateSharedFindingCounts(executionResult.Modes);

            List<ExceptionAnalysisModeRunDto> modeRuns = new();
            ExceptionAnalysisModeRunDto? directRun = null;

            foreach (ExceptionModeExecutionResult modeExecution in executionResult.Modes)
            {
                RunMetricsDto metrics = RunMetricsCalculator.From(modeExecution.Result);

                ExceptionAnalysisModeRunDto modeRun = CreateModeRunDto(
                    modeExecution,
                    metrics,
                    sharedFindingCounts);

                if (modeExecution.Mode == ExceptionAnalysisMode.Direct)
                {
                    directRun = modeRun;
                }

                modeRuns.Add(modeRun);
            }

            if (directRun != null)
            {
                foreach (ExceptionAnalysisModeRunDto modeRun in modeRuns)
                {
                    FillDeltaToDirect(modeRun, directRun);
                }
            }

            ExceptionAnalysisModeComparisonReportDto comparisonReport = new()
            {
                Tool = ToolMetadata.Name,
                Version = ToolMetadata.Version,
                GeneratedAtUtc = DateTime.UtcNow,
                TargetPath = options.TargetPath,
                SharedMetrics = CreateSharedMetrics(sharedMetricsSource),
                SharedFindingCounts = sharedFindingCounts,
                Timings = CreateTimings(executionResult),
                Modes = modeRuns
            };

            string comparisonOutputPath = ResolveComparisonOutputPath(options);
            ExceptionAnalysisModeComparisonReportWriter.Write(comparisonOutputPath, comparisonReport);

            PrintComparisonSummary(comparisonReport, comparisonOutputPath);

            return comparisonReport;
        }

        /// <summary>
        /// Creates the shared metrics block for the comparison report.
        /// </summary>
        /// <param name="metrics">The source metrics.</param>
        /// <returns>The shared metrics DTO.</returns>
        private static ExceptionAnalysisModeSharedMetricsDto CreateSharedMetrics(RunMetricsDto metrics)
        {
            return new ExceptionAnalysisModeSharedMetricsDto
            {
                Sloc = metrics.Sloc,
                Totals = new Dictionary<string, int>(metrics.Totals, StringComparer.Ordinal),
                Coverage = new Dictionary<string, double>(metrics.Coverage, StringComparer.Ordinal)
            };
        }

        /// <summary>
        /// Creates the timing DTO from the internal comparison execution result.
        /// </summary>
        /// <param name="executionResult">The internal comparison execution result.</param>
        /// <returns>The timing DTO.</returns>
        private static ExceptionAnalysisModeTimingDto CreateTimings(
            ExceptionComparisonExecutionResult executionResult)
        {
            ExceptionAnalysisModeTimingDto timings = new()
            {
                SharedDetectorsDurationMs = executionResult.SharedDetectorsDurationMs
            };

            foreach (ExceptionModeExecutionResult mode in executionResult.Modes)
            {
                switch (mode.Mode)
                {
                    case ExceptionAnalysisMode.Direct:
                        timings.DirectExceptionDurationMs = mode.ExceptionDetectorDurationMs;
                        break;

                    case ExceptionAnalysisMode.ProjectTransitive:
                        timings.ProjectTransitiveExceptionDurationMs = mode.ExceptionDetectorDurationMs;
                        break;

                    case ExceptionAnalysisMode.ProjectTransitiveProjectExceptions:
                        timings.ProjectTransitiveProjectExceptionsExceptionDurationMs = mode.ExceptionDetectorDurationMs;
                        break;

                    case ExceptionAnalysisMode.SolutionTransitive:
                        timings.SolutionTransitiveExceptionDurationMs = mode.ExceptionDetectorDurationMs;
                        break;
                }
            }

            return timings;
        }

        /// <summary>
        /// Creates smell counts that are identical across all compared modes.
        /// </summary>
        /// <param name="modeExecutions">The per-mode execution results.</param>
        /// <returns>The shared finding counts.</returns>
        private static Dictionary<string, int> CreateSharedFindingCounts(
            IReadOnlyList<ExceptionModeExecutionResult> modeExecutions)
        {
            Dictionary<string, int> shared = new(StringComparer.Ordinal);

            if (modeExecutions.Count == 0)
            {
                return shared;
            }

            RunMetricsDto firstMetrics = RunMetricsCalculator.From(modeExecutions[0].Result);

            foreach (KeyValuePair<string, int> pair in firstMetrics.TotalFindingCounts)
            {
                bool identicalInAllModes = true;

                for (int i = 1; i < modeExecutions.Count; i++)
                {
                    RunMetricsDto otherMetrics = RunMetricsCalculator.From(modeExecutions[i].Result);

                    if (!otherMetrics.TotalFindingCounts.TryGetValue(pair.Key, out int otherCount) ||
                        otherCount != pair.Value)
                    {
                        identicalInAllModes = false;
                        break;
                    }
                }

                if (identicalInAllModes)
                {
                    shared[pair.Key] = pair.Value;
                }
            }

            return shared;
        }

        /// <summary>
        /// Creates one mode run DTO.
        /// </summary>
        /// <param name="execution">The internal execution result for one mode.</param>
        /// <param name="metrics">The calculated mode metrics.</param>
        /// <param name="sharedFindingCounts">The finding counts shared by all modes.</param>
        /// <returns>The mode run DTO.</returns>
        private static ExceptionAnalysisModeRunDto CreateModeRunDto(
            ExceptionModeExecutionResult execution,
            RunMetricsDto metrics,
            Dictionary<string, int> sharedFindingCounts)
        {
            int doc610 = GetSmellCount(metrics, "DOC610");
            int doc611 = GetSmellCount(metrics, "DOC611");
            int doc620 = GetSmellCount(metrics, "DOC620");
            int doc630 = GetSmellCount(metrics, "DOC630");
            int doc631 = GetSmellCount(metrics, "DOC631");
            int doc632 = GetSmellCount(metrics, "DOC632");
            int doc640 = GetSmellCount(metrics, "DOC640");
            int doc660 = GetSmellCount(metrics, "DOC660");
            int doc670 = GetSmellCount(metrics, "DOC670");
            int doc680 = GetSmellCount(metrics, "DOC680");

            int exceptionFindingCount =
                doc610 + doc611 + doc620 + doc630 + doc631 + doc632 + doc640 + doc660 + doc670 + doc680;

            double exceptionFindingsPerKLoc = metrics.Sloc > 0
                ? exceptionFindingCount / (metrics.Sloc / 1000.0)
                : 0.0;

            double doc631Share = exceptionFindingCount > 0
                ? doc631 / (double)exceptionFindingCount
                : 0.0;

            Dictionary<string, int> modeFindingCounts = new(StringComparer.Ordinal);
            foreach (KeyValuePair<string, int> pair in metrics.TotalFindingCounts)
            {
                if (!sharedFindingCounts.TryGetValue(pair.Key, out int sharedValue) ||
                    sharedValue != pair.Value)
                {
                    modeFindingCounts[pair.Key] = pair.Value;
                }
            }

            return new ExceptionAnalysisModeRunDto
            {
                Mode = execution.Mode,
                ReportPath = execution.ReportPath,
                FindingCount = metrics.FindingCount,
                ErrorCount = metrics.ErrorCount,
                WarningCount = metrics.WarningCount,
                SuggestionCount = metrics.SuggestionCount,
                ChangedFiles = metrics.ChangedFiles,
                FindingsPerKLoc = metrics.FindingsPerKLoc,
                ErrorsPerKLoc = metrics.ErrorsPerKLoc,
                WarningsPerKLoc = metrics.WarningsPerKLoc,
                SuggestionsPerKLoc = metrics.SuggestionsPerKLoc,
                ModeFindingCounts = modeFindingCounts,
                ExceptionFindingCount = exceptionFindingCount,
                ExceptionFindingsPerKLoc = exceptionFindingsPerKLoc,
                Doc610Count = doc610,
                Doc611Count = doc611,
                Doc620Count = doc620,
                Doc630Count = doc630,
                Doc631Count = doc631,
                Doc632Count = doc632,
                Doc640Count = doc640,
                Doc660Count = doc660,
                Doc670Count = doc670,
                Doc680Count = doc680,
                Doc631Share = doc631Share
            };
        }

        /// <summary>
        /// Fills the delta-to-direct values for one mode.
        /// </summary>
        /// <param name="current">The current mode run.</param>
        /// <param name="direct">The direct baseline.</param>
        private static void FillDeltaToDirect(
            ExceptionAnalysisModeRunDto current,
            ExceptionAnalysisModeRunDto direct)
        {
            current.FindingCountDeltaToDirect = current.FindingCount - direct.FindingCount;
            current.FindingsPerKLocDeltaToDirect = current.FindingsPerKLoc - direct.FindingsPerKLoc;
            current.ExceptionFindingCountDeltaToDirect = current.ExceptionFindingCount - direct.ExceptionFindingCount;
            current.ExceptionFindingsPerKLocDeltaToDirect =
                current.ExceptionFindingsPerKLoc - direct.ExceptionFindingsPerKLoc;
            current.Doc611CountDeltaToDirect = current.Doc611Count - direct.Doc611Count;
            current.Doc631CountDeltaToDirect = current.Doc631Count - direct.Doc631Count;
            current.Doc632CountDeltaToDirect = current.Doc632Count - direct.Doc632Count;
            current.FindingAmplificationFactorToDirect =
                CalculateFactor(current.FindingCount, direct.FindingCount);
            current.ExceptionFindingAmplificationFactorToDirect =
                CalculateFactor(current.ExceptionFindingCount, direct.ExceptionFindingCount);
        }

        /// <summary>
        /// Calculates a factor relative to a baseline count.
        /// </summary>
        /// <param name="current">The current value.</param>
        /// <param name="baseline">The baseline value.</param>
        /// <returns>The factor relative to the baseline, or 0 when the baseline is 0.</returns>
        private static double CalculateFactor(int current, int baseline)
        {
            if (baseline <= 0)
            {
                return 0.0;
            }

            return current / (double)baseline;
        }

        /// <summary>
        /// Gets the count of a smell identifier from the metrics snapshot.
        /// </summary>
        /// <param name="metrics">The metrics snapshot.</param>
        /// <param name="smellId">The smell identifier.</param>
        /// <returns>The stored count, or 0 if absent.</returns>
        private static int GetSmellCount(RunMetricsDto metrics, string smellId)
        {
            if (!metrics.TotalFindingCounts.TryGetValue(smellId, out int count))
            {
                return 0;
            }

            return count;
        }

        /// <summary>
        /// Resolves the output path of the comparison report.
        /// </summary>
        /// <param name="options">The tool options.</param>
        /// <returns>The comparison report path.</returns>
        private static string ResolveComparisonOutputPath(ToolOptions options)
        {
            if (string.IsNullOrWhiteSpace(options.OutputPath))
            {
                return "artifacts/exception-analysis-mode-comparison.json";
            }

            string directory = Path.GetDirectoryName(options.OutputPath) ?? string.Empty;
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(options.OutputPath);
            string extension = Path.GetExtension(options.OutputPath);

            if (string.IsNullOrWhiteSpace(extension))
            {
                extension = ".json";
            }

            fileNameWithoutExtension = RemoveKnownModeSuffix(fileNameWithoutExtension);

            string fileName = $"{fileNameWithoutExtension}_exception-analysis-mode-comparison.json";
            return string.IsNullOrWhiteSpace(directory)
                ? fileName
                : Path.Combine(directory, fileName);
        }

        /// <summary>
        /// Removes one known exception-mode suffix from a file name without extension.
        /// </summary>
        /// <param name="fileNameWithoutExtension">The file name without extension.</param>
        /// <returns>The normalized base file name.</returns>
        private static string RemoveKnownModeSuffix(string fileNameWithoutExtension)
        {
            string[] suffixes =
            [
                "_direct",
                "_project-transitive",
                "_project-transitive-project-exceptions",
                "_solution-transitive"
            ];

            foreach (string suffix in suffixes)
            {
                if (fileNameWithoutExtension.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                {
                    return fileNameWithoutExtension[..^suffix.Length];
                }
            }

            return fileNameWithoutExtension;
        }

        /// <summary>
        /// Prints a compact console summary of the comparison run.
        /// </summary>
        /// <param name="report">The comparison report.</param>
        /// <param name="outputPath">The written output path.</param>
        private static void PrintComparisonSummary(
            ExceptionAnalysisModeComparisonReportDto report,
            string outputPath)
        {
            Console.WriteLine();
            Console.WriteLine("Exception analysis mode comparison");
            Console.WriteLine("----------------------------------");

            foreach (ExceptionAnalysisModeRunDto modeRun in report.Modes)
            {
                Console.WriteLine(
                    $"{modeRun.Mode}: Findings={modeRun.FindingCount}, " +
                    $"Findings/KLOC={modeRun.FindingsPerKLoc:F2}, " +
                    $"ExceptionFindings={modeRun.ExceptionFindingCount}, " +
                    $"DOC611={modeRun.Doc611Count}, " +
                    $"DOC631={modeRun.Doc631Count}, " +
                    $"DOC632={modeRun.Doc632Count}");
            }

            Console.WriteLine();
            Console.WriteLine("Timings");
            Console.WriteLine("-------");
            Console.WriteLine($"Shared detectors: {report.Timings.SharedDetectorsDurationMs} ms");
            Console.WriteLine($"Direct exception detector: {report.Timings.DirectExceptionDurationMs} ms");
            Console.WriteLine($"ProjectTransitive exception detector: {report.Timings.ProjectTransitiveExceptionDurationMs} ms");
            Console.WriteLine($"ProjectTransitiveProjectExceptions exception detector: {report.Timings.ProjectTransitiveProjectExceptionsExceptionDurationMs} ms");
            Console.WriteLine($"SolutionTransitive exception detector: {report.Timings.SolutionTransitiveExceptionDurationMs} ms");

            Console.WriteLine();
            Console.WriteLine($"Comparison report written to: {outputPath}");
        }
    }
}
