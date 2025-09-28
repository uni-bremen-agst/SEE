using NUnit.Framework;
using SEE.Utils.Paths;

namespace SEE.VCS
{
    /// <summary>
    /// Tests for <see cref="GitRepository"/> using a GitHub repository.
    /// </summary>
    internal class TestGitHub : TestGitRepository
    {
        /// <summary>
        /// Test for <see cref="GitRepository.Clone(string, string)"/>.
        /// </summary>
        [Test, Order(1)]
        public void TestCloneGitHub()
        {
            CloneGitHub();
        }

        /// <summary>
        /// Make sure that an access token for <see cref="TestGitHub"/> has been provided.
        /// </summary>
        [SetUp]
        public static void Setup()
        {
            if (string.IsNullOrWhiteSpace(testRepositoryAccessToken))
            {
                Assert.Inconclusive($"No access token provided. Please add your GitHub access token to the {nameof(testRepositoryAccessToken)} definition in {nameof(TestGitHub)} to run the tests.");
            }
        }

        /// <summary>
        /// Clones the repository at <see cref="repositoryUrl"/> into a temporary directory.
        /// </summary>
        private static void CloneGitHub()
        {
            string localPath = LocalPath(testRepositoryUrl);
            DeleteDirectoryIfItExists(localPath);

            using GitRepository repo = new(new DataPath(localPath), null, testRepositoryAccessToken);
            repo.Clone(testRepositoryUrl);
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

            string localPath = LocalPath(testRepositoryUrl);
            {
                // We need the using block to ensure that repo is disposed, otherwise
                // the subsequent deletion of the directory may fail because the
                // repo still has files open.
                using GitRepository repo = new(new DataPath(localPath), null, testRepositoryAccessToken);
                repo.FetchRemotes();
            }
            DeleteDirectoryIfItExists(localPath);
        }
    }
}
