using NUnit.Framework;
using SEE.Utils.Paths;

namespace SEE.VCS
{
    /// <summary>
    /// Tests for <see cref="VCS.GitRepository"/> using a GitHub repository.
    /// </summary>
    internal class TestGitHub : TestGitRepository
    {
        /// <summary>
        /// URL for the test project on GitHub.
        /// </summary>
        private const string repositoryUrl = "https://github.com/koschke/TestProjectForSEE.git";

        /// <summary>
        /// Test for <see cref="GitRepository.Clone(string, string)"/>.
        /// </summary>
        [Test, Order(1)]
        public void TestCloneGitHub()
        {
            CloneGitHub();
        }

        /// <summary>
        /// Clones the repository at <see cref="repositoryUrl"/> into a temporary directory.
        /// </summary>
        private static void CloneGitHub()
        {
            // ADD YOUR TOKEN HERE TO RUN THE TESTS. DO NOT CHECK IN YOUR TOKEN!
            string accessToken = "";
            string localPath = LocalPath(repositoryUrl);
            DeleteDirectoryIfItExists(localPath);

            using GitRepository repo = new(new DataPath(localPath), null);
            repo.Clone(repositoryUrl, accessToken);
        }

        /// <summary>
        /// Test for <see cref="GitRepository.FetchRemotes()"/>.
        /// </summary>
        /// <remarks>Must be run after <see cref="TestCloneGitHub"/> because it assumes
        /// an existing local repository.</remarks>
        [Test, Order(2)]
        public void TestFetchRemotesGithub()
        {
            CloneGitHub();

            string localPath = LocalPath(repositoryUrl);
            {
                // We need the using block to ensure that repo is disposed, otherwise
                // the subsequent deletion of the directory may fail because the
                // repo still has files open.
                using GitRepository repo = new(new DataPath(localPath), null);
                repo.FetchRemotes();
            }
            DeleteDirectoryIfItExists(localPath);
        }
    }
}
