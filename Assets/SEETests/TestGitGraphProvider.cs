using Cysharp.Threading.Tasks;
using LibGit2Sharp;
using NUnit.Framework;
using SEE.DataModel.DG;
using SEE.Game.City;
using SEE.GraphProviders.Evolution;
using SEE.Utils;
using SEE.Utils.Paths;
using SEE.VCS;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.TestTools;
using static SEE.DataModel.DG.VCS;

namespace SEE.GraphProviders
{
    /// <summary>
    /// Tests of <see cref="GitBranchesGraphProvider"/> and
    /// <see cref="GitEvolutionGraphProvider"/>"/>.
    /// </summary>
    public class TestGitGraphProvider
    {
        /// <summary>
        /// The default value for the start date of relevant commits.
        /// </summary>
        private const string defaultDate = "2024/01/01";

        /// <summary>
        /// The name of the file that most test cases write to and then expect
        /// as a node in the resulting graph. It is also the node's ID.
        /// </summary>
        private const string firstFile = "firstFile.cs";

        /// <summary>
        /// The name of a second file used by the test cases that need more than
        /// one file. It is also the node's ID.
        /// </summary>
        private const string anotherFile = "AnotherFile.cs";

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
        private readonly Signature developerA =
            new(
                "John Doe",
                "doe@example.com",
                new DateTimeOffset(2024, 04, 01, 1, 1, 1, TimeSpan.Zero)
            );

        /// <summary>
        /// Signature of another developer.
        /// </summary>
        private readonly Signature developerB =
            new(
                "Jan Muller",
                "muller@example.com",
                new DateTimeOffset(2024, 04, 01, 1, 1, 1, TimeSpan.Zero)
            );

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
            developerA.When.AddHours(1);
            repo.Commit("One Commit", author, author);
        }

        /// <summary>
        /// Executes the graph provider.
        /// </summary>
        /// <param name="date">An optional date limit for the graph provider</param>
        /// <param name="branches">Optional branch filters for the graph provider.</param>
        /// <returns>The generated Graph</returns>
        private async UniTask<Graph> ProvidingGraphAsync(string date = defaultDate, IEnumerable<string> branches = null)
        {
            GameObject go = new();
            BranchCity city = go.AddComponent<BranchCity>();
            GitRepository gitRepository = new(new DataPath(gitDirPath),
                                              new SEE.VCS.Filter(globbing: new Globbing() { { "**/*.cs", true } },
                                                                 repositoryPaths: null,
                                                                 branches: branches));
            GitBranchesGraphProvider provider = new()
            {
                GitRepository = gitRepository,
                SimplifyGraph = true,
            };
            city.Date = date;

            static void ReportProgress(float x)
            {
                // Do nothing here
            }

            Graph g = await provider.ProvideAsync(
                new Graph(""),
                city,
                changePercentage: ReportProgress
            );
            return g;
        }

        /// <summary>
        /// Executes the graph provider.
        /// </summary>
        /// <param name="date">An optional date limit for the graph provider</param>
        /// <returns>The generated Graph</returns>
        private async UniTask<IList<Graph>> ProvidingGraphSeriesAsync(string date = defaultDate)
        {

            GitRepository gitRepository = new(new DataPath(gitDirPath),
                                              new SEE.VCS.Filter(globbing: new Globbing() { { "**/*.cs", true } },
                                                                 repositoryPaths: null,
                                                                 branches: null));
            GitEvolutionGraphProvider provider = new()
            {
                Date = date,
                GitRepository = gitRepository
            };

            static void ReportProgress(float x)
            {
                // Do nothing here
            }

            List<Graph> g = await provider.ProvideAsync(
                new List<Graph>(),
                null, // This is not used in the evolution provider.
                changePercentage: ReportProgress
            );
            return g;
        }

