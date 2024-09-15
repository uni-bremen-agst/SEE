using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Cysharp.Threading.Tasks;
using LibGit2Sharp;
using NUnit.Framework;
using SEE.DataModel.DG;
using SEE.Game.City;
using SEE.GraphProviders;
using SEE.GraphProviders.Evolution;
using SEE.Utils.Paths;
using UnityEngine;
using UnityEngine.TestTools;

namespace SEETests
{
    /// <summary>
    /// Tests of <see cref="AllGitBranchesSingleGraphProvider"/> and
    /// <see cref="GitEvolutionGraphProvider"/>"/>.
    /// </summary>
    public class TestGitGraphProvider
    {
        /// <summary>
        /// Path to the git directory.
        /// </summary>
        private string gitDirPath;

        /// <summary>
        /// The Git repository.
        /// </summary>
        private Repository repo;

        /// <summary>
        /// Signature of one developer.
        /// </summary>
        private readonly Signature testSig = new("John Doe", "doe@example.com",
            new DateTimeOffset(2024, 04, 01, 1, 1, 1, TimeSpan.Zero));

        /// <summary>
        /// Signature of another developer.
        /// </summary>
        private readonly Signature testSig2 = new("Jan Muller", "muller@example.com",
            new DateTimeOffset(2024, 04, 01, 1, 1, 1, TimeSpan.Zero));

        /// <summary>
        /// Creates a new file in the path <paramref name="path"/> and fills or appends the file with
        /// the given <paramref name="text"/>.
        ///
        /// Then a git commit is made
        /// </summary>
        /// <param name="path">The path of the file</param>
        /// <param name="text">The text the file should have</param>
        /// <param name="author">The author of the commit</param>
        private void WriteFile(string path, string text, Signature author)
        {
            if (Path.GetDirectoryName(path) != "")
            {
                Directory.CreateDirectory(Path.Combine(gitDirPath, Path.GetDirectoryName(path)));
            }

            File.AppendAllText(Path.Combine(gitDirPath, path), text);
            repo.Index.Add(path);
            repo.Index.Write();
            testSig.When.AddHours(1);
            repo.Commit("One Commit", author, author);
        }

        /// <summary>
        /// Executes the graph provider.
        /// </summary>
        /// <param name="date">An optional date limit for the graph provider</param>
        /// <returns>The generated Graph</returns>
        private async UniTask<Graph> ProvidingGraphAsync(string date = "01/01/2024")
        {
            GameObject go = new();
            BranchCity city = go.AddComponent<BranchCity>();
            city.VCSPath = new DataPath(gitDirPath);
            AllGitBranchesSingleGraphProvider provider = new();
            provider.PathGlobbing = new Dictionary<string, bool>()
            {
                { "**/*.cs", true }
            };
            city.Date = date;

            static void ReportProgress(float x)
            {
                // Do nothing here
            }

            Graph g = await provider.ProvideAsync(new Graph(""), city, changePercentage: ReportProgress);
            return g;
        }

        /// <summary>
        /// Executes the graph provider.
        /// </summary>
        /// <param name="date">An optional date limit for the graph provider</param>
        /// <returns>The generated Graph</returns>
        private async UniTask<IList<Graph>> ProvidingGraphSeriesAsync(string date = "01/01/2024")
        {
            GameObject go = new();
            SEECityEvolution city = go.AddComponent<SEECityEvolution>();

            GitEvolutionGraphProvider provider = new();
            provider.Date = date;
            provider.GitRepository = new GitRepository()
            {
                RepositoryPath = new DataPath(gitDirPath),
                PathGlobbing = new Dictionary<string, bool>() { { "**/*.cs", true } }
            };

            static void ReportProgress(float x)
            {
                // Do nothing here
            }

            List<Graph> g = await provider.ProvideAsync(new List<Graph>(), city, changePercentage: ReportProgress);
            return g;
        }

