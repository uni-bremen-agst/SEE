using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using SEE.Graphs;
using SEE.Game.City;
using SEE.GraphProviders.VCS;
using SEE.Utils;
using SEE.Utils.Paths;
using SEE.VCS;
using Unity.PerformanceTesting;
using UnityEngine;
using UnityEngine.TestTools;

namespace SEE.GraphProviders
{
    /// <summary>
    /// A performance test for the <see cref="GitBranchesGraphProvider"/> class.
    /// </summary>
    public class TestGitProviderPerformance
    {
        /// <summary>
        /// The date in the version history at which to start adding nodes to the graph.
        /// </summary>
        private const string defaultDate = "2026/06/01";

        private async UniTask ProvideAsync(string gitDir, Globbing glob, string branch, string repoName)
        {

            GameObject go = new();

            BranchCity city = go.AddComponent<BranchCity>();
            GitRepository gitRepository = new(new DataPath(gitDir),
                                              new SEE.VCS.Filter(globbing: glob,
                                                                 branches: new List<string>() { branch }));
            static void ReportProgress(float x)
            {
                // Do nothing here
            }

            GitBranchesGraphProvider provider = new()
            {
                GitRepository = gitRepository,
                SimplifyGraph = true,
            };
            city.Date = defaultDate;

            Graph g = await provider.ProvideAsync(
                new Graph(""),
                city,
                changePercentage: ReportProgress
            );
        }

        /// <summary>
        /// Number of times <see cref="ProvideAsync"/> is run and measured per test.
        /// </summary>
        private const int measurementCount = 5;

        /// <summary>
        /// Runs <see cref="ProvideAsync"/> <see cref="measurementCount"/> times and
        /// records the duration of each run, including all of its asynchronous
        /// work, in <paramref name="sampleGroup"/>.
        /// </summary>
        /// <param name="sampleGroup">The sample group the measurements are recorded in.</param>
        /// <param name="gitDir">The directory of the git repository.</param>
        /// <param name="glob">The globbing filter for the files to be considered.</param>
        /// <param name="branch">The branch to be analyzed.</param>
        /// <param name="repoName">The name of the repository.</param>
        /// <returns>A coroutine running the measurements.</returns>
        private IEnumerator MeasureProvide(string sampleGroup, string gitDir, Globbing glob, string branch, string repoName)
        {
            return UniTask.ToCoroutine(async () =>
            {
                SampleGroup group = new(sampleGroup, SampleUnit.Microsecond);
                for (int i = 0; i < measurementCount; i++)
                {
                    using (Measure.Scope(group))
                    {
                        await ProvideAsync(gitDir, glob, branch, repoName);
                    }
                }
            });
        }

        [Performance]
        public IEnumerator TestProvideSmallRepo()
        {
            return MeasureProvide("GitPerformance.SmallRepo", "TestRepos/bubbletea",
                                  new Globbing() { { "**/*.go", true } }, "origin/main", "bubbletea");
        }

        [Performance]
        public IEnumerator TestProvideMedium1Repo()
        {
            return MeasureProvide("GitPerformance.SmallRepo", "TestRepos/express",
                                  new Globbing() { { "**/*.js", true } }, "origin/master", "express");
        }

        [Performance]
        public IEnumerator TestProvideBig2Repo()
        {
            return MeasureProvide("GitPerformance.SmallRepo", "TestRepos/node",
                                  new Globbing() { { "**/*.js", true } }, "origin/main", "node");
        }
    }
}
