namespace XMLDocNormalizer.Cli.Output
{
    /// <summary>
    /// Defines the console colors used by the CLI output.
    /// </summary>
    /// <remarks>
    /// Centralizing these values ensures consistent coloring across
    /// log messages, warnings, and run summaries.
    /// </remarks>
    internal static class ConsoleColors
    {
        /// <summary>
        /// Color used for success messages and positive results.
        /// </summary>
        public const ConsoleColor Success = ConsoleColor.Green;

        /// <summary>
        /// Color used for errors and failed runs.
        /// </summary>
        public const ConsoleColor Error = ConsoleColor.Red;

        /// <summary>
        /// Color used for warnings.
        /// </summary>
        public const ConsoleColor Warning = ConsoleColor.DarkYellow;

        /// <summary>
        /// Color used for suggestions or informational hints.
        /// </summary>
        public const ConsoleColor Suggestion = ConsoleColor.Yellow;

        /// <summary>
        /// Color used for neutral informational values (e.g., metrics).
        /// </summary>
        public const ConsoleColor Info = ConsoleColor.Blue;
    }
}