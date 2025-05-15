using LibGit2Sharp;
using NUnit.Framework;
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
        public static string ProjectFolder()
        {
            return Regex.Replace(Application.dataPath, "/Assets$", string.Empty);
        }

        [Test]
        public void TestCommitsAfter()
        {
            DateTime date = new(2000, 1, 1);

            using Repository repo = new(ProjectFolder());
            IEnumerable<Commit> commits = repo.CommitsAfter(date);
            // commits.Count() should be the same as:
            //  git log --oneline --no-merges | wc -l
            Debug.Log($"Number of commits: {commits.Count()}\n");
            //Print(commits);
        }

        private static void Print(IEnumerable<Commit> commits)
        {
            foreach (Commit commit in commits)
            {
                Debug.Log(commit.ToString() + "\n");
            }
        }
    }
}
