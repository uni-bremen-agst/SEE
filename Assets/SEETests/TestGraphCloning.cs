using NUnit.Framework;

namespace SEE.DataModel.DG
{
    /// <summary>
    /// Test of method Clone() of all Attributables.
    /// </summary>
    internal class TestGraphCloning
    {
        private Node NewNode(string linkname)
        {
            Node node = new Node();
            node.Type = "Routine";
            node.ID = linkname;
            node.SourceName = "Source_" + linkname;
            node.SetFloat("float", 1.0f);
            node.SetInt("int", 2);
            node.SetString("string", "hello");
            node.SetToggle("toggle");
            return node;
        }

        [Test]
        public void TestCloneNode()
        {
            Node original = NewNode("node1");
            Graph graph = new Graph("DUMMYBASEPATH");
            graph.AddNode(original);

            Node clone = (Node)original.Clone();
            Assert.That(clone.Type, Is.EqualTo(original.Type));
            Assert.That(clone.ID, Is.EqualTo(original.ID));
            Assert.That(clone.SourceName, Is.EqualTo(original.SourceName));
            Assert.That(clone.GetFloat("float"), Is.EqualTo(original.GetFloat("float")));
            Assert.That(clone.GetInt("int"), Is.EqualTo(original.GetInt("int")));
            Assert.That(clone.GetString("string"), Is.EqualTo(original.GetString("string")));
            Assert.That(clone.HasToggle("toggle"), Is.True, "The toggle must be cloned.");
            // Note: Hierarchy information (parent, children, level) is cloned only when a
            // graph is cloned.
            Assert.That(clone.Level, Is.EqualTo(0));
            Assert.That(clone.Parent, Is.Null);
            Assert.That(clone.Children(), Is.Empty);
            // cloned nodes do not yet belong to any graph
            Assert.That(clone.ItsGraph, Is.Null);
        }

        /// <summary>
        /// Unique ID for edges.
        /// </summary>
        private int edgeID = 1;

        private Edge NewEdge(Node source, Node target, string edgeType = "Call")
        {
            Edge edge = new Edge(source, target, edgeType);
            edgeID++;
            edge.SetFloat("float", 1.0f);
            edge.SetInt("int", 2);
            edge.SetString("string", "hello");
            edge.SetToggle("toggle");
            return edge;
        }

        [Test]
        public void TestCloneEdge()
        {
            Graph graph = new Graph("DUMMYBASEPATH");
            Node source = NewNode("source");
            graph.AddNode(source);
            Node target = NewNode("target");
            graph.AddNode(target);
            Edge original = NewEdge(source, target);
            graph.AddEdge(original);

            Edge clone = (Edge)original.Clone();
            Assert.That(clone.Type, Is.EqualTo(original.Type));
            Assert.That(clone.GetFloat("float"), Is.EqualTo(original.GetFloat("float")));
            Assert.That(clone.GetInt("int"), Is.EqualTo(original.GetInt("int")));
            Assert.That(clone.GetString("string"), Is.EqualTo(original.GetString("string")));
            Assert.That(clone.HasToggle("toggle"), Is.True, "The toggle must be cloned.");
            // Note: Source and target of an edge should be cloned (shallow copy), too.
            Assert.That(clone.Source, Is.SameAs(original.Source));
            Assert.That(clone.Target, Is.SameAs(original.Target));
            // cloned edges do not yet belong to any graph
            Assert.That(clone.ItsGraph, Is.Null);
        }

