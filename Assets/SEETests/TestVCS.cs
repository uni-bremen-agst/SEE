using NUnit.Framework;
using SEE.Utils.Paths;
using System.Reflection.Emit;
using UnityEngine;

namespace SEE.VCS
{
    /// <summary>
    /// Tests for <see cref="IVersionControl"/>.
    /// </summary>
    internal class TestVCS
    {

        private IVersionControl vcs;

        [SetUp]
        public void SetUp()
        {
            // We are using our own git repository of SEE as a guinea pig.
            vcs = VersionControlFactory.GetVersionControl("git", DataPath.ProjectFolder());
            Assert.IsNotNull(vcs);
            Assert.IsTrue(vcs is GitVersionControl);
        }

        [TearDown]
        public void TearDown()
        {
            vcs = null;
        }

        [Test]
        public void TestShowGitVersionControl()
        {
            AssertFile("Assets/SEE/VCS/GitVersionControl.cs", "17357897093e352ab9d4e039af81a3cfd6eeb1e7", -1400052609);
        }

        [Test]
        public void TestShowVersionControlSystems()
        {
            AssertFile("Assets/SEE/VCS/VersionControlSystems.cs", "642531cb3cf12527135a81a8c466b30b3cb0e78f", -1400052609);
        }

        private void AssertFile(string fileName, string revision, int expectedHash)
        {
            string content = vcs.Show(fileName, revision);
            Assert.IsNotNull(content);
            Assert.AreNotEqual(0, content.Length);
            // Comparing the hash code is a convenient way to check whether we found the right file.
            Assert.AreEqual(expectedHash, content.GetHashCode());
        }
    }
}
