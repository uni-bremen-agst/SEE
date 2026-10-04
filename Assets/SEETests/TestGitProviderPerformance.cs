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

        public async UniTask ProvideAsync(string gitDir, Globbing glob, string branch, string repoName)
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

        [Performance]
        public IEnumerator TestProvideSmallRepo()
        {
            return UniTask.ToCoroutine(async () =>
            {
                Measure.Method(() =>
                {
                    ProvideAsync("TestRepos/bubbletea", new Globbing() { { "**/*.go", true } }, "origin/main", "bubbletea").ToCoroutine();
                })
                .SampleGroup(new SampleGroup($"GitPerformance.SmallRepo", SampleUnit.Microsecond))
                .MeasurementCount(5)
                .Run();
            });
        }

        [Performance]
        public IEnumerator TestProvideMedium1Repo()
        {
            return UniTask.ToCoroutine(async () =>
            {
                Measure.Method(() =>
                {
                    ProvideAsync("TestRepos/express", new Globbing() { { "**/*.js", true } }, "origin/master", "express").ToCoroutine();
                })
                .SampleGroup(new SampleGroup($"GitPerformance.SmallRepo", SampleUnit.Microsecond))
                .MeasurementCount(5)
                .Run();
            });
        }

        [Performance]
        public IEnumerator TestProvideBig2Repo()
        {
            return UniTask.ToCoroutine(async () =>
            {
                Measure.Method(() =>
                {
                    ProvideAsync("TestRepos/node", new Globbing() { { "**/*.js", true }, }, "origin/main", "node").ToCoroutine();
                })
                .SampleGroup(new SampleGroup($"GitPerformance.SmallRepo", SampleUnit.Microsecond))
                .MeasurementCount(5)
                .Run();
            });
        }
    }
}
