using NUnit.Framework;
using UnityEngine;

using SEE.DataModel;

namespace SEE.Tools
{
    internal class TestReflexionAnalysisStress : TestReflexionAnalysis
    {
        [Test]
        public void TestMinilax()
        {
            LoadAll("minilax", out Graph impl, out Graph arch, out Graph mapping);
            Performance p = Performance.Begin("Running reflexion analysis");
            reflexion = new Reflexion(impl, arch, mapping);
            reflexion.Register(this);
            reflexion.Run();
            p.End();
            //reflexion.dump_results();
        }

        private Graph Load(string path)
        {
            string platformPath = Filenames.OnCurrentPlatform(path);
            Debug.LogFormat("Loading graph from {0}...\n", platformPath);
            GraphReader graphCreator = new GraphReader(platformPath, HierarchicalEdges, "", logger);
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
