using NUnit.Framework;
using SEE.Utils.Paths;
using System.IO;
using UnityEngine;

namespace SEE.Utils
{
    /// <summary>
    /// Defines asserts for Config I/O that can be shared.
    /// </summary>
    internal class AbstractTestConfigIO
    {
        /// <summary>
        /// Checks whether the two data paths <paramref name="expected"/> and <paramref name="actual"/>
        /// are equal (by value).
        /// </summary>
        /// <param name="expected">expected data path</param>
        /// <param name="actual">actual data path</param>
        protected static void AreEqual(DataPath expected, DataPath actual)
        {
            Assert.AreEqual(expected.Root, actual.Root);
            Assert.AreEqual(expected.RelativePath, actual.RelativePath);
            Assert.AreEqual(expected.AbsolutePath, actual.AbsolutePath);
        }

        /// <summary>
        /// Prints the content of <paramref name="filename"/> if it exists.
        /// </summary>
        /// <param name="filename">name of the file to be printed</param>
        protected static void Print(string filename)
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