        [Ignore("GitEvolutionGraphProvider is not implemented yet.")]
        [UnityTest]
        public IEnumerator TestGitEvolutionProvider()
        {
            return UniTask.ToCoroutine(async () =>
            {
                WriteFile(firstFile, "This is a test", developerA);
                WriteFile(anotherFile, "This is a test", developerA);
                WriteFile(anotherFile, "This is a test", developerB);

                IList<Graph> series = await ProvidingGraphSeriesAsync();
                Assert.That(series.Count, Is.EqualTo(3));

                Assert.That(series[0].GetNode(firstFile).IntAttributes[NumberOfCommits],
                            Is.EqualTo(1));

                Assert.That(series[1].GetNode(anotherFile).IntAttributes[NumberOfCommits],
                            Is.EqualTo(1));
                Assert.That(series[1].GetNode(firstFile).IntAttributes[NumberOfCommits],
                            Is.EqualTo(1));

                Assert.That(series[2].GetNode(anotherFile).IntAttributes[NumberOfCommits],
                            Is.EqualTo(2));
                Assert.That(series[2].GetNode(firstFile).IntAttributes[NumberOfCommits],
                            Is.EqualTo(1));
            });
        }

        [UnityTest]
        public IEnumerator TestGitProviderForMultipleFiles()
        {
            return UniTask.ToCoroutine(async () =>
            {
                WriteFile(firstFile, "This is a test", developerA);
                WriteFile(anotherFile, "This is a test", developerA);
                WriteFile(anotherFile, "This is a test", developerB);
                WriteFile(
                    Path.Combine("dir1", "dir2", "actualFile.cs"),
                    "This is a test",
                    developerB
                );

                Graph g = await ProvidingGraphAsync();
                Assert.That(g.GetNode(firstFile), Is.Not.Null, $"There is no node {firstFile}.");
                Node n1 = g.GetNode(firstFile);
                Assert.That(n1.IntAttributes[NumberOfCommits], Is.EqualTo(1));
                Assert.That(n1.IntAttributes[NumberOfDevelopers], Is.EqualTo(1));

                Assert.That(g.GetNode(anotherFile), Is.Not.Null, $"There is no node {anotherFile}.");
                Node n2 = g.GetNode(anotherFile);
                Assert.That(n2.IntAttributes[NumberOfCommits], Is.EqualTo(2));
                Assert.That(n2.IntAttributes[NumberOfDevelopers], Is.EqualTo(2));

                Assert.That(g.GetNode("dir1/dir2/actualFile.cs"), Is.Not.Null,
                            "There is no node dir1/dir2/actualFile.cs.");
            });
        }

        [UnityTest]
        public IEnumerator TestGitProviderForTooOldFile()
        {
            return UniTask.ToCoroutine(async () =>
            {
                WriteFile(firstFile, "This is a test", developerA);

                Graph g = await ProvidingGraphAsync(date: "2024/12/01");
                // This file should be too old by now
                Assert.That(g.GetNode(firstFile).IntAttributes[NumberOfDevelopers], Is.EqualTo(0));
                Assert.That(g.GetNode(firstFile).IntAttributes[NumberOfCommits], Is.EqualTo(0));
                Graph g2 = await ProvidingGraphAsync();
                Assert.That(g2.GetNode(firstFile), Is.Not.Null, $"There is no node {firstFile}.");
                Node n = g2.GetNode(firstFile);
                Assert.That(n.IntAttributes[NumberOfCommits], Is.EqualTo(1));
                Assert.That(n.IntAttributes[NumberOfDevelopers], Is.EqualTo(1));
            });
        }

        [Test]
        public void TestCommitsAfterIncludesCommitWithOlderCommitterDate()
        {
            DateTime startDate = new(2024, 01, 01);
            Signature author = new("John Doe", "doe@example.com",
                                   new DateTimeOffset(2024, 04, 01, 1, 1, 1, TimeSpan.Zero));
            Signature committer = new("Jan Mueller", "mueller@example.com",
                                      new DateTimeOffset(2023, 12, 01, 1, 1, 1, TimeSpan.Zero));

            File.WriteAllText(Path.Join(gitDirPath, firstFile), "This is a test");
            Commands.Stage(repo, firstFile);
            Commit commit = repo.Commit("Commit with an older committer date", author, committer);

            GitRepository gitRepository = new(new DataPath(gitDirPath), new SEE.VCS.Filter());
            using GitRepositorySession session = gitRepository.OpenGitSession();

            Assert.That(session.CommitsAfter(startDate), Does.Contain(commit.Sha));
        }

