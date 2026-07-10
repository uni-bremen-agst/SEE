using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.Http.Headers;
using Cysharp.Threading.Tasks;
using Dissonance;
using NUnit.Framework;
using SEE.DataModel.DG;
using SEE.Game.City;
using SEE.GraphProviders.VCS;
using SEE.Net.Actions.Drawable;
using SEE.Utils;
using SEE.Utils.Paths;
using SEE.VCS;
using Unity.PerformanceTesting;
using UnityEditor.PackageManager.UI;
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

        [UnityTest, Performance]
        public IEnumerator TestProvideSmallRepo()
        {
            return UniTask.ToCoroutine(async () =>
            {
                Measure.Method(() =>
                {
                    Provide("/home/maakinoh/Development/SEE/TestRepos/bubbletea", new Globbing() { { "**/*.go", true } }, null, "origin/main", "bubbletea").ToCoroutine();
                })
                .SampleGroup(new SampleGroup($"GitPerformance.SmallRepo", SampleUnit.Microsecond))
                .MeasurementCount(5)
                .Run();
            });
        }

        [UnityTest, Performance]
        public IEnumerator TestProvideMedium1Repo()
        {
            return UniTask.ToCoroutine(async () =>
            {
                Measure.Method(() =>
                {
                    Provide("/home/maakinoh/Development/SEE/TestRepos/express", new Globbing() { { "**/*.js", true } }, null, "origin/master", "express").ToCoroutine();
                })
                .SampleGroup(new SampleGroup($"GitPerformance.SmallRepo", SampleUnit.Microsecond))
                .MeasurementCount(5)
                .Run();
            });
        }

        // [UnityTest, Performance]
        // public IEnumerator TestProvideMedium2Repo()
        // {
        //     return UniTask.ToCoroutine(async () =>
        //     {
        //         Provide("/home/maakinoh/Development/SEE/TestRepos/numpy", new Globbing() { { "**/*.py", true }, { "**/*.c", true }, { "**/*.h", true } }, null, "origin/main", "numpy").ToCoroutine();
        //     });
        // }


        // [UnityTest, Performance]
        // public IEnumerator TestProvideBig1Repo()
        // {
        //     return UniTask.ToCoroutine(async () =>
        //     {
        //         Provide("/home/maakinoh/Development/SEE/TestRepos/godot", new Globbing() { { "**/*.cpp", true } }, null, "origin/master", "godot").ToCoroutine();
        //     });
        // }

        [UnityTest, Performance]
        public IEnumerator TestProvideBig2Repo()
        {
            return UniTask.ToCoroutine(async () =>
            {
                Measure.Method(() =>
                {
                    Provide("/home/maakinoh/Development/SEE/TestRepos/node", new Globbing() { { "**/*.js", true }, }, null, "origin/main", "node").ToCoroutine();
                })
                .SampleGroup(new SampleGroup($"GitPerformance.SmallRepo", SampleUnit.Microsecond))
                .MeasurementCount(5)
                .Run();
            });
        }

        // [UnityTest, Performance]
        // public IEnumerator TestProvideExtremeRepo()
        // {
        //     return UniTask.ToCoroutine(async () =>
        //     {
        //         Provide("/home/maakinoh/Development/SEE/TestRepos/linux", new Globbing() { { "**/*.c", true } }, null, "origin/master", "linux").ToCoroutine();
        //     });
        // }


    }
}