        [UnityTest]
        public IEnumerator TestGitEvolutionProvider()
        {
            return UniTask.ToCoroutine(async () =>
            {
                WriteFile("firstFile.cs", "This is a test", testSig);
                WriteFile("AnotherFile.cs", "This is a test", testSig);
                WriteFile("AnotherFile.cs", "This is a test", testSig2);

                IList<Graph> series = await ProvidingGraphSeriesAsync();
                Assert.AreEqual(3, series.Count);

                Assert.AreEqual(1, series[0].GetNode("firstFile.cs-Evo").IntAttributes[VCS.CommitFrequency]);

                Assert.AreEqual(1, series[1].GetNode("AnotherFile.cs-Evo").IntAttributes[VCS.CommitFrequency]);
                Assert.AreEqual(1, series[1].GetNode("firstFile.cs-Evo").IntAttributes[VCS.CommitFrequency]);

                Assert.AreEqual(2, series[2].GetNode("AnotherFile.cs-Evo").IntAttributes[VCS.CommitFrequency]);
                Assert.AreEqual(1, series[2].GetNode("firstFile.cs-Evo").IntAttributes[VCS.CommitFrequency]);
            });
        }

        [UnityTest]
        public IEnumerator TestGitProviderForMultipleFiles()
        {
            return UniTask.ToCoroutine(async () =>
            {
                WriteFile("firstFile.cs", "This is a test", testSig);
                WriteFile("AnotherFile.cs", "This is a test", testSig);
                WriteFile("AnotherFile.cs", "This is a test", testSig2);
                WriteFile(Path.Combine("dir1", "dir2", "actualFile.cs"), "This is a test", testSig2);

                Graph g = await ProvidingGraphAsync();
                Assert.NotNull(g.GetNode("firstFile.cs"));
                Node n1 = g.GetNode("firstFile.cs");
                Assert.AreEqual(1, n1.IntAttributes[VCS.CommitFrequency]);
                Assert.AreEqual(1, n1.IntAttributes[VCS.NumberOfDevelopers]);


                Assert.NotNull(g.GetNode("AnotherFile.cs"));
                Node n2 = g.GetNode("AnotherFile.cs");
                Assert.AreEqual(2, n2.IntAttributes[VCS.CommitFrequency]);
                Assert.AreEqual(2, n2.IntAttributes[VCS.NumberOfDevelopers]);


                Assert.NotNull(g.GetNode("dir1/dir2/actualFile.cs"));
            });
        }

        [UnityTest]
        public IEnumerator TestGitProviderForTooOldFile()
        {
            return UniTask.ToCoroutine(async () =>
            {
                WriteFile("firstFile.cs", "This is a test", testSig);

                Graph g = await ProvidingGraphAsync(date: "01/12/2024");
                // This file should be too old by now
                Assert.AreEqual(0,g.GetNode("firstFile.cs").IntAttributes[VCS.NumberOfDevelopers]);
                Assert.AreEqual(0, g.GetNode("firstFile.cs").IntAttributes[VCS.CommitFrequency]);
                Graph g2 = await ProvidingGraphAsync();
                Assert.NotNull(g2.GetNode("firstFile.cs"));
                Node n = g2.GetNode("firstFile.cs");
                Assert.AreEqual(1, n.IntAttributes[VCS.CommitFrequency]);
                Assert.AreEqual(1, n.IntAttributes[VCS.NumberOfDevelopers]);
            });
        }

        [UnityTest]
        public IEnumerator TestGitProviderMultipleAuthors()
        {
            return UniTask.ToCoroutine(async () =>
            {
                WriteFile("firstFile.cs", "This is a test", testSig);
                WriteFile("firstFile.cs", "This is a test from Jan", testSig2);

                Graph g = await ProvidingGraphAsync();
                // Check data of firstFile.cs
                Assert.DoesNotThrow(() => g.GetNode("firstFile.cs"));
                Assert.NotNull(g.GetNode("firstFile.cs"));
                Node n = g.GetNode("firstFile.cs");
                Assert.AreEqual(2, n.IntAttributes[VCS.CommitFrequency]);
                Assert.AreEqual(2, n.IntAttributes[VCS.NumberOfDevelopers]);
            });
        }

