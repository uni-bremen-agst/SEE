namespace XMLDocNormalizer.Reporting.Sarif
{
    /// <summary>
    /// Normalizes paths for SARIF consumers.
    /// </summary>
    internal static class SarifPathNormalizer
    {
        /// <summary>
        /// Normalizes a file path for SARIF output.
        /// </summary>
        /// <remarks>
        /// GitHub code scanning works best with repository-relative paths.
        /// This method only normalizes directory separators; path relativization
        /// can be added later if required.
        /// </remarks>
        /// <param name="path">The input path.</param>
        /// <returns>A normalized path using forward slashes.</returns>
        public static string Normalize(string path)
        {
            return (path ?? string.Empty).Replace('\\', '/');
        }
    }
}
