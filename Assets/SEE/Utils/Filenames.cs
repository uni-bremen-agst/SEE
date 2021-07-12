using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SEE.Utils
{
    /// <summary>
    /// Utilities for pathnames.
    /// </summary>
    public class Filenames
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
        /// of AbstractSEECity instances are persisted.
        /// </summary>
        public const string ConfigExtension = ".cfg";

        /// <summary>
        /// File extension for Speech Recognition Grammar Specifications (SRGS).
        /// </summary>
        public const string GrammarExtension = ".grxml";

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
            return extension.Substring(1);
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
        /// 
        /// Precondition: <paramref name="directory"/> must not be null or empty and must exist
        /// as a directory in the file system.
        /// </summary>
        /// <param name="directory">name of the directory in which to search for GXL files</param>
        /// <returns>sorted list of GXL filenames</returns>
        public static IEnumerable<string> GXLFilenames(string directory)
        {
            return FilenamesInDirectory(directory, Globbing(GXLExtension));
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
            return FilenamesInDirectory(directory, Globbing(CSVExtension));
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
            if (String.IsNullOrEmpty(directory))
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
    }
}