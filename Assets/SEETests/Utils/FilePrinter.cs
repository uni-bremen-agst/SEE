using System.IO;
using UnityEngine;

namespace SEE.Utils
{
    /// <summary>
    /// Utilities to print files helping debugging failing tests.
    /// </summary>
    internal static class FilePrinter
    {
        /// <summary>
        /// Prints the content of <paramref name="filename"/> if it exists.
        /// </summary>
        /// <param name="filename">name of the file to be printed</param>
        internal static void Print(string filename)
        {
            if (!string.IsNullOrWhiteSpace(filename) && File.Exists(filename))
            {
                foreach (string line in File.ReadAllLines(filename))
                {
                    Debug.Log(line);
                }
            }
        }
    }
}
