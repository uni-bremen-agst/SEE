using NUnit.Framework;
using SEE.Utils;
using SEE.Utils.Config;
using System.IO;
using UnityEngine;

namespace SEE.GraphProviders
{
    /// <summary>
    /// Test cases for input/output of <see cref="GraphProviders"/>.
    /// </summary>
    internal class TestGraphProviderIO : AbstractTestConfigIO
    {
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

        private void AreEqualGXLProviders(GXLGraphProvider saved, GraphProvider loaded)
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

        private void AreEqualCSVProviders(CSVGraphProvider saved, GraphProvider loaded)
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
            Print();
            AreEqualPipelineProviders(saved, Load());
        }

        [Test]
        public void TestEmptyPipelineProvider()
        {
            PipelineGraphProvider saved = new();
            Save(saved);
            Print();
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
            Print();
            AreEqualPipelineProviders(saved, Load());
        }

        private void AreEqualPipelineProviders(PipelineGraphProvider saved, GraphProvider loaded)
        {
            Assert.IsTrue(saved.GetType() == loaded.GetType());
            PipelineGraphProvider pipelineLoaded = loaded as PipelineGraphProvider;
            Assert.AreEqual(saved.Pipeline.Count, pipelineLoaded.Pipeline.Count);
            for (int i = 0; i < saved.Pipeline.Count; i++)
            {
                GraphProvider savedProvider = saved.Pipeline[i];
                GraphProvider loadedProvider = pipelineLoaded.Pipeline[i];
                Assert.IsTrue(savedProvider.GetType() == loadedProvider.GetType());
                if (savedProvider is GXLGraphProvider gXLGraphProvider)
                {
                    AreEqualGXLProviders(gXLGraphProvider, loadedProvider);
                }
                else if (savedProvider is CSVGraphProvider csvGraphProvider)
                {
                    AreEqualCSVProviders(csvGraphProvider, loadedProvider);
                }
                else if (savedProvider is PipelineGraphProvider pipelineGraphProvider)
                {
                    AreEqualPipelineProviders(pipelineGraphProvider, loadedProvider);
                }
                else
                {
                    throw new System.NotImplementedException();
                }
            }
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

        private void Print()
        {
            if (!string.IsNullOrWhiteSpace(filename) && File.Exists(filename))
            {
                foreach (string line in File.ReadAllLines(filename))
                {
                    Debug.Log(line);
                }
            }
        }
    }
}
