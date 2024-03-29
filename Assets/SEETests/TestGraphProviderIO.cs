﻿using NUnit.Framework;
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
