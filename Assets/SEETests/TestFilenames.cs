using NUnit.Framework;

namespace SEE.Utils
{
    /// <summary>
    /// Tests for <see cref="Filenames"/>.
    /// </summary>
    internal class TestFilenames
    {
        [Test]
        public void TestTrimEndingDirectorySeparator1()
        {
            string pathWithTrailingSeparator = "Assets/SEE/GraphProviders/VCS/";
            string expectedPath = "Assets/SEE/GraphProviders/VCS";
            string trimmedPath = Filenames.TrimEndingDirectorySeparator(pathWithTrailingSeparator, '/');
            Assert.AreEqual(expectedPath, trimmedPath);
        }

        [Test]
        public void TestTrimEndingDirectorySeparator2()
        {
            string path = "X/";
            string trimmedPath = Filenames.TrimEndingDirectorySeparator(path, '/');
            Assert.AreEqual("X", trimmedPath);
        }

        [Test]
        [TestCase("Assets/SEE/GraphProviders/VCS")]
        [TestCase("Assets/SEE/GraphProviders/VCS/MyFile.cs")]
        [TestCase("MyFile.cs")]
        [TestCase("")]
        [TestCase(null)]
        /// <summary>
        /// Tests that a path without a trailing directory separator remains unchanged.
        /// </summary>
        public void TestTrimEndingDirectorySeparator(string path)
        {
            string trimmedPath = Filenames.TrimEndingDirectorySeparator(path, '/');
            Assert.AreEqual(path, trimmedPath);
        }

        [Test]
        [TestCase("Assets/SEE/GraphProviders/VCS/MyFile.cs", "Assets/SEE/GraphProviders/VCS")]
        [TestCase("/", "")]
        [TestCase("MyFile.cs", "")]
        [TestCase("MyDir/", "")]
        [TestCase("MyDir", "")]
        [TestCase("Super/Sub/", "Super")]
        [TestCase("Super/Sub", "Super")]
        [TestCase("", "")]
        [TestCase(null, "")]
        public void TestGetDirectoryName(string path, string expected)
        {
            string directory = Filenames.GetDirectoryName(path, '/');
            Assert.AreEqual(expected, directory);
        }

        [Test]
        [TestCase("Assets/SEE/GraphProviders/VCS/MyFile.cs", "MyFile.cs")]
        [TestCase("/", "")]
        [TestCase("MyFile.cs", "MyFile.cs")]
        [TestCase("MyDir/", "")]
        [TestCase("MyFile", "MyFile")]
        [TestCase("Super/Sub/", "")]
        [TestCase("Super/Sub", "Sub")]
        [TestCase("", "")]
        [TestCase(null, "")]
        public void TestGetFilename(string path, string expected)
        {
            string filename = Filenames.Basename(path, '/');
            Assert.AreEqual(expected, filename);
        }
    }
}
