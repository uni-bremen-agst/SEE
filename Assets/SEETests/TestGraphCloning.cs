using NUnit.Framework;
using System.Collections.Generic;

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
            Graph graph = new Graph();
            graph.AddNode(original);

            Node clone = (Node)original.Clone();
            Assert.That(clone.Type == original.Type);
            Assert.That(clone.ID == original.ID);
            Assert.That(clone.SourceName == original.SourceName);
            Assert.That(clone.GetFloat("float") == original.GetFloat("float"));
            Assert.That(clone.GetInt("int") == original.GetInt("int"));
            Assert.That(clone.GetString("string") == original.GetString("string"));
            Assert.That(clone.HasToggle("toggle"));
            // Note: Hierarchy information (parent, children, level) is cloned only when a
            // graph is cloned.
            Assert.That(clone.Level == 0);
            Assert.That(clone.Parent, Is.Null);
            Assert.That(clone.Children().Count == 0);
            // cloned nodes do not yet belong to any graph
            Assert.That(clone.ItsGraph, Is.Null);
        }

        /// <summary>
        /// Unique ID for edges.
        /// </summary>
        private int edgeID = 1;

        private Edge NewEdge(Node source, Node target)
        {
            Edge edge = new Edge(edgeID.ToString());
            edgeID++;
            edge.Source = source;
            edge.Target = target;
            edge.Type = "Call";
            edge.SetFloat("float", 1.0f);
            edge.SetInt("int", 2);
            edge.SetString("string", "hello");
            edge.SetToggle("toggle");
            return edge;
        }

        [Test]
        public void TestCloneEdge()
        {
            Graph graph = new Graph();
            Node source = NewNode("source");
            graph.AddNode(source);
            Node target = NewNode("target");
            graph.AddNode(target);
            Edge original = NewEdge(source, target);
            graph.AddEdge(original);

            Edge clone = (Edge)original.Clone();
            Assert.That(clone.Type == original.Type);
            Assert.That(clone.GetFloat("float") == original.GetFloat("float"));
            Assert.That(clone.GetInt("int") == original.GetInt("int"));
            Assert.That(clone.GetString("string") == original.GetString("string"));
            Assert.That(clone.HasToggle("toggle"));
            // Note: Source and target of an edge should be cloned (shallow copy), too.
            Assert.That(clone.Source == original.Source);
            Assert.That(clone.Target == original.Target);
            // cloned edges do not yet belong to any graph
            Assert.That(clone.ItsGraph, Is.Null);
        }

        [Test]
        public void TestCloneGraph()
        {
            Graph original = new Graph();
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
            Edge e2 = NewEdge(n2, n3);
            Edge e3 = NewEdge(n2, n3);
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
            Assert.That(clone.Path == original.Path);
            Assert.That(clone.Name == original.Name);
            Assert.That(clone.NodeCount == original.NodeCount);
            Assert.That(clone.EdgeCount == original.EdgeCount);

            // all cloned nodes must be in the cloned graph
            foreach (Node node in clone.Nodes())
            {
                Assert.AreEqual(clone, node.ItsGraph);
            }
            // all cloned edges must be in the cloned graph (and their
            // source and target, too)
            foreach (Edge edge in clone.Edges())
            {
                Assert.AreEqual(clone, edge.ItsGraph);
                Assert.AreEqual(clone, edge.Source.ItsGraph);
                Assert.AreEqual(clone, edge.Target.ItsGraph);
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
                    Assert.Fail();
                }
            }
        }

        private void CompareHierarchy(Node node, Graph clone, Node clonedNode)
        {
            Assert.That(node.ID == clonedNode.ID,
                        "Linknames differ: " + node.ID + " != " + clonedNode.ID);
            Assert.That(node.NumberOfChildren() == clonedNode.NumberOfChildren());
            Assert.That(node.Level == clonedNode.Level,
                        "levels differ between correspondings nodes with linkname "
                        + node.ID + ": "
                        + node.Level + " (expected) != " + clonedNode.Level + " (actual)");

            if (node.IsRoot())
            {
                Assert.That(clonedNode.IsRoot());
            }
            else
            {
                Assert.That(!clonedNode.IsRoot(),
                            clonedNode.ToString() + " should not be a root. Corresponding node in original graph: "
                            + node.ToString());
                Assert.That(node.Parent.ID == clonedNode.Parent.ID);
            }

            foreach (Node nodeChild in node.Children())
            {
                if (clone.TryGetNode(nodeChild.ID, out Node clonedChild))
                {
                    CompareHierarchy(nodeChild, clone, clonedChild);
                }
                else
                {
                    Assert.Fail();
                }
            }
        }
    }
}
