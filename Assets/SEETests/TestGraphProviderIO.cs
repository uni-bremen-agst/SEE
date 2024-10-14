﻿using NUnit.Framework;
using SEE.Utils;
using SEE.Utils.Config;
using SEE.Utils.Paths;
using System.Collections.Generic;
using System.IO;
using SEE.GraphProviders.Evolution;
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

        private static void AreEqualGXLEvolutionProviders(GXLEvolutionGraphProvider saved, MultiGraphProvider loaded)
        {
            Assert.IsTrue(saved.GetType() == loaded.GetType());
            GXLEvolutionGraphProvider gxlLoaded = loaded as GXLEvolutionGraphProvider;
            AreEqual(gxlLoaded.GXLDirectory, saved.GXLDirectory);
            Assert.AreEqual(gxlLoaded.MaxRevisionsToLoad, saved.MaxRevisionsToLoad);
        }

        private static void AreEqualGitEvolutionProviders(GitEvolutionGraphProvider saved, MultiGraphProvider loaded)
        {
            Assert.IsTrue(saved.GetType() == loaded.GetType());
            GitEvolutionGraphProvider gitLoaded = loaded as GitEvolutionGraphProvider;
            Assert.AreEqual(gitLoaded.Date, saved.Date);
            AreEqual(gitLoaded.GitRepository.RepositoryPath, saved.GitRepository.RepositoryPath);
            Assert.AreEqual(gitLoaded.GitRepository.PathGlobbing, saved.GitRepository.PathGlobbing);
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
                Date = "01/05/2024",
                GitRepository = GetGitRepository()
            };
        }

        private static void AreEqualGXLProviders(GXLSingleGraphProvider saved, SingleGraphProvider loaded)
        {
            Assert.IsTrue(saved.GetType() == loaded.GetType());
            GXLSingleGraphProvider gxlSingleLoaded = loaded as GXLSingleGraphProvider;
            AreEqual(saved.Path, gxlSingleLoaded.Path);
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

        private static void AreEqualCSVProviders(CSVGraphProvider saved, SingleGraphProvider loaded)
        {
            Assert.IsTrue(saved.GetType() == loaded.GetType());
            CSVGraphProvider gxlLoaded = loaded as CSVGraphProvider;
            AreEqual(saved.Path, gxlLoaded.Path);
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

        private static void AreEqualJaCoCoGraphProviders(JaCoCoGraphProvider saved, SingleGraphProvider loaded)
        {
            Assert.IsTrue(saved.GetType() == loaded.GetType());
            JaCoCoGraphProvider loadedProvider = loaded as JaCoCoGraphProvider;
            AreEqual(saved.Path, loadedProvider.Path);
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

        private static void AreEqualSinglePipelineProviders(SingleGraphPipelineProvider saved,
            SingleGraphProvider loaded)
        {
            Assert.IsTrue(saved.GetType() == loaded.GetType());
            SingleGraphPipelineProvider graphPipelineLoaded = loaded as SingleGraphPipelineProvider;
            Assert.AreEqual(saved.Pipeline.Count, graphPipelineLoaded.Pipeline.Count);
            for (int i = 0; i < saved.Pipeline.Count; i++)
            {
                AreEqual(saved.Pipeline[i], graphPipelineLoaded.Pipeline[i]);
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

        private void AreEqualAllBranchGitSingleProvider(AllGitBranchesSingleGraphProvider saved,
            SingleGraphProvider loaded)
        {
            Assert.AreEqual(saved.GetType(), loaded.GetType());
            AllGitBranchesSingleGraphProvider gitBranchesLoaded = loaded as AllGitBranchesSingleGraphProvider;
            Assert.AreEqual(gitBranchesLoaded.SimplifyGraph, saved.SimplifyGraph);
            Assert.AreEqual(gitBranchesLoaded.AutoFetch, saved.AutoFetch);
        }

        private AllGitBranchesSingleGraphProvider GetAllBranchGitSingleProvider()
        {
            return new AllGitBranchesSingleGraphProvider()
            {
                PathGlobbing = new Dictionary<string, bool>()
                {
                    {".cs", true},
                },
                AutoFetch = true,
                PollingInterval = 5,
                MarkerTime = 10,
            };
        }

        private GitRepository GetGitRepository()
        {
            return new GitRepository()
            {
                RepositoryPath = new DataPath("/path/to/repo"),
                PathGlobbing = new Dictionary<string, bool>() { { ".cs", true }, { ".c", true }, { ".cbl", false } }
            };
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

        private static void AreEqualReflexionGraphProviders(ReflexionGraphProvider saved, SingleGraphProvider loaded)
        {
            Assert.IsTrue(saved.GetType() == loaded.GetType());
            ReflexionGraphProvider reflexionLoaded = loaded as ReflexionGraphProvider;
            AreEqual(saved.Architecture, reflexionLoaded.Architecture);
            AreEqual(saved.Implementation, reflexionLoaded.Implementation);
            AreEqual(saved.Mapping, reflexionLoaded.Mapping);
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

        private static void AreEqualDiffMergeGraphProviders(MergeDiffGraphProvider saved, SingleGraphProvider loaded)
        {
            Assert.IsTrue(saved.GetType() == loaded.GetType());
            MergeDiffGraphProvider loadedProvider = loaded as MergeDiffGraphProvider;
            AreEqual(saved.OldGraph, loadedProvider.OldGraph);
        }

        #endregion

        #region VCSGraphProvider

        public void TestVCSGraphProvider()
        {
            VCSGraphProvider saved = GetVCSGraphProvider();
            Save(saved);
            AreEqualVCSGraphProviders(saved, LoadSingleGraph());
        }

        private void AreEqualVCSGraphProviders(VCSGraphProvider saved, SingleGraphProvider loaded)
        {
            Assert.IsTrue(saved.GetType() == loaded.GetType());
            VCSGraphProvider loadedProvider = loaded as VCSGraphProvider;
            AreEqual(saved.RepositoryPath, loadedProvider.RepositoryPath);
            Assert.AreEqual(saved.CommitID, loadedProvider.CommitID);
            Assert.AreEqual(saved.BaselineCommitID, loadedProvider.BaselineCommitID);
            AreEqual(saved.PathGlobbing, loadedProvider.PathGlobbing);
        }

        private void AreEqual(IDictionary<string, bool> expected, IDictionary<string, bool> actual)
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
            Dictionary<string, bool> pathGlobbing = new()
                {
                    { "Assets/SEE/**/*.cs", true }
                };

            return new VCSGraphProvider()
            {
                RepositoryPath = new DataPath()
                {
                    Path = Path.GetDirectoryName(Application.dataPath)
                },
                CommitID = "b10e1f49c144c0a22aa0d972c946f93a82ad3461",
                BaselineCommitID = "5efa95913a6e894e5340f07fab26c9958b5c1096",
                PathGlobbing = pathGlobbing
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

