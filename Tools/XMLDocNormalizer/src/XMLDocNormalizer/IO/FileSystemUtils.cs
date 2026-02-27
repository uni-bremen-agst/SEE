namespace XMLDocNormalizer.IO
{
    /// <summary>
    /// Provides common file system helper methods used across the application.
    /// </summary>
    internal static class FileSystemUtils
    {
        /// <summary>
        /// Ensures that the parent directory of the specified file path exists.
        /// </summary>
        /// <param name="filePath">The file path whose parent directory should be created if missing.</param>
        /// <remarks>
        /// If the file path does not contain a directory component (e.g. "report.json"),
        /// this method performs no action.
        /// </remarks>
        public static void EnsureParentDirectoryExists(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return;
            }

            string? directory = Path.GetDirectoryName(filePath);

            if (string.IsNullOrWhiteSpace(directory))
            {
                return;
            }

            Directory.CreateDirectory(directory);
        }
    }
}