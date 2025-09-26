using LibGit2Sharp;
using NUnit.Framework;
using SEE.Utils;
using SEE.Utils.Paths;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace SEE.VCS
{
    /// <summary>
    /// Tests for <see cref="GitRepository"/> using the local Git SEE repository
    /// in which these tests are run.
    /// </summary>
    internal class TestGitSEE : TestGitRepository
    {
        /// <summary>
        /// The project folder of SEE.
        /// </summary>
        /// <returns>project folder of SEE</returns>
        private static string ProjectFolder()
        {
            return Regex.Replace(Application.dataPath, "/Assets$", string.Empty);
        }

        /// <summary>
        /// Returns a filter that matches all C# files in the Assets/SEE folder
        /// on the given <paramref name="branches"/>.
        /// </summary>
        /// <param name="branches">the branches to be considered; can be null in which
        /// case the filter will consider all existing branches</param>
        /// <returns>filter for C# files in Assets/Folder for given <paramref name="branches"/></returns>
        private static Filter GetFilter(params string[] branches)
        {
            return new Filter(globbing: new Globbing() { { "**/*.cs", true } },
                              repositoryPaths: new List<string>() { "Assets/SEE" },
                              branches: branches);
        }

        /// <summary>
        /// Returns a <see cref="GitRepository"/> instance for the SEE project.
        /// </summary>
        /// <returns>a git repository</returns>
        private static GitRepository GetRepository(params string[] branches)
        {
            return new(new DataPath(ProjectFolder()), GetFilter(branches));
        }

        [Test]
        // Same commit as both old and new commit.
        [TestCase("8e66aa028412984ce92c192d43feb106311c676c", "8e66aa028412984ce92c192d43feb106311c676c", 0)]
        // Old commit is an immediate predecessor of new commit.
        [TestCase("bbc9ba9246fa005be073ae2ae9b750b7f4450c97", "8e66aa028412984ce92c192d43feb106311c676c", 1)]
        // Old commit is an immediate successor of new commit.
        [TestCase("8e66aa028412984ce92c192d43feb106311c676c", "bbc9ba9246fa005be073ae2ae9b750b7f4450c97", 0)]
        // Old and new commit are on the same branch. New commit is a transitive successor of old commit.
        [TestCase("50b00fbecf51b76cbc15cb04293ea644ac6af100", "8e66aa028412984ce92c192d43feb106311c676c", 4)]
        // New commit is on a branch different from the master branch.
        // Its immediate predecessor ff243537f267195bf52fd99c6cf183aa4a58cb11 is on the same branch and is
        // a merge commit merging the master branch into the branch of new commit.
        // The branch of new commit has not yet been merged into the master branch.
        // That is, we should obtain all commits on the branch of new commit from the
        // point in time where this branch diverged from the master branch.
        // The following query identifes the number of all commits on the branch of new commit after
        // it was created (diverged from the master branch):
        //    git rev-list --no-merges origin..536a570161b5101013917bfc85c74a30c5963905|wc -l
        [TestCase("c75f364ef9f99d7688098405e07b866f3ea6539b", "536a570161b5101013917bfc85c74a30c5963905", 103)]
        public void TestCommitsBetween(string oldCommit, string newCommit, int expectedCount)
        {
            GitRepository repo = GetRepository(null); // all branches
            Performance p = Performance.Begin(nameof(GitRepository.CommitsBetween));
            IEnumerable<Commit> commits = repo.CommitsBetween(oldCommit, newCommit);
            p.End(true);
            //Debug.Log($"Number of commits between {oldCommit} and {newCommit}: {commits.Count()}\n");
            //Print(commits);
            Assert.AreEqual(expectedCount, commits.Count(), $"Expected {expectedCount} commits between {oldCommit} and {newCommit}, but found {commits.Count()}.");
        }

        /// <summary>
        /// Should be equivalent to
        ///   git rev-list --topo-order --reverse --no-merges 0878f91f900dc90d89c594c521ac1d3b9edd7097 ^a5fe5e6a2692f41aeb8448d5114000e6f82e605e
        /// </summary>
        [Test]
        public void TestCommitsBetweenContent()
        {
            string oldCommit = "a5fe5e6a2692f41aeb8448d5114000e6f82e605e";
            string newCommit = "0878f91f900dc90d89c594c521ac1d3b9edd7097";

            List<string> expected = new()
            {
                "806885d3aed509863808a66b011882a6823889ba",
                "6a1ae7731b98bfe254ccc82811858c2bf936ed98",
                "05aaf40797a4a58b4aa6318c030013482a3c9bf2",
                "fddf16442de5e98109aa90451193c8ef7d5305db",
                "c7578a5ec5bf0110c7b5be9f509214631cba7824",
                "c8ea6c1900bdf27dc7db520b33ba8af42519e74d",
                "a04da2cbd011bd6dde1bcbc86f5ff2f85712f633",
                "2b909fabee93817b1ed4545a8e676c68921921be",
                "4c0176e397c1e0d4695e011e60b4ff473266671b",
                "45d3c28d76e22941457b8eb16795df3dfaa1de19",
                "774c63339434cab3d1f2eae09234fc325586d88a",
                "964dcbce8ea171ff7f2c102738fa9d1b6ef72ff0",
                "1273f2d6e2f0c1cdd89148abc866ff5a1a6e5e1b",
                "0c692e6113547473e4dafeb3f40b74f2a9487b28",
                "d00d22723ada6b6051e3e5c8676c221c24ae3834",
                "592069c171503ca4a0c517ff481cd4391ea74514",
                "b2c0173386e995bda47c46ca5eaf31b26cf5290e",
                "316901098e3e182a11d1602c273b17ccb44a327f",
                "802d14248f7f1de62c3e37eba8adc709570cdd78",
                "4a06b3b2bbd33da363a4d59d16f702285ec1d835",
                "79b009835a16481da6bc73e916b0feac534fb922",
                "f4b0768d8619f022df6b2eea2effc3bc2bd9b620",
                "543067ae3b08c854c8a7bf5a04261517e9180e53",
                "b70f3ed7368b43fb74ee294d029d6b792fa272e4",
                "60c83927360d3a71fa007625efdd722d9132a2ff",
                "ccc9f30a6f7d9e0924bdf14331db10f4f7d935ac",
                "4c46422e123effd6c0bdad0dd1bf9317a9d78e64",
                "d4d747b9e70f28d56fbc842742bfed818acec4fc",
                "8cc105878c74efdab2483187210f925369bff88f",
                "5d47dd371a0f474423dce7ba935d1bd42b8ca943",
                "280bdc4ca1e7260e190097466da24515107355ee",
                "3d371ad3a3fae30da9624ed733b1becc8bcc0f2e",
                "0afa976812f375269957b7231d76e3bb0bbcdeb1",
                "af6b7cbdc5ef48627affbf687baaf0acf0721abe",
                "c8f376ed9ad2a3c6536592273f6d1de4896c9a8f",
                "c7651e3969ad4e583442a98e94583be1893202dd",
                "ab2b7cf0d6cbcccd6a1cf909b946258d3d78bb8b",
                "9956b161b2f62c6fc732dc713dd872cb4faed21d",
                "2cc81d4f95238ad7ade55dc0075dd3d3b8a4cbae",
                "10de19deb558de319f8be852b53ea7fffc78b99c",
                "9f9c1d52e57d62e86898be6e12d47647f6e0f814",
                "9502bbbbdee3c1d16657ab13b7c8c578c4cb1d96",
                "515a2b8b00aa5d7b9fce49146f2c4bcb03528589",
                "657d736b7fd951c6db26c5bbb4eb44ea2d94d840",
                "106504667d9d7cb511115b85a7e8177e6a980bdc",
                "7d2871b27ffefbd6ce132b5661bb7462eb3c8d70",
                "9f2f5f1cbb59388df89271f3f4fdd355ede65882",
                "f877e0352ae3c767bd73b6695d8c34b527c082c2",
                "1aa2fb74522ed4094d6f8f134bd8b489ff067a59",
                "f2e0034d1b14fa86a82ee9bfdc7b404821bd7232",
                "0b35b6dfdb8017dd54e3aa37dae296b484d9ca47",
                "f43b9d9796986681f1c5d2789c358615bedbf88a",
                "5ace28d94e8963dd9cb145b0c71bf7a6429f0387",
                "3ba03e0ce592113141928aac08234a0a32ec52a7",
                "8da105d133245a17dbf52e9433d326619fde43a4",
                "eaa4fddd354263a386caa44cb517e32dcde1eaae",
                "198a39853ba63cf24c5cb56eed94e9b1dad2dd41",
                "318524c61ec58b3517dfc1d4ccf1ed6eb91db379",
                "a92a6e97b10d94530bdbd74e39d4b309ed14206a",
                "0ec851d6ad8b1052ee3d4156645248075ac2b7da",
                "8c2bc0c019868de33b9232984cf3365c73bf845a",
                "3ad85eeab2b910f6e0360750e5b608dc279d33bc",
                "0d312123574751844ce522f578c277899ff80689",
                "89d38ad332781a1cf557d07a63794ad1c4092e08",
                "c461b6d94ca8b4eee15fb5420f05cf230826e4d4",
                "fbd9b51df8d9cdd9c7c9b1d946e9ff1972bc7d81",
                "4cc49e1270bc98cb216cd3926deac93dc7c45ec5",
                "09628ee8ed7992cc02fc0c74cd43c5b2ffb1dc99",
                "7d0d066de2544e2f0dd38fd31ca3a18bbcbe9c9f",
                "ffcef28bfe2e1fc2f86abb3e2354ced93d84e9b9",
                "bd629749464ca4d5c1cd611d519e133f14be5ca1",
                "6b32ac2f50bdd6a3f1f415d9eeaaa69b3a302f27",
                "b75532155a59421836e737825da2815dedf9a2ee",
                "f0ff46a5cf80275fc9139b69f3adf5f856fb5fef",
                "ac016ed3926e968517885e2f7b1aa8a1fbccd84e",
                "84fa7603974b21fa56b233378bca5d016932c8d3",
                "22e5fad21eac7235e326098fd5cdcaf91fed17b1",
                "e27e224ca6b8f4dfbfce403c71e8d08c65d98d67",
                "7d77b862746928008ce2d2f78c40c55d34a7a44a",
                "0878f91f900dc90d89c594c521ac1d3b9edd7097",
            };

            Globbing pathGlobbing = new()
                {
                    { "**/*", true }
                };

            GitRepository repo = new(new DataPath(Path.GetDirectoryName(Application.dataPath)),
                                     new Filter(globbing: pathGlobbing, repositoryPaths: null, branches: null));
            List<string> commits = repo.CommitsBetween(oldCommit, newCommit).Select(c => c.Sha).ToList();
            Assert.AreEqual(expected, commits);
        }

        /// <summary>
        /// Test for <see cref="GitRepository.CommitsAfter(DateTime)"/>.
        /// </summary>
        [Test]
        public void TestCommitsAfter()
        {
            DateTime date = new(2000, 1, 1);

            GitRepository repo = GetRepository();
            Performance p = Performance.Begin(nameof(GitRepository.CommitsAfter));
            IList<Commit> commits = repo.CommitsAfter(date);
            p.End(true);
            // commits.Count() should be the same as:
            //  git log --oneline --no-merges | wc -l
            Debug.Log($"Number of commits: {commits.Count()}\n");
            //Print(commits);
        }

        /// <summary>
        /// Test for <see cref="GitRepository.AllFiles()"/> on the master branch.
        /// /// Should be equivalent to <see cref="TestMasterFiles2"/>.
        /// </summary>
        [Test]
        public void TestMasterFiles1()
        {
            string branchName = "master";
            GitRepository repo = GetRepository(branchName);
            Performance p = Performance.Begin(nameof(GitRepository.AllFiles));
            HashSet<string> files = repo.AllFiles("HEAD");
            p.End(true);
            Debug.Log($"Number of files in branch '{branchName}': {files.Count()}\n");
            //Print(files);
        }

        /// <summary>
        /// Test for <see cref="GitRepository.AllFiles()"/> on the master branch.
        /// Should be equivalent to <see cref="TestMasterFiles1"/>.
        /// </summary>
        [Test]
        public void TestMasterFiles2()
        {
            string branchName = "master";
            GitRepository repo = GetRepository(branchName);
            Performance p = Performance.Begin(nameof(GitRepository.AllFiles));
            HashSet<string> files = repo.AllFiles();
            p.End(true);
            Debug.Log($"Number of files in branch '{branchName}': {files.Count()}\n");
            //Print(files);
        }

        /// <summary>
        /// Test for <see cref="GitRepository.AllFiles()"/> on all existing branches.
        /// </summary>
        [Test]
        public void TestAllBranchesFiles()
        {
            GitRepository repo = GetRepository();
            Performance p = Performance.Begin(nameof(GitRepository.AllFiles));
            HashSet<string> files = repo.AllFiles();
            p.End(true);
            Debug.Log($"Number of files: {files.Count()}\n");
            //Print(files);
        }

        /// <summary>
        /// Test for <see cref="Queries.AllFiles(Filter)"/> for particular branches
        /// given as complete branch names.
        /// </summary>
        [Test]
        public void TestSpecificBranchesFiles()
        {
            GitRepository repo = GetRepository("origin/645-debug-adapter-protocol",
                                               "origin/682-save-and-load-keybindings",
                                               "origin/723-git-metrics-in-diff-city");
            Performance p = Performance.Begin(nameof(GitRepository.AllFiles));
            HashSet<string> files = repo.AllFiles();
            p.End(true);
            Debug.Log($"Number of files: {files.Count()}\n");
            //Print(files);
        }

        /// <summary>
        /// Test for <see cref="GitRepository.AllFiles()"/> for particular branches
        /// matching a regular expression.
        /// </summary>
        [Test]
        public void TestRegularExpressionBranchesFiles()
        {
            GitRepository repo = GetRepository("71");
            Performance p = Performance.Begin(nameof(GitRepository.AllFiles));
            HashSet<string> files = repo.AllFiles();
            p.End(true);
            Debug.Log($"Number of files: {files.Count()}\n");
            //Print(files);
        }

        /// <summary>
        /// Test for <see cref="GitRepository.AllBranchNames()"/>.
        /// </summary>
        [Test]
        public void TestAllBranches()
        {
            GitRepository repo = GetRepository();
            Performance p = Performance.Begin(nameof(GitRepository.AllBranchNames));
            IEnumerable<string> branches = repo.AllBranchNames();
            p.End(true);
            Debug.Log($"Number of branches: {branches.Count()}\n");
            //Print(branches);
        }

        /// <summary>
        /// Diff of two successive commits in the repository.
        /// </summary>
        [Test]
        public void TestDiffImmediate()
        {
            Print(GetRepository().Diff("ea764b42cdd5d94ca3d1fc2a1f581c8d75409f22", "e9183c9e67448738f3428f22e05dec178bc383fb"));
        }

        /// <summary>
        /// Diff of two commits farther away in the repository.
        /// </summary>
        [Test]
        public void TestDiffLargerHistory()
        {
            Print(GetRepository().Diff("b0aa9acadf6f7ea7c90494099eccd9e431da3523", "95e392c4fd66df3a25c99bf64c4062725e0b0979"));
        }


        /// <summary>
        /// Test for <see cref="GitRepository.FetchRemotes()"/>.
        /// </summary>
        [Test]
        public void TestFetchRemotesForSEE()
        {
            GitRepository repo = GetRepository();
            if (string.IsNullOrWhiteSpace(repo.AccessToken))
            {
                Assert.Inconclusive($"No access token provided. Please add your GitHub access token to the {nameof(repo.AccessToken)} definition in {nameof(TestGitSEE)} to run the tests.");
            }
            Performance p = Performance.Begin(nameof(GitRepository.FetchRemotes));
            repo.FetchRemotes();
            p.End(true);
        }

        /// <summary>
        /// Lists all branches in the repository and reports whether they are remote
        /// and/or tracking.
        /// </summary>
        [Test]
        public void TestBranches()
        {
            using Repository repo = new(ProjectFolder());
            foreach (Branch b in repo.Branches)
            {
                Debug.Log($"Canonical={b.CanonicalName} FriendlyName={b.FriendlyName} IsRemote={b.IsRemote} IsTracking={b.IsTracking}\n");
            }
        }
    }
}
