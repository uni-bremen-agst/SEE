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
    internal sealed class ToolOptions
        (string targetPath, 
        bool checkOnly, 
        bool cleanBackups, 
        bool useTest, 
        XmlDocOptions xmlDocOptions, 
        OutputFormat outputFormat, 
        string? outputPath, 
        bool verbose)
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
    }
}