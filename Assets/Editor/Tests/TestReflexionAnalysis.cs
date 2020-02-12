using System;
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
        private const string call = "call";
        private Graph impl;
        private Graph arch;
        private Graph mapping;
        private Reflexion reflexion;
        private SEELogger logger = new SEE.SEELogger();
        /// <summary>
        /// The names of the edge types of hierarchical edges.
        /// </summary>
        private HashSet<string> HierarchicalEdges;

        //----------------------------
        // implementation nodes
        //----------------------------
        /// <summary>
        /// Node "n1" in implementation.
        /// </summary>
        private Node n1;
        /// <summary>
        /// Node "n1_c1" in implementation.
        /// </summary>
        private Node n1_c1;
        /// <summary>
        /// Node "n1_c1_c2" in implementation.
        /// </summary>
        private Node n1_c1_c1;
        /// <summary>
        /// Node "n1_c1_c1" in implementation.
        /// </summary>
        private Node n1_c1_c2;
        /// <summary>
        /// Node "n1_c1_c2" in implementation.
        /// </summary>
        private Node n1_c2;
        /// <summary>
        /// Node "n2" in implementation.
        /// </summary>
        private Node n2;
        /// <summary>
        /// Node "n2_c1" in implementation.
        /// </summary>
        private Node n2_c1;
        /// <summary>
        /// Node "n3" in implementation.
        /// </summary>
        private Node n3;

        //----------------------------
        // architecture nodes
        //----------------------------
        /// <summary>
        /// Node "N1" in architecture.
        /// </summary>
        private Node N1;
        /// <summary>
        /// Node "N1_C1" in architecture.
        /// </summary>
        private Node N1_C1;
        /// <summary>
        /// Node "N1_C2" in architecture.
        /// </summary>
        private Node N1_C2;
        /// <summary>
        /// Node "N2" in architecture.
        /// </summary>
        private Node N2;
        /// <summary>
        /// Node "N2_C1" in architecture.
        /// </summary>
        private Node N2_C1;
        /// <summary>
        /// Node "N3" in architecture.
        /// </summary>
        private Node N3;

        /// <summary>
        /// Sets the implementation nodes (retrieved from impl graph) and architecture
        /// nodes (retrieved from arch graph).
        /// </summary>
        private void SetNodes()
        {
            // implementation nodes
            n1       = impl.GetNode("n1");
            n1_c1    = impl.GetNode("n1_c1");
            n1_c2    = impl.GetNode("n1_c2");
            n1_c1_c1 = impl.GetNode("n1_c1_c1");
            n1_c1_c2 = impl.GetNode("n1_c1_c2");
            n2       = impl.GetNode("n2");
            n2_c1    = impl.GetNode("n2_c1");
            n3       = impl.GetNode("n3");
            // architecture nodes
            N1    = arch.GetNode("N1");
            N1_C1 = arch.GetNode("N1_C1");
            N1_C2 = arch.GetNode("N1_C2");
            N2    = arch.GetNode("N2");
            N2_C1 = arch.GetNode("N2_C1");
            N3    = arch.GetNode("N3");
        }
        /// <summary>
        /// Resets the implementation nodes (retrieved from impl graph) and architecture
        /// nodes (retrieved from arch graph) to null.
        /// </summary>
        private void ClearNodes()
        {
            // implementation nodes
            n1 = null;
            n1_c1 = null;
            n1_c2 = null;
            n1_c1_c1 = null;
            n1_c1_c2 = null;
            n2 = null;
            n2_c1 = null;
            n3 = null;
            // architecture nodes
            N1 = null;
            N1_C1 = null;
            N1_C2 = null;
            N2 = null;
            N2_C1 = null;
            N3 = null;
        }

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
            ClearEvents();
            SetNodes();
        }

        /// <summary>
        /// Sets the event chaches edgeChanges, propagatedEdges, and removedEdges to
        /// to their initial value (empty).
        /// </summary>
        private void ClearEvents()
        {
            edgeChanges = new List<EdgeChange>();
            propagatedEdges = new List<PropagatedEdge>();
            removedEdges = new List<RemovedEdge>();
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
            edgeChanges = null;
            propagatedEdges = null;
            removedEdges = null;
            ClearNodes();
        }

        /// <summary>
        /// List of edges changed for a single reflexion-analysis run.
        /// </summary>
        private List<EdgeChange> edgeChanges = new List<EdgeChange>();
        /// <summary>
        /// List of edges propagated from the implementation onto the architecture for 
        /// a single reflexion-analysis run.
        /// </summary>
        private List<PropagatedEdge> propagatedEdges = new List<PropagatedEdge>();
        /// <summary>
        /// List of edges removed for a single reflexion-analysis run.
        /// </summary>
        private List<RemovedEdge> removedEdges = new List<RemovedEdge>();

        /// <summary>
        /// Dumps the results collected in edgeChanges, propagatedEdges, and removedEdges
        /// to standard output.
        /// </summary>
        private void DumpEvents()
        {
            Debug.Log("DEPENDENCIES PROPAGATED TO ARCHITECTURE\n");
            foreach (PropagatedEdge e in propagatedEdges)
            {
                Debug.LogFormat("propagated {0}\n", e.propagatedEdge.ToString());
            }
            Debug.Log("DEPENDENCIES CHANGED IN ARCHITECTURE\n");
            foreach (EdgeChange e in edgeChanges)
            {
                Debug.LogFormat("changed {0} from {1} to {2}\n", e.edge.ToString(), e.oldState, e.newState);
            }
            Debug.Log("DEPENDENCIES REMOVED FROM ARCHITECTURE\n");
            foreach (RemovedEdge e in removedEdges)
            {
                Debug.LogFormat("removed {0}\n", e.edge.ToString());
            }
        }

        /// <summary>
        /// Callback of reflexion analysis. Will be called by reflexion analysis on every
        /// state change. Collects the events in the respective change-event lists
        /// edgeChanges, propagatedEdges, removedEdges.
        /// </summary>
        /// <param name="changeEvent">the event that occurred</param>
        public void Update(ChangeEvent changeEvent)
        {
            if (changeEvent is EdgeChange)
            {
                edgeChanges.Add(changeEvent as EdgeChange);
            }
            else if (changeEvent is PropagatedEdge)
            {
                propagatedEdges.Add(changeEvent as PropagatedEdge);
            }
            else if (changeEvent is RemovedEdge)
            {
                removedEdges.Add(changeEvent as RemovedEdge);
            }
            else
            {
                Debug.LogErrorFormat("UNHANDLED CALLBACK: {0}\n", changeEvent.ToString());
            }
        }

        // [Test] FIXME: Temporarily disabled.
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
        /// call(N3, N2_C1) marked as Architecture.Is_Optional
        /// </summary>
        /// <returns>new architecture graph</returns>
        private Graph NewArchitecture()
        {
            Graph graph = new Graph();
            graph.Path = "none";
            graph.Name = "architecture";

            // Root nodes
            Node N1 = NewNode(graph, "N1", "Component");
            Node N2 = NewNode(graph, "N2", "Component");
            Node N3 = NewNode(graph, "N3", "Component");

            // Second level
            Node N1_C1 = NewNode(graph, "N1_C1", "Component");
            Node N1_C2 = NewNode(graph, "N1_C2", "Component");
            N1.AddChild(N1_C1);
            N1.AddChild(N1_C2);
            Node N2_C1 = NewNode(graph, "N2_C1", "Component");
            N2.AddChild(N2_C1);

            NewEdge(graph, N1_C1, N2_C1, call);
            NewEdge(graph, N2, N1, call);
            Edge edge = NewEdge(graph, N3, N2_C1, call);
            edge.SetToggle("Architecture.Is_Optional");
            NewEdge(graph, N3, N1_C2, call);
            
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
        private Graph CallGraph()
        {
            Graph graph = NewImplementationNodeHierarchy();
            Node n1 = graph.GetNode("n1");
            Node n2 = graph.GetNode("n2");
            Node n3 = graph.GetNode("n3");
            NewEdge(graph, n1, n2, call);
            NewEdge(graph, n2, n3, call);
            NewEdge(graph, n2, n3, call);
            return graph;
        }

        /// <summary>
        /// True if edgeChanges has an edge from source to target with given edgeType whose new state is the
        /// given state.
        /// </summary>
        /// <param name="edgeChanges">list of edge-change events</param>
        /// <param name="source">source of edge</param>
        /// <param name="target">target of edge</param>
        /// <param name="edgeType">type of edge</param>
        /// <param name="state">new state</param>
        /// <returns>true if such an edge exists</returns>
        private bool HasNewState(List<EdgeChange> edgeChanges, Node source, Node target, string edgeType, State state)
        {
            foreach (EdgeChange e in edgeChanges)
            {
                if (e.edge.Source == source && e.edge.Target == target && e.edge.Type == edgeType && e.newState == state)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Equivalent to: HasNewState(edgeChanges, source, target, edgeType, State.convergent).
        /// </summary>
        /// <param name="edgeChanges">list of edge-change events</param>
        /// <param name="source">source of edge</param>
        /// <param name="target">target of edge</param>
        /// <param name="edgeType">type of edge</param>
        /// <param name="state">new state</param>
        /// <returns>true if such an edge exists</returns>
        private bool IsConvergent(List<EdgeChange> edgeChanges, Node source, Node target, string edgeType)
        {
            return HasNewState(edgeChanges, source, target, edgeType, State.convergent);
        }

        /// <summary>
        /// Equivalent to: HasNewState(edgeChanges, source, target, edgeType, State.allowed).
        /// </summary>
        /// <param name="edgeChanges">list of edge-change events</param>
        /// <param name="source">source of edge</param>
        /// <param name="target">target of edge</param>
        /// <param name="edgeType">type of edge</param>
        /// <param name="state">new state</param>
        /// <returns>true if such an edge exists</returns>
        private bool IsAllowed(List<EdgeChange> edgeChanges, Node source, Node target, string edgeType)
        {
            return HasNewState(edgeChanges, source, target, edgeType, State.allowed);
        }

        /// <summary>
        /// Equivalent to: HasNewState(edgeChanges, source, target, edgeType, State.absent).
        /// </summary>
        /// <param name="edgeChanges">list of edge-change events</param>
        /// <param name="source">source of edge</param>
        /// <param name="target">target of edge</param>
        /// <param name="edgeType">type of edge</param>
        /// <param name="state">new state</param>
        /// <returns>true if such an edge exists</returns>
        private bool IsAbsent(List<EdgeChange> edgeChanges, Node source, Node target, string edgeType)
        {
            return HasNewState(edgeChanges, source, target, edgeType, State.absent);
        }

        /// <summary>
        /// Equivalent to: HasNewState(edgeChanges, source, target, edgeType, State.implicitly_allowed).
        /// </summary>
        /// <param name="edgeChanges">list of edge-change events</param>
        /// <param name="source">source of edge</param>
        /// <param name="target">target of edge</param>
        /// <param name="edgeType">type of edge</param>
        /// <param name="state">new state</param>
        /// <returns>true if such an edge exists</returns>
        private bool IsImplicitlyAllowed(List<EdgeChange> edgeChanges, Node source, Node target, string edgeType)
        {
            return HasNewState(edgeChanges, source, target, edgeType, State.implicitly_allowed);
        }

        /// <summary>
        /// Equivalent to: HasNewState(edgeChanges, source, target, edgeType, State.allowed_absent).
        /// </summary>
        /// <param name="edgeChanges">list of edge-change events</param>
        /// <param name="source">source of edge</param>
        /// <param name="target">target of edge</param>
        /// <param name="edgeType">type of edge</param>
        /// <param name="state">new state</param>
        /// <returns>true if such an edge exists</returns>
        private bool IsAllowedAbsent(List<EdgeChange> edgeChanges, Node source, Node target, string edgeType)
        {
            return HasNewState(edgeChanges, source, target, edgeType, State.allowed_absent);
        }

        /// <summary>
        /// Equivalent to: HasNewState(edgeChanges, source, target, edgeType, State.divergent).
        /// </summary>
        /// <param name="edgeChanges">list of edge-change events</param>
        /// <param name="source">source of edge</param>
        /// <param name="target">target of edge</param>
        /// <param name="edgeType">type of edge</param>
        /// <param name="state">new state</param>
        /// <returns>true if such an edge exists</returns>
        private bool IsDivergent(List<EdgeChange> edgeChanges, Node source, Node target, string edgeType)
        {
            return HasNewState(edgeChanges, source, target, edgeType, State.divergent);
        }

        //-------------------
        // Implicitly allowed 
        //-------------------

        private void CommonImplicitlyAllowed()
        {
            // 1 propagated edges
            Assert.AreEqual(1, propagatedEdges.Count);

            // 1 implicitly allowed propagated dependencies
            Assert.That(IsImplicitlyAllowed(edgeChanges, N1_C1, N1_C1, call));
            // 4 absences 
            Assert.That(IsAbsent(edgeChanges, N2, N1, call));
            Assert.That(IsAbsent(edgeChanges, N1_C1, N2_C1, call));
            Assert.That(IsAbsent(edgeChanges, N3, N1_C2, call));
            Assert.That(IsAllowedAbsent(edgeChanges, N3, N2_C1, call));
            // 0 divergences
            Assert.AreEqual(5, edgeChanges.Count);

            // 0 removed edges
            Assert.AreEqual(0, removedEdges.Count);
        }

        [Test]
        public void TestImplicitlyAllowed1()
        {
            // dependency between siblings not mapped
            NewEdge(impl, n1_c1_c1, n1_c1_c2, call);
            reflexion.Run();

            CommonImplicitlyAllowed();
        }

        [Test]
        public void TestImplicitlyAllowed2()
        {
            // self dependency for node not mapped
            NewEdge(impl, n1_c1_c1, n1_c1_c1, call);
            reflexion.Run();

            CommonImplicitlyAllowed();
        }

        [Test]
        public void TestImplicitlyAllowed3()
        {
            // self dependency for node mapped
            NewEdge(impl, n1_c1, n1_c1, call);
            reflexion.Run();

            CommonImplicitlyAllowed();
        }

        [Test]
        public void TestImplicitlyAllowed4()
        {
            // dependency to parent where source is not mapped and target is mapped
            NewEdge(impl, n1_c1_c1, n1_c1, call);
            reflexion.Run();

            CommonImplicitlyAllowed();
        }

        //---------------------------
        // Access along the hierarchy
        //---------------------------

        private void CommonHierarchyAccess()
        {
            // 1 propagated edges
            Assert.AreEqual(1, propagatedEdges.Count);

            // 4 absences 
            Assert.That(IsAbsent(edgeChanges, N2, N1, call));
            Assert.That(IsAbsent(edgeChanges, N1_C1, N2_C1, call));
            Assert.That(IsAbsent(edgeChanges, N3, N1_C2, call));
            Assert.That(IsAllowedAbsent(edgeChanges, N3, N2_C1, call));
            // 0 divergences
            Assert.AreEqual(5, edgeChanges.Count);

            // 0 removed edges
            Assert.AreEqual(0, removedEdges.Count);
        }

        [Test]
        public void TestAllowedParentAccess1()
        {
            // dependency to parent where source and target are mapped explicitly
            NewEdge(impl, n1_c1, n1, call);
            reflexion.Run();

            // 1 implicitly allowed propagated dependencies
            Assert.That(IsImplicitlyAllowed(edgeChanges, N1_C1, N1, call));
            CommonHierarchyAccess();
        }

        [Test]
        public void TestAllowedParentAccess2()
        {
            // dependency to parent where source is not mapped explicitly but 
            // target is mapped explicitly
            NewEdge(impl, n1_c1_c1, n1, call);
            reflexion.Run();

            // 1 implicitly allowed propagated dependencies
            Assert.That(IsImplicitlyAllowed(edgeChanges, N1_C1, N1, call));
            CommonHierarchyAccess();
        }

        [Test]
        public void TestAllowedChildAccess1()
        {
            // dependency from parent to child where both are mapped explicitly 
            NewEdge(impl, n1_c1, n1_c1_c1, call);
            reflexion.Run();

            // 1 implicitly allowed propagated dependencies
            Assert.That(IsImplicitlyAllowed(edgeChanges, N1_C1, N1_C1, call));
            CommonHierarchyAccess();
        }

        [Test]
        public void TestDisallowedChildAccess()
        {
            // dependency from parent to child where both are mapped explicitly 
            NewEdge(impl, n1, n1_c1, call);
            reflexion.Run();

            // 1 disallowed propagated dependencies
            Assert.That(IsDivergent(edgeChanges, N1, N1_C1, call));
            CommonHierarchyAccess();
        }

        //-------------------
        // Convergences 
        //-------------------

        [Test]
        public void TestConvergences1()
        {
            // Note: only one propagated edge for the following two implementation dependencies
            // will be created that covers both.
            NewEdge(impl, n1_c1_c1, n2_c1, call);
            NewEdge(impl, n1_c1_c2, n2_c1, call);
            NewEdge(impl, n2, n1_c2, call);
            NewEdge(impl, n3, n2_c1, call);
            NewEdge(impl, n3, n1_c2, call);
            reflexion.Run();

            // 4 propagated edges
            Assert.AreEqual(4, propagatedEdges.Count);

            // 4 convergences
            Assert.That(IsConvergent(edgeChanges, N2, N1, call));
            Assert.That(IsConvergent(edgeChanges, N1_C1, N2_C1, call));
            Assert.That(IsConvergent(edgeChanges, N3, N1_C2, call));
            Assert.That(IsConvergent(edgeChanges, N3, N2_C1, call));

            // 4 allowed propagated dependencies
            Assert.That(IsAllowed(edgeChanges, N1_C1, N2_C1, call));  // covers n1_c1_c1 -> n2_c1 and n1_c1_c2 -> n2_c1
            Assert.That(IsAllowed(edgeChanges, N2, N1_C2, call));
            Assert.That(IsAllowed(edgeChanges, N3, N2_C1, call));
            Assert.That(IsAllowed(edgeChanges, N3, N1_C2, call));
            // 0 absences

            // 0 divergences
            Assert.AreEqual(8, edgeChanges.Count);
            // 0 removed edges
            Assert.AreEqual(0, removedEdges.Count);
        }

        [Test]
        public void TestConvergences2()
        {
            NewEdge(impl, n2, n1, call);
            reflexion.Run();

            // 1 propagated edges
            Assert.AreEqual(1, propagatedEdges.Count);

            // 1 convergences
            Assert.That(IsConvergent(edgeChanges, N2, N1, call));
            // 1 allowed propagated dependencies
            Assert.That(IsAllowed(edgeChanges, N2, N1, call));
            // 3 absences
            Assert.That(IsAbsent(edgeChanges, N1_C1, N2_C1, call));
            Assert.That(IsAbsent(edgeChanges, N3, N1_C2, call));
            Assert.That(IsAllowedAbsent(edgeChanges, N3, N2_C1, call));
            // 0 divergences
            Assert.AreEqual(5, edgeChanges.Count);
            // 0 removed edges
            Assert.AreEqual(0, removedEdges.Count);
        }

        private void CommonTestConvergences345()
        {
            // 1 propagated edges
            Assert.AreEqual(1, propagatedEdges.Count);

            // 1 convergences
            Assert.That(IsConvergent(edgeChanges, N2, N1, call));
            // 3 absences
            Assert.That(IsAbsent(edgeChanges, N1_C1, N2_C1, call));
            Assert.That(IsAbsent(edgeChanges, N3, N1_C2, call));
            Assert.That(IsAllowedAbsent(edgeChanges, N3, N2_C1, call));
            // 0 divergences
            Assert.AreEqual(5, edgeChanges.Count);
            // 0 removed edges
            Assert.AreEqual(0, removedEdges.Count);
        }

        [Test]
        public void TestConvergences3()
        {
            NewEdge(impl, n2_c1, n1_c2, call);
            reflexion.Run();

            // 1 allowed propagated dependencies
            Assert.That(IsAllowed(edgeChanges, N2_C1, N1_C2, call));

            CommonTestConvergences345();
        }

        [Test]
        public void TestConvergences4()
        {
            NewEdge(impl, n2_c1, n1_c1_c2, call);
            reflexion.Run();

            // 1 allowed propagated dependencies
            Assert.That(IsAllowed(edgeChanges, N2_C1, N1_C1, call));

            CommonTestConvergences345();
        }

        [Test]
        public void TestConvergences5()
        {
            NewEdge(impl, n2_c1, n1, call);
            reflexion.Run();

            // 1 allowed propagated dependencies
            Assert.That(IsAllowed(edgeChanges, N2_C1, N1, call));

            CommonTestConvergences345();
        }

        //-------------------
        // Divergences 
        //-------------------

        private void CommonAbsences()
        {
            // 1 propagated edges
            Assert.AreEqual(1, propagatedEdges.Count);

            // 4 absences 
            Assert.That(IsAbsent(edgeChanges, N2, N1, call));
            Assert.That(IsAbsent(edgeChanges, N1_C1, N2_C1, call));
            Assert.That(IsAbsent(edgeChanges, N3, N1_C2, call));
            Assert.That(IsAllowedAbsent(edgeChanges, N3, N2_C1, call));
            // 0 convergences
            Assert.AreEqual(5, edgeChanges.Count);

            // 0 removed edges
            Assert.AreEqual(0, removedEdges.Count);
        }

        [Test]
        public void TestDivergences1()
        {
            NewEdge(impl, n1, n2, call);
            reflexion.Run();

            // 1 divergent propagated dependency
            Assert.That(IsDivergent(edgeChanges, N1, N2, call));

            CommonAbsences();
        }

        [Test]
        public void TestDivergences2()
        {
            NewEdge(impl, n1_c1, n2, call);
            reflexion.Run();

            // 1 divergent propagated dependency
            Assert.That(IsDivergent(edgeChanges, N1_C1, N2, call));

            CommonAbsences();
        }

        [Test]
        public void TestDivergences3()
        {
            NewEdge(impl, n1_c1_c1, n2, call);
            NewEdge(impl, n1_c1_c1, n2, call);
            NewEdge(impl, n1_c1_c2, n2, call);
            reflexion.Run();

            // 1 divergent propagated dependency
            Assert.That(IsDivergent(edgeChanges, N1_C1, N2, call));

            CommonAbsences();
        }

        [Test]
        public void TestDivergences4()
        {
            NewEdge(impl, n1, n2_c1, call);
            reflexion.Run();

            // 1 divergent propagated dependency
            Assert.That(IsDivergent(edgeChanges, N1, N2_C1, call));

            CommonAbsences();
        }

        [Test]
        public void TestDivergences5()
        {
            NewEdge(impl, n1_c2, n2_c1, call);
            reflexion.Run();

            // 1 divergent propagated dependency
            Assert.That(IsDivergent(edgeChanges, N1_C2, N2_C1, call));

            CommonAbsences();
        }

        [Test]
        public void TestDivergences6()
        {
            NewEdge(impl, n1_c1_c1, n1_c2, call);
            NewEdge(impl, n1_c1_c1, n1_c2, call);
            NewEdge(impl, n1_c1_c2, n1_c2, call);
            reflexion.Run();

            // 1 divergent propagated dependency
            Assert.That(IsDivergent(edgeChanges, N1_C1, N1_C2, call));

            CommonAbsences();
        }

        //--------------------
        // Incremental mapping 
        //--------------------

        [Test]
        public void TestIncrementalMapping()
        {
            // This test case follows the scenarios described in the paper
            // "Incremental Reflexion Analysis", Rainer Koschke, Journal on Software Maintenance
            // and Evolution, 2011, DOI 10.1002 / smr.542 in Figure 8.

            // We will first create the implementation graph
            impl = new Graph();
            Dictionary<int, Node> i = new Dictionary<int, Node>();
            for (int j = 1; j <= 17; j++)
            {
                i.Add(j, NewNode(impl, "i" + j));
            }
            i[1].AddChild(i[2]);
            i[1].AddChild(i[11]);

            i[2].AddChild(i[3]);
            i[2].AddChild(i[7]);

            i[3].AddChild(i[4]);
            i[3].AddChild(i[5]);
            i[3].AddChild(i[6]);

            i[7].AddChild(i[8]);
            i[7].AddChild(i[9]);
            i[7].AddChild(i[10]);

            i[11].AddChild(i[12]);
            i[11].AddChild(i[13]);

            Dictionary<int, Edge> e = new Dictionary<int, Edge>();
            for (int j = 1; j <= 9; j++)
            {
                Edge edge = new Edge();
                edge.Type = "call";
                e.Add(j, edge);
            }
            e[1].Type = "import";
            e[2].Type = "set";
            e[3].Type = "use";

            AddToGraph(impl, e[1], i[3], i[15]);
            

            // Architecture and implementation graphs are created and will not
            // be touched by this test case. We will only map the implementation
            // nodes incrementally here.

            mapping = new Graph(); // reset the mapping to the empty mapping
            // because we reset the mapping, we need to reset the reflexion analysis, too
            reflexion = new Reflexion(impl, arch, mapping);
            reflexion.Register(this);
            ClearEvents();
            SetNodes();

            // Note: we do not 
        }

        private void AddToGraph(Graph graph, Edge edge, Node from, Node to)
        {
            edge.Source = from;
            edge.Target = to;
            graph.AddEdge(edge);
        }
    }
}