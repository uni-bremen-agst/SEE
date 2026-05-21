using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

namespace SEE.Utils
{
    /// <summary>
    /// Utility class for creating zip archives.
    /// </summary>
    public static class Archiver
    {
        /// <summary>
        /// Creates a new zip folder at <paramref name="targetPath"/> with the files at <paramref name="filePaths"/> as contents.
        /// This method will create a new directory in the users temp directory which will be deleted after creating the archive.
        ///
        /// Files specified in <paramref name="filePaths"/> will remain where they are.
        ///
        /// If any of the files in <paramref name="filePaths"/> don't exist an <see cref="ArgumentException"/> will be thrown.
        /// </summary>
        /// <param name="filePaths">The files to put in the zip folder</param>
        /// <param name="targetPath">The target path of the zip folder, which may or may not end with ".zip".</param>
        /// <exception cref="ArgumentException">Will be thrown, if any of the files in <paramref name="filePaths"/> don't exist.</exception>
        public static void CreateArchive(IEnumerable<string> filePaths, string targetPath)
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "see-archive-" + Path.GetRandomFileName());
            Directory.CreateDirectory(tempDir);
            try
            {
                foreach (string path in filePaths)
                {
                    if (!File.Exists(path))
                    {
                        throw new ArgumentException($"File {path} does not exist");
                    }

                    File.Copy(path, Path.Combine(tempDir, Path.GetFileName(path)));
                }
            }
            finally
            {
                Directory.Delete(tempDir, true);
            }
            CreateArchive(tempDir, targetPath);
            Directory.Delete(tempDir, true);
        }

        /// <summary>
        /// Creates a new zip-compressed archive of a given directory <paramref name="sourceDir"/> and writes it to <paramref name="targetArchive"/>.
        /// </summary>
        /// <param name="sourceDir">The directory to create the archive from. This directory must exist.</param>
        /// <param name="targetArchive">The target path, to which the file should be written.</param>
        /// <exception cref="ArgumentException">Will be thrown, if <paramref name="sourceDir"/> doesn't exists.</exception>
        public static void CreateArchive(string sourceDir, string targetArchive)
        {
            if (!Directory.Exists(sourceDir))
            {
                throw new ArgumentException($"sourceDir {sourceDir} doesn't exist");
            }
            ZipFile.CreateFromDirectory(sourceDir, targetArchive);
        }

        /// <summary>
        /// Reads a given zip archive.
        ///
        /// The contents will then be written into <paramref name="targetPath"/>.
        /// </summary>
        /// <param name="archivePath">The path to the zip archive. Must exist</param>
        /// <param name="targetPath">The path to which the files should be extracted. Must exist</param>
        /// <exception cref="ArgumentException">Will be thrown if <paramref name="archivePath"/> or <paramref name="targetPath"/> do not exist.</exception>
        public static void ExtractArchive(string archivePath, string targetPath)
        {
            if (!File.Exists(archivePath))
            {
                throw new ArgumentException($"Archive file at: {archivePath} does not exist");
            }

            if (!Directory.Exists(targetPath))
            {
                throw new ArgumentException($"target path at: {targetPath} does not exist");
            }

            ZipFile.ExtractToDirectory(archivePath, targetPath);
        }

        /// <summary>
        /// Reads a given zip archive and returns the extracted file paths.
        ///
        /// The contents will be written into a temporary directory which will not be deleted automatically.
        /// </summary>
        /// <param name="archivePath">The path to the zip archive to extract.</param>
        /// <returns>A list with the paths to the extracted files.</returns>
        public static IList<string> ExtractArchive(string archivePath)
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "see-archive-extract-" + Path.GetRandomFileName());
            Directory.CreateDirectory(tempDir);
            ExtractArchive(archivePath, tempDir);
            return Directory.GetFiles(tempDir);
        }
    }
}
