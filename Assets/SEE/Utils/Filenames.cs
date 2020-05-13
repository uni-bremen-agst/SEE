using System.IO;

namespace SEE.Utils
{
    /// <summary>
    /// Utilities for pathnames.
    /// </summary>
    public class Filenames
    {
        /// <summary>
        /// Returns path where all Unity directory separators have been replaced by
        /// the directory separator of the current operating-system platform.
        /// </summary>
        /// <param name="path">path to be adjusted</param>
        /// <returns>path with replaced directory separators</returns>
        public static string OnCurrentPlatform(string path)
        {
            if (Path.DirectorySeparatorChar == '\\')
            {
                return path.Replace('/', Path.DirectorySeparatorChar);
            }
            else
            {
                return path;
            }
        }
    }
}