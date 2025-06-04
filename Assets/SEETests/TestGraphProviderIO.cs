using NUnit.Framework;
using SEE.GraphProviders.Evolution;
using SEE.Utils;
using SEE.Utils.Config;
using SEE.Utils.Paths;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SEE.GraphProviders
{
    /// <summary>
    /// Test cases for input/output of <see cref="GraphProviders"/>.
    /// </summary>
    internal class TestGraphProviderIO : AbstractTestConfigIO
    {
        public static void AreEqual(MultiGraphPipelineProvider expected, MultiGraphProvider actual)
        {
            Assert.IsTrue(expected.GetType() == actual.GetType());
            MultiGraphPipelineProvider graphPipelineLoaded = actual as MultiGraphPipelineProvider;
            Assert.AreEqual(expected.Pipeline.Count, graphPipelineLoaded.Pipeline.Count);
            for (int i = 0; i < expected.Pipeline.Count; i++)
            {
                AreEqual(expected.Pipeline[i], graphPipelineLoaded.Pipeline[i]);
            }
        }

        /// <summary>
        /// Checks whether the two graph providers have identical types
        /// and whether their attributes are the same.
        /// </summary>
        /// <param name="expected">expected graph provider</param>
        /// <param name="actual">actual graph provider</param>
        /// <exception cref="System.NotImplementedException"></exception>
        public static void AreEqual(MultiGraphProvider expected, MultiGraphProvider actual)
        {
            Assert.IsTrue(expected.GetType() == actual.GetType());
            if (expected is GXLEvolutionGraphProvider gxlEvolution)
            {
                AreEqualGXLEvolutionProviders(gxlEvolution, actual);
            }
            else
            {
                throw new System.NotImplementedException();
            }
        }

        private static void AreEqualGXLEvolutionProviders(GXLEvolutionGraphProvider expected, MultiGraphProvider actual)
        {
            Assert.IsTrue(expected.GetType() == actual.GetType());
            GXLEvolutionGraphProvider gxlLoaded = actual as GXLEvolutionGraphProvider;
            AreEqual(expected.GXLDirectory, gxlLoaded.GXLDirectory);
            Assert.AreEqual(expected.MaxRevisionsToLoad, gxlLoaded.MaxRevisionsToLoad);
        }

        private static void AreEqualGitEvolutionProviders(GitEvolutionGraphProvider expected, MultiGraphProvider actual)
        {
            Assert.IsTrue(expected.GetType() == actual.GetType());
            GitEvolutionGraphProvider gitLoaded = actual as GitEvolutionGraphProvider;
            Assert.AreEqual(expected.Date, gitLoaded.Date);
            AreEqual(expected.GitRepository.RepositoryPath, gitLoaded.GitRepository.RepositoryPath);
            AreEqualFilters(expected.GitRepository.VCSFilter, gitLoaded.GitRepository.VCSFilter);
        }

        private static void AreEqualFilters(SEE.VCS.Filter expected, SEE.VCS.Filter actual)
        {
            Assert.AreEqual(expected.Globbing, actual.Globbing);
            Assert.AreEqual(expected.RepositoryPaths, actual.RepositoryPaths);
            Assert.AreEqual(expected.Branches, actual.Branches);
        }

        /// <summary>
        /// Checks whether the two graph providers have identical types
        /// and whether their attributes are the same.
        /// </summary>
        /// <param name="expected">expected graph provider</param>
        /// <param name="actual">actual graph provider</param>
        /// <exception cref="System.NotImplementedException"></exception>
        public static void AreEqual(SingleGraphProvider expected, SingleGraphProvider actual)
        {
            Assert.IsTrue(expected.GetType() == actual.GetType());
            if (expected is GXLSingleGraphProvider gXLGraphProvider)
            {
                AreEqualGXLProviders(gXLGraphProvider, actual);
            }
            else if (expected is CSVGraphProvider csvGraphProvider)
            {
                AreEqualCSVProviders(csvGraphProvider, actual);
            }
            else if (expected is SingleGraphPipelineProvider pipelineGraphProvider)
            {
                AreEqualSinglePipelineProviders(pipelineGraphProvider, actual);
            }
            else if (expected is ReflexionGraphProvider reflexionGraphProvider)
            {
                AreEqualReflexionGraphProviders(reflexionGraphProvider, actual);
            }
            else if (expected is JaCoCoGraphProvider jacocoGraphProvider)
            {
                AreEqualJaCoCoGraphProviders(jacocoGraphProvider, actual);
            }
            else if (expected is MergeDiffGraphProvider diffMergeGraphProvider)
            {
                AreEqualDiffMergeGraphProviders(diffMergeGraphProvider, actual);
            }
            else
            {
                throw new System.NotImplementedException();
            }
        }

        private const string providerLabel = "provider";

        private string filename;

        [SetUp]
        public void SetUp()
        {
            filename = Path.GetTempFileName();
        }

        [TearDown]
        public void TearDown()
        {
            FileIO.DeleteIfExists(filename);
        }

        #region GXL provider

        [Test]
        public void TestGXLGraphProvider()
        {
            GXLSingleGraphProvider saved = GetGXLProvider();
            Save(saved);
            AreEqualGXLProviders(saved, LoadSingleGraph());
        }

        private GXLSingleGraphProvider GetGXLProvider()
        {
            return new GXLSingleGraphProvider()
            {
                Path = new DataPath("mydir/myfile.gxl")
            };
        }

        private MultiGraphProvider GetGXLEvolutionProvider()
        {
            return new GXLEvolutionGraphProvider()
            {
                GXLDirectory = new DataPath() { Path = "/path/to/gxl/files" }
            };
        }

        private GitEvolutionGraphProvider GetGitEvolutionProvider()
        {
            return new GitEvolutionGraphProvider()
            {
                Date = "2024/01/05",
                GitRepository = GetGitRepository()
            };
        }

        private static void AreEqualGXLProviders(GXLSingleGraphProvider expected, SingleGraphProvider actual)
        {
            Assert.IsTrue(expected.GetType() == actual.GetType());
            GXLSingleGraphProvider gxlSingleLoaded = actual as GXLSingleGraphProvider;
            AreEqual(expected.Path, gxlSingleLoaded.Path);
        }

        #endregion

        #region CSV provider

        [Test]
        public void TestCSVGraphProvider()
        {
            CSVGraphProvider saved = GetCSVProvider();
            Save(saved);
            AreEqualCSVProviders(saved, LoadSingleGraph());
        }

        private CSVGraphProvider GetCSVProvider()
        {
            return new CSVGraphProvider()
            {
                Path = new DataPath(Application.streamingAssetsPath + "/mydir/myfile.csv")
            };
        }

        private static void AreEqualCSVProviders(CSVGraphProvider expected, SingleGraphProvider actual)
        {
            Assert.IsTrue(expected.GetType() == actual.GetType());
            CSVGraphProvider gxlLoaded = actual as CSVGraphProvider;
            AreEqual(expected.Path, gxlLoaded.Path);
        }

        #endregion

        #region JaCoCo provider

        [Test]
        public void TestJaCoCoGraphProvider()
        {
            JaCoCoGraphProvider saved = GetJaCoCoProvider();
            Save(saved);
            AreEqualJaCoCoGraphProviders(saved, LoadSingleGraph());
        }

        private JaCoCoGraphProvider GetJaCoCoProvider()
        {
            return new JaCoCoGraphProvider()
            {
                Path = new DataPath(Application.streamingAssetsPath + "/mydir/jacoco.xml")
            };
        }

        private static void AreEqualJaCoCoGraphProviders(JaCoCoGraphProvider expected, SingleGraphProvider actual)
        {
            Assert.IsTrue(expected.GetType() == actual.GetType());
            JaCoCoGraphProvider loadedProvider = actual as JaCoCoGraphProvider;
            AreEqual(expected.Path, loadedProvider.Path);
        }

        #endregion

        #region Pipeline provider

        [Test]
        public void TestPipelineProvider()
        {
            SingleGraphPipelineProvider saved = new();
            saved.Pipeline.Add(GetGXLProvider());
            saved.Pipeline.Add(GetCSVProvider());
            Save(saved);
            AreEqualSinglePipelineProviders(saved, LoadSingleGraph());
        }


        [Test]
        public void TestMultiGraphPipelineProvider()
        {
            MultiGraphPipelineProvider saved = new();
            saved.Pipeline.Add(GetGXLEvolutionProvider());

            Save(saved);
            AreEqual(saved, LoadMultiGraph());
        }

        [Test]
        public void TestEmptyGraphPipelineProvider()
        {
            MultiGraphPipelineProvider saved = new();

            Save(saved);
            AreEqual(saved, LoadMultiGraph());
        }

        [Test]
        public void TestEmptyPipelineProvider()
        {
            SingleGraphPipelineProvider saved = new();
            Save(saved);
            AreEqualSinglePipelineProviders(saved, LoadSingleGraph());
        }

        [Test]
        public void TestNestedPipelineProvider()
        {
            SingleGraphPipelineProvider saved = new();
            for (int i = 1; i <= 3; i++)
            {
                SingleGraphPipelineProvider nested = new();
                nested.Pipeline.Add(GetGXLProvider());
                nested.Pipeline.Add(GetCSVProvider());
                saved.Pipeline.Add(nested);
            }

            Save(saved);
            AreEqualSinglePipelineProviders(saved, LoadSingleGraph());
        }

        private static void AreEqualSinglePipelineProviders(SingleGraphPipelineProvider expected,
                                                            SingleGraphProvider actual)
        {
            Assert.IsTrue(expected.GetType() == actual.GetType());
            SingleGraphPipelineProvider graphPipelineLoaded = actual as SingleGraphPipelineProvider;
            Assert.AreEqual(expected.Pipeline.Count, graphPipelineLoaded.Pipeline.Count);
            for (int i = 0; i < expected.Pipeline.Count; i++)
            {
                AreEqual(expected.Pipeline[i], graphPipelineLoaded.Pipeline[i]);
            }
        }

        #endregion

        #region GitProvider

        [Test]
        public void TestGitEvolutionProvider()
        {
            GitEvolutionGraphProvider saved = GetGitEvolutionProvider();
            Save(saved);
            AreEqualGitEvolutionProviders(saved, LoadMultiGraph());
        }

        [Test]
        public void TestAllBranchGitSingleProvider()
        {
            AllGitBranchesSingleGraphProvider saved = GetAllBranchGitSingleProvider();
            Save(saved);
            AreEqualAllBranchGitSingleProvider(saved, LoadSingleGraph());
        }

        private void AreEqualAllBranchGitSingleProvider(AllGitBranchesSingleGraphProvider expected,
                                                        SingleGraphProvider actual)
        {
            Assert.AreEqual(expected.GetType(), actual.GetType());
            AllGitBranchesSingleGraphProvider gitBranchesLoaded = actual as AllGitBranchesSingleGraphProvider;
            Assert.AreEqual(gitBranchesLoaded.SimplifyGraph, expected.SimplifyGraph);
            Assert.AreEqual(gitBranchesLoaded.AutoFetch, expected.AutoFetch);
        }

        private AllGitBranchesSingleGraphProvider GetAllBranchGitSingleProvider()
        {
            return new AllGitBranchesSingleGraphProvider()
            {
                GitRepository = new GitRepository
                                      (new DataPath("path/to/repo"),
                                       new SEE.VCS.Filter(globbing: new Globbing() { { "**/*.cs", true } },
                                                          repositoryPaths: new List<string>() { "path1", "path2" },
                                                          branches: new List<string>() { "^branch1$", "master" })),
                SimplifyGraph = true,
                AutoFetch = true,
                PollingInterval = 60,
                MarkerTime = 3,
            };
        }

        private GitRepository GetGitRepository()
        {
            return new GitRepository
                        (new DataPath("anotherpath/to/repoX"),
                         new SEE.VCS.Filter(globbing: new Globbing() { { "**/*.cpp", true } },
                                            repositoryPaths: new List<string>() { "path2", "path3" },
                                            branches: new List<string>() { "^branch5$", "main" }));
        }

        #endregion

        #region Reflexion provider

        [Test]
        public void TestReflexionGraphProvider()
        {
            ReflexionGraphProvider saved = GetReflexionProvider();
            Save(saved);
            AreEqualReflexionGraphProviders(saved, LoadSingleGraph());
        }

        private ReflexionGraphProvider GetReflexionProvider()
        {
            return new ReflexionGraphProvider()
            {
                Architecture = new DataPath("mydir/Architecture.gxl"),
                Implementation = new DataPath("mydir/Implementation.gxl"),
                Mapping = new DataPath("mydir/Mapping.gxl"),
            };
        }

        private static void AreEqualReflexionGraphProviders(ReflexionGraphProvider expected, SingleGraphProvider actual)
        {
            Assert.IsTrue(expected.GetType() == actual.GetType());
            ReflexionGraphProvider reflexionLoaded = actual as ReflexionGraphProvider;
            AreEqual(expected.Architecture, reflexionLoaded.Architecture);
            AreEqual(expected.Implementation, reflexionLoaded.Implementation);
            AreEqual(expected.Mapping, reflexionLoaded.Mapping);
        }

        #endregion

        #region DiffMerge provider

        [Test]
        public void TestDiffMergeGraphProvider()
        {
            MergeDiffGraphProvider saved = GetDiffMergeProvider();
            Save(saved);
            AreEqualDiffMergeGraphProviders(saved, LoadSingleGraph());
        }

        private MergeDiffGraphProvider GetDiffMergeProvider()
        {
            return new MergeDiffGraphProvider()
            {
                OldGraph = new JaCoCoGraphProvider()
                {
                    Path = new DataPath(Application.streamingAssetsPath + "/mydir/jacoco.xml")
                }
            };
        }

        private static void AreEqualDiffMergeGraphProviders(MergeDiffGraphProvider expected, SingleGraphProvider actual)
        {
            Assert.IsTrue(expected.GetType() == actual.GetType());
            MergeDiffGraphProvider loadedProvider = actual as MergeDiffGraphProvider;
            AreEqual(expected.OldGraph, loadedProvider.OldGraph);
        }

        #endregion

        #region VCSGraphProvider

        public void TestVCSGraphProvider()
        {
            VCSGraphProvider saved = GetVCSGraphProvider();
            Save(saved);
            AreEqualVCSGraphProviders(saved, LoadSingleGraph());
        }

        private void AreEqualVCSGraphProviders(VCSGraphProvider expected, SingleGraphProvider actual)
        {
            Assert.IsTrue(expected.GetType() == actual.GetType());
            VCSGraphProvider loadedProvider = actual as VCSGraphProvider;
            AreEqual(expected.GitRepository, loadedProvider.GitRepository);
            Assert.AreEqual(expected.CommitID, loadedProvider.CommitID);
            Assert.AreEqual(expected.BaselineCommitID, loadedProvider.BaselineCommitID);
        }

        private static void AreEqual(GitRepository expected, GitRepository actual)
        {
            Assert.IsNotNull(expected);
            Assert.IsNotNull(actual);
            AreEqual(expected.RepositoryPath, actual.RepositoryPath);
            AreEqualFilters(expected.VCSFilter, actual.VCSFilter);
        }

        private void AreEqualDictionaries(IDictionary<string, bool> expected, IDictionary<string, bool> actual)
        {
            Assert.AreEqual(expected.Count, actual.Count);
            foreach (var kv in expected)
            {
                Assert.IsTrue(actual.TryGetValue(kv.Key, out bool value));
                Assert.AreEqual(kv.Value, value);
            }
        }

        private VCSGraphProvider GetVCSGraphProvider()
        {
            Globbing pathGlobbing = new()
                {
                    { "Assets/SEE/**/*.cs", true }
                };

            return new VCSGraphProvider()
            {
                GitRepository = new GitRepository
                    (new DataPath(Path.GetDirectoryName(Application.dataPath)),
                     new SEE.VCS.Filter(globbing: pathGlobbing, repositoryPaths: null, branches: null)),
                CommitID = "b10e1f49c144c0a22aa0d972c946f93a82ad3461",
                BaselineCommitID = "5efa95913a6e894e5340f07fab26c9958b5c1096",
            };
        }

        #endregion

        private SingleGraphProvider LoadSingleGraph()
        {
            using ConfigReader stream = new(filename);
            SingleGraphProvider loaded = SingleGraphProvider.Restore(stream.Read(), providerLabel);
            Assert.IsNotNull(loaded);
            return loaded;
        }

        private void DumpSaved()
        {
            if (!File.Exists(filename))
            {
                Debug.LogError($"File {filename} does not exist after saving.");
                return;
            }
            string content = File.ReadAllText(filename);
            Debug.Log($"Saved content of {filename}:\n");
            Debug.Log(content + "\n");
        }

        private void Save(SingleGraphProvider saved)
        {
            using ConfigWriter writer = new(filename);
            saved.Save(writer, providerLabel);
        }

        private MultiGraphProvider LoadMultiGraph()
        {
            using ConfigReader stream = new(filename);
            MultiGraphProvider loaded = MultiGraphProvider.Restore(stream.Read(), providerLabel);
            Assert.IsNotNull(loaded);
            return loaded;
        }

        private void Save(MultiGraphProvider saved)
        {
            using ConfigWriter writer = new(filename);
            saved.Save(writer, providerLabel);
        }
    }
}
