using LibGit2Sharp;
using NUnit.Framework;
using SEE.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace SEE.VCS
{
    /// <summary>
    /// Tests for <see cref="SEE.VCS.Queries"/>.
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

        [Test]
        public void TestCommitsAfter()
        {
            DateTime date = new(2000, 1, 1);

            using Repository repo = new(ProjectFolder());
            Performance p = Performance.Begin(nameof(Queries.CommitsAfter));
            IEnumerable<Commit> commits = repo.CommitsAfter(date);
            p.End(true);
            // commits.Count() should be the same as:
            //  git log --oneline --no-merges | wc -l
            Debug.Log($"Number of commits: {commits.Count()}\n");
            //Print(commits);
        }

        [Test]
        public void TestListTree()
        {
            string branchName = "master";
            using Repository repo = new(ProjectFolder());
            Branch branch = repo.Branches[branchName];
            Commit tip = branch.Tip;
            LibGit2Sharp.Tree tree = tip.Tree;
            Performance p = Performance.Begin(nameof(Queries.ListTree));
            IEnumerable<string> files
                = Queries.ListTree(tree,
                                   PathGlobbing.ToMatcher(new Globbing() { { "Assets/SEE/**/*.cs", true } }));
            p.End(true);
            Debug.Log($"Number of files in branch '{branchName}': {files.Count()}\n");
            //Print(files);
        }

        [Test]
        public void TestAllFiles()
        {
            using Repository repo = new(ProjectFolder());
            Performance p = Performance.Begin(nameof(Queries.AllFiles));
            IEnumerable<string> files = repo.AllFiles(new Globbing() { { "Assets/SEE/**/*.cs", true } });
            p.End(true);
            Debug.Log($"Number of files: {files.Count()}\n");
            //Print(files);
        }

        [Test]
        public void TestAllBranches()
        {
            using Repository repo = new(ProjectFolder());
            Performance p = Performance.Begin(nameof(Queries.AllBranches));
            IEnumerable<string> branches = repo.AllBranches();
            p.End(true);
            Debug.Log($"Number of branches: {branches.Count()}\n");
            Print(branches);
        }

        [Test]
        public void TestBranches()
        {
            using Repository repo = new(ProjectFolder());
            foreach (Branch b in repo.Branches)
            {
                Debug.Log($"Canonical={b.CanonicalName} FriendlyName={b.FriendlyName} IsRemote={b.IsRemote} IsTracking={b.IsTracking}\n");
                Assert.IsFalse(b.IsRemote && b.IsTracking);
            }
        }
    }
}
