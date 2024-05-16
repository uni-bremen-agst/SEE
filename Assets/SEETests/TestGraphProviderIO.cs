using NUnit.Framework;
using SEE.DataModel.DG;
using SEE.Game.City;
using SEE.Utils;
using SEE.Utils.Config;
using SEE.Utils.Paths;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cysharp.Threading.Tasks;
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
            if (expected is GitEvolutionGraphProvider gitEvolution)
            {
                AreEqualGitEvolutionProviders(gitEvolution, actual);
            }
            else if (expected is GXLEvolutionGraphProvider gxlEvolution)
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
            AreEqual(gitLoaded.Repository.RepositoryPath, saved.Repository.RepositoryPath);
            Assert.AreEqual(gitLoaded.Repository.PathGlobbing, saved.Repository.PathGlobbing);
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
            else if (expected is JaCoCoSingleGraphProvider jacocoGraphProvider)
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
                Path = new Utils.Paths.FilePath("mydir/myfile.gxl")
            };
        }

        private MultiGraphProvider GetGXLEvolutionProvider()
        {
            return new GXLEvolutionGraphProvider()
            {
                GXLDirectory = new DirectoryPath() { Path = "/path/to/gxl/files" }
            };
        }

        private GitEvolutionGraphProvider GetGitEvolutionProvider()
        {
            return new GitEvolutionGraphProvider()
            {
                Date = "01/05/2024",
                Repository = GetGitRepository()
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
                Path = new Utils.Paths.FilePath(Application.streamingAssetsPath + "/mydir/myfile.csv")
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
            JaCoCoSingleGraphProvider saved = GetJaCoCoProvider();
            Save(saved);
            AreEqualJaCoCoGraphProviders(saved, LoadSingleGraph());
        }

        private JaCoCoSingleGraphProvider GetJaCoCoProvider()
        {
            return new JaCoCoSingleGraphProvider()
            {
                Path = new Utils.Paths.FilePath(Application.streamingAssetsPath + "/mydir/jacoco.xml")
            };
        }

        private static void AreEqualJaCoCoGraphProviders(JaCoCoSingleGraphProvider saved, SingleGraphProvider loaded)
        {
            Assert.IsTrue(saved.GetType() == loaded.GetType());
            JaCoCoSingleGraphProvider loadedProvider = loaded as JaCoCoSingleGraphProvider;
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
            saved.Pipeline.Add(GetGitEvolutionProvider());
            saved.Pipeline.Add(GetGXLEvolutionProvider());

            Save(saved);
            AreEqual(saved, LoadMultiGraph());
        }

        [Test]
        public void TestEmptyGraphPipelineProvider()
        {
            MultiGraphPipelineProvider saved = new();
            //saved.Pipeline.Add(GetGitEvolutionProvider());
            //saved.Pipeline.Add(GetGXLEvolutionProvider());

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

        private void AreEqualAllBranchGitSingleProvider(AllGitBranchesSingleGraphProvider saved, SingleGraphProvider loaded)
        {
            Assert.IsTrue(saved.GetType() == loaded.GetType());
            AllGitBranchesSingleGraphProvider gitBranchesLoaded = loaded as AllGitBranchesSingleGraphProvider;
            Assert.AreEqual(gitBranchesLoaded.Date, saved.Date);
            AreEqual(gitBranchesLoaded.RepositoryData.RepositoryPath, saved.RepositoryData.RepositoryPath);
            Assert.AreEqual(gitBranchesLoaded.RepositoryData.PathGlobbing, saved.RepositoryData.PathGlobbing);
            Assert.AreEqual(gitBranchesLoaded.SimplifyGraph, saved.SimplifyGraph);
            Assert.AreEqual(gitBranchesLoaded.AutoFetch, saved.AutoFetch);
        }

        private AllGitBranchesSingleGraphProvider GetAllBranchGitSingleProvider()
        {
            return new AllGitBranchesSingleGraphProvider()
            {
                Date = "01/05/2024",
                RepositoryData = GetGitRepository()
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
                Architecture = new Utils.Paths.FilePath("mydir/Architecture.gxl"),
                Implementation = new Utils.Paths.FilePath("mydir/Implementation.gxl"),
                Mapping = new Utils.Paths.FilePath("mydir/Mapping.gxl"),
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
                OldGraph = new JaCoCoSingleGraphProvider()
                {
                    Path = new Utils.Paths.FilePath(Application.streamingAssetsPath + "/mydir/jacoco.xml")
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

        private VCSGraphProvider GetVCSGraphProvider()
        {
            return new VCSGraphProvider()
            {
                Repository = new GitRepository()
                    { RepositoryPath = new DirectoryPath(System.IO.Path.GetDirectoryName(Application.dataPath)) },
                CommitID = "b10e1f49c144c0a22aa0d972c946f93a82ad3461",
            };
        }

        private GitRepository GetGitRepository()
        {
            return new GitRepository()
            {
                RepositoryPath = new DirectoryPath("/path/to/repo"),
                PathGlobbing = new() { { ".cs", true }, { ".c", true }, { ".cbl", false } }
            };
        }

        private static T NewVanillaSEECity<T>() where T : Component
        {
            return new GameObject().AddComponent<T>();
        }

        public async System.Threading.Tasks.Task<Graph> GetVCSGraphAsync()
        {
            VCSGraphProvider saved = GetVCSGraphProvider();
            SEECity testCity = NewVanillaSEECity<SEECity>();
            ;
            Graph testGraph = new("test", "test");
            Graph graph = await saved.ProvideAsync(testGraph, testCity);
            return graph;
        }

        [Test]
        public void TestVCSGraphProvider()
        {
            Graph graph = GetVCSGraphAsync().GetAwaiter().GetResult();
            List<string> pathsFromGraph = new();
            foreach (GraphElement elem in graph.Elements())
            {
                pathsFromGraph.Add(elem.ID.Replace('\\', '/'));
            }

            List<string> actualList = new()
            {
                "SEE",
                ".gitignore",
                "Assets",
                "Assets/Scenes.meta",
                "Assets/Scenes",
                "Assets/Scenes/SampleScene.unity",
                "Assets/Scenes/SampleScene.unity.meta",
                "Packages",
                "Packages/manifest.json",
                "ProjectSettings",
                "ProjectSettings/AudioManager.asset",
                "ProjectSettings/ClusterInputManager.asset",
                "ProjectSettings/DynamicsManager.asset",
                "ProjectSettings/EditorBuildSettings.asset",
                "ProjectSettings/EditorSettings.asset",
                "ProjectSettings/GraphicsSettings.asset",
                "ProjectSettings/InputManager.asset",
                "ProjectSettings/NavMeshAreas.asset",
                "ProjectSettings/Physics2DSettings.asset",
                "ProjectSettings/PresetManager.asset",
                "ProjectSettings/ProjectSettings.asset",
                "ProjectSettings/ProjectVersion.txt",
                "ProjectSettings/QualitySettings.asset",
                "ProjectSettings/TagManager.asset",
                "ProjectSettings/TimeManager.asset",
                "ProjectSettings/UnityConnectSettings.asset",
                "ProjectSettings/VFXManager.asset",
                "ProjectSettings/XRSettings.asset"
            };
            Assert.IsTrue(actualList.SequenceEqual(pathsFromGraph));
        }

        private SingleGraphProvider LoadSingleGraph()
        {
            using ConfigReader stream = new(filename);
            SingleGraphProvider loaded = SingleGraphProvider.Restore(stream.Read(), providerLabel);
            Assert.IsNotNull(loaded);
            return loaded;
        }

        private MultiGraphProvider LoadMultiGraph()
        {
            using ConfigReader stream = new(filename);
            MultiGraphProvider loaded = MultiGraphProvider.Restore(stream.Read(), providerLabel);
            Assert.IsNotNull(loaded);
            return loaded;
        }

        private void Save(SingleGraphProvider saved)
        {
            using ConfigWriter writer = new(filename);
            saved.Save(writer, providerLabel);
        }

        private void Save(MultiGraphProvider saved)
        {
            using ConfigWriter writer = new(filename);
            saved.Save(writer, providerLabel);
        }
    }
}