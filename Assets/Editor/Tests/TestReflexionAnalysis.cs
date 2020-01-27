using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace SEE.DataModel
{
    /// <summary>
    /// Unit tests for ReflexionAnalysis.
    /// </summary>
    internal class TestReflexionAnalysis : Observer
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

        /// <summary>
        /// Sets up all three graphs (implementation, architecture,
        /// mapping) and registers itself at the reflexion analysis
        /// to obtain the results via the callback Update(ChangeEvent).
        /// Does not really run the analysis, however.
        /// </summary>
        [SetUp]
        public virtual void Setup()
        {
            HierarchicalEdges = Hierarchical_Edge_Types();
            impl = NewImplementationNodeHierarchy();
            arch = NewArchitecture();
            mapping = NewMapping();
            reflexion = new Reflexion(impl, arch, mapping);
            reflexion.Register(this);
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
            LoadAll("minilax", out Graph impl, out Graph arch, out Graph mapping);
            reflexion = new Reflexion(impl, arch, mapping);
            reflexion.Register(this);
            reflexion.Run();
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

        private void LoadAll(string folderName, out Graph impl, out Graph arch, out Graph mapping)
        {
            string path = Application.dataPath + "/../Data/GXL/reflexion/" + folderName + "/";
            impl = Load(path + "CodeFacts.gxl");
            arch = Load(path + "Architecture.gxl");
            mapping = Load(path + "Mapping.gxl");
        }

        private static Node NewNode(Graph graph, string linkname, string type = "Routine")
        {
            Node result = new Node();
            result.LinkName = linkname;
            result.SourceName = linkname;
            result.Type = type;
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
        /// Returns new architecture graph with the following node hierarchy:
        /// 
        ///  N1
        ///   -- N1_C1
        ///   -- N1_C2
        ///  N2
        ///   -- N2_C1
        ///  N3
        ///  
        /// edges:
        /// 
        /// call(N1_C1, N2_C1)
        /// call(N2, N1)
        /// call(N3, N1_C2)
        /// call(N3, N2_C1)
        /// </summary>
        /// <returns>new architecture graph</returns>
        private Graph NewArchitecture()
        {
            Graph graph = new Graph();
            graph.Path = "none";
            graph.Name = "architecture";

            // Root nodes
            Node n1 = NewNode(graph, "N1", "Component");
            Node n2 = NewNode(graph, "N2", "Component");
            Node n3 = NewNode(graph, "N3", "Component");

            // Second level
            Node n1_c1 = NewNode(graph, "N1_C1", "Component");
            Node n1_c2 = NewNode(graph, "N1_C2", "Component");
            n1.AddChild(n1_c1);
            n1.AddChild(n1_c2);
            Node n2_c1 = NewNode(graph, "N2_C1", "Component");
            n2.AddChild(n2_c1);

            NewEdge(graph, n1_c1, n2_c1, "call");
            NewEdge(graph, n2, n1, "call");
            NewEdge(graph, n3, n2_c1, "call");
            NewEdge(graph, n3, n1_c2, "call");

            return graph;
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
        /// <returns>new implementation graph with a hierarchy of nodes</returns>
        private Graph NewImplementationNodeHierarchy()
        {
            Graph graph = new Graph();
            graph.Path = "path";
            graph.Name = "implementation";

            // Root nodes
            Node n1 = NewNode(graph, "n1");
            Node n2 = NewNode(graph, "n2");
            Node n3 = NewNode(graph, "n3");

            // Second level
            Node n1_c1 = NewNode(graph, "n1_c1");
            Node n1_c2 = NewNode(graph, "n1_c2");
            n1.AddChild(n1_c1);
            n1.AddChild(n1_c2);

            Node n2_c1 = NewNode(graph, "n2_c1");
            n2.AddChild(n2_c1);

            // Third level
            Node n1_c1_c1 = NewNode(graph, "n1_c1_c1");
            Node n1_c1_c2 = NewNode(graph, "n1_c1_c2");
            n1_c1.AddChild(n1_c1_c1);
            n1_c1.AddChild(n1_c1_c2);

            // Note: The levels must be calculated when the hierarchy has been
            // established. This is not done automatically.
            graph.CalculateLevels();

            return graph;
        }

        /// <summary>
        /// Returns a new mapping between implementation and architecture as follows:
        /// 
        /// n1    -Maps_To-> N1
        /// n2    -Maps_To-> N2
        /// n3    -Maps_To-> N3
        /// n1_c1 -Maps_To-> N1_C1
        /// n1_c2 -Maps_To-> N1_C2
        /// n2_c1 -Maps_To-> N2_C1
        /// </summary>
        /// <returns>mapping from implementation onto architecture</returns>
        private Graph NewMapping()
        {
            Graph graph = new Graph();
            graph.Path = "whatever";
            graph.Name = "mapping";

            // architecture
            Node N1 = NewNode(graph, "N1", "Component");
            Node N2 = NewNode(graph, "N2", "Component");
            Node N3 = NewNode(graph, "N3", "Component");
            Node N1_C1 = NewNode(graph, "N1_C1", "Component");
            Node N1_C2 = NewNode(graph, "N1_C2", "Component");
            Node N2_C1 = NewNode(graph, "N2_C1", "Component");

            // implementation
            Node n1 = NewNode(graph, "n1");
            Node n2 = NewNode(graph, "n2");
            Node n3 = NewNode(graph, "n3");
            Node n1_c1 = NewNode(graph, "n1_c1");
            Node n1_c2 = NewNode(graph, "n1_c2");
            Node n2_c1 = NewNode(graph, "n2_c1");

            // n1_c1_c1 and n1_c1_c2 are implicitly mapped

            NewEdge(graph, n1, N1, "Maps_To");
            NewEdge(graph, n2, N2, "Maps_To");
            NewEdge(graph, n3, N3, "Maps_To");
            NewEdge(graph, n1_c1, N1_C1, "Maps_To");
            NewEdge(graph, n1_c2, N1_C2, "Maps_To");
            NewEdge(graph, n2_c1, N2_C1, "Maps_To");

            return graph;
        }

        /// <summary>
        /// Returns the implementation graph created by NewImplementationNodeHierarchy()
        /// with the following additional edges:
        ///   call(n1, n2)
        ///   call(n2, n3)
        ///   call(n2, n3)
        /// </summary>
        /// <returns>implementation graph</returns>
        private Graph ImportGraph()
        {
            Graph graph = NewImplementationNodeHierarchy();
            Node n1 = graph.GetNode("n1");
            Node n2 = graph.GetNode("n2");
            Node n3 = graph.GetNode("n3");
            NewEdge(graph, n1, n2, "call");
            NewEdge(graph, n2, n3, "call");
            NewEdge(graph, n2, n3, "call");
            return graph;
        }

        [Test]
        public void TestConvergences()
        {
            Node n1 = impl.GetNode("n1");
            Node n2 = impl.GetNode("n2");
            Node n3 = impl.GetNode("n3");

            Node n1_c1_c1 = impl.GetNode("n1_c1_c1");
            Node n1_c1_c2 = impl.GetNode("n1_c1_c2");
            Node n2_c1 = impl.GetNode("n2_c1");

            NewEdge(impl, n2, n1, "call");
            NewEdge(impl, n3, n2_c1, "call");
            NewEdge(impl, n1, n2, "call");
            NewEdge(impl, n2, n3, "call");
            NewEdge(impl, n2, n3, "call");

            reflexion.Run();
        }

        public void Update(ChangeEvent changeEvent)
        {
            Debug.Log(changeEvent.ToString());
        }
    }
}