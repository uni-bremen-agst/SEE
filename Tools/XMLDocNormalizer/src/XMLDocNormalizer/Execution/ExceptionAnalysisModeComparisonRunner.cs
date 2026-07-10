using System.Diagnostics;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
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
    /// <remarks>
    /// Comparison mode executes each exception analysis mode in an isolated child process.
    /// This avoids sharing Roslyn workspaces, semantic models, JIT state and analyzer caches between modes.
    /// </remarks>
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

            List<IsolatedModeExecutionResult> modeExecutions = ExecuteModesInIsolatedProcesses(options);

            Dictionary<string, int> sharedFindingCounts =
                CreateSharedFindingCounts(modeExecutions);

            List<ExceptionAnalysisModeRunDto> modeRuns = new();
            ExceptionAnalysisModeRunDto? directRun = null;

            foreach (IsolatedModeExecutionResult modeExecution in modeExecutions)
            {
                ExceptionAnalysisModeRunDto modeRun = CreateModeRunDto(
                    modeExecution,
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

            RunMetricsDto sharedMetricsSource = modeExecutions.Count > 0
                ? modeExecutions[0].Report.Metrics
                : new RunMetricsDto();

            ExceptionAnalysisModeComparisonReportDto comparisonReport = new()
            {
                Tool = ToolMetadata.Name,
                Version = ToolMetadata.Version,
                GeneratedAtUtc = DateTime.UtcNow,
                TargetPath = options.TargetPath,
                SharedMetrics = CreateSharedMetrics(sharedMetricsSource),
                SharedFindingCounts = sharedFindingCounts,
                Timings = CreateTimings(modeExecutions),
                Modes = modeRuns
            };

            string comparisonOutputPath = ResolveComparisonOutputPath(options);
            ExceptionAnalysisModeComparisonReportWriter.Write(comparisonOutputPath, comparisonReport);

            PrintComparisonSummary(comparisonReport, comparisonOutputPath);

            return comparisonReport;
        }

        /// <summary>
        /// Executes every exception analysis mode in a dedicated child process.
        /// </summary>
        /// <param name="options">The base comparison options.</param>
        /// <returns>The isolated mode execution results.</returns>
        private static List<IsolatedModeExecutionResult> ExecuteModesInIsolatedProcesses(ToolOptions options)
        {
            List<IsolatedModeExecutionResult> results = new();

            Console.WriteLine("Project/solution detected. Running comparison with isolated child processes.");

            foreach (ExceptionAnalysisMode mode in GetComparisonModes())
            {
                string reportPath = ResolveModeReportPath(options, mode);

                ProcessStartInfo startInfo = CreateModeProcessStartInfo(
                    options,
                    mode,
                    reportPath);

                Console.WriteLine($"Running isolated mode: {mode}");

                Stopwatch stopwatch = Stopwatch.StartNew();
                using Process process = StartProcess(startInfo);

                string standardOutput = process.StandardOutput.ReadToEnd();
                string standardError = process.StandardError.ReadToEnd();

                process.WaitForExit();
                stopwatch.Stop();

                if (!IsAcceptedChildExitCode(process.ExitCode))
                {
                    throw CreateChildProcessException(
                        mode,
                        process.ExitCode,
                        standardOutput,
                        standardError);
                }

                JsonReport report = ReadModeReport(reportPath);

                results.Add(new IsolatedModeExecutionResult
                {
                    Mode = mode,
                    ReportPath = reportPath,
                    Report = report,
                    WallClockDurationMs = stopwatch.ElapsedMilliseconds,
                    ProcessExitCode = process.ExitCode
                });
            }

            return results;
        }

        /// <summary>
        /// Starts a child process for one isolated mode run.
        /// </summary>
        /// <param name="startInfo">The configured process start information.</param>
        /// <returns>The started process.</returns>
        private static Process StartProcess(ProcessStartInfo startInfo)
        {
            Process? process = Process.Start(startInfo);

            if (process == null)
            {
                throw new InvalidOperationException("Could not start isolated exception comparison child process.");
            }

            return process;
        }

        /// <summary>
        /// Creates process start information for one isolated mode run.
        /// </summary>
        /// <param name="options">The base comparison options.</param>
        /// <param name="mode">The exception analysis mode to execute.</param>
        /// <param name="reportPath">The JSON report path for the child process.</param>
        /// <returns>The configured process start information.</returns>
        private static ProcessStartInfo CreateModeProcessStartInfo(
            ToolOptions options,
            ExceptionAnalysisMode mode,
            string reportPath)
        {
            string toolAssemblyPath = ResolveToolAssemblyPath();
            string fileName;
            List<string> leadingArguments = new();

            if (toolAssemblyPath.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            {
                fileName = ResolveDotNetHostPath();
                leadingArguments.Add(toolAssemblyPath);
            }
            else
            {
                fileName = toolAssemblyPath;
            }

            ProcessStartInfo startInfo = new()
            {
                FileName = fileName,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WorkingDirectory = Environment.CurrentDirectory
            };

            foreach (string argument in leadingArguments)
            {
                startInfo.ArgumentList.Add(argument);
            }

            foreach (string argument in CreateChildToolArguments(options, mode, reportPath))
            {
                startInfo.ArgumentList.Add(argument);
            }

            return startInfo;
        }

        /// <summary>
        /// Resolves the XMLDocNormalizer assembly path used for child processes.
        /// </summary>
        /// <returns>The current tool assembly path.</returns>
        private static string ResolveToolAssemblyPath()
        {
            string assemblyPath = typeof(ExceptionAnalysisModeComparisonRunner).Assembly.Location;

            if (string.IsNullOrWhiteSpace(assemblyPath))
            {
                throw new InvalidOperationException("Could not resolve XMLDocNormalizer assembly path.");
            }

            return assemblyPath;
        }

        /// <summary>
        /// Resolves the dotnet host executable used to launch framework-dependent child processes.
        /// </summary>
        /// <returns>The dotnet host path or command name.</returns>
        private static string ResolveDotNetHostPath()
        {
            string? processPath = Environment.ProcessPath;

            if (!string.IsNullOrWhiteSpace(processPath) && IsDotNetHost(processPath))
            {
                return processPath;
            }

            return "dotnet";
        }

        /// <summary>
        /// Determines whether a process path points to the dotnet host.
        /// </summary>
        /// <param name="processPath">The process path to inspect.</param>
        /// <returns>True if the process path points to dotnet; otherwise false.</returns>
        private static bool IsDotNetHost(string processPath)
        {
            string fileName = Path.GetFileNameWithoutExtension(processPath);
            return fileName.Equals("dotnet", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Creates the child tool arguments for one isolated mode run.
        /// </summary>
        /// <param name="options">The base comparison options.</param>
        /// <param name="mode">The exception analysis mode to execute.</param>
        /// <param name="reportPath">The JSON report path for the child process.</param>
        /// <returns>The child process argument list.</returns>
        private static List<string> CreateChildToolArguments(
            ToolOptions options,
            ExceptionAnalysisMode mode,
            string reportPath)
        {
            List<string> arguments = new()
            {
                "--check",
                "--format",
                "json",
                "--output",
                reportPath,
                "--exception-analysis-mode",
                ToCommandLineValue(mode)
            };

            if (options.FullAnalysis)
            {
                arguments.Add("--full");
            }

            if (!string.IsNullOrWhiteSpace(options.ProjectName))
            {
                arguments.Add("--project");
                arguments.Add(options.ProjectName);
            }

            if (options.IncludeGenerated)
            {
                arguments.Add("--include-generated");
            }

            if (options.IncludeTests)
            {
                arguments.Add("--include-tests");
            }

            if (!options.XmlDocOptions.CheckEnumMembers)
            {
                arguments.Add("--no-check-enum-members");
            }

            if (!options.XmlDocOptions.RequireSummaryForFields)
            {
                arguments.Add("--no-require-field-summary");
            }

            arguments.Add(options.TargetPath);

            return arguments;
        }

        /// <summary>
        /// Returns the stable comparison mode order.
        /// </summary>
        /// <returns>The comparison modes in display and execution order.</returns>
        private static IReadOnlyList<ExceptionAnalysisMode> GetComparisonModes()
        {
            return new[]
            {
                ExceptionAnalysisMode.Direct,
                ExceptionAnalysisMode.ProjectTransitive,
                ExceptionAnalysisMode.ProjectTransitiveDeclaredExceptions,
                ExceptionAnalysisMode.SolutionTransitive
            };
        }

        /// <summary>
        /// Converts an exception analysis mode to its canonical command-line value.
        /// </summary>
        /// <param name="mode">The exception analysis mode.</param>
        /// <returns>The canonical command-line value.</returns>
        private static string ToCommandLineValue(ExceptionAnalysisMode mode)
        {
            return mode switch
            {
                ExceptionAnalysisMode.Direct => "direct",
                ExceptionAnalysisMode.ProjectTransitive => "project-transitive",
                ExceptionAnalysisMode.ProjectTransitiveDeclaredExceptions => "project-transitive-declared-exceptions",
                ExceptionAnalysisMode.SolutionTransitive => "solution-transitive",
                _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unknown exception analysis mode.")
            };
        }

        /// <summary>
        /// Determines whether a child process exit code represents a completed check run.
        /// </summary>
        /// <param name="exitCode">The child process exit code.</param>
        /// <returns>True if the exit code is accepted; otherwise false.</returns>
        private static bool IsAcceptedChildExitCode(int exitCode)
        {
            return exitCode == ToolExitCodes.Success ||
                   exitCode == ToolExitCodes.Findings;
        }

        /// <summary>
        /// Creates an exception that includes child process output for diagnostics.
        /// </summary>
        /// <param name="mode">The mode that failed.</param>
        /// <param name="exitCode">The child process exit code.</param>
        /// <param name="standardOutput">The captured standard output.</param>
        /// <param name="standardError">The captured standard error.</param>
        /// <returns>The exception to throw.</returns>
        private static InvalidOperationException CreateChildProcessException(
            ExceptionAnalysisMode mode,
            int exitCode,
            string standardOutput,
            string standardError)
        {
            string message =
                $"Isolated exception comparison run for mode {mode} failed with exit code {exitCode}." +
                Environment.NewLine +
                "Standard output:" +
                Environment.NewLine +
                standardOutput +
                Environment.NewLine +
                "Standard error:" +
                Environment.NewLine +
                standardError;

            return new InvalidOperationException(message);
        }

        /// <summary>
        /// Reads one child process JSON report.
        /// </summary>
        /// <param name="reportPath">The report path.</param>
        /// <returns>The deserialized JSON report.</returns>
        private static JsonReport ReadModeReport(string reportPath)
        {
            if (!File.Exists(reportPath))
            {
                throw new FileNotFoundException("The isolated mode JSON report was not written.", reportPath);
            }

            JsonSerializerOptions serializerOptions = CreateJsonSerializerOptions();
            string json = File.ReadAllText(reportPath);

            JsonReport? report = JsonSerializer.Deserialize<JsonReport>(json, serializerOptions);

            if (report == null)
            {
                throw new InvalidOperationException($"Could not read isolated mode JSON report: {reportPath}");
            }

            return report;
        }

        /// <summary>
        /// Creates JSON serializer options for reading child reports.
        /// </summary>
        /// <returns>The serializer options.</returns>
        private static JsonSerializerOptions CreateJsonSerializerOptions()
        {
            return new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                Converters = { new JsonStringEnumConverter() }
            };
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
        /// Creates the timing DTO from the isolated process results.
        /// </summary>
        /// <param name="modeExecutions">The isolated mode execution results.</param>
        /// <returns>The timing DTO.</returns>
        private static ExceptionAnalysisModeTimingDto CreateTimings(
            IReadOnlyList<IsolatedModeExecutionResult> modeExecutions)
        {
            ExceptionAnalysisModeTimingDto timings = new()
            {
                ExecutionIsolation = "Process",
                ModeOrderStrategy = "Fixed",
                IncludesProcessStartup = true,
                SharedDetectorsDurationMs = 0
            };

            foreach (IsolatedModeExecutionResult mode in modeExecutions)
            {
                long reportedAnalysisDurationMs = mode.Report.Metrics.AnalysisDurationMs;

                switch (mode.Mode)
                {
                    case ExceptionAnalysisMode.Direct:
                        timings.DirectExceptionDurationMs = reportedAnalysisDurationMs;
                        timings.DirectReportedAnalysisDurationMs = reportedAnalysisDurationMs;
                        timings.DirectWallClockDurationMs = mode.WallClockDurationMs;
                        break;

                    case ExceptionAnalysisMode.ProjectTransitive:
                        timings.ProjectTransitiveExceptionDurationMs = reportedAnalysisDurationMs;
                        timings.ProjectTransitiveReportedAnalysisDurationMs = reportedAnalysisDurationMs;
                        timings.ProjectTransitiveWallClockDurationMs = mode.WallClockDurationMs;
                        break;

                    case ExceptionAnalysisMode.ProjectTransitiveDeclaredExceptions:
                        timings.ProjectTransitiveDeclaredExceptionsExceptionDurationMs = reportedAnalysisDurationMs;
                        timings.ProjectTransitiveDeclaredExceptionsReportedAnalysisDurationMs = reportedAnalysisDurationMs;
                        timings.ProjectTransitiveDeclaredExceptionsWallClockDurationMs = mode.WallClockDurationMs;
                        break;

                    case ExceptionAnalysisMode.SolutionTransitive:
                        timings.SolutionTransitiveExceptionDurationMs = reportedAnalysisDurationMs;
                        timings.SolutionTransitiveReportedAnalysisDurationMs = reportedAnalysisDurationMs;
                        timings.SolutionTransitiveWallClockDurationMs = mode.WallClockDurationMs;
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
            IReadOnlyList<IsolatedModeExecutionResult> modeExecutions)
        {
            Dictionary<string, int> shared = new(StringComparer.Ordinal);

            if (modeExecutions.Count == 0)
            {
                return shared;
            }

            RunMetricsDto firstMetrics = modeExecutions[0].Report.Metrics;

            foreach (KeyValuePair<string, int> pair in firstMetrics.TotalFindingCounts)
            {
                bool identicalInAllModes = true;

                for (int i = 1; i < modeExecutions.Count; i++)
                {
                    RunMetricsDto otherMetrics = modeExecutions[i].Report.Metrics;

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
        /// <param name="execution">The isolated execution result for one mode.</param>
        /// <param name="sharedFindingCounts">The finding counts shared by all modes.</param>
        /// <returns>The mode run DTO.</returns>
        private static ExceptionAnalysisModeRunDto CreateModeRunDto(
            IsolatedModeExecutionResult execution,
            Dictionary<string, int> sharedFindingCounts)
        {
            RunMetricsDto metrics = execution.Report.Metrics;

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

            double exceptionFindingsPerKSloc = metrics.Sloc > 0
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
                ReportedAnalysisDurationMs = metrics.AnalysisDurationMs,
                WallClockDurationMs = execution.WallClockDurationMs,
                FindingCount = metrics.FindingCount,
                ErrorCount = metrics.ErrorCount,
                WarningCount = metrics.WarningCount,
                SuggestionCount = metrics.SuggestionCount,
                ChangedFiles = metrics.ChangedFiles,
                FindingsPerKSloc = metrics.FindingsPerKSloc,
                ErrorsPerKSloc = metrics.ErrorsPerKSloc,
                WarningsPerKSloc = metrics.WarningsPerKSloc,
                SuggestionsPerKSloc = metrics.SuggestionsPerKSloc,
                ModeFindingCounts = modeFindingCounts,
                ExceptionFindingCount = exceptionFindingCount,
                ExceptionFindingsPerKSloc = exceptionFindingsPerKSloc,
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
            current.FindingsPerKSlocDeltaToDirect = current.FindingsPerKSloc - direct.FindingsPerKSloc;
            current.ExceptionFindingCountDeltaToDirect = current.ExceptionFindingCount - direct.ExceptionFindingCount;
            current.ExceptionFindingsPerKSlocDeltaToDirect =
                current.ExceptionFindingsPerKSloc - direct.ExceptionFindingsPerKSloc;
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
        /// Resolves the per-mode JSON report path for comparison outputs.
        /// </summary>
        /// <param name="baseOptions">The base comparison options.</param>
        /// <param name="mode">The target exception analysis mode.</param>
        /// <returns>The per-mode JSON report path.</returns>
        private static string ResolveModeReportPath(
            ToolOptions baseOptions,
            ExceptionAnalysisMode mode)
        {
            string basePath = baseOptions.OutputPath ?? "artifacts/exception-mode-comparison.json";
            string directory = Path.GetDirectoryName(basePath) ?? string.Empty;
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(basePath);

            fileNameWithoutExtension = RemoveKnownModeSuffix(fileNameWithoutExtension);

            string fileName = $"{fileNameWithoutExtension}_{ToCommandLineValue(mode)}.json";
            return string.IsNullOrWhiteSpace(directory)
                ? fileName
                : Path.Combine(directory, fileName);
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
                "_project-transitive-declared-exceptions",
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
            Console.WriteLine("Execution: isolated child processes");
            Console.WriteLine("Timing: wall-clock process duration / reported analysis duration");
            Console.WriteLine();

            foreach (ExceptionAnalysisModeRunDto modeRun in report.Modes)
            {
                Console.WriteLine(
                    $"{modeRun.Mode}: Findings={modeRun.FindingCount}, " +
                    $"Findings/KLOC={modeRun.FindingsPerKSloc:F2}, " +
                    $"ExceptionFindings={modeRun.ExceptionFindingCount}, " +
                    $"DOC610={modeRun.Doc610Count}, " +
                    $"DOC611={modeRun.Doc611Count}, " +
                    $"DOC630={modeRun.Doc630Count}, " +
                    $"DOC631={modeRun.Doc631Count}, " +
                    $"DOC632={modeRun.Doc632Count}, " +
                    $"WallClock={modeRun.WallClockDurationMs} ms, " +
                    $"Analysis={modeRun.ReportedAnalysisDurationMs} ms");
            }

            Console.WriteLine();
            Console.WriteLine("Timings");
            Console.WriteLine("-------");
            Console.WriteLine($"Execution isolation: {report.Timings.ExecutionIsolation}");
            Console.WriteLine($"Includes process startup: {report.Timings.IncludesProcessStartup}");
            Console.WriteLine($"Mode order strategy: {report.Timings.ModeOrderStrategy}");

            foreach (ExceptionAnalysisModeRunDto modeRun in report.Modes)
            {
                Console.WriteLine(
                    $"{modeRun.Mode}: WallClock={modeRun.WallClockDurationMs} ms, " +
                    $"ReportedAnalysis={modeRun.ReportedAnalysisDurationMs} ms");
            }

            Console.WriteLine();
            Console.WriteLine($"Comparison report written to: {outputPath}");
        }

        /// <summary>
        /// Represents one isolated child-process execution result.
        /// </summary>
        private sealed class IsolatedModeExecutionResult
        {
            /// <summary>
            /// Gets or sets the executed exception analysis mode.
            /// </summary>
            public ExceptionAnalysisMode Mode { get; set; }

            /// <summary>
            /// Gets or sets the JSON report path produced by the child process.
            /// </summary>
            public string ReportPath { get; set; } = string.Empty;

            /// <summary>
            /// Gets or sets the JSON report produced by the child process.
            /// </summary>
            public JsonReport Report { get; set; } = null!;

            /// <summary>
            /// Gets or sets the child process wall-clock duration in milliseconds.
            /// </summary>
            public long WallClockDurationMs { get; set; }

            /// <summary>
            /// Gets or sets the child process exit code.
            /// </summary>
            public int ProcessExitCode { get; set; }
        }
    }
}
