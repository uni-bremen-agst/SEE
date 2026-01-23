using XMLDocNormalizer.Checks.Configuration;

namespace XMLDocNormalizer.Cli
{
    /// <summary>
    /// Represents the parsed command-line options for the XML documentation tool.
    /// </summary>
    internal sealed class ToolOptions
    {
        /// <summary>
        /// Gets the root directory or single file path that should be processed.
        /// </summary>
        public string TargetPath { get; }

        /// <summary>
        /// Gets a value indicating whether the tool runs in check-only mode.
        /// In this mode, no files are modified.
        /// </summary>
        public bool CheckOnly { get; }

        /// <summary>
        /// Gets a value indicating whether old backup files should be deleted before processing.
        /// </summary>
        public bool CleanBackups { get; }

        /// <summary>
        /// Gets a value indication whether the tool runs in test mode.
        /// In this mode, the original files are not modified. Instead, a .bak copy is created and rewritten.
        /// </summary>
        public bool UseTest { get; }

        /// <summary>
        /// Gets the documentation analysis options that control which declaration kinds are checked.
        /// </summary>
        public XmlDocOptions XmlDocOptions { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ToolOptions"/> class.
        /// </summary>
        /// <param name="targetPath">The root directory or file path to process.</param>
        /// <param name="checkOnly">True to run in check-only mode.</param>
        /// <param name="cleanBackups">True to delete old backup files before processing.</param>
        /// <param name="useTest">True to run in test mode (rewrite .bak files only).</param>
        /// <param name="xmlDocOptions">The documentation analysis options.</param>
        public ToolOptions(string targetPath, bool checkOnly, bool cleanBackups, bool useTest, XmlDocOptions xmlDocOptions)
        {
            TargetPath = targetPath;
            CheckOnly = checkOnly;
            CleanBackups = cleanBackups;
            UseTest = useTest;
            XmlDocOptions = xmlDocOptions;
        }
    }
}