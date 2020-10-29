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
        private const char WindowsDirectorySeparator = '\\';
        public const char UnixDirectorySeparator = '/';

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
            return FilenamesInDirectory(directory, "*.gxl");
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
            return FilenamesInDirectory(directory, "*.csv");
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

        /// <summary>
        /// Extension for IEnumerable<string>, that sorts by numbers in the string.
        /// For example {a-1, a-11, a-2} becomes {a-1, a-2, a-11}.
        /// </summary>
        internal static class NumericalSortExtension
        {
            /// <summary>
            /// Sorts the given IEnumerable<string> by numbers contained in the string.
            /// For example {a-1, a-11, a-2} becomes {a-1, a-2, a-11}.
            /// </summary>
            /// <param name="list">An IEnumerable<string> to be sorted</param>
            /// <returns>The passed list sorted by numbers</returns>
            public static IEnumerable<string> NumericalSort(this IEnumerable<string> list)
            {
                int maxLen = list.Select(s => s.Length).Max();

                return list.Select(s => new
                {
                    OrgStr = s,
                    SortStr = Regex.Replace(s, @"(\d+)|(\D+)", m => m.Value.PadLeft(maxLen, char.IsDigit(m.Value[0]) ? ' ' : '\xffff'))
                })
                .OrderBy(x => x.SortStr)
                .Select(x => x.OrgStr);
            }
        }
    
}