using LibGit2Sharp;
using NUnit.Framework;
using SEE.GraphProviders;
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
    internal class TestVCSQueries
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
            foreach (T commit in values)
            {
                Debug.Log(commit.ToString() + "\n");
            }
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
            Print(branches);
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
