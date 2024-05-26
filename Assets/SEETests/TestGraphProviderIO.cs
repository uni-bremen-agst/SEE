using NUnit.Framework;
using SEE.DataModel.DG;
using SEE.Game.City;
using SEE.Utils;
using SEE.Utils.Config;
using SEE.Utils.Paths;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace SEE.GraphProviders
{
    /// <summary>
    /// Test cases for input/output of <see cref="GraphProviders"/>.
    /// </summary>
    internal class TestGraphProviderIO : AbstractTestConfigIO
    {
        /// <summary>
        /// Checks whether the two graph providers have identical types
        /// and whether their attributes are the same.
        /// </summary>
        /// <param name="expected">expected graph provider</param>
        /// <param name="actual">actual graph provider</param>
        /// <exception cref="System.NotImplementedException"></exception>
        public static void AreEqual(GraphProvider expected, GraphProvider actual)
        {
            Assert.IsTrue(expected.GetType() == actual.GetType());
            if (expected is GXLGraphProvider gXLGraphProvider)
            {
                AreEqualGXLProviders(gXLGraphProvider, actual);
            }
            else if (expected is CSVGraphProvider csvGraphProvider)
            {
                AreEqualCSVProviders(csvGraphProvider, actual);
            }
            else if (expected is PipelineGraphProvider pipelineGraphProvider)
            {
                AreEqualPipelineProviders(pipelineGraphProvider, actual);
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
            GXLGraphProvider saved = GetGXLProvider();
            Save(saved);
            AreEqualGXLProviders(saved, Load());
        }

        private GXLGraphProvider GetGXLProvider()
        {
            return new GXLGraphProvider()
            {
                Path = new Utils.Paths.FilePath("mydir/myfile.gxl")
            };
        }

        private static void AreEqualGXLProviders(GXLGraphProvider saved, GraphProvider loaded)
        {
            Assert.IsTrue(saved.GetType() == loaded.GetType());
            GXLGraphProvider gxlLoaded = loaded as GXLGraphProvider;
            AreEqual(saved.Path, gxlLoaded.Path);
        }

        #endregion

        #region CSV provider
        [Test]
        public void TestCSVGraphProvider()
        {
            CSVGraphProvider saved = GetCSVProvider();
            Save(saved);
            AreEqualCSVProviders(saved, Load());
        }

        private CSVGraphProvider GetCSVProvider()
        {
            return new CSVGraphProvider()
            {
                Path = new Utils.Paths.FilePath(Application.streamingAssetsPath + "/mydir/myfile.csv")
            };
        }

        private static void AreEqualCSVProviders(CSVGraphProvider saved, GraphProvider loaded)
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
            AreEqualJaCoCoGraphProviders(saved, Load());
        }

        private JaCoCoGraphProvider GetJaCoCoProvider()
        {
            return new JaCoCoGraphProvider()
            {
                Path = new Utils.Paths.FilePath(Application.streamingAssetsPath + "/mydir/jacoco.xml")
            };
        }

        private static void AreEqualJaCoCoGraphProviders(JaCoCoGraphProvider saved, GraphProvider loaded)
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
            PipelineGraphProvider saved = new();
            saved.Pipeline.Add(GetGXLProvider());
            saved.Pipeline.Add(GetCSVProvider());
            Save(saved);
            AreEqualPipelineProviders(saved, Load());
        }

        [Test]
        public void TestEmptyPipelineProvider()
        {
            PipelineGraphProvider saved = new();
            Save(saved);
            AreEqualPipelineProviders(saved, Load());
        }

        [Test]
        public void TestNestedPipelineProvider()
        {
            PipelineGraphProvider saved = new();
            for (int i = 1; i <= 3; i++)
            {
                PipelineGraphProvider nested = new();
                nested.Pipeline.Add(GetGXLProvider());
                nested.Pipeline.Add(GetCSVProvider());
                saved.Pipeline.Add(nested);
            }
            Save(saved);
            AreEqualPipelineProviders(saved, Load());
        }

        private static void AreEqualPipelineProviders(PipelineGraphProvider saved, GraphProvider loaded)
        {
            Assert.IsTrue(saved.GetType() == loaded.GetType());
            PipelineGraphProvider pipelineLoaded = loaded as PipelineGraphProvider;
            Assert.AreEqual(saved.Pipeline.Count, pipelineLoaded.Pipeline.Count);
            for (int i = 0; i < saved.Pipeline.Count; i++)
            {
                AreEqual(saved.Pipeline[i], pipelineLoaded.Pipeline[i]);
            }
        }

        #endregion

        #region Reflexion provider

        [Test]
        public void TestReflexionGraphProvider()
        {
            ReflexionGraphProvider saved = GetReflexionProvider();
            Save(saved);
            AreEqualReflexionGraphProviders(saved, Load());
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

        private static void AreEqualReflexionGraphProviders(ReflexionGraphProvider saved, GraphProvider loaded)
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
            AreEqualDiffMergeGraphProviders(saved, Load());
        }

        private MergeDiffGraphProvider GetDiffMergeProvider()
        {
            return new MergeDiffGraphProvider()
            {
                OldGraph = new JaCoCoGraphProvider()
                {
                    Path = new Utils.Paths.FilePath(Application.streamingAssetsPath + "/mydir/jacoco.xml")
                }
            };
        }

        private static void AreEqualDiffMergeGraphProviders(MergeDiffGraphProvider saved, GraphProvider loaded)
        {
            Assert.IsTrue(saved.GetType() == loaded.GetType());
            MergeDiffGraphProvider loadedProvider = loaded as MergeDiffGraphProvider;
            AreEqual(saved.OldGraph, loadedProvider.OldGraph);
        }

        #endregion

        private static T NewVanillaSEECity<T>() where T : Component
        {
            return new GameObject().AddComponent<T>();
        }

        public async System.Threading.Tasks.Task<Graph> GetVCSGraphAsync()
        {
            VCSGraphProvider saved = GetVCSGraphProvider();
            SEECity testCity = NewVanillaSEECity<SEECity>(); ;
            Graph testGraph = new("test", "test");
            Graph graph = await saved.ProvideAsync(testGraph, testCity);
            return graph;
        }

        [Test]
        public async Task TestVCSGraphProviderAsync()
        {
            Graph graph = await GetVCSGraphAsync();
            List<string> pathsFromGraph = new();
            foreach (GraphElement elem in graph.Elements())
            {
                pathsFromGraph.Add(elem.ID);
            }

            string repositoryPath = Application.dataPath;
            string projectPath = repositoryPath.Substring(0, repositoryPath.LastIndexOf("/"));
            string projectName = Path.GetFileName(projectPath);

            List<string> actualList = new()
            {
                projectName,
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
            Assert.AreEqual(28, pathsFromGraph.Count());
            Assert.IsTrue(actualList.OrderByDescending(x => x).ToList().SequenceEqual(pathsFromGraph.OrderByDescending(x => x).ToList()));
        }

        private VCSGraphProvider GetVCSGraphProvider()
        {
            return new VCSGraphProvider()
            {
                RepositoryPath = new DirectoryPath(System.IO.Path.GetDirectoryName(Application.dataPath)),
                CommitID = "b10e1f49c144c0a22aa0d972c946f93a82ad3461",
            };
        }

        private GraphProvider Load()
        {
            using ConfigReader stream = new(filename);
            GraphProvider loaded = GraphProvider.Restore(stream.Read(), providerLabel);
            Assert.IsNotNull(loaded);
            return loaded;
        }

        private void Save(GraphProvider saved)
        {
            using ConfigWriter writer = new(filename);
            saved.Save(writer, providerLabel);
        }
    }
}
