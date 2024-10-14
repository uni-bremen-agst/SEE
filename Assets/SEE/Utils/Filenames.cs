using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SEE.Utils
{
    /// <summary>
    /// Utilities for pathnames.
    /// </summary>
    public abstract class Filenames
    {
        /// <summary>
        /// Directory separator on Windows.
        /// </summary>
        public const char WindowsDirectorySeparator = '\\';

        /// <summary>
        /// Directory separator on Unix.
        /// </summary>
        public const char UnixDirectorySeparator = '/';

        /// <summary>
        /// File extension of GXL filenames.
        /// </summary>
        public const string GXLExtension = ".gxl";

        /// <summary>
        /// File extension of layout files in the format of Axivion's Gravis.
        /// These kinds of layout files contain only 2D co-ordinates.
        /// </summary>
        public const string GVLExtension = ".gvl";

        /// <summary>
        /// File extension of layout files in the SEE format:
        /// SEE Layout Data (SLD).
        /// These kinds of layout files contain the complete Transform
        /// data of the game objects.
        /// </summary>
        public const string SLDExtension = ".sld";

        /// <summary>
        /// File extension of CSV filenames.
        /// </summary>
        public const string CSVExtension = ".csv";

        /// <summary>
        /// File extension of LJG filenames.
        /// </summary>
        public const string JLGExtension = ".jlg";

        /// <summary>
        /// File extension of DYN filenames.
        /// </summary>
        public const string DYNExtension = ".dyn";

        /// <summary>
        /// File extension of filenames for configuration files in which attributes
        /// of drawables are persisted.
        /// </summary>
        public const string DrawableConfigExtension = ".drw";

        /// <summary>
        /// File extension of filenames for configuration files in which attributes
        /// of metric boards are persisted.
        /// </summary>
        public const string MetricBoardConfigExtension = ".mbc";

        /// <summary>
        /// File extension of filenames for configuration files in which attributes
        /// of AbstractSEECity instances are persisted.
        /// </summary>
        public const string CityConfigExtension = ".cfg";

        /// <summary>
        /// File extension for Speech Recognition Grammar Specifications (SRGS).
        /// </summary>
        public const string GrammarExtension = ".grxml";

        /// <summary>
        /// File extension for projects in Visual Studio.
        /// </summary>
        public const string SolutionExtension = ".sln";

        /// <summary>
        /// File extension for LZMA compressed data streams/files.
        /// </summary>
        public const string CompressedExtension = ".xz";

        /// <summary>
        /// File extension of PNG filenames.
        /// </summary>
        public const string PNGExtension = ".png";

        /// <summary>
        /// File extension of JPG filenames.
        /// </summary>
        public const string JPGExtension = ".jpg";

        /// <summary>
        /// Alternative file extension of JPG filenames.
        /// </summary>
        public const string JPEGExtension = ".jpeg";

        /// <summary>
        /// Returns the last part of the given <paramref name="extension"/>
        /// without the period.
        ///
        /// Precondition: <paramref name="extension"/> must start with a period.
        /// </summary>
        /// <param name="extension"></param>
        /// <returns><paramref name="extension"/> without leading period</returns>
        public static string ExtensionWithoutPeriod(string extension)
        {
            return extension[1..];
        }

        /// <summary>
        /// True if <paramref name="filename"/> has <paramref name="extension"/>.
        /// If <paramref name="filename"/> is null or if it has no extension
        /// separated by a period, false is returned.
        /// </summary>
        /// <param name="filename">filename to be checked for the extension</param>
        /// <param name="extension">the extension the filename should have</param>
        /// <returns></returns>
        public static bool HasExtension(string filename, string extension)
        {
            string extensionOfFilename = Path.GetExtension(filename);
            return !string.IsNullOrEmpty(extensionOfFilename)
                && extensionOfFilename == extension;
        }

        /// <summary>
        /// Yields string "*" + <paramref name="extension"/>.
        /// </summary>
        /// <param name="extension">file extension to be appended to "*"</param>
        /// <returns>"*" + <paramref name="extension"/></returns>
        private static string Globbing(string extension)
        {
            return "*" + extension;
        }

        /// <summary>
        /// Returns path where all Unity directory separators have been replaced by
        /// the directory separator of the current operating-system platform.
        /// </summary>
        /// <param name="path">path to be adjusted</param>
        /// <returns>path with replaced directory separators</returns>
        public static string OnCurrentPlatform(string path)
        {
            if (Path.DirectorySeparatorChar == WindowsDirectorySeparator)
            {
                // We are running on Windows.
                return path.Replace(UnixDirectorySeparator, Path.DirectorySeparatorChar);
            }
            else
            {
                // We are running on Unix or Mac OS.
                return path;
            }
        }

        /// <summary>
        /// Returns path where all directory separators of the current operating
        /// system platform have been replaced by the Unix (or Unity) directory separator.
        /// </summary>
        /// <param name="path">path to be adjusted</param>
        /// <returns>path with Unix directory separators</returns>
        public static string ToInternalRepresentation(string path)
        {
            return path.Replace(WindowsDirectorySeparator, UnixDirectorySeparator);
        }

        /// <summary>
        /// Returns the sorted list of GXL filenames of the given <paramref name="directory"/>.
        /// Note that this also finds compressed GXL files.
        ///
        /// Precondition: <paramref name="directory"/> must not be null or empty and must exist
        /// as a directory in the file system.
        /// </summary>
        /// <param name="directory">name of the directory in which to search for GXL files</param>
        /// <returns>sorted list of GXL filenames</returns>
        public static IEnumerable<string> GXLFilenames(string directory)
        {
            return FilenamesInDirectory(directory, Globbing(GXLExtension))
                .Concat(FilenamesInDirectory(directory, Globbing(GXLExtension + CompressedExtension)));
        }

        /// <summary>
        /// Returns the sorted list of CSV filenames of the given <paramref name="directory"/>.
        ///
        /// Precondition: <paramref name="directory"/> must not be null or empty and must exist
        /// as a directory in the file system.
        /// </summary>
        /// <param name="directory">name of the directory in which to search for CSV files</param>
        /// <returns>sorted list of CSV filenames</returns>
        public static IEnumerable<string> CSVFilenames(string directory)
        {
            return FilenamesInDirectory(directory, Globbing(CSVExtension + CompressedExtension));
        }

        /// <summary>
        /// Returns the sorted list of filenames matching the <paramref name="globbing"/> in the
        /// given <paramref name="directory"/>.
        ///
        /// Precondition: <paramref name="directory"/> must not be null or empty and must exist
        /// as a directory in the file system.
        /// </summary>
        /// <param name="directory">name of the directory in which to search for files</param>
        /// <param name="globbing">globbing parameter that the filenames are to match</param>
        /// <returns>sorted list of filenames in <paramref name="directory"/> matching the
        /// <paramref name="globbing"/></returns>
        public static IEnumerable<string> FilenamesInDirectory(string directory, string globbing)
        {
            if (string.IsNullOrEmpty(directory))
            {
                throw new Exception("Directory not set.");
            }
            else if (!Directory.Exists(directory))
            {
                throw new Exception("Directory " + directory + " does not exist.");
            }

            // get all files matching the globbing expression sorted by numbers in their name
            IEnumerable<string> sortedGraphNames = Directory
                .GetFiles(directory, globbing, SearchOption.TopDirectoryOnly)
                .Where(e => !string.IsNullOrEmpty(e));

            sortedGraphNames = sortedGraphNames.Distinct().NumericalSort();
            return sortedGraphNames;
        }

        /// <summary>
        /// Returns the concatenation of <paramref name="directory"/> and <paramref name="filename"/>.
        /// Both are assumed to be paths using <see cref="UnixDirectorySeparator"/> as a directory
        /// separator. If <paramref name="directory"/> does not end with a <see cref="UnixDirectorySeparator"/>,
        /// one will be added at the end of <paramref name="directory"/> before the concatenation takes place.
        /// </summary>
        /// <param name="directory">directory path</param>
        /// <param name="filename">filename</param>
        /// <returns>path concatenation</returns>
        internal static string Join(string directory, string filename)
        {
            if (string.IsNullOrEmpty(directory))
            {
                return filename;
            }
            // directory has at least one character
            return directory[^1] == UnixDirectorySeparator ?
                directory + filename : directory + UnixDirectorySeparator + filename;
        }

        /// <summary>
        /// Recursively deletes a directory as well as any subdirectories and files.
        /// If the files are read-only, they are flagged as normal and then deleted.
        /// </summary>
        /// <param name="directory">The name of the directory to remove.</param>
        /// <remarks>Source: https://stackoverflow.com/questions/25549589/programmatically-delete-local-repository-with-libgit2sharp
        /// by AJ Richardson</remarks>
        public static void DeleteReadOnlyDirectory(string directory)
        {
            foreach (string subdirectory in Directory.EnumerateDirectories(directory))
            {
                DeleteReadOnlyDirectory(subdirectory);
            }
            foreach (string fileName in Directory.EnumerateFiles(directory))
            {
                FileInfo fileInfo = new(fileName)
                {
                    Attributes = FileAttributes.Normal
                };
                fileInfo.Delete();
            }
            Directory.Delete(directory);
        }

        /// <summary>
        /// Returns the innermost directory name of the given <paramref name="directoryPath"/>
        /// where <paramref name="directoryPath"/> is a (possibly nested) platform-dependent
        /// path to a directory
        /// </summary>
        /// <param name="directoryPath">platform-dependent directory path</param>
        /// <returns>innermost directory name</returns>
        /// <exception cref="ArgumentException">if <paramref name="directoryPath"/> is null or empty</exception>
        /// <example>If <paramref name="directoryPath"/> is C:\Users\someone\develop\SEE\
        /// while running on a Windows computer, then SEE will be returned; likewise if it
        /// is C:\Users\someone\develop\SEE. If <paramref name="directoryPath"/> is
        /// /home/someone/develop/SEE/ while running on a Unix computer, then SEE will be returned;
        /// likewise if it is /home/someone/develop/SEE.
        /// </example>
        public static string InnermostDirectoryName(string directoryPath)
        {
            if (string.IsNullOrWhiteSpace(directoryPath))
            {
                throw new ArgumentException("Directory path must neither be null nor empty.");
            }
            string path = directoryPath[^1] == Path.DirectorySeparatorChar ?
                directoryPath[..^1] : directoryPath;

            return Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar)
                .Split(Path.DirectorySeparatorChar).Last();
        }
    }
}