        [UnityTest]
        public IEnumerator TestGitProviderMultipleBranches()
        {
            return UniTask.ToCoroutine(async () =>
            {
                WriteFile("firstFile.cs", "This is a test", testSig);
                repo.CreateBranch("newBranch");
                WriteFile("firstFile.cs", "This is a test on newBranch", testSig);
                Commands.Checkout(repo, repo.Branches["master"]);

                Graph g = await ProvidingGraphAsync();
                // Check data of firstFile.cs
                Assert.DoesNotThrow(() => g.GetNode("firstFile.cs"));
                Assert.NotNull(g.GetNode("firstFile.cs"));
                Node n = g.GetNode("firstFile.cs");
                Assert.AreEqual(2, n.IntAttributes[VCS.CommitFrequency]);
                Assert.AreEqual(1, n.IntAttributes[VCS.NumberOfDevelopers]);
            });
        }

        [UnityTest]
        public IEnumerator TestGitProviderMultipleCommitsButWithUnrelatedFile()
        {
            return UniTask.ToCoroutine(async () =>
            {
                WriteFile("firstFile.cs", "This is a test", testSig);
                WriteFile("firstFile.cs", "This is another test", testSig);
                WriteFile("otherfile.notcs", "This is another test in another file", testSig);

                Graph g = await ProvidingGraphAsync();
                // Check data of firstFile.cs
                Assert.DoesNotThrow(() => g.GetNode("firstFile.cs"));
                Assert.NotNull(g.GetNode("firstFile.cs"));
                Node n = g.GetNode("firstFile.cs");
                Assert.AreEqual(2, n.IntAttributes[VCS.CommitFrequency]);
                Assert.AreEqual(1, n.IntAttributes[VCS.NumberOfDevelopers]);

                Assert.IsNull(g.GetNode("otherfile.notcs"));
            });
        }

        [UnityTest]
        public IEnumerator TestGitProviderMultipleCommits()
        {
            return UniTask.ToCoroutine(async () =>
            {
                WriteFile("firstFile.cs", "This is a test", testSig);
                WriteFile("firstFile.cs", "This is another test", testSig);

                Graph g = await ProvidingGraphAsync();
                // Check data of firstFile.cs
                Assert.DoesNotThrow(() => g.GetNode("firstFile.cs"));
                Assert.NotNull(g.GetNode("firstFile.cs"));
                Node n = g.GetNode("firstFile.cs");
                Assert.AreEqual(2, n.IntAttributes[VCS.CommitFrequency]);
                Assert.AreEqual(1, n.IntAttributes[VCS.NumberOfDevelopers]);
            });
        }

        [UnityTest]
        public IEnumerator TestGitProviderFileDoesNotExist()
        {
            return UniTask.ToCoroutine(async () =>
                {
                    WriteFile("firstFile.cs", "This is a test", testSig);

                    Graph g = await ProvidingGraphAsync();
                    Assert.IsNull(g.GetNode("file/does/not/exists"));
                }
            );
        }

        [UnityTest]
        public IEnumerator TestGitProviderSingleFileCommit()
        {
            return UniTask.ToCoroutine(async () =>
            {
                WriteFile("firstFile.cs", "This is a test", testSig);

                Graph g = await ProvidingGraphAsync();
                // Check data of firstFile.cs
                Assert.DoesNotThrow(() => g.GetNode("firstFile.cs"));
                Assert.NotNull(g.GetNode("firstFile.cs"));
                Node n = g.GetNode("firstFile.cs");
                Assert.AreEqual(1, n.IntAttributes[VCS.CommitFrequency]);
                Assert.AreEqual(1, n.IntAttributes[VCS.NumberOfDevelopers]);
            });
        }

        [SetUp, UnitySetUp]
        public void Setup()
        {
            gitDirPath = Path.GetTempPath() + "seeGitTest";
            Directory.CreateDirectory(gitDirPath);
            Debug.Log($"Created a temporary Git repository at {gitDirPath}\n");
            repo = new Repository(Repository.Init(gitDirPath));
        }

        [TearDown, UnityTearDown]
        public void TearDown()
        {
            repo?.Dispose();
            if (Directory.Exists(gitDirPath))
            {
                SEE.Utils.Filenames.DeleteReadOnlyDirectory(gitDirPath);
            }
        }
    }
}
