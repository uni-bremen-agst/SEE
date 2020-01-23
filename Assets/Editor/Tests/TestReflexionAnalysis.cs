using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace SEE.DataModel
{
    /// <summary>
    /// Unit tests for ReflexionAnalysis.
    /// </summary>
    internal class TestReflexionAnalysis
    {
        private Graph impl;
        private Graph arch;
        private Graph mapping;
        private Reflexion reflexion;
        private SEELogger logger = new SEE.SEELogger();
        /// <summary>
        /// The names of the edge types of hierarchical edges.
        /// </summary>
        private HashSet<string> HierarchicalEdges;

        [SetUp]
        public virtual void Setup()
        {
            HierarchicalEdges = Hierarchical_Edge_Types();
        }

        /// <summary>
        /// The names of the edge types of hierarchical edges.
        /// </summary>
        private static HashSet<string> Hierarchical_Edge_Types()
        {
            HashSet<string> result = new HashSet<string>
            {
                "Enclosing",
                "Belongs_To",
                "Part_Of",
                "Defined_In"
            };
            return result;
        }

        [TearDown]
        public virtual void Teardown()
        {
            impl = null;
            arch = null;
            mapping = null;
            reflexion = null;
            HierarchicalEdges = null;
            logger = null;
        }

        [Test]
        public void TestMinilax()
        {
            LoadAll("minilax");
            reflexion = new Reflexion(impl, arch, mapping, "Reflexion", "Source_Dependency");
            reflexion.dump_results();
        }

        private Graph Load(string path)
        {
            string platformPath = Filenames.OnCurrentPlatform(path);
            Debug.LogFormat("Loading graph from {0}...\n", platformPath);
            GraphReader graphCreator = new GraphReader(platformPath, HierarchicalEdges, logger);
            graphCreator.Load();
            Graph result = graphCreator.GetGraph();
            Assert.That(result, !Is.Null);
            Debug.LogFormat("Loaded {0} nodes and {1} edges.\n", result.NodeCount, result.EdgeCount);
            //result.DumpTree();
            return result;
        }
        private void LoadAll(string v)
        {
            string path = Application.dataPath + "/../Data/GXL/reflexion/" + v + "/";
            impl = Load(path + "CodeFacts.gxl");
            arch = Load(path + "Architecture.gxl");
            mapping = Load(path + "Mapping.gxl");
        }
    }
}