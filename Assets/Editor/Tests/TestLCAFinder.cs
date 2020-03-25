using NUnit.Framework;
using SEE.DataModel;
using System;

namespace SEE.Layout
{
    /// <summary>
    /// Unit tests for Graph.
    /// </summary>
    class TestLCAFinder
    {
        Graph graph;
        private int nodeID = 0;

        private Node NewVertex(string name = "")
        {
            Node node = new Node();
            if (string.IsNullOrEmpty(name))
            {
                node.LinkName = nodeID.ToString();
                nodeID++;
            }
            else
            {
                node.LinkName = name;
            }
            graph.AddNode(node);
            return node;
        }

        [SetUp]
        protected void Setup()
        {
            graph = new Graph();
            nodeID = 0;
        }

        [Test]
        public void TestEmpty()
        {
            Assert.That(() => new LCAFinder(graph, (Node)null), Throws.ArgumentNullException);
        }

        [Test]
        public void TestSingle()
        {
            Node root = NewVertex();
            LCAFinder lca = new LCAFinder(graph, root);

            Assert.AreEqual(root, lca.LCA(root, root));
        }

        [Test]
        public void TestSimple()
        {
            Node root = NewVertex();
            Node a = NewVertex();
            Node b = NewVertex();
            root.AddChild(a);
            root.AddChild(b);

            LCAFinder lca = new LCAFinder(graph, root);

            Assert.AreEqual(root, lca.LCA(a, b));
            Assert.AreEqual(root, lca.LCA(root, b));
            Assert.AreEqual(root, lca.LCA(b, root));
            Assert.AreEqual(root, lca.LCA(a, root));
            Assert.AreEqual(root, lca.LCA(root, a));
        }

        [Test]
        public void TestMultiLevel()
        {
            //        root
            //       / |   \
            //      a  b    c
            //     /\  |    /\
            //   a1 a2 b1 c1 c2
            //            /\ 
            //          c11 c12

            Node root = NewVertex("root");
            Node a = NewVertex("a");
            Node b = NewVertex("b");
            Node c = NewVertex("c");
            Node a1 = NewVertex("a1");
            Node a2 = NewVertex("a2");
            Node b1 = NewVertex("b1");
            Node c1 = NewVertex("c1");
            Node c2 = NewVertex("c2");
            Node c11 = NewVertex("c11");
            Node c12 = NewVertex("c12");

            root.AddChild(a);
            root.AddChild(b);
            root.AddChild(c);

            a.AddChild(a1);
            a.AddChild(a2);

            b.AddChild(b1);

            c.AddChild(c1);
            c.AddChild(c2);
            c1.AddChild(c11);
            c1.AddChild(c12);

            LCAFinder lca = new LCAFinder(graph, root);

            Assert.AreEqual(a, lca.LCA(a1, a2));
            Assert.AreEqual(root, lca.LCA(a2, b1));
            Assert.AreEqual(root, lca.LCA(b1, c12));
            Assert.AreEqual(c1, lca.LCA(c11, c12));
            Assert.AreEqual(c, lca.LCA(c2, c12));
            Assert.AreEqual(c, lca.LCA(c1, c));
        }
    }
}