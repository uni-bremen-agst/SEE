using NUnit.Framework;
using SEE.Graphs;
using SEE.Graphs.Events;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SEE.Graphs.Core
{
    /// <summary>
    /// Unit tests for <see cref="Graph"/>. <see cref="Node"/>, and <see cref="Edge"/>.
    /// </summary>
    internal class TestGraph
    {
        /// <summary>
        /// Creates a new node with the given <paramref name="id"/> and <paramref name="type"/>
        /// and adds it to <paramref name="graph"/>.
        /// </summary>
        private static Node NewNode(Graph graph, string id, string type = "Routine")
        {
            Node node = new() { ID = id, Type = type, SourceName = id };
            graph.AddNode(node);
            return node;
        }

        /// <summary>
        /// Creates a new edge between <paramref name="from"/> and <paramref name="to"/>
        /// and adds it to <paramref name="graph"/>.
        /// </summary>
        private static Edge NewEdge(Graph graph, Node from, Node to, string type = "Call")
        {
            return graph.AddEdge(from, to, type);
        }

        #region Constructors

        [Test]
        public void TestDefaultConstructor()
        {
            Graph graph = new();
            Assert.That(graph.Name, Is.EqualTo(string.Empty));
            Assert.That(graph.BasePath, Is.EqualTo(string.Empty));
            Assert.That(graph.NodeCount, Is.EqualTo(0));
            Assert.That(graph.EdgeCount, Is.EqualTo(0));
        }

        [Test]
        public void TestConstructorWithNameAndBasePath()
        {
            Graph graph = new("basePath", "MyGraph");
            Assert.That(graph.Name, Is.EqualTo("MyGraph"));
            Assert.That(graph.BasePath, Is.EqualTo("basePath"));
        }

        [Test]
        public void TestCopyConstructor()
        {
            Graph original = new("basePath", "MyGraph");
            Node n1 = NewNode(original, "n1");
            Node n2 = NewNode(original, "n2");
            n1.AddChild(n2);
            NewEdge(original, n1, n2);

            Graph copy = new(original);

            Assert.That(copy.Name, Is.EqualTo(original.Name));
            Assert.That(copy.NodeCount, Is.EqualTo(original.NodeCount));
            Assert.That(copy.EdgeCount, Is.EqualTo(original.EdgeCount));
            Assert.That(copy.GetNode("n1"), Is.Not.Null);
            Assert.That(copy.GetNode("n2"), Is.Not.Null);
            // Must be a deep copy: nodes should not be the same reference.
            Assert.That(copy.GetNode("n1"), Is.Not.SameAs(original.GetNode("n1")));
        }

        #endregion

        #region AddNode / RemoveNode / ContainsNode

        [Test]
        public void TestAddNode()
        {
            Graph graph = new();
            Node node = new() { ID = "n1", Type = "Routine" };
            graph.AddNode(node);

            Assert.That(graph.NodeCount, Is.EqualTo(1));
            Assert.That(graph.ContainsNode(node), Is.True);
            Assert.That(node.ItsGraph, Is.SameAs(graph));
        }

        [Test]
        public void TestAddNodeNullThrows()
        {
            Graph graph = new();
            Assert.That(() => graph.AddNode(null), Throws.TypeOf<ArgumentNullException>());
        }

        [Test]
        public void TestAddNodeEmptyIDThrows()
        {
            Graph graph = new();
            Node node = new() { Type = "Routine" };
            Assert.That(() => graph.AddNode(node), Throws.TypeOf<ArgumentException>());
        }

        [Test]
        public void TestAddNodeDuplicateIDThrows()
        {
            Graph graph = new();
            NewNode(graph, "n1");
            Node duplicate = new() { ID = "n1", Type = "Routine" };
            Assert.That(() => graph.AddNode(duplicate), Throws.TypeOf<InvalidOperationException>());
        }

        [Test]
        public void TestAddNodeAlreadyInAnotherGraphThrows()
        {
            Graph graph1 = new();
            Graph graph2 = new();
            Node node = NewNode(graph1, "n1");
            Assert.That(() => graph2.AddNode(node), Throws.TypeOf<InvalidOperationException>());
        }

        [Test]
        public void TestRemoveNode()
        {
            Graph graph = new();
            Node node = NewNode(graph, "n1");
            graph.RemoveNode(node);

            Assert.That(graph.NodeCount, Is.EqualTo(0));
            Assert.That(node.ItsGraph, Is.Null);
        }

        [Test]
        public void TestRemoveNodeNullThrows()
        {
            Graph graph = new();
            Assert.That(() => graph.RemoveNode(null), Throws.Exception);
        }

        [Test]
        public void TestRemoveNodeNotInGraphThrows()
        {
            Graph graph1 = new();
            Graph graph2 = new();
            Node node = NewNode(graph1, "n1");
            Assert.That(() => graph2.RemoveNode(node), Throws.Exception);
        }

        [Test]
        public void TestRemoveNodeRemovesIncidentEdges()
        {
            Graph graph = new();
            Node n1 = NewNode(graph, "n1");
            Node n2 = NewNode(graph, "n2");
            Edge edge = NewEdge(graph, n1, n2);

            graph.RemoveNode(n1);

            Assert.That(graph.EdgeCount, Is.EqualTo(0));
            Assert.That(edge.ItsGraph, Is.Null);
        }

        [Test]
        public void TestRemoveNodeOrphansBecomeRoots()
        {
            Graph graph = new();
            Node parent = NewNode(graph, "parent");
            Node child = NewNode(graph, "child");
            parent.AddChild(child);

            graph.RemoveNode(parent, orphansBecomeRoots: true);

            Assert.That(child.IsRoot(), Is.True);
        }

        [Test]
        public void TestRemoveNodeOrphansBecomeChildrenOfGrandparent()
        {
            Graph graph = new();
            Node grandparent = NewNode(graph, "grandparent");
            Node parent = NewNode(graph, "parent");
            Node child = NewNode(graph, "child");
            grandparent.AddChild(parent);
            parent.AddChild(child);

            graph.RemoveNode(parent, orphansBecomeRoots: false);

            Assert.That(child.Parent, Is.SameAs(grandparent));
        }

        [Test]
        public void TestContainsNode()
        {
            Graph graph = new();
            Node node = NewNode(graph, "n1");
            Assert.That(graph.ContainsNode(node), Is.True);

            Node other = new() { ID = "n2", Type = "Routine" };
            Assert.That(graph.ContainsNode(other), Is.False);
        }

        [Test]
        public void TestContainsNodeNullThrows()
        {
            Graph graph = new();
            Assert.That(() => graph.ContainsNode(null), Throws.TypeOf<ArgumentNullException>());
        }

        [Test]
        public void TestContainsNodeID()
        {
            Graph graph = new();
            NewNode(graph, "n1");
            Assert.That(graph.ContainsNodeID("n1"), Is.True);
            Assert.That(graph.ContainsNodeID("n2"), Is.False);
        }

        #endregion

        #region AddEdge / RemoveEdge / ContainsEdge

        [Test]
        public void TestAddEdge()
        {
            Graph graph = new();
            Node n1 = NewNode(graph, "n1");
            Node n2 = NewNode(graph, "n2");
            Edge edge = NewEdge(graph, n1, n2);

            Assert.That(graph.EdgeCount, Is.EqualTo(1));
            Assert.That(graph.ContainsEdge(edge), Is.True);
            Assert.That(edge.ItsGraph, Is.SameAs(graph));
            Assert.That(n1.Outgoings.Contains(edge), Is.True);
            Assert.That(n2.Incomings.Contains(edge), Is.True);
        }

        [Test]
        public void TestAddEdgeNullThrows()
        {
            Graph graph = new();
            Assert.That(() => graph.AddEdge(null), Throws.TypeOf<ArgumentNullException>());
        }

        [Test]
        public void TestAddEdgeNullSourceOrTargetThrows()
        {
            Graph graph = new();
            Node n1 = NewNode(graph, "n1");
            Edge edge = new(n1, null, "Call");
            Assert.That(() => graph.AddEdge(edge), Throws.TypeOf<ArgumentException>());
        }

        [Test]
        public void TestAddEdgeSourceNotInGraphThrows()
        {
            Graph graph1 = new();
            Graph graph2 = new();
            Node n1 = NewNode(graph1, "n1");
            Node n2 = NewNode(graph2, "n2");
            Assert.That(() => graph2.AddEdge(n1, n2, "Call"), Throws.TypeOf<InvalidOperationException>());
        }

        [Test]
        public void TestAddEdgeAlreadyInGraphThrows()
        {
            Graph graph = new();
            Node n1 = NewNode(graph, "n1");
            Node n2 = NewNode(graph, "n2");
            Edge edge = NewEdge(graph, n1, n2);
            Assert.That(() => graph.AddEdge(edge), Throws.Exception);
        }

        [Test]
        public void TestRemoveEdge()
        {
            Graph graph = new();
            Node n1 = NewNode(graph, "n1");
            Node n2 = NewNode(graph, "n2");
            Edge edge = NewEdge(graph, n1, n2);

            graph.RemoveEdge(edge);

            Assert.That(graph.EdgeCount, Is.EqualTo(0));
            Assert.That(edge.ItsGraph, Is.Null);
            Assert.That(n1.Outgoings.Contains(edge), Is.False);
            Assert.That(n2.Incomings.Contains(edge), Is.False);
        }

        [Test]
        public void TestRemoveEdgeNullThrows()
        {
            Graph graph = new();
            Assert.That(() => graph.RemoveEdge(null), Throws.TypeOf<ArgumentNullException>());
        }

        [Test]
        public void TestRemoveEdgeNotInGraphThrows()
        {
            Graph graph1 = new();
            Graph graph2 = new();
            Node n1 = NewNode(graph1, "n1");
            Node n2 = NewNode(graph1, "n2");
            Edge edge = NewEdge(graph1, n1, n2);
            Assert.That(() => graph2.RemoveEdge(edge), Throws.TypeOf<ArgumentException>());
        }

        [Test]
        public void TestContainsEdge()
        {
            Graph graph = new();
            Node n1 = NewNode(graph, "n1");
            Node n2 = NewNode(graph, "n2");
            Edge edge = NewEdge(graph, n1, n2);

            Assert.That(graph.ContainsEdge(edge), Is.True);
        }

        [Test]
        public void TestContainsEdgeNullThrows()
        {
            Graph graph = new();
            Assert.That(() => graph.ContainsEdge(null), Throws.TypeOf<ArgumentNullException>());
        }

        [Test]
        public void TestContainsEdgeID()
        {
            Graph graph = new();
            Node n1 = NewNode(graph, "n1");
            Node n2 = NewNode(graph, "n2");
            Edge edge = NewEdge(graph, n1, n2);

            Assert.That(graph.ContainsEdgeID(edge.ID), Is.True);
            Assert.That(graph.ContainsEdgeID("nonexistent"), Is.False);
        }

        #endregion

        #region RemoveElement

        [Test]
        public void TestRemoveElementNode()
        {
            Graph graph = new();
            Node node = NewNode(graph, "n1");
            graph.RemoveElement(node);
            Assert.That(graph.NodeCount, Is.EqualTo(0));
        }

        [Test]
        public void TestRemoveElementEdge()
        {
            Graph graph = new();
            Node n1 = NewNode(graph, "n1");
            Node n2 = NewNode(graph, "n2");
            Edge edge = NewEdge(graph, n1, n2);
            graph.RemoveElement(edge);
            Assert.That(graph.EdgeCount, Is.EqualTo(0));
        }

        #endregion

        #region GetNode / TryGetNode

        [Test]
        public void TestGetNode()
        {
            Graph graph = new();
            Node node = NewNode(graph, "n1");
            Assert.That(graph.GetNode("n1"), Is.SameAs(node));
        }

        [Test]
        public void TestGetNodeNotFound()
        {
            Graph graph = new();
            Assert.That(graph.GetNode("nonexistent"), Is.Null);
        }

        [Test]
        public void TestGetNodeEmptyIdThrows()
        {
            Graph graph = new();
            Assert.That(() => graph.GetNode(""), Throws.TypeOf<ArgumentException>());
            Assert.That(() => graph.GetNode(null), Throws.TypeOf<ArgumentException>());
        }

        [Test]
        public void TestTryGetNode()
        {
            Graph graph = new();
            Node node = NewNode(graph, "n1");

            Assert.That(graph.TryGetNode("n1", out Node found), Is.True);
            Assert.That(found, Is.SameAs(node));

            Assert.That(graph.TryGetNode("nonexistent", out Node notFound), Is.False);
            Assert.That(notFound, Is.Null);
        }

        [Test]
        public void TestTryGetNodeEmptyIdThrows()
        {
            Graph graph = new();
            Assert.That(() => graph.TryGetNode("", out _), Throws.TypeOf<ArgumentException>());
        }

        #endregion

        #region GetEdge / TryGetEdge

        [Test]
        public void TestGetEdge()
        {
            Graph graph = new();
            Node n1 = NewNode(graph, "n1");
            Node n2 = NewNode(graph, "n2");
            Edge edge = NewEdge(graph, n1, n2);

            Assert.That(graph.GetEdge(edge.ID), Is.SameAs(edge));
        }

        [Test]
        public void TestGetEdgeNotFound()
        {
            Graph graph = new();
            Assert.That(graph.GetEdge("nonexistent"), Is.Null);
        }

        [Test]
        public void TestGetEdgeEmptyIdThrows()
        {
            Graph graph = new();
            Assert.That(() => graph.GetEdge(""), Throws.TypeOf<ArgumentException>());
        }

        [Test]
        public void TestTryGetEdge()
        {
            Graph graph = new();
            Node n1 = NewNode(graph, "n1");
            Node n2 = NewNode(graph, "n2");
            Edge edge = NewEdge(graph, n1, n2);

            Assert.That(graph.TryGetEdge(edge.ID, out Edge found), Is.True);
            Assert.That(found, Is.SameAs(edge));

            Assert.That(graph.TryGetEdge("nonexistent", out Edge notFound), Is.False);
            Assert.That(notFound, Is.Null);
        }

        [Test]
        public void TestTryGetEdgeEmptyIdThrows()
        {
            Graph graph = new();
            Assert.That(() => graph.TryGetEdge("", out _), Throws.TypeOf<ArgumentException>());
        }

        #endregion

        #region AddSingleRoot

        [Test]
        public void TestAddSingleRootNoNodes()
        {
            Graph graph = new();
            bool created = graph.AddSingleRoot(out Node root);
            Assert.That(created, Is.False);
            Assert.That(root, Is.Null);
        }

        [Test]
        public void TestAddSingleRootOneExistingRoot()
        {
            Graph graph = new();
            Node existingRoot = NewNode(graph, "n1");

            bool created = graph.AddSingleRoot(out Node root);

            Assert.That(created, Is.False);
            Assert.That(root, Is.SameAs(existingRoot));
        }

        [Test]
        public void TestAddSingleRootMultipleRoots()
        {
            Graph graph = new("", "MyGraph");
            Node n1 = NewNode(graph, "n1");
            Node n2 = NewNode(graph, "n2");

            bool created = graph.AddSingleRoot(out Node root);

            Assert.That(created, Is.True);
            Assert.That(root, Is.Not.Null);
            Assert.That(root.ID, Is.EqualTo("MyGraph#ROOT"));
            Assert.That(root.Type, Is.EqualTo(Graph.RootType));
            Assert.That(root.HasToggle(Graph.RootToggle), Is.True);
            Assert.That(n1.Parent, Is.SameAs(root));
            Assert.That(n2.Parent, Is.SameAs(root));
        }

        [Test]
        public void TestAddSingleRootInitialGraphForcesCreation()
        {
            Graph graph = new();
            Node existingRoot = NewNode(graph, "n1");

            bool created = graph.AddSingleRoot(out Node root, initialGraph: true);

            Assert.That(created, Is.True);
            Assert.That(existingRoot.Parent, Is.SameAs(root));
        }

        [Test]
        public void TestAddSingleRootCustomNameAndType()
        {
            Graph graph = new();
            NewNode(graph, "n1");
            NewNode(graph, "n2");

            graph.AddSingleRoot(out Node root, name: "CustomRoot", type: "CustomType");

            Assert.That(root.ID, Is.EqualTo("CustomRoot"));
            Assert.That(root.Type, Is.EqualTo("CustomType"));
        }

        #endregion

        #region AllNodeTypes / AllEdgeTypes / AllElementTypes

        [Test]
        public void TestAllNodeTypes()
        {
            Graph graph = new();
            NewNode(graph, "n1", "TypeA");
            NewNode(graph, "n2", "TypeB");
            NewNode(graph, "n3", "TypeA");

            HashSet<string> types = graph.AllNodeTypes();

            Assert.That(types.Count, Is.EqualTo(2));
            Assert.That(types.ToList(), Does.Contain("TypeA"));
            Assert.That(types.ToList(), Does.Contain("TypeB"));
        }

        [Test]
        public void TestAllEdgeTypes()
        {
            Graph graph = new();
            Node n1 = NewNode(graph, "n1");
            Node n2 = NewNode(graph, "n2");
            NewEdge(graph, n1, n2, "Call");
            NewEdge(graph, n2, n1, "Use");

            HashSet<string> types = graph.AllEdgeTypes();

            Assert.That(types.Count, Is.EqualTo(2));
            Assert.That(types.ToList(), Does.Contain("Call"));
            Assert.That(types.ToList(), Does.Contain("Use"));
        }

        [Test]
        public void TestAllElementTypes()
        {
            Graph graph = new();
            Node n1 = NewNode(graph, "n1", "TypeA");
            Node n2 = NewNode(graph, "n2", "TypeA");
            NewEdge(graph, n1, n2, "Call");

            HashSet<string> types = graph.AllElementTypes();

            Assert.That(types.Count, Is.EqualTo(2));
            Assert.That(types.ToList(), Does.Contain("TypeA"));
            Assert.That(types.ToList(), Does.Contain("Call"));
        }

        #endregion

        #region Nodes / Edges / Elements

        [Test]
        public void TestNodes()
        {
            Graph graph = new();
            Node n1 = NewNode(graph, "n1");
            Node n2 = NewNode(graph, "n2");

            IList<Node> nodes = graph.Nodes();

            Assert.That(nodes.Count, Is.EqualTo(2));
            Assert.That(nodes, Does.Contain(n1));
            Assert.That(nodes, Does.Contain(n2));
        }

        [Test]
        public void TestEdges()
        {
            Graph graph = new();
            Node n1 = NewNode(graph, "n1");
            Node n2 = NewNode(graph, "n2");
            Edge edge = NewEdge(graph, n1, n2);

            IList<Edge> edges = graph.Edges();

            Assert.That(edges.Count, Is.EqualTo(1));
            Assert.That(edges, Does.Contain(edge));
        }

        [Test]
        public void TestElements()
        {
            Graph graph = new();
            Node n1 = NewNode(graph, "n1");
            Node n2 = NewNode(graph, "n2");
            Edge edge = NewEdge(graph, n1, n2);

            List<GraphElement> elements = graph.Elements().ToList();

            Assert.That(elements.Count, Is.EqualTo(3));
            Assert.That(elements, Does.Contain(n1));
            Assert.That(elements, Does.Contain(n2));
            Assert.That(elements, Does.Contain(edge));
        }

        #endregion

        #region GetRoots / MaxDepth / DumpTree

        [Test]
        public void TestGetRootsEmptyGraph()
        {
            Graph graph = new();
            Assert.That(graph.GetRoots().Count, Is.EqualTo(0));
        }

        [Test]
        public void TestGetRootsSingleRoot()
        {
            Graph graph = new();
            Node root = NewNode(graph, "root");
            Node child = NewNode(graph, "child");
            root.AddChild(child);

            List<Node> roots = graph.GetRoots();

            Assert.That(roots.Count, Is.EqualTo(1));
            Assert.That(roots[0], Is.SameAs(root));
        }

        [Test]
        public void TestGetRootsMultipleRoots()
        {
            Graph graph = new();
            Node r1 = NewNode(graph, "r1");
            Node r2 = NewNode(graph, "r2");

            List<Node> roots = graph.GetRoots();

            Assert.That(roots.Count, Is.EqualTo(2));
        }

        [Test]
        public void TestMaxDepthEmptyGraph()
        {
            Graph graph = new();
            Assert.That(graph.MaxDepth, Is.EqualTo(0));
        }

        [Test]
        public void TestMaxDepthSingleLevel()
        {
            Graph graph = new();
            NewNode(graph, "n1");
            Assert.That(graph.MaxDepth, Is.EqualTo(1));
        }

        [Test]
        public void TestMaxDepthMultipleLevels()
        {
            Graph graph = new();
            Node root = NewNode(graph, "root");
            Node child = NewNode(graph, "child");
            Node grandchild = NewNode(graph, "grandchild");
            root.AddChild(child);
            child.AddChild(grandchild);

            Assert.That(graph.MaxDepth, Is.EqualTo(3));
        }

        [Test]
        public void TestDumpTreeDoesNotThrow()
        {
            Graph graph = new();
            Node root = NewNode(graph, "root");
            Node child = NewNode(graph, "child");
            root.AddChild(child);

            Assert.That(() => graph.DumpTree(), Throws.Nothing);
        }

        #endregion

        #region Destroy

        [Test]
        public void TestDestroy()
        {
            Graph graph = new();
            Node n1 = NewNode(graph, "n1");
            Node n2 = NewNode(graph, "n2");
            NewEdge(graph, n1, n2);

            graph.Destroy();

            Assert.That(graph.NodeCount, Is.EqualTo(0));
            Assert.That(graph.EdgeCount, Is.EqualTo(0));
        }

        #endregion

        #region SortHierarchy / SortHierarchyByName

        [Test]
        public void TestSortHierarchyByName()
        {
            Graph graph = new();
            Node root = NewNode(graph, "root");
            Node childB = NewNode(graph, "b");
            Node childA = NewNode(graph, "a");
            root.AddChild(childB);
            root.AddChild(childA);

            graph.SortHierarchyByName();

            IList<Node> children = root.Children();
            Assert.That(children[0], Is.SameAs(childA));
            Assert.That(children[1], Is.SameAs(childB));
        }

        [Test]
        public void TestSortHierarchyCustomComparison()
        {
            Graph graph = new();
            Node root = NewNode(graph, "root");
            Node childA = NewNode(graph, "a");
            Node childB = NewNode(graph, "b");
            root.AddChild(childA);
            root.AddChild(childB);

            // Reverse order comparison.
            graph.SortHierarchy((x, y) => string.Compare(y.ID, x.ID, StringComparison.Ordinal));

            IList<Node> children = root.Children();
            Assert.That(children[0], Is.SameAs(childB));
            Assert.That(children[1], Is.SameAs(childA));
        }

        #endregion

        #region MergeWith

        [Test]
        public void TestMergeWithNullThrows()
        {
            Graph graph = new();
            Assert.That(() => graph.MergeWith<Graph>(null), Throws.TypeOf<ArgumentNullException>());
        }

        [Test]
        public void TestMergeWithDisjointGraphs()
        {
            Graph graph1 = new("", "Graph1");
            Node n1 = NewNode(graph1, "n1");

            Graph graph2 = new("", "Graph2");
            Node n2 = NewNode(graph2, "n2");

            Graph merged = graph1.MergeWith<Graph>(graph2);

            Assert.That(merged.NodeCount, Is.EqualTo(2));
            Assert.That(merged.GetNode("n1"), Is.Not.Null);
            Assert.That(merged.GetNode("n2"), Is.Not.Null);
        }

        [Test]
        public void TestMergeWithSuffixesAvoidCollisions()
        {
            Graph graph1 = new("", "Graph1");
            NewNode(graph1, "n1");

            Graph graph2 = new("", "Graph2");
            NewNode(graph2, "n1");

            Graph merged = graph1.MergeWith<Graph>(graph2, nodeIdSuffix: "_other");

            Assert.That(merged.NodeCount, Is.EqualTo(2));
            Assert.That(merged.GetNode("n1"), Is.Not.Null);
            Assert.That(merged.GetNode("n1_other"), Is.Not.Null);
        }

        [Test]
        public void TestMergeWithPreservesHierarchyAndEdges()
        {
            Graph graph1 = new("", "Graph1");
            Node root1 = NewNode(graph1, "root1");
            Node child1 = NewNode(graph1, "child1");
            root1.AddChild(child1);
            NewEdge(graph1, root1, child1, "Call");

            Graph graph2 = new("", "Graph2");
            Node root2 = NewNode(graph2, "root2");

            Graph merged = graph1.MergeWith<Graph>(graph2);

            Assert.That(merged.NodeCount, Is.EqualTo(3));
            Assert.That(merged.EdgeCount, Is.EqualTo(1));
            Node mergedChild1 = merged.GetNode("child1");
            Assert.That(mergedChild1.Parent.ID, Is.EqualTo("root1"));
        }

        [Test]
        public void TestMergeWithDoesNotModifyOriginalGraphs()
        {
            Graph graph1 = new("", "Graph1");
            NewNode(graph1, "n1");
            int originalCount1 = graph1.NodeCount;

            Graph graph2 = new("", "Graph2");
            NewNode(graph2, "n2");
            int originalCount2 = graph2.NodeCount;

            graph1.MergeWith<Graph>(graph2);

            Assert.That(graph1.NodeCount, Is.EqualTo(originalCount1));
            Assert.That(graph2.NodeCount, Is.EqualTo(originalCount2));
        }

        #endregion

        #region ConnectingEdges

        [Test]
        public void TestConnectingEdges()
        {
            Graph graph = new();
            Node n1 = NewNode(graph, "n1");
            Node n2 = NewNode(graph, "n2");
            Node n3 = NewNode(graph, "n3");
            Edge edge12 = NewEdge(graph, n1, n2);
            NewEdge(graph, n1, n3); // not within selection

            IList<Edge> connecting = graph.ConnectingEdges(new[] { n1, n2 });

            Assert.That(connecting.Count, Is.EqualTo(1));
            Assert.That(connecting[0], Is.SameAs(edge12));
        }

        [Test]
        public void TestConnectingEdgesNoMatches()
        {
            Graph graph = new();
            Node n1 = NewNode(graph, "n1");
            Node n2 = NewNode(graph, "n2");
            NewEdge(graph, n1, n2);

            IList<Edge> connecting = graph.ConnectingEdges(new[] { n1 });

            Assert.That(connecting.Count, Is.EqualTo(0));
        }

        #endregion

        #region ToString

        [Test]
        public void TestToStringContainsNameAndPath()
        {
            Graph graph = new("", "MyGraph") { Path = "/some/path" };
            NewNode(graph, "n1");

            string result = graph.ToString();

            Assert.That(result, Does.Contain("MyGraph"));
            Assert.That(result, Does.Contain("/some/path"));
            Assert.That(result, Does.Contain("n1"));
        }

        #endregion

        #region FinalizeNodeHierarchy / NodeHierarchyHasChanged

        [Test]
        public void TestFinalizeNodeHierarchySetsLevelsAndRoots()
        {
            Graph graph = new();
            Node root = NewNode(graph, "root");
            Node child = NewNode(graph, "child");
            root.AddChild(child);

            graph.FinalizeNodeHierarchy();

            Assert.That(root.Level, Is.EqualTo(0));
            Assert.That(child.Level, Is.EqualTo(1));
            Assert.That(graph.NodeHierarchyHasChanged, Is.False);
        }

        [Test]
        public void TestNodeHierarchyHasChangedAfterAddChild()
        {
            Graph graph = new();
            Node root = NewNode(graph, "root");
            Node child = NewNode(graph, "child");
            graph.FinalizeNodeHierarchy();
            Assert.That(graph.NodeHierarchyHasChanged, Is.False);

            root.AddChild(child);

            Assert.That(graph.NodeHierarchyHasChanged, Is.True);
        }

        #endregion

        #region Clone

        [Test]
        public void TestCloneCreatesDeepCopy()
        {
            Graph graph = new("basePath", "MyGraph");
            Node n1 = NewNode(graph, "n1");
            Node n2 = NewNode(graph, "n2");
            n1.AddChild(n2);
            Edge edge = NewEdge(graph, n1, n2);

            Graph clone = graph.Clone();

            Assert.That(clone.Name, Is.EqualTo(graph.Name));
            Assert.That(clone.NodeCount, Is.EqualTo(graph.NodeCount));
            Assert.That(clone.EdgeCount, Is.EqualTo(graph.EdgeCount));
            Assert.That(clone.GetNode("n1"), Is.Not.SameAs(graph.GetNode("n1")));
            Assert.That(clone.GetEdge(edge.ID), Is.Not.SameAs(graph.GetEdge(edge.ID)));
            // Hierarchy must be preserved.
            Assert.That(clone.GetNode("n2").Parent, Is.SameAs(clone.GetNode("n1")));
        }

        [Test]
        public void TestCloneEmptyGraph()
        {
            Graph graph = new("basePath", "MyGraph");
            Graph clone = graph.Clone();

            Assert.That(clone.NodeCount, Is.EqualTo(0));
            Assert.That(clone.EdgeCount, Is.EqualTo(0));
        }

        #endregion

        #region CopyEdgesTo

        [Test]
        public void TestCopyEdgesToThrowsWhenNodeMissing()
        {
            Graph source = new();
            Node n1 = NewNode(source, "n1");
            Node n2 = NewNode(source, "n2");
            NewEdge(source, n1, n2);

            Graph target = new();
            // Target does not have corresponding nodes.
            Assert.That(() => source.CopyEdgesTo(target), Throws.TypeOf<InvalidOperationException>());
        }

        [Test]
        public void TestCopyEdgesToSucceedsWithMatchingNodes()
        {
            Graph source = new();
            Node n1 = NewNode(source, "n1");
            Node n2 = NewNode(source, "n2");
            NewEdge(source, n1, n2, "Call");

            Graph target = new();
            NewNode(target, "n1");
            NewNode(target, "n2");

            source.CopyEdgesTo(target);

            Assert.That(target.EdgeCount, Is.EqualTo(1));
        }

        #endregion

        #region SubgraphByNodeType

        [Test]
        public void TestSubgraphByNodeType()
        {
            Graph graph = new();
            Node a = NewNode(graph, "a", "TypeA");
            Node b = NewNode(graph, "b", "TypeB");
            a.AddChild(b);
            NewEdge(graph, a, b, "Call");

            Graph subgraph = graph.SubgraphByNodeType(new[] { "TypeA" });

            Assert.That(subgraph.NodeCount, Is.EqualTo(1));
            Assert.That(subgraph.GetNode("a"), Is.Not.Null);
            Assert.That(subgraph.GetNode("b"), Is.Null);
        }

        [Test]
        public void TestSubgraphByNodeTypeLiftsEdges()
        {
            Graph graph = new();
            Node a = NewNode(graph, "a", "TypeA");
            Node b = NewNode(graph, "b", "TypeB");
            Node c = NewNode(graph, "c", "TypeA");
            a.AddChild(b);
            b.AddChild(c);
            NewEdge(graph, b, c, "Call"); // Will be lifted onto a -> c

            Graph subgraph = graph.SubgraphByNodeType(new[] { "TypeA" });

            Assert.That(subgraph.NodeCount, Is.EqualTo(2));
            Assert.That(subgraph.EdgeCount, Is.EqualTo(1));
            Edge liftedEdge = subgraph.Edges()[0];
            Assert.That(liftedEdge.Source.ID, Is.EqualTo("a"));
            Assert.That(liftedEdge.Target.ID, Is.EqualTo("c"));
            Assert.That(liftedEdge.HasToggle(Edge.IsLiftedToggle), Is.True);
        }

        #endregion

        #region SubgraphByToggleAttributes

        [Test]
        public void TestSubgraphByToggleAttributes()
        {
            Graph graph = new();
            Node a = NewNode(graph, "a");
            a.SetToggle("Keep");
            Node b = NewNode(graph, "b");

            Graph subgraph = graph.SubgraphByToggleAttributes(new[] { "Keep" });

            Assert.That(subgraph.NodeCount, Is.EqualTo(1));
            Assert.That(subgraph.GetNode("a"), Is.Not.Null);
        }

        #endregion

        #region SubgraphByEdges

        [Test]
        public void TestSubgraphByEdges()
        {
            Graph graph = new();
            Node a = NewNode(graph, "a");
            Node b = NewNode(graph, "b");
            Node c = NewNode(graph, "c");
            Edge keptEdge = NewEdge(graph, a, b, "Call");
            NewEdge(graph, b, c, "Use");

            Graph subgraph = graph.SubgraphByEdges(e => e.Type == "Call");

            Assert.That(subgraph.EdgeCount, Is.EqualTo(1));
            Assert.That(subgraph.NodeCount, Is.EqualTo(2));
            Assert.That(subgraph.GetNode("a"), Is.Not.Null);
            Assert.That(subgraph.GetNode("b"), Is.Not.Null);
            Assert.That(subgraph.GetNode("c"), Is.Null);
        }

        #endregion

        #region SubgraphBy

        [Test]
        public void TestSubgraphByIncludeAllReturnsEquivalentGraph()
        {
            Graph graph = new();
            Node a = NewNode(graph, "a");
            Node b = NewNode(graph, "b");
            a.AddChild(b);
            NewEdge(graph, a, b, "Call");

            Graph subgraph = graph.SubgraphBy(_ => true);

            Assert.That(subgraph.NodeCount, Is.EqualTo(graph.NodeCount));
            Assert.That(subgraph.EdgeCount, Is.EqualTo(graph.EdgeCount));
        }

        [Test]
        public void TestSubgraphByIncludeNoneReturnsEmptyGraph()
        {
            Graph graph = new();
            Node a = NewNode(graph, "a");
            Node b = NewNode(graph, "b");
            NewEdge(graph, a, b, "Call");

            Graph subgraph = graph.SubgraphBy(_ => false);

            Assert.That(subgraph.NodeCount, Is.EqualTo(0));
            Assert.That(subgraph.EdgeCount, Is.EqualTo(0));
        }

        [Test]
        public void TestSubgraphByIgnoreSelfLoops()
        {
            Graph graph = new();
            Node a = NewNode(graph, "a", "TypeA");
            Node b = NewNode(graph, "b", "TypeB");
            a.AddChild(b);
            NewEdge(graph, b, b, "SelfCall"); // self loop on b which is excluded

            Graph subgraph = graph.SubgraphBy(element =>
                element is Node node ? node.Type == "TypeA" : true, ignoreSelfLoops: true);

            // b is excluded, its self-loop would be lifted to a -> a, which should be ignored.
            Assert.That(subgraph.EdgeCount, Is.EqualTo(0));
        }

        #endregion

        #region Traverse

        [Test]
        public void TestTraverseLeafAction()
        {
            Graph graph = new();
            Node root = NewNode(graph, "root");
            Node leaf1 = NewNode(graph, "leaf1");
            Node leaf2 = NewNode(graph, "leaf2");
            root.AddChild(leaf1);
            root.AddChild(leaf2);

            List<string> visitedLeaves = new();
            graph.Traverse(leaf => visitedLeaves.Add(leaf.ID));

            Assert.That(visitedLeaves.Count, Is.EqualTo(2));
            Assert.That(visitedLeaves, Is.EquivalentTo(new[] { "leaf1", "leaf2" }));
        }

        [Test]
        public void TestTraverseInnerAndLeafActions()
        {
            Graph graph = new();
            Node root = NewNode(graph, "root");
            Node inner = NewNode(graph, "inner");
            Node leaf = NewNode(graph, "leaf");
            root.AddChild(inner);
            inner.AddChild(leaf);

            List<string> innerNodes = new();
            List<string> leafNodes = new();
            graph.Traverse(innerNode => innerNodes.Add(innerNode.ID), leafNode => leafNodes.Add(leafNode.ID));

            Assert.That(innerNodes, Does.Contain("inner"));
            Assert.That(leafNodes, Does.Contain("leaf"));
        }

        [Test]
        public void TestTraverseWithRootAction()
        {
            Graph graph = new();
            Node root = NewNode(graph, "root");
            Node leaf = NewNode(graph, "leaf");
            root.AddChild(leaf);

            List<string> rootNodes = new();
            List<string> leafNodes = new();
            graph.Traverse(rootNode => rootNodes.Add(rootNode.ID), null, leafNode => leafNodes.Add(leafNode.ID));

            Assert.That(rootNodes, Is.EqualTo(new[] { "root" }));
            Assert.That(leafNodes, Is.EqualTo(new[] { "leaf" }));
        }

        [Test]
        public void TestTraverseRootThatIsLeafCallsRootActionNotLeafAction()
        {
            // A root node without children is reported via rootAction only,
            // since the leaf/inner distinction applies only to its children.
            Graph graph = new();
            Node root = NewNode(graph, "root");

            List<string> rootNodes = new();
            List<string> leafNodes = new();
            graph.Traverse(rootNode => rootNodes.Add(rootNode.ID), null, leafNode => leafNodes.Add(leafNode.ID));

            Assert.That(rootNodes, Is.EqualTo(new[] { "root" }));
            Assert.That(leafNodes.Count, Is.EqualTo(0));
        }

        #endregion

        #region Equals / GetHashCode

        [Test]
        public void TestEqualsSameNameAndPath()
        {
            Graph graph1 = new("", "MyGraph") { Path = "/path" };
            Graph graph2 = new("", "MyGraph") { Path = "/path" };

            Assert.That(graph1.Equals(graph2), Is.True);
        }

        [Test]
        public void TestEqualsDifferentName()
        {
            Graph graph1 = new("", "Graph1");
            Graph graph2 = new("", "Graph2");

            Assert.That(graph1.Equals(graph2), Is.False);
        }

        [Test]
        public void TestEqualsNull()
        {
            Graph graph = new();
            Assert.That(graph.Equals(null), Is.False);
        }

        [Test]
        public void TestEqualsDifferentType()
        {
            Graph graph = new();
            Assert.That(graph.Equals("not a graph"), Is.False);
        }

        [Test]
        public void TestGetHashCodeConsistentWithEquals()
        {
            Graph graph1 = new("", "MyGraph") { Path = "/path" };
            Graph graph2 = new("", "MyGraph") { Path = "/path" };

            Assert.That(graph1.GetHashCode(), Is.EqualTo(graph2.GetHashCode()));
        }

        #endregion

        #region implicit bool operator

        /// <summary>
        /// Tests the implicit bool operator <see cref="Graph.implicit operator bool(Graph)"/>
        /// for a non-null <see cref="Graph"/> instance.
        /// </summary>
        [Test]
        public void TestImplicitBoolOperatorNonNullGraph()
        {
            Graph graph = new();
            // Force the implicit conversion to bool to test the operator.
            bool evaluatesToTrue = graph;
            Assert.That(evaluatesToTrue, Is.True);
        }

        /// <summary>
        /// Tests the implicit bool operator <see cref="Graph.implicit operator bool(Graph)"/>
        /// for a non-null <see cref="Graph"/> instance.
        /// </summary>
        [Test]
        public void TestImplicitBoolOperatorNullGraph()
        {
            Graph graph = null;
            // Force the implicit conversion to bool to test the operator.
            bool evaluatesToFalse = graph;
            Assert.That(evaluatesToFalse, Is.False);
        }

        #endregion

        #region NotifyRootNodeDeletion


        private class MyObserver : IObserver<ChangeEvent>
        {
            public bool Notified = false;

            public void OnCompleted() { }
            public void OnError(Exception error) { }
            public void OnNext(ChangeEvent e)
            {
                if (e is NodeEvent nodeEvent && nodeEvent.Change == ChangeType.Removal)
                {
                    Notified = true;
                }
            }
        }

        [Test]
        public void TestNotifyRootNodeDeletionForActualRoot()
        {
            Graph graph = new();
            Node root = NewNode(graph, "root");

            MyObserver observer = new();

            graph.Subscribe(observer);

            graph.NotifyRootNodeDeletion(root);

            Assert.That(observer.Notified, Is.True);
        }

        [Test]
        public void TestNotifyRootNodeDeletionForNonRootDoesNothing()
        {
            Graph graph = new();
            Node root = NewNode(graph, "root");
            Node child = NewNode(graph, "child");
            root.AddChild(child);

            MyObserver observer = new();
            graph.Subscribe(observer);

            graph.NotifyRootNodeDeletion(child);

            Assert.That(observer.Notified, Is.False);
        }

        #endregion
    }
}