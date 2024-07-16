using NUnit.Framework;
using SEE.Utils.Paths;

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
    }
}
