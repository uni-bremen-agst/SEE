using LibGit2Sharp;
using NUnit.Framework;
using SEE.Utils;
using SEE.Utils.Paths;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace SEE.VCS
{
    /// <summary>
    /// Tests for <see cref="VCS.GitRepository"/>.
    /// </summary>
    internal class TestGitRepository
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

        /// <summary>
        /// Prints the given <paramref name="values"/>.
        /// </summary>
        /// <typeparam name="T">type of the values</typeparam>
        /// <param name="values">the values to be printed</param>
        private static void Print<T>(IEnumerable<T> values)
        {
            foreach (T value in values)
            {
                Debug.Log(value.ToString() + "\n");
            }
        }

        [Test]
        // Same commit as both old and new commit.
        [TestCase("8e66aa028412984ce92c192d43feb106311c676c", "8e66aa028412984ce92c192d43feb106311c676c", 0)]
        // Old commit is an immedidate predecessor of new commit.
        [TestCase("bbc9ba9246fa005be073ae2ae9b750b7f4450c97", "8e66aa028412984ce92c192d43feb106311c676c", 1)]
        // Old commit is an immedidate successor of new commit.
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
        //    git rev-list origin..536a570161b5101013917bfc85c74a30c5963905|wc -l
        [TestCase("c75f364ef9f99d7688098405e07b866f3ea6539b", "536a570161b5101013917bfc85c74a30c5963905", 109)]
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
        /// Prints the given <paramref name="patch"/> to the debug log.
        /// </summary>
        /// <param name="patch">to be printed</param>
        private static void Print(Patch patch)
        {
            foreach (PatchEntryChanges entry in patch)
            {
                Debug.Log($"Path: {entry.Path}\n");
                Debug.Log($"OldPath: {entry.OldPath}\n");
                Debug.Log($"Status: {entry.Status}\n");
                Debug.Log($"LinesAdded: {entry.LinesAdded}\n");
                Debug.Log($"LinesDeleted: {entry.LinesDeleted}\n");
                Debug.Log($"Patch: {entry.Patch}\n");

            }
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