        [Test]
        public void TestCommitsAfterContinuesPastRebasedCommit()
        {
            DateTime startDate = new(2024, 01, 01);
            Signature qualifyingDate = new("John Doe", "doe@example.com",
                                           new DateTimeOffset(2024, 02, 01, 1, 1, 1, TimeSpan.Zero));

            File.WriteAllText(Path.Join(gitDirPath, firstFile), "This is a test");
            Commands.Stage(repo, firstFile);
            Commit qualifyingCommit = repo.Commit("Qualifying commit", qualifyingDate, qualifyingDate);

            Signature originalAuthor = new("Jan Mueller", "mueller@example.com",
                                           new DateTimeOffset(2023, 12, 01, 1, 1, 1, TimeSpan.Zero));
            Signature rebaseCommitter = new("Jan Mueller", "mueller@example.com",
                                            new DateTimeOffset(2024, 04, 01, 1, 1, 1, TimeSpan.Zero));
            File.WriteAllText(Path.Join(gitDirPath, anotherFile), "This is another test");
            Commands.Stage(repo, anotherFile);
            Commit rebasedCommit = repo.Commit("Rebased commit", originalAuthor, rebaseCommitter);

            GitRepository gitRepository = new(new DataPath(gitDirPath), new SEE.VCS.Filter());
            using GitRepositorySession session = gitRepository.OpenGitSession();
            IList<string> commits = session.CommitsAfter(startDate);

            Assert.That(commits, Does.Contain(qualifyingCommit.Sha));
            Assert.That(commits, Does.Not.Contain(rebasedCommit.Sha));
        }

        [UnityTest]
        public IEnumerator TestGitProviderMultipleAuthors()
        {
            return UniTask.ToCoroutine(async () =>
            {
                WriteFile(firstFile, "This is a test", developerA);
                WriteFile(firstFile, "This is a test from Jan", developerB);

                Graph g = await ProvidingGraphAsync();
                // Check data of firstFile.cs
                Assert.That(() => g.GetNode(firstFile), Throws.Nothing);
                Assert.That(g.GetNode(firstFile), Is.Not.Null, $"There is no node {firstFile}.");
                Node n = g.GetNode(firstFile);
                Assert.That(n.IntAttributes[NumberOfCommits], Is.EqualTo(2));
                Assert.That(n.IntAttributes[NumberOfDevelopers], Is.EqualTo(2));
            });
        }

        [UnityTest]
        public IEnumerator TestGitProviderMultipleBranches()
        {
            return UniTask.ToCoroutine(async () =>
            {
                WriteFile(firstFile, "This is a test", developerA);
                repo.CreateBranch("newBranch");
                WriteFile(firstFile, "This is a test on newBranch", developerA);
                Branch branch = repo.Branches["main"] ?? repo.Branches["master"];
                if (branch == null)
                {
                    throw new Exception(
                        $"main/master branch not found! (branches: {string.Join(", ", repo.Branches)})"
                    );
                }
                Commands.Checkout(repo, branch);

                Graph g = await ProvidingGraphAsync();
                // Check data of firstFile.cs
                Assert.That(() => g.GetNode(firstFile), Throws.Nothing);
                Assert.That(g.GetNode(firstFile), Is.Not.Null, $"There is no node {firstFile}.");
                Node n = g.GetNode(firstFile);
                Assert.That(n.IntAttributes[NumberOfCommits], Is.EqualTo(2));
                Assert.That(n.IntAttributes[NumberOfDevelopers], Is.EqualTo(1));
            });
        }

