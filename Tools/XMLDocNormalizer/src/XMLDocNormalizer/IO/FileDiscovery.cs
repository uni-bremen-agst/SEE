namespace XMLDocNormalizer.IO
{
    /// <summary>
    /// Provides file discovery helpers for locating C# source files.
    /// </summary>
    internal static class FileDiscovery
    {
        /// <summary>
        /// Enumerates all C# files from a directory or returns the file itself if a file path is provided.
        /// </summary>
        /// <param name="targetPath">A directory path or a single file path.</param>
        /// <returns>A list of C# files to process.</returns>
        public static List<string> EnumerateCsFiles(string targetPath)
        {
            List<string> files = new List<string>();

            if (File.Exists(targetPath))
            {
                files.Add(targetPath);
                return files;
            }

            string root = targetPath;

            foreach (string file in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
            {
                // Skip typical build output folders.
                if (IsInIgnoredDirectory(file))
                {
                    continue;
                }

                files.Add(file);
            }

            return files;
        }

        /// <summary>
        /// Determines whether the given file path is located inside a directory that should be ignored by the tool.
        /// This is used to avoid rewriting generated or external files, such as build outputs.
        /// </summary>
        /// <param name="filePath">The file path to test.</param>
        /// <returns>
        /// True if the file path is inside an ignored directory (e.g., bin, obj, .git);
        /// otherwise, false.
        /// </returns>
        private static bool IsInIgnoredDirectory(string filePath)
        {
            string normalized = filePath.Replace('\\', '/');

            if (normalized.Contains("/bin/", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (normalized.Contains("/obj/", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (normalized.Contains("/.git/", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return false;
        }
    }
}
