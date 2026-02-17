namespace XMLDocNormalizer.Execution
{
    /// <summary>
    /// Defines process exit codes used by the tool.
    /// </summary>
    internal static class ToolExitCodes
    {
        /// <summary>
        /// Indicates successful execution.
        /// </summary>
        public const int Success = 0;

        /// <summary>
        /// Indicates that findings were reported (e.g., malformed XML docs).
        /// </summary>
        public const int Findings = 1;

        /// <summary>
        /// Indicates invalid command-line arguments or configuration.
        /// </summary>
        public const int InvalidArguments = 2;

        /// <summary>
        /// Indicates that a specified project was not found in the solution.
        /// </summary>
        public const int ProjectNotFound = 3;
    }
}
