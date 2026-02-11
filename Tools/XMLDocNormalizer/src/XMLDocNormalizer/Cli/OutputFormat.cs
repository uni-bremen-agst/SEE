namespace XMLDocNormalizer.Cli
{
    /// <summary>
    /// Defines supported findings output formats.
    /// </summary>
    internal enum OutputFormat
    {
        /// <summary>
        /// Human-readable console output.
        /// </summary>
        Console,

        /// <summary>
        /// Machine-readable JSON output.
        /// </summary>
        Json,

        /// <summary>
        /// SARIF 2.1.0 report for GitHub code scanning.
        /// </summary>
        Sarif
    }
}