        [UnityTest]
        public IEnumerator TestGitProviderMultipleCommitsButWithUnrelatedFile()
        {
            return UniTask.ToCoroutine(async () =>
            {
                WriteFile(firstFile, "This is a test", developerA);
                WriteFile(firstFile, "This is another test", developerA);
                WriteFile("otherfile.notcs", "This is another test in another file", developerA);

                Graph g = await ProvidingGraphAsync();
                // Check data of firstFile.cs
                Assert.That(() => g.GetNode(firstFile), Throws.Nothing);
                Assert.That(g.GetNode(firstFile), Is.Not.Null, $"There is no node {firstFile}.");
                Node n = g.GetNode(firstFile);
                Assert.That(n.IntAttributes[NumberOfCommits], Is.EqualTo(2));
                Assert.That(n.IntAttributes[NumberOfDevelopers], Is.EqualTo(1));

                Assert.That(g.GetNode("otherfile.notcs"), Is.Null, "There must be no node otherfile.notcs.");
            });
        }

        [UnityTest]
        public IEnumerator TestGitProviderMultipleCommits()
        {
            return UniTask.ToCoroutine(async () =>
            {
                WriteFile(firstFile, "This is a test", developerA);
                WriteFile(firstFile, "This is another test", developerA);

                Graph g = await ProvidingGraphAsync();
                // Check data of firstFile.cs
                Assert.That(() => g.GetNode(firstFile), Throws.Nothing);
                Assert.That(g.GetNode(firstFile), Is.Not.Null, $"There is no node {firstFile}.");
                Node n = g.GetNode(firstFile);
                Assert.That(n.IntAttributes[NumberOfCommits], Is.EqualTo(2));
                Assert.That(n.IntAttributes[NumberOfDevelopers], Is.EqualTo(1));
            });
        }

        [UnityTest]
        public IEnumerator TestGitProviderFileDoesNotExist()
        {
            return UniTask.ToCoroutine(async () =>
            {
                WriteFile(firstFile, "This is a test", developerA);

                Graph g = await ProvidingGraphAsync();
                Assert.That(g.GetNode("file/does/not/exists"), Is.Null, "There must be no node file/does/not/exists.");
            });
        }

        [UnityTest]
        public IEnumerator TestGitProviderExcludesFilesFromFilteredBranches()
        {
            return UniTask.ToCoroutine(async () =>
            {
                WriteFile(firstFile, "This is a test", developerA);
                string includedBranch = repo.Head.FriendlyName;
                const string excludedBranch = "excluded";
                repo.CreateBranch(excludedBranch);
                Commands.Checkout(repo, excludedBranch);
                WriteFile(anotherFile, "This is a test", developerA);
                Commands.Checkout(repo, includedBranch);

                Graph graph = await ProvidingGraphAsync(branches: new[] { includedBranch });

                Assert.That(graph.GetNode(firstFile), Is.Not.Null);
                Assert.That(graph.GetNode(anotherFile), Is.Null,
                            $"Files unique to filtered branches must not be added as nodes: {anotherFile}");
            });
        }

        [UnityTest]
        public IEnumerator TestGitProviderSingleFileCommit()
        {
            return UniTask.ToCoroutine(async () =>
            {
                WriteFile(firstFile, "This is a test", developerA);

                Graph g = await ProvidingGraphAsync();
                // Check data of firstFile.cs
                Assert.That(() => g.GetNode(firstFile), Throws.Nothing);
                Assert.That(g.GetNode(firstFile), Is.Not.Null, $"There is no node {firstFile}.");
                Node n = g.GetNode(firstFile);
                Assert.That(n.IntAttributes[NumberOfCommits], Is.EqualTo(1));
                Assert.That(n.IntAttributes[NumberOfDevelopers], Is.EqualTo(1));
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
                Utils.Filenames.DeleteReadOnlyDirectory(gitDirPath);
            }
        }
    }
}
