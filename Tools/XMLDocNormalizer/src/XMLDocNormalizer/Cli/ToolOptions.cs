using XMLDocNormalizer.Configuration;

namespace XMLDocNormalizer.Cli
{
    /// <summary>
    /// Represents the parsed command-line options for the XML documentation tool.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the <see cref="ToolOptions"/> class.
    /// </remarks>
    /// <param name="targetPath">The root directory or file path to process.</param>
    /// <param name="checkOnly">True to run in check-only mode.</param>
    /// <param name="cleanBackups">True to delete old backup files before processing.</param>
    /// <param name="useTest">True to run in test mode (rewrite .bak files only).</param>
    /// <param name="xmlDocOptions">The documentation analysis options.</param>
    /// <param name="outputFormat">The output format for reporting findings.</param>
    /// <param name="outputPath">The output file path for machine-readable formats.</param>
    /// <param name="verbose">True to enable verbose logging.</param>
    /// <param name="fullAnalysis">True to analyze the entire solution when a .sln file is provided.</param>
    /// <param name="projectName">
    /// The optional project name to analyze when a solution file is provided.
    /// If null, the first project will be analyzed.
    /// </param>
    /// <param name="includeGenerated">Whether generated files should be included.</param>
    /// <param name="includeTests">Whether test files should be included.</param>
    /// <param name="compareExceptionAnalysisModes">
    /// Whether all exception analysis modes should be executed and compared in a dedicated study run.
    /// </param>
    /// <param name="exceptionAnalysisComparisonRuns">
    /// The measured isolated comparison run count per exception analysis mode.
    /// </param>
    /// <param name="exceptionAnalysisComparisonWarmupRuns">
    /// The warmup run count per exception analysis mode.
    /// Warmup runs are executed before measured runs and are excluded from reported timing statistics.
    /// </param>
    /// <param name="enableStatistics">
    /// Enables generation of statistics output for study and evaluation purposes.
    /// </param>
    /// <param name="statisticsOutputPath">
    /// The optional output path of the statistics JSON file.
    /// If omitted, a derived default path is used.
    /// </param>
    internal sealed class ToolOptions(
        string targetPath,
        bool checkOnly,
        bool cleanBackups,
        bool useTest,
        XmlDocOptions xmlDocOptions,
        OutputFormat outputFormat,
        string? outputPath,
        bool verbose,
        bool fullAnalysis,
        string? projectName = null,
        bool includeGenerated = false,
        bool includeTests = false,
        bool compareExceptionAnalysisModes = false,
        int exceptionAnalysisComparisonRuns = 1,
        int exceptionAnalysisComparisonWarmupRuns = 0,
        bool enableStatistics = false,
        string? statisticsOutputPath = null)
    {
        /// <summary>
        /// Gets the root directory or single file path that should be processed.
        /// </summary>
        /// <value>
        /// The root directory or single file path that should be processed.
        /// </value>
        public string TargetPath { get; } = targetPath;

        /// <summary>
        /// Gets a value indicating whether the tool runs in check-only mode.
        /// In this mode, no files are modified.
        /// </summary>
        /// <value>
        /// True if the tool runs without modifying files; otherwise false.
        /// </value>
        public bool CheckOnly { get; } = checkOnly;

        /// <summary>
        /// Gets a value indicating whether old backup files should be deleted before processing.
        /// </summary>
        /// <value>
        /// True if old backup files should be deleted before processing; otherwise false.
        /// </value>
        public bool CleanBackups { get; } = cleanBackups;

        /// <summary>
        /// Gets a value indicating whether the tool runs in test mode.
        /// In this mode, the original files are not modified. Instead, a .bak copy is created and rewritten.
        /// </summary>
        /// <value>
        /// True if the tool rewrites backup copies instead of original files; otherwise false.
        /// </value>
        public bool UseTest { get; } = useTest;

        /// <summary>
        /// Gets the documentation analysis options that control which declaration kinds are checked.
        /// </summary>
        /// <value>
        /// The documentation analysis options used by the run.
        /// </value>
        public XmlDocOptions XmlDocOptions { get; } = xmlDocOptions;

        /// <summary>
        /// Gets the output format used for reporting findings.
        /// </summary>
        /// <value>
        /// The output format used for reporting findings.
        /// </value>
        public OutputFormat OutputFormat { get; } = outputFormat;

        /// <summary>
        /// Gets the output file path for machine-readable formats.
        /// </summary>
        /// <value>
        /// The output file path for machine-readable formats, 
        /// or null when no explicit output path was provided.
        /// </value>
        public string? OutputPath { get; } = outputPath;

        /// <summary>
        /// Gets a value indicating whether verbose logging is enabled.
        /// </summary>
        /// <value>
        /// True if verbose logging is enabled; otherwise false.
        /// </value>
        public bool Verbose { get; } = verbose;

        /// <summary>
        /// Gets a value indicating whether the tool should analyze the entire solution when a solution file is provided.
        /// </summary>
        /// <value>
        /// True if the full solution should be analyzed when a solution file is provided; otherwise false.
        /// </value>
        public bool FullAnalysis { get; } = fullAnalysis;

        /// <summary>
        /// Gets the explicit project name to analyze when a solution file is provided.
        /// </summary>
        /// <value>
        /// The explicit project name to analyze, or null when the first project should be selected.
        /// </value>
        public string? ProjectName { get; } = projectName;

        /// <summary>
        /// Gets a value indicating whether generated files should be included.
        /// </summary>
        /// <value>
        /// True if generated files should be included in analysis; otherwise false.
        /// </value>
        public bool IncludeGenerated { get; } = includeGenerated;

        /// <summary>
        /// Gets a value indicating whether test files should be included.
        /// </summary>
        /// <value>
        /// True if test files should be included in analysis; otherwise false.
        /// </value>
        public bool IncludeTests { get; } = includeTests;

        /// <summary>
        /// Gets a value indicating whether all exception analysis modes should be executed
        /// and compared in a dedicated study run.
        /// </summary>
        /// <value>
        /// True if all exception analysis modes should be executed and compared; otherwise false.
        /// </value>
        public bool CompareExceptionAnalysisModes { get; } = compareExceptionAnalysisModes;

        /// <summary>
        /// Gets the measured isolated comparison run count per exception analysis mode.
        /// </summary>
        /// <value>
        /// The measured isolated comparison run count per exception analysis mode.
        /// </value>
        public int ExceptionAnalysisComparisonRuns { get; } = exceptionAnalysisComparisonRuns;

        /// <summary>
        /// Gets the warmup run count per exception analysis mode.
        /// Warmup runs are executed before measured runs and excluded from timing statistics.
        /// </summary>
        /// <value>
        /// The warmup run count per exception analysis mode.
        /// </value>
        public int ExceptionAnalysisComparisonWarmupRuns { get; } = exceptionAnalysisComparisonWarmupRuns;

        /// <summary>
        /// Gets a value indicating whether study/statistics output should be generated.
        /// </summary>
        /// <value>
        /// True if study and statistics output should be generated; otherwise false.
        /// </value>
        public bool EnableStatistics { get; } = enableStatistics;

        /// <summary>
        /// Gets the output path of the statistics report.
        /// If null, a derived default path is used.
        /// </summary>
        /// <value>
        /// The output path of the statistics report, or null when a derived default path should be used.
        /// </value>
        public string? StatisticsOutputPath { get; } = statisticsOutputPath;
    }
}
