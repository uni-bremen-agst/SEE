using NUnit.Framework;
using SEE.DataModel.DG;
using SEE.Utils;
using SEE.Utils.Paths;
using SEE.VCS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace SEE.GraphProviders.VCS
{
    /// <summary>
    /// Tests for <see cref="GitGraphGenerator"/>. They exist to be run under a
    /// profiler and to report the elapsed time to the console, and they hold an
    /// account of the graph derived against the one the run before derived, so
    /// that a change to the code deriving it does not pass unnoticed.
    /// </summary>
    /// <remarks>
    /// The graph is the one <see cref="GitGraphGenerator"/> produces, which
    /// <see cref="TestGitFileChurn"/> does not exercise: that test walks the
    /// history itself. Nothing else guards what the providers of SEE build,
    /// and a change to the date a commit is taken from, say, shows here and
    /// nowhere else.
    /// </remarks>
    internal class TestGitGraphGeneratorPerformance
    {
        /// <summary>
        /// The date of the oldest commit to be taken into account. The further
        /// back this reaches, the longer <see cref="TestAddNodesAfterDate"/> runs.
        ///
        /// The same date <see cref="TestGitFileChurn"/> reports from, as are the
        /// directories and the branches below, so that the two accounts may be
        /// held against one another. A date denotes the instant its day begins
        /// at in UTC, which is the instant that test names outright.
        /// </summary>
        private static readonly DateTime startDate = new(2025, 1, 1);

        /// <summary>
        /// Measures the time to derive a graph from the commits of the SEE
        /// repository itself since <see cref="startDate"/>.
        /// </summary>
        /// <remarks>
        /// The <see cref="DeepProfiler"/> scope below yields a dotTrace snapshot
        /// covering only the generation, with every method transitively called in
        /// it. How to set dotTrace up for this is documented at
        /// <see cref="DataModel.DG.IO.TestGraphIO.TestReadingRealBigGraph"/>; the
        /// essential part is that "Control profiling" must be set to "Using API",
        /// because otherwise the calls made here are inert.
        ///
        /// The profiling type has to be Timeline. dotTrace offers no other type for
        /// a Unity target: "Only the Timeline type is supported for profiling a
        /// Mono/Unity application." A Timeline snapshot can only be read by
        /// dotTrace's Timeline viewer; it is not a .dtp snapshot and the
        /// command-line Reporter cannot turn it into a report.
        ///
        /// Unlike <see cref="DataModel.DG.IO.TestGraphIO.TestReadingRealBigGraph"/>,
        /// the work here runs synchronously on the thread of this test method, so
        /// its call tree is the one to look at in the viewer.
        /// </remarks>
        [Test]
        public void TestAddNodesAfterDate()
        {
            Graph graph = new();
            DataPath seeProjectPath = new(DataPath.ProjectFolder());
            Filter filter = new()
            {
                Globbing = new Globbing() { { "**/*.cs", true } },
                RepositoryPaths = new string[] { "Assets/SEE", "Assets/SEETests" },
                Branches = new HashSet<string>() { "master", "origin/.*" }
            };
            GitRepository repository = new(seeProjectPath, filter);

            Performance p = Performance.Begin($"Adding nodes for SEE commits since {startDate:yyyy-MM-dd}");
            using (DeepProfiler.Capture(nameof(TestAddNodesAfterDate)))
            {
                GitGraphGenerator.AddNodesAfterDate(
                  graph: graph,
                  simplifyGraph: true,
                  repositoryConfiguration: repository,
                  repositoryName: "SEE",
                  startDate: startDate,
                  computeCoFileChanges: true,
                  changePercentage: null,
                  token: default
                );
            }
            p.End(true);

            Debug.Log(GraphAccount.Of(graph));
            Baseline.CompareOrWrite(TestContext.CurrentContext.Test.Name,
                                    Context(repository), GraphAccount.Of(graph));
        }

        /// <summary>
        /// The circumstances the graph was derived under, to be recorded beside
        /// the account of it and not compared with it.
        /// </summary>
        /// <remarks>
        /// The tip of every branch the filter holds relevant is named, so that
        /// an account differing from its baseline because the repository has
        /// gained commits can be told from one differing because the code
        /// deriving the graph has changed.
        /// </remarks>
        /// <param name="repository">The repository the graph was derived from.</param>
        /// <returns>The circumstances.</returns>
        private static string Context(GitRepository repository)
        {
            StringBuilder result = new();
            result.AppendLine($"repository: {repository.RepositoryPath.Path}");
            result.AppendLine($"since:      {startDate:o}");
            result.AppendLine("paths:      "
                              + string.Join(", ", repository.VCSFilter.RepositoryPaths
                                                  ?? Array.Empty<string>()));
            result.AppendLine("branches:   "
                              + string.Join(", ", repository.VCSFilter.Branches
                                                  ?? Enumerable.Empty<string>()));
            using GitRepositorySession session = repository.OpenGitSession();
            foreach (LibGit2Sharp.Branch branch in session.RelevantBranches()
                                             .OrderBy(branch => branch.FriendlyName,
                                                      StringComparer.Ordinal))
            {
                result.AppendLine($"tip {branch.FriendlyName} {branch.Tip.Sha}");
            }
            return result.ToString();
        }
    }
}
