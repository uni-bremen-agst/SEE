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
    /// <param name="outputPath">The output file path for machine-readable formats.</param
    /// <param name="verbose">True to enable verbose logging.</param>
    /// <param name="fullAnalysis">True to analyze the entire solution when a .sln file is provided.</param>
    /// <param name="projectName">The optional project name to analyze when a solution file is provided.
    /// If null, the first project will be analyzed.</param>
    /// <param name="includeGenerated">Whether generated files should be included.</param>
    /// <param name="includeTests">Whether test files should be included.</param>
    /// <param name="compareExceptionAnalysisModes">
    /// Whether all exception analysis modes should be executed and compared in a dedicated study run.
    /// </param>
    /// <param name="enableStatistics">
    /// Enables generation of statistics output for study and evaluation purposes.
    /// </param>
    /// <param name="statisticsOutputPath">
    /// The optional output path of the statistics JSON file.
    /// If omitted, a derived default path is used.
    /// </param>
    internal sealed class ToolOptions
        (string targetPath,
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
        bool enableStatistics = false,
        string? statisticsOutputPath = null)
    {
        /// <summary>
        /// Gets the root directory or single file path that should be processed.
        /// </summary>
        public string TargetPath { get; } = targetPath;

        /// <summary>
        /// Gets a value indicating whether the tool runs in check-only mode.
        /// In this mode, no files are modified.
        /// </summary>
        public bool CheckOnly { get; } = checkOnly;

        /// <summary>
        /// Gets a value indicating whether old backup files should be deleted before processing.
        /// </summary>
        public bool CleanBackups { get; } = cleanBackups;

        /// <summary>
        /// Gets a value indicating whether the tool runs in test mode.
        /// In this mode, the original files are not modified. Instead, a .bak copy is created and rewritten.
        /// </summary>
        public bool UseTest { get; } = useTest;

        /// <summary>
        /// Gets the documentation analysis options that control which declaration kinds are checked.
        /// </summary>
        public XmlDocOptions XmlDocOptions { get; } = xmlDocOptions;

        /// <summary>
        /// Gets the output format used for reporting findings.
        /// </summary>
        public OutputFormat OutputFormat { get; } = outputFormat;

        /// <summary>
        /// Gets the output file path for machine-readable formats (e.g. JSON).
        /// </summary>
        public string? OutputPath { get; } = outputPath;

        /// <summary> 
        /// Gets a value indicating whether verbose logging is enabled. 
        /// </summary>
        public bool Verbose { get; } = verbose;

        /// <summary>
        /// Gets a value indicating whether the tool should analyze the entire solution when a .sln file is provided.
        /// </summary>
        public bool FullAnalysis { get; } = fullAnalysis;

        /// <summary>
        /// Gets the explicit project name to analyze when a solution
        /// file is provided.
        /// </summary>
        public string? ProjectName { get; } = projectName;

        /// <summary>
        /// Gets or sets a value indicating whether generated files should be included.
        /// Default is false.
        /// </summary>
        public bool IncludeGenerated { get; } = includeGenerated;

        /// <summary>
        /// Gets or sets a value indicating whether test files should be included.
        /// Default is false.
        /// </summary>
        public bool IncludeTests { get; } = includeTests;

        /// <summary>
        /// Gets a value indicating whether all exception analysis modes should be executed
        /// and compared in a dedicated study run.
        /// </summary>
        public bool CompareExceptionAnalysisModes { get; } = compareExceptionAnalysisModes;

        /// <summary>
        /// Gets a value indicating whether study/statistics output should be generated.
        /// </summary>
        public bool EnableStatistics { get; } = enableStatistics;

        /// <summary>
        /// Gets the output path of the statistics report.
        /// If null, a derived default path is used.
        /// </summary>
        public string? StatisticsOutputPath { get; } = statisticsOutputPath;
    }
}