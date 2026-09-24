using NUnit.Framework;
using SEE.DataModel.DG;
using SEE.GraphProviders.VCS;
using SEE.Utils;
using SEE.Utils.Paths;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace SEE.VCS
{
    /// <summary>
    /// Tests the graph derived for a range of commits, which is what
    /// <see cref="GraphProviders.BetweenCommitsGraphProvider"/> builds a city
    /// from. A run emits
    /// an account of the graph and a table of the files changed in the range,
    /// and holds both against what the run before produced.
    /// </summary>
    /// <remarks>
    /// The two commits are named by their SHA and never move, so this baseline
    /// is stronger than <see cref="TestGitFileChurn"/>'s: that one reports on
    /// branches, whose tips advance, so a difference there may be one of
    /// history. Here it cannot be. The graph is a function of the code and of
    /// two commits that are what they were, and any difference at all is the
    /// code having changed its answer.
    ///
    /// That is what this exists for. The implementation deriving this graph is
    /// to be replaced by <see cref="ChurnGraphGenerator"/>'s walk, and the
    /// account taken here beforehand is what the replacement will be held to.
    /// </remarks>
    internal class TestGitCommitRange
    {
        /// <summary>
        /// The commit the files are taken from and the changes are counted up
        /// to. The tip of master when this test was written.
        /// </summary>
        private const string commitID = "c57e48f86ec3729dbe8f1c106cfe437e079e30c0";

        /// <summary>
        /// The commit the changes are counted from, excluded itself. An ancestor
        /// of <see cref="commitID"/> a few hundred commits back: far enough to
        /// take in a fair spread of the history, near enough that the run stays
        /// short.
        /// </summary>
        private const string baselineCommitID = "e5fb7dfc727ed0d92fac7b52ddb11cb67cd1ef59";

        /// <summary>
        /// Derives the graph of the commits between <see cref="baselineCommitID"/>
        /// and <see cref="commitID"/>, emits an account of it and a table of the
        /// files changed to the console, and holds both against the baseline of
        /// the previous run.
        /// </summary>
        [Test]
        public void TestCommitRange()
        {
            GitRepository repositoryConfiguration = Configuration();
            string repositoryPath = repositoryConfiguration.RepositoryPath.Path;
            string repositoryName = Filenames.InnermostDirectoryName(repositoryPath);
            Graph graph = new(repositoryPath, repositoryName);

            Performance p = Performance.Begin($"Adding nodes for {repositoryName} commits "
                                              + $"{Short(baselineCommitID)}..{Short(commitID)}");
            ChurnGraphGenerator.AddNodesForCommit
                (graph: graph,
                 simplifyGraph: true,
                 repositoryConfiguration: repositoryConfiguration,
                 repositoryName: repositoryName,
                 commitID: commitID,
                 baselineCommitID: baselineCommitID,
                 computeCoFileChanges: true,
                 changePercentage: null,
                 token: default);
            p.End(true);

            ChurnReport.Check(graph);
            string report = ChurnReport.Of(graph, $"commits {Short(baselineCommitID)}.."
                                                  + Short(commitID));
            Debug.Log(report);
            Baseline.CompareOrWrite(TestContext.CurrentContext.Test.Name,
                                    Context(repositoryConfiguration), report);
        }

        /// <summary>
        /// The repository the graph is derived from, along with the filter
        /// stating which of its files are taken into account.
        /// </summary>
        /// <remarks>
        /// The globbing and the directories are the ones
        /// <see cref="TestGitFileChurn"/> uses, so that the two accounts may be
        /// held against one another. No branch is named: which commits are
        /// walked is settled by the two SHAs, not by the filter.
        /// </remarks>
        /// <returns>The repository configuration.</returns>
        private static GitRepository Configuration()
        {
            Filter filter = new
                (globbing: new Globbing() { { "**/*.cs", true } },
                 repositoryPaths: new string[] { "Assets/SEE", "Assets/SEETests" });
            return new GitRepository(new DataPath(DataPath.ProjectFolder()), filter);
        }

        /// <summary>
        /// The first seven characters of <paramref name="sha"/>, as git abbreviates
        /// one.
        /// </summary>
        /// <param name="sha">The SHA to be abbreviated.</param>
        /// <returns>The abbreviation.</returns>
        private static string Short(string sha)
        {
            return sha.Substring(0, 7);
        }

        /// <summary>
        /// The circumstances the graph was derived under, to be recorded beside
        /// the report and not compared with it.
        /// </summary>
        /// <remarks>
        /// No tip is named here, unlike in <see cref="TestGitFileChurn"/>: the
        /// commits reported on are named outright and cannot move, so a
        /// difference from the baseline is never one of history.
        /// </remarks>
        /// <param name="repositoryConfiguration">The repository the graph was derived
        /// from.</param>
        /// <returns>The circumstances.</returns>
        private static string Context(GitRepository repositoryConfiguration)
        {
            Filter filter = repositoryConfiguration.VCSFilter;
            StringBuilder result = new();
            result.AppendLine($"repository: {repositoryConfiguration.RepositoryPath.Path}");
            result.AppendLine($"commit:     {commitID}");
            result.AppendLine($"baseline:   {baselineCommitID}");
            result.AppendLine("paths:      "
                              + string.Join(", ", filter.RepositoryPaths
                                                  ?? Array.Empty<string>()));
            return result.ToString();
        }
    }
}