        [Test]
        public void TestCloneGraph()
        {
            Graph original = new Graph("DUMMYBASEPATH");
            original.Path = "path";
            original.Name = "name";

            // Root nodes
            Node n1 = NewNode("n1");
            Node n2 = NewNode("n2");
            Node n3 = NewNode("n3");
            original.AddNode(n1);
            original.AddNode(n2);
            original.AddNode(n3);

            Edge e1 = NewEdge(n1, n2);
            Edge e2 = NewEdge(n2, n3, "DynamicCall");
            Edge e3 = NewEdge(n2, n3, "StaticCall");
            original.AddEdge(e1);
            original.AddEdge(e2);
            original.AddEdge(e3);

            // Second level
            Node n1_c1 = NewNode("n1_c1");
            Node n1_c2 = NewNode("n1_c2");
            original.AddNode(n1_c1);
            original.AddNode(n1_c2);
            n1.AddChild(n1_c1);
            n1.AddChild(n1_c2);

            Node n2_c1 = NewNode("n2_c1");
            original.AddNode(n2_c1);
            n2.AddChild(n2_c1);

            // Third level
            Node n1_c1_c1 = NewNode("n1_c1_c1");
            Node n1_c1_c2 = NewNode("n1_c1_c2");
            original.AddNode(n1_c1_c1);
            original.AddNode(n1_c1_c2);
            n1_c1.AddChild(n1_c1_c1);
            n1_c1.AddChild(n1_c1_c2);

            Graph clone = (Graph)original.Clone();
            Assert.That(clone.Path, Is.EqualTo(original.Path));
            Assert.That(clone.Name, Is.EqualTo(original.Name));
            Assert.That(clone.NodeCount, Is.EqualTo(original.NodeCount));
            Assert.That(clone.EdgeCount, Is.EqualTo(original.EdgeCount));

            // All cloned nodes must be in the cloned graph.
            // Note: The graph is compared by identity (Is.SameAs), not by equality
            // (Is.EqualTo). Graph.Equals considers only Name and Path, both of which a
            // clone shares with its original, hence Is.EqualTo would hold even for an
            // element that still belonged to the original graph. That is precisely the
            // defect these assertions are meant to detect, so identity is required here.
            foreach (Node node in clone.Nodes())
            {
                Assert.That(node.ItsGraph, Is.SameAs(clone));
            }
            // All cloned edges must be in the cloned graph (and their
            // source and target, too).
            foreach (Edge edge in clone.Edges())
            {
                Assert.That(edge.ItsGraph, Is.SameAs(clone));
                Assert.That(edge.Source.ItsGraph, Is.SameAs(clone));
                Assert.That(edge.Target.ItsGraph, Is.SameAs(clone));
            }
            CompareHierarchy(original, clone);
        }

        private void CompareHierarchy(Graph original, Graph clone)
        {
            foreach (Node root in original.GetRoots())
            {
                if (clone.TryGetNode(root.ID, out Node clonedRoot))
                {
                    CompareHierarchy(root, clone, clonedRoot);
                }
                else
                {
                    Assert.Fail($"Root {root.ID} of the original graph is missing in the clone.");
                }
            }
        }

        private void CompareHierarchy(Node node, Graph clone, Node clonedNode)
        {
            Assert.That(clonedNode.ID, Is.EqualTo(node.ID), "Linknames differ.");
            Assert.That(clonedNode.NumberOfChildren(), Is.EqualTo(node.NumberOfChildren()),
                        $"Number of children differs for node {node.ID}.");
            Assert.That(clonedNode.Level, Is.EqualTo(node.Level),
                        $"Levels differ between corresponding nodes with linkname {node.ID}.");

            if (node.IsRoot())
            {
                Assert.That(clonedNode.IsRoot(), Is.True,
                            $"{clonedNode} should be a root, because its corresponding node {node} is one.");
            }
            else
            {
                Assert.That(clonedNode.IsRoot(), Is.False,
                            $"{clonedNode} should not be a root. Corresponding node in original graph: {node}");
                Assert.That(clonedNode.Parent.ID, Is.EqualTo(node.Parent.ID));
            }

            foreach (Node nodeChild in node.Children())
            {
                if (clone.TryGetNode(nodeChild.ID, out Node clonedChild))
                {
                    CompareHierarchy(nodeChild, clone, clonedChild);
                }
                else
                {
                    Assert.Fail($"Child {nodeChild.ID} of node {node.ID} is missing in the clone.");
                }
            }
        }
    }
}
