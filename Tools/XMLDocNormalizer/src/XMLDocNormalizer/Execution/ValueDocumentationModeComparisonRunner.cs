using XMLDocNormalizer.Cli;
using XMLDocNormalizer.Configuration;
using XMLDocNormalizer.Models;
using XMLDocNormalizer.Models.DTO;
using XMLDocNormalizer.Reporting.Json;

namespace XMLDocNormalizer.Execution
{
    /// <summary>
    /// Executes all value-documentation modes and produces a comparison report.
    /// </summary>
    internal static class ValueDocumentationModeComparisonRunner
    {
        /// <summary>
        /// Executes all value-documentation modes and writes a comparison report.
        /// </summary>
        /// <param name="options">The base tool options.</param>
        /// <returns>The comparison report DTO.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="options"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when the configured target path is invalid.
        /// </exception>
        public static ValueDocumentationModeComparisonReportDto Run(ToolOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);

            IReadOnlyList<ValueDocumentationMode> modes = GetComparisonModes();
            List<ValueDocumentationModeRunDto> modeRuns = new();

            int allReadableMissingValueTagCount = 0;
            int sharedSloc = 0;

            Console.WriteLine("Running value-documentation mode comparison.");

            foreach (ValueDocumentationMode mode in modes)
            {
                Console.WriteLine($"Running value-documentation mode: {mode}");

                ToolOptions modeOptions = CreateModeSpecificOptions(options, mode);
                RunResult result = ToolRunner.Run(modeOptions);

                int missingValueTagCount = GetFindingCount(result, XmlDocSmells.MissingValueTag.ID);

                if (mode == ValueDocumentationMode.AllReadableProperties)
                {
                    allReadableMissingValueTagCount = missingValueTagCount;
                    sharedSloc = result.Sloc;
                }

                modeRuns.Add(new ValueDocumentationModeRunDto
                {
                    Mode = mode,
                    FindingCount = result.FindingCount,
                    ErrorCount = result.ErrorCount,
                    WarningCount = result.WarningCount,
                    SuggestionCount = result.SuggestionCount,
                    MissingValueTagCount = missingValueTagCount,
                    SuppressedMissingValueTagCount = Math.Max(
                        0,
                        allReadableMissingValueTagCount - missingValueTagCount),
                    FindingsPerKSloc = CalculatePerKSloc(result.FindingCount, result.Sloc),
                    SuggestionsPerKSloc = CalculatePerKSloc(result.SuggestionCount, result.Sloc)
                });
            }

            ValueDocumentationModeComparisonReportDto report = new()
            {
                Tool = ToolMetadata.Name,
                Version = ToolMetadata.Version,
                GeneratedAtUtc = DateTime.UtcNow,
                TargetPath = options.TargetPath,
                Sloc = sharedSloc,
                Modes = modeRuns
            };

            string comparisonOutputPath = ResolveComparisonOutputPath(options);
            ValueDocumentationModeComparisonReportWriter.Write(comparisonOutputPath, report);

            PrintComparisonSummary(report, comparisonOutputPath);

            return report;
        }

        /// <summary>
        /// Gets the value-documentation modes that should be compared.
        /// </summary>
        /// <returns>
        /// The compared value-documentation modes.
        /// </returns>
        private static IReadOnlyList<ValueDocumentationMode> GetComparisonModes()
        {
            return
            [
                ValueDocumentationMode.AllReadableProperties,
                ValueDocumentationMode.ExcludeDtoLikeTypes,
                ValueDocumentationMode.IndexersOnly,
                ValueDocumentationMode.None
            ];
        }

        /// <summary>
        /// Creates a tool options instance for one value-documentation mode.
        /// </summary>
        /// <param name="baseOptions">
        /// The base comparison options.
        /// </param>
        /// <param name="mode">
        /// The value-documentation mode to use.
        /// </param>
        /// <returns>
        /// A new <see cref="ToolOptions"/> instance for the specified mode.
        /// </returns>
        private static ToolOptions CreateModeSpecificOptions(
            ToolOptions baseOptions,
            ValueDocumentationMode mode)
        {
            XmlDocOptions xmlDocOptions = CloneXmlDocOptions(baseOptions.XmlDocOptions);
            xmlDocOptions.ValueDocumentationMode = mode;

            string reportPath = ResolveModeReportPath(baseOptions, mode);

            return new ToolOptions(
                targetPath: baseOptions.TargetPath,
                checkOnly: true,
                cleanBackups: false,
                useTest: false,
                xmlDocOptions: xmlDocOptions,
                outputFormat: OutputFormat.Json,
                outputPath: reportPath,
                verbose: baseOptions.Verbose,
                fullAnalysis: baseOptions.FullAnalysis,
                projectName: baseOptions.ProjectName,
                includeGenerated: baseOptions.IncludeGenerated,
                includeTests: baseOptions.IncludeTests,
                compareExceptionAnalysisModes: false,
                compareValueDocumentationModes: false,
                exceptionAnalysisComparisonRuns: baseOptions.ExceptionAnalysisComparisonRuns,
                exceptionAnalysisComparisonWarmupRuns: baseOptions.ExceptionAnalysisComparisonWarmupRuns,
                enableStatistics: false,
                statisticsOutputPath: null);
        }

        /// <summary>
        /// Clones XML documentation options.
        /// </summary>
        /// <param name="options">
        /// The options to clone.
        /// </param>
        /// <returns>
        /// A cloned options instance.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="options"/> is null.
        /// </exception>
        private static XmlDocOptions CloneXmlDocOptions(XmlDocOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);

