using NUnit.Framework;
using SEE.DataModel.DG;
using SEE.DataModel.DG.IO;
using SEE.Tools.ReflexionAnalysis;
using SEE.Utils;
using UnityEngine;
using static SEE.Tools.ReflexionAnalysis.ReflexionGraphTools;

namespace SEE.Tools.Architecture
{
    internal class TestReflexionAnalysisStress : TestReflexionAnalysis
    {
        /// <summary>
        /// Non-incremental reflexion analysis for minilax example.
        /// </summary>
        [Test]
        public void TestMinilaxNonIncrementally()
        {
            NonIncrementally("minilax");
            //reflexion.DumpArchitecture();
        }

        private void NonIncrementally(string folderName)
        {
            LoadAll(folderName, out Graph impl, out Graph arch, out Graph mapping);
            Performance p = Performance.Begin("Running non-incremental reflexion analysis");
            reflexion = new Reflexion(impl, arch, mapping);
            fullGraph = reflexion.FullGraph;
            reflexion.Register(this);
            reflexion.Run();
            p.End();
        }

        /// <summary>
        /// Incremental reflexion analysis for minilax example.
        /// </summary>
        [Test]
        public void TestMinilaxIncrementally()
        {
            Incrementally("minilax");
            //reflexion.DumpArchitecture();
        }

        private void Incrementally(string folderName)
        {
            LoadAll(folderName, out Graph impl, out Graph arch, out Graph mapping);
            Performance p = Performance.Begin("Running incremental reflexion analysis");
            // Passing the empty graph as mapping argument to reflexion.
            reflexion = new Reflexion(impl, arch, new Graph("DUMMYBASEPATH"));
            fullGraph = reflexion.FullGraph;
            reflexion.Register(this);
            reflexion.Run(); // from scratch
            // Now add the mappings incrementally.
            foreach (Edge map in mapping.Edges())
            {
                Node source = fullGraph.GetNode(map.Source.ID);
                Assert.IsTrue(source.IsInImplementation());
                Node target = fullGraph.GetNode(map.Target.ID);
                Assert.IsTrue(target.IsInArchitecture());
                Assert.NotNull(source);
                Assert.NotNull(target);
                reflexion.AddToMapping(source, target);
            }
            p.End();
        }

        /// <summary>
        /// Compares the result of incremental and non-incremental reflexion analysis for minilax example.
        /// </summary>
        [Test]
        public void TestMinilaxComparison()
        {
            const string folderName = "minilax";
            NonIncrementally(folderName);
            int[] incrementally = reflexion.Summary();
            Teardown();
            Setup();
            Incrementally(folderName);
            int[] nonIncrementally = reflexion.Summary();
            Assert.AreEqual(incrementally, nonIncrementally);
        }

        private Graph Load(string path)
        {
            string platformPath = Filenames.OnCurrentPlatform(path);
            Debug.LogFormat("Loading graph from {0}...\n", platformPath);
            GraphReader graphCreator = new GraphReader(platformPath, HierarchicalEdges, basePath: "", rootID: "", logger);
            graphCreator.Load();
            Graph result = graphCreator.GetGraph();
            Assert.That(result, !Is.Null);
            Debug.LogFormat("Loaded {0} nodes and {1} edges.\n", result.NodeCount, result.EdgeCount);
            //result.DumpTree();
            return result;
        }

        private void LoadAll(string folderName, out Graph impl, out Graph arch, out Graph mapping)
        {
            string path = Application.dataPath + "/../Data/GXL/reflexion/" + folderName + "/";
            Performance p = Performance.Begin("Loading graphs");
            impl = Load(path + "CodeFacts.gxl");
            arch = Load(path + "Architecture.gxl");
            mapping = Load(path + "Mapping.gxl");
            p.End();
        }
    }
}
