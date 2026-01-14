using System.Text.RegularExpressions;

namespace XMLDocNormalizer.IO
{
    /// <summary>
    /// Provides backup file creation and cleanup utilities.
    /// </summary>
    internal static class BackupManager
    {
        /// <summary>
        /// Creates a timestamped .bak copy of the specified file.
        /// </summary>
        /// <param name="filePath">The source file path.</param>
        /// <returns>The created backup path.</returns>
        public static string CreateBackup(string filePath)
        {
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string backupFile = filePath + "." + timestamp + ".bak";

            File.Copy(filePath, backupFile, true);
            Console.WriteLine($"Create backup: {backupFile}");

            return backupFile;
        }

        /// <summary>
        /// Deletes backup files created by this tool under the given path.
        /// </summary>
        /// <param name="targetPath">A directory or a single file path.</param>
        public static void DeleteOldBackups(string targetPath)
        {
            string root = Directory.Exists(targetPath)
                ? targetPath
                : (Path.GetDirectoryName(targetPath) ?? Directory.GetCurrentDirectory());

            Regex backupPattern = new Regex(@"\.\d{8}_\d{6}\.bak$", RegexOptions.Compiled);

            foreach (string bakFile in Directory.EnumerateFiles(root, "*.bak", SearchOption.AllDirectories))
            {
                if (backupPattern.IsMatch(bakFile))
                {
                    File.Delete(bakFile);
                }
            }
        }
    }
}