            return new XmlDocOptions
            {
                CheckEnumMembers = options.CheckEnumMembers,
                RequireSummaryForFields = options.RequireSummaryForFields,
                RequireDocumentationForNamespaces = options.RequireDocumentationForNamespaces,
                ExceptionAnalysisMode = options.ExceptionAnalysisMode,
                ValueDocumentationMode = options.ValueDocumentationMode
            };
        }

        /// <summary>
        /// Gets the count for a finding ID from a run result.
        /// </summary>
        /// <param name="result">
        /// The run result to inspect.
        /// </param>
        /// <param name="smellId">
        /// The finding ID to count.
        /// </param>
        /// <returns>
        /// The number of matching findings.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="result"/> or
        /// <paramref name="smellId"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="smellId"/> is empty or consists only of white-space characters.
        /// </exception>
        private static int GetFindingCount(RunResult result, string smellId)
        {
            ArgumentNullException.ThrowIfNull(result);
            ArgumentException.ThrowIfNullOrWhiteSpace(smellId);

            return result.SmellCounts.TryGetValue(smellId, out int count)
                ? count
                : 0;
        }

        /// <summary>
        /// Calculates a per-1000-SLOC density.
        /// </summary>
        /// <param name="count">
        /// The item count.
        /// </param>
        /// <param name="sloc">
        /// The source lines of code.
        /// </param>
        /// <returns>
        /// The calculated density per 1000 SLOC.
        /// </returns>
        private static double CalculatePerKSloc(int count, int sloc)
        {
            if (sloc <= 0)
            {
                return 0;
            }

            return count / (sloc / 1000.0);
        }

        /// <summary>
        /// Resolves the output path for the comparison report.
        /// </summary>
        /// <param name="options">
        /// The base comparison options.
        /// </param>
        /// <returns>
        /// The comparison report output path.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="options"/> is null.
        /// </exception>
        private static string ResolveComparisonOutputPath(ToolOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);

            if (!string.IsNullOrWhiteSpace(options.OutputPath))
            {
                return options.OutputPath;
            }

            return "artifacts/value-documentation-mode-comparison.json";
        }

        /// <summary>
        /// Resolves the output path for one mode-specific findings report.
        /// </summary>
        /// <param name="baseOptions">
        /// The base comparison options.
        /// </param>
        /// <param name="mode">
        /// The value-documentation mode.
        /// </param>
        /// <returns>
        /// The mode-specific report output path.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="baseOptions"/> is null.
        /// </exception>
        private static string ResolveModeReportPath(
            ToolOptions baseOptions,
            ValueDocumentationMode mode)
        {
            ArgumentNullException.ThrowIfNull(baseOptions);

            string comparisonPath = ResolveComparisonOutputPath(baseOptions);
            string directory = Path.GetDirectoryName(comparisonPath) ?? string.Empty;
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(comparisonPath);
            string extension = Path.GetExtension(comparisonPath);

            if (string.IsNullOrWhiteSpace(extension))
            {
                extension = ".json";
            }

            string suffix = GetModeFileSuffix(mode);
            string fileName = $"{fileNameWithoutExtension}_{suffix}{extension}";

            return string.IsNullOrWhiteSpace(directory)
                ? fileName
                : Path.Combine(directory, fileName);
        }

        /// <summary>
        /// Gets a file-name-safe suffix for a value-documentation mode.
        /// </summary>
        /// <param name="mode">
        /// The value-documentation mode.
        /// </param>
        /// <returns>
        /// A file-name-safe mode suffix.
        /// </returns>
        private static string GetModeFileSuffix(ValueDocumentationMode mode)
        {
            return mode switch
            {
                ValueDocumentationMode.None => "disabled",
                ValueDocumentationMode.AllReadableProperties => "all-readable-properties",
                ValueDocumentationMode.ExcludeDtoLikeTypes => "exclude-dto-like-types",
                ValueDocumentationMode.IndexersOnly => "indexers-only",
                _ => mode.ToString().ToLowerInvariant()
            };
        }

        /// <summary>
        /// Prints a concise comparison summary to the console.
        /// </summary>
        /// <param name="report">
        /// The comparison report.
        /// </param>
        /// <param name="comparisonOutputPath">
        /// The written comparison report path.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="report"/> or
        /// <paramref name="comparisonOutputPath"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="comparisonOutputPath"/> is empty
        /// or consists only of white-space characters.
        /// </exception>
        private static void PrintComparisonSummary(
            ValueDocumentationModeComparisonReportDto report,
            string comparisonOutputPath)
        {
            ArgumentNullException.ThrowIfNull(report);
            ArgumentException.ThrowIfNullOrWhiteSpace(comparisonOutputPath);

            Console.WriteLine();
            Console.WriteLine($"Value documentation mode comparison written to: {comparisonOutputPath}");
            Console.WriteLine();
            Console.WriteLine("Mode summary:");
            Console.WriteLine("  Mode                         Findings  Warnings  Suggestions  DOC800  Suppressed");
            Console.WriteLine("  -------------------------------------------------------------------------------");

            foreach (ValueDocumentationModeRunDto mode in report.Modes)
            {
                Console.WriteLine(
                    $"  {mode.Mode,-28} {mode.FindingCount,8} {mode.WarningCount,9} {mode.SuggestionCount,12} {mode.MissingValueTagCount,7} {mode.SuppressedMissingValueTagCount,11}");
            }
        }
    }
}
