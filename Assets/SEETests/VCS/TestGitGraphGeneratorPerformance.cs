using NUnit.Framework;
using SEE.DataModel.DG;
using SEE.Utils;
using SEE.Utils.Paths;
using SEE.VCS;
using System;
using System.Collections.Generic;

namespace SEE.GraphProviders.VCS
{
    /// <summary>
    /// Performance tests for <see cref="GitGraphGenerator"/>. These do not assert
    /// anything; they exist to be run under a profiler and to report the elapsed
    /// time to the console.
    /// </summary>
    internal class TestGitGraphGeneratorPerformance
    {
        /// <summary>
        /// The date of the oldest commit to be taken into account. The further
        /// back this reaches, the longer <see cref="TestAddNodesAfterDate"/> runs.
        /// </summary>
        private static readonly DateTime startDate = new(2025, 10, 1);

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
                RepositoryPaths = new string[] { "Assets/SEE" },
                Branches = new HashSet<string>() { "origin/master" } // FIXME: All branches
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
        }
    }
}
