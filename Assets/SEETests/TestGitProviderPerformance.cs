using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using SEE.DataModel.DG;
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
    public class TestGitProviderPerformance
    {
        private const string defaultDate = "2026/06/01";

        public async UniTask Provide(string gitDir, Globbing glob, List<string> repoPaths, string branch, string repoName)
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

            GitGraphGenerator.AddNodesAfterDate(new Graph(), false, gitRepository, repoName, DateTime.Now, false, null, false, ReportProgress, default);

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
                    Provide("TestRepos/bubbletea", new Globbing() { { "**/*.go", true } }, null, "origin/main", "bubbletea").ToCoroutine();
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
                    Provide("TestRepos/express", new Globbing() { { "**/*.js", true } }, null, "origin/master", "express").ToCoroutine();
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
                    Provide("TestRepos/node", new Globbing() { { "**/*.js", true }, }, null, "origin/main", "node").ToCoroutine();
                })
                .SampleGroup(new SampleGroup($"GitPerformance.SmallRepo", SampleUnit.Microsecond))
                .MeasurementCount(5)
                .Run();
            });
        }
    }
}
