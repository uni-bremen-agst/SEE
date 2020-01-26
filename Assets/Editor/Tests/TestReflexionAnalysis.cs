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
            reflexion = new Reflexion(impl, arch, mapping);
            //reflexion.dump_results();
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

        private static Node NewNode(Graph graph, string linkname)
        {
            Node result = new Node();
            result.LinkName = linkname;
            result.Type = "Routine";
            graph.AddNode(result);
            return result;
        }

        private static Edge NewEdge(Graph graph, Node from, Node to, string type)
        {
            Edge result = new Edge();
            result.Type = type;
            result.Source = from;
            result.Target = to;
            graph.AddEdge(result);
            return result;
        }

        /// <summary>
        /// Creates a graph with the following node hierarchy:
        ///  n1
        ///   -- n1_c1
        ///         -- n1_c1_c1
        ///         -- n1_c1_c2
        ///   -- n1_c2
        ///  n2
        ///   -- n2_c1
        ///  n3
        ///  
        /// edges:
        ///   import(n1, n2)
        ///   import(n2, n3)
        ///   import(n2, n3)
        /// </summary>
        /// <returns>new graph with a hierarchy of nodes</returns>
        private Graph NewImplementationNodeHierarchy()
        {
            Graph graph = new Graph();
            graph.Path = "path";
            graph.Name = "name";

            // Root nodes
            Node n1 = NewNode(graph, "n1");
            Node n2 = NewNode(graph, "n2");
            Node n3 = NewNode(graph, "n3");
            graph.AddNode(n1);
            graph.AddNode(n2);
            graph.AddNode(n3);

            // Second level
            Node n1_c1 = NewNode(graph, "n1_c1");
            Node n1_c2 = NewNode(graph, "n1_c2");
            graph.AddNode(n1_c1);
            graph.AddNode(n1_c2);
            n1.AddChild(n1_c1);
            n1.AddChild(n1_c2);

            Node n2_c1 = NewNode(graph, "n2_c1");
            graph.AddNode(n2_c1);
            n2.AddChild(n2_c1);

            // Third level
            Node n1_c1_c1 = NewNode(graph, "n1_c1_c1");
            Node n1_c1_c2 = NewNode(graph, "n1_c1_c2");
            graph.AddNode(n1_c1_c1);
            graph.AddNode(n1_c1_c2);
            n1_c1.AddChild(n1_c1_c1);
            n1_c1.AddChild(n1_c1_c2);

            // Note: The levels must be calculated when the hierarchy has been
            // established. This is not done automatically.
            graph.CalculateLevels();

            return graph;
        }

        /// <summary>
        /// Returns the implementation graph created by NewImplementationNodeHierarchy()
        /// with the following additional edges:
        ///   import(n1, n2)
        ///   import(n2, n3)
        ///   import(n2, n3)
        /// </summary>
        /// <returns>implementation graph</returns>
        private Graph ImportGraph()
        {
            Graph graph = NewImplementationNodeHierarchy();
            Node n1 = graph.GetNode("n1");
            Node n2 = graph.GetNode("n2");
            Node n3 = graph.GetNode("n3");
            Edge e1 = NewEdge(graph, n1, n2, "import");
            Edge e2 = NewEdge(graph, n2, n3, "import");
            Edge e3 = NewEdge(graph, n2, n3, "import");
            graph.AddEdge(e1);
            graph.AddEdge(e2);
            graph.AddEdge(e3);
            return graph;
        }
    }
}