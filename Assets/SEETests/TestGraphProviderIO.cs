using NUnit.Framework;
using SEE.DataModel.DG;
using SEE.Game.City;
using SEE.Utils;
using SEE.Utils.Config;
using SEE.Utils.Paths;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
                "Assets",
                "Assets/Camera",
                "Assets/Camera/FlyCamera.cs",
                "Assets/Editor",
                "Assets/Editor/CityEditor.cs",
                "Assets/Editor/ImportProcessor.cs",
                "Assets/Editor/SEEEditor.cs",
                "Assets/Editor/UnityEditorStartup.cs",
                "Assets/SEECity",
                "Assets/SEECity/DataModel",
                "Assets/SEECity/DataModel/Attributable.cs",
                "Assets/SEECity/DataModel/Edge.cs",
                "Assets/SEECity/DataModel/GXLParser.cs",
                "Assets/SEECity/DataModel/Graph.cs",
                "Assets/SEECity/DataModel/GraphCreator.cs",
                "Assets/SEECity/DataModel/GraphElement.cs",
                "Assets/SEECity/DataModel/IAttributable.cs",
                "Assets/SEECity/DataModel/IEdge.cs",
                "Assets/SEECity/DataModel/IGraph.cs",
                "Assets/SEECity/DataModel/IGraphElement.cs",
                "Assets/SEECity/DataModel/IGraphSettings.cs",
                "Assets/SEECity/DataModel/INode.cs",
                "Assets/SEECity/DataModel/ISceneGraph.cs",
                "Assets/SEECity/DataModel/Node.cs",
                "Assets/SEECity/DataModel/Tags.cs",
                "Assets/SEECity/DataModel/UnknownAttribute.cs",
                "Assets/SEECity/Layout",
                "Assets/SEECity/Layout/BalloonLayout.cs",
                "Assets/SEECity/Layout/GridLayout.cs",
                "Assets/SEECity/Layout/ILayout.cs",
                "Assets/SEECity/Layout/MeshFactory.cs",
                "Assets/SEECity/SceneGraphs.cs",
                "Assets/SEECity/Utils",
                "Assets/SEECity/Utils/ILogger.cs",
                "Assets/SEECity/Utils/Performance.cs",
                "Assets/SEECity/Utils/SEELogger.cs",
                "SEE.cs"
            };
            Assert.IsTrue(actualList.SequenceEqual(pathsFromGraph));
        }

        private VCSGraphProvider GetVCSGraphProvider()
        {
            return new VCSGraphProvider()
            {
                RepositoryPath = new DirectoryPath(System.IO.Path.GetDirectoryName(Application.dataPath)),
                CommitID = "9a1a15375d2465d0d05e442ef8368f2645ba4344",
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
