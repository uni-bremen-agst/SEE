using NUnit.Framework;
using System.Collections.Generic;

namespace SEE.DataModel.DG
{
    /// <summary>
    /// Unit tests for Graph.
    /// </summary>
    internal class TestGraph
    {
        private static Node NewNode(Graph graph, string id, string type = "Routine")
        {
            Node result = new Node();
            result.ID = id;
            result.Type = type;
            graph.AddNode(result);
            return result;
        }

        /// <summary>
        /// Unique ID for edges.
        /// </summary>
        private static int edgeID = 1;

        private static Edge NewEdge(Graph graph, Node from, Node to, string type)
        {
            edgeID++;
            Edge result = new Edge(edgeID.ToString());
            result.Type = type;
            result.Source = from;
            result.Target = to;
            graph.AddEdge(result);
            return result;
        }

        private bool HasEdge(Node source, Node target, string edgeType)
        {
            foreach (Edge outgoing in source.Outgoings)
            {
                if (outgoing.Type == edgeType && outgoing.Target == target)
                {
                    return true;
                }
            }
            return false;
        }

        private void AssertHasChild(Graph subgraph, Node parent, Node child)
        {
            Assert.AreSame(Pendant(subgraph, parent), Pendant(subgraph, child).Parent);
        }

        private Node Pendant(Graph subgraph, Node baa)
        {
            return subgraph.GetNode(baa.ID);
        }

        private static Node Child(Graph g, Node parent, string id, string nodeType)
        {
            Node child = NewNode(g, id, nodeType);
            parent.AddChild(child);
            return child;
        }

        /// <summary>
        /// Tests the following operations:
        ///   graph.AddNode
        ///   graph.AddEdge
        ///   graph.RemoveEdge
        ///   graph.RemoveNode
        ///   node.Outgoings
        ///   node.From_To
        /// </summary>
        [Test]
        public void AddingRemovingGraphElements()
        {
            Graph g = new Graph();

            Node n1 = NewNode(g, "n1");
            Node n2 = NewNode(g, "n2");
            Node n3 = NewNode(g, "n3");

            Assert.AreEqual(new HashSet<Edge>(), AsSet(n1.Outgoings));
            Edge call_n1_n1 = NewEdge(g, n1, n1, "call");
            Assert.AreEqual(new HashSet<Edge>() { call_n1_n1 }, AsSet(n1.Outgoings));
            Edge call_n1_n2 = NewEdge(g, n1, n2, "call");
            Assert.AreEqual(new HashSet<Edge>() { call_n1_n1, call_n1_n2 }, AsSet(n1.Outgoings));
            Edge call_n1_n3 = NewEdge(g, n1, n3, "call");
            Assert.AreEqual(new HashSet<Edge>() { call_n1_n1, call_n1_n2, call_n1_n3 }, AsSet(n1.Outgoings));
            Edge use_n1_n3_a = NewEdge(g, n1, n3, "use");
            Assert.AreEqual(new HashSet<Edge>() { call_n1_n1, call_n1_n2, call_n1_n3, use_n1_n3_a }, AsSet(n1.Outgoings));
            Edge use_n1_n3_b = NewEdge(g, n1, n3, "use");
            // We have overridden Equals() for edges so that they are considered the same if
            // they have the same type, same source and target linknames, and same attributes.
            // Based on this comparison, use_n1_n3_a and use_n1_n3_b are equal. To make them different,
            // we set an attribute for the latter.
            use_n1_n3_b.SetToggle("Duplicated");
            Assert.AreEqual(new HashSet<Edge>() { call_n1_n1, call_n1_n2, call_n1_n3, use_n1_n3_a, use_n1_n3_b }, AsSet(n1.Outgoings));

            Assert.AreEqual(new HashSet<Edge>(), AsSet(n1.From_To(n3, "none")));
            Assert.AreEqual(new HashSet<Edge>() { call_n1_n3 }, AsSet(n1.From_To(n3, "call")));
            Assert.AreEqual(new HashSet<Edge>() { use_n1_n3_a, use_n1_n3_b }, AsSet(n1.From_To(n3, "use")));

            Edge call_n2_n3 = NewEdge(g, n2, n3, "call");

            Edge call_n2_n2 = NewEdge(g, n2, n2, "call");

            HashSet<Node> nodes = new HashSet<Node>() { n1, n2, n3 };
            HashSet<Edge> edges = new HashSet<Edge>() { call_n1_n1, call_n1_n2, call_n1_n3, use_n1_n3_a, use_n1_n3_b, call_n2_n3, call_n2_n2 };

            Assert.AreEqual(nodes.Count, g.NodeCount);
            Assert.AreEqual(edges.Count, g.EdgeCount);

            Assert.AreEqual(nodes, g.Nodes());
            Assert.AreEqual(edges, g.Edges());

            g.RemoveEdge(use_n1_n3_b);
            edges = new HashSet<Edge>() { call_n1_n1, call_n1_n2, call_n1_n3, use_n1_n3_a, call_n2_n3, call_n2_n2 };
            Assert.AreEqual(nodes.Count, g.NodeCount);
            Assert.AreEqual(edges.Count, g.EdgeCount);
            Assert.AreEqual(nodes, g.Nodes());
            Assert.AreEqual(edges, g.Edges());

            Assert.AreEqual(new HashSet<Edge>() { call_n1_n3 }, AsSet(n1.From_To(n3, "call")));
            Assert.AreEqual(new HashSet<Edge>() { use_n1_n3_a }, AsSet(n1.From_To(n3, "use")));
            Assert.AreEqual(new HashSet<Edge>() { call_n1_n2 }, AsSet(n1.From_To(n2, "call")));
            Assert.AreEqual(new HashSet<Edge>() { call_n1_n1 }, AsSet(n1.From_To(n1, "call")));
            Assert.AreEqual(new HashSet<Edge>(), AsSet(n1.From_To(n2, "use")));
            Assert.AreEqual(new HashSet<Edge>() { call_n1_n1, call_n1_n2, call_n1_n3, use_n1_n3_a }, AsSet(n1.Outgoings));
        }

        private HashSet<Edge> AsSet(List<Edge> edges)
        {
            return new HashSet<Edge>(edges);
        }

        /// <summary>
        /// Tests primarily the following operations when a node is removed that has
        /// outgoing and incoming edges:
        ///   graph.RemoveNode
        ///   graph.RemoveEdge
        /// </summary>
        [Test]
        public void RemoveNode()
        {
            Graph g = new Graph();

            Node n1 = NewNode(g, "n1");
            Node n2 = NewNode(g, "n2");
            Node n3 = NewNode(g, "n3");

            Edge call_n1_n2 = NewEdge(g, n1, n2, "call");
            Edge use_n1_n2 = NewEdge(g, n1, n2, "use");

            Edge call_n2_n3 = NewEdge(g, n2, n3, "call");
            Edge use_n2_n3 = NewEdge(g, n2, n3, "use");
            Edge call_n2_n2 = NewEdge(g, n2, n2, "call");
            Edge use_n2_n2 = NewEdge(g, n2, n2, "use");

            Edge call_n1_n3 = NewEdge(g, n1, n3, "call");
            Edge call_n3_n1 = NewEdge(g, n3, n1, "call");

            HashSet<Node> nodes = new HashSet<Node>() { n1, n2, n3 };
            HashSet<Edge> edges = new HashSet<Edge>() { call_n1_n2, use_n1_n2, call_n2_n3, use_n2_n3, call_n2_n2, use_n2_n2,
                                                        call_n1_n3, call_n3_n1};

            Assert.AreEqual(nodes.Count, g.NodeCount);
            Assert.AreEqual(edges.Count, g.EdgeCount);

            Assert.AreEqual(nodes, g.Nodes());
            Assert.AreEqual(edges, g.Edges());

            Assert.AreEqual(n1.Outgoings,
                            new HashSet<Edge>() { call_n1_n2, use_n1_n2, call_n1_n3 });
            Assert.AreEqual(n1.Incomings,
                            new HashSet<Edge>() { call_n3_n1 });

            Assert.AreEqual(n2.Outgoings,
                            new HashSet<Edge>() { call_n2_n3, use_n2_n3, call_n2_n2, use_n2_n2 });
            Assert.AreEqual(n2.Incomings,
                            new HashSet<Edge>() { call_n1_n2, use_n1_n2, call_n2_n2, use_n2_n2 });

            Assert.AreEqual(n3.Outgoings,
                            new HashSet<Edge>() { call_n3_n1 });
            Assert.AreEqual(n3.Incomings,
                            new HashSet<Edge>() { call_n2_n3, use_n2_n3, call_n1_n3 });

            // If a node is removed, all its incoming and outgoing edges must
            // be removed, too, and its successors and predecessors must be adjusted, too.

            g.RemoveNode(n2);
            nodes = new HashSet<Node>() { n1, n3 };
            edges = new HashSet<Edge>() { call_n1_n3, call_n3_n1 };

            Assert.AreEqual(nodes.Count, g.NodeCount);
            Assert.AreEqual(edges.Count, g.EdgeCount);

            Assert.AreEqual(nodes, g.Nodes());
            Assert.AreEqual(edges, g.Edges());

            Assert.AreEqual(n1.Outgoings,
                            new HashSet<Edge>() { call_n1_n3 });
            Assert.AreEqual(n1.Incomings,
                            new HashSet<Edge>() { call_n3_n1 });

            Assert.AreEqual(n2.Outgoings,
                            new HashSet<Edge>() { });
            Assert.AreEqual(n2.Incomings,
                            new HashSet<Edge>() { });

            Assert.AreEqual(n3.Outgoings,
                            new HashSet<Edge>() { call_n3_n1 });
            Assert.AreEqual(n3.Incomings,
                            new HashSet<Edge>() { call_n1_n3 });

            // If an edge is removed, it must be removed from the incoming and outgoing
            // edges of its source and target, respectively.
            g.RemoveEdge(call_n3_n1);
            nodes = new HashSet<Node>() { n1, n3 };
            edges = new HashSet<Edge>() { call_n1_n3 };

            Assert.AreEqual(nodes.Count, g.NodeCount);
            Assert.AreEqual(edges.Count, g.EdgeCount);

            Assert.AreEqual(nodes, g.Nodes());
            Assert.AreEqual(edges, g.Edges());

            Assert.AreEqual(n1.Outgoings,
                            new HashSet<Edge>() { call_n1_n3 });
            Assert.AreEqual(n1.Incomings,
                            new HashSet<Edge>() { });

            Assert.AreEqual(n3.Outgoings,
                            new HashSet<Edge>() { });
            Assert.AreEqual(n3.Incomings,
                            new HashSet<Edge>() { call_n1_n3 });

            // After removing n3, the graph should have only a single node left.
            g.RemoveNode(n3);
            nodes = new HashSet<Node>() { n1 };
            edges = new HashSet<Edge>() { };

            Assert.AreEqual(nodes.Count, g.NodeCount);
            Assert.AreEqual(edges.Count, g.EdgeCount);

            Assert.AreEqual(nodes, g.Nodes());
            Assert.AreEqual(edges, g.Edges());

            Assert.AreEqual(n1.Outgoings,
                            new HashSet<Edge>() { });
            Assert.AreEqual(n1.Incomings,
                            new HashSet<Edge>() { });

            Assert.AreEqual(n3.Outgoings,
                            new HashSet<Edge>() { });
            Assert.AreEqual(n3.Incomings,
                            new HashSet<Edge>() { });
        }

        [Test]
        public void TestReparent()
        {
            string t = "Routine";

            Graph g = new Graph();
            Assert.AreEqual(0, g.MaxDepth);

            Node a = NewNode(g, "a", t);
            Node b = NewNode(g, "b", t);

            // hierarchy:
            //  a   b
            Assert.AreEqual(0, a.Level);
            Assert.AreEqual(0, b.Level);
            Assert.AreEqual(1, a.Depth());
            Assert.AreEqual(1, b.Depth());
            Assert.AreEqual(1, g.MaxDepth);

            a.Reparent(null);
            // hierarchy:
            //  a   b
            // no change expected
            Assert.AreEqual(0, a.Level);
            Assert.AreEqual(0, b.Level);
            Assert.AreEqual(1, a.Depth());
            Assert.AreEqual(1, b.Depth());
            Assert.AreEqual(1, g.MaxDepth);

            Node bc = Child(g, b, "bc", t);
            // hierarchy:
            //  a   b
            //      |
            //      bc
            Assert.AreEqual(0, a.Level);
            Assert.AreEqual(0, b.Level);
            Assert.AreEqual(1, bc.Level);
            Assert.AreEqual(1, a.Depth());
            Assert.AreEqual(2, b.Depth());
            Assert.AreEqual(1, bc.Depth());
            Assert.AreEqual(2, g.MaxDepth);

            bc.Reparent(null);
            // hierarchy:
            //  a   b  bc
            Assert.AreEqual(0, a.Level);
            Assert.AreEqual(1, a.Depth());
            Assert.AreEqual(0, b.Level);
            Assert.AreEqual(1, b.Depth());
            Assert.AreEqual(0, bc.Level);
            Assert.AreEqual(1, bc.Depth());
            Assert.AreEqual(1, g.MaxDepth);

            Node ac = Child(g, a, "ac", t);
            Node acc = Child(g, ac, "acc", t);
            Node bcc = Child(g, bc, "bcc", t);
            bc.Reparent(b);
            // hierarchy:
            //  a   b
            //  |   |
            // ac   bc
            //  |   |
            // acc bcc
            Assert.AreEqual(0, a.Level);
            Assert.AreEqual(3, a.Depth());
            Assert.AreEqual(1, ac.Level);
            Assert.AreEqual(2, ac.Depth());
            Assert.AreEqual(2, acc.Level);
            Assert.AreEqual(1, acc.Depth());

            Assert.AreEqual(0, b.Level);
            Assert.AreEqual(3, b.Depth());
            Assert.AreEqual(1, bc.Level);
            Assert.AreEqual(2, bc.Depth());
            Assert.AreEqual(2, bcc.Level);
            Assert.AreEqual(1, bcc.Depth());

            Assert.AreEqual(3, g.MaxDepth);
            bc.Reparent(ac);
            // hierarchy:
            //    a     b
            //    |
            //    ac
            //  /   \
            //  |   |
            // acc bc
            //      |
            //     bcc
            Assert.AreEqual(0, a.Level);
            Assert.AreEqual(4, a.Depth());
            Assert.AreEqual(1, ac.Level);
            Assert.AreEqual(3, ac.Depth());
            Assert.AreEqual(2, acc.Level);
            Assert.AreEqual(1, acc.Depth());
            Assert.AreEqual(2, bc.Level);
            Assert.AreEqual(2, bc.Depth());
            Assert.AreEqual(3, bcc.Level);
            Assert.AreEqual(1, bcc.Depth());
            Assert.AreEqual(0, b.Level);
            Assert.AreEqual(1, b.Depth());
            Assert.AreEqual(4, g.MaxDepth);
        }

        [Test]
        public void TestSubGraph()
        {
            Graph g = new Graph();
            string r = "relevant";
            string i = "irrelevant";

            Node a = NewNode(g, "a", i);
            Node b = NewNode(g, "b", i);
            Node ba = Child(g, b, "ba", i);
            Node baa = Child(g, ba, "baa", r);
            Node baaa = Child(g, baa, "baaa", i);
            Node baaaa = Child(g, baaa, "baaaa", r);
            Node bb = Child(g, b, "bb", r);
            Node bba = Child(g, bb, "bba", r);
            Node bbaa = Child(g, bba, "bbaa", i);
            Node bc = Child(g, b, "bc", i);
            Node bca = Child(g, bc, "bca", r);
            Node bcaa = Child(g, bca, "bcaa", r);
            Node bcab = Child(g, bca, "bcab", r);
            Node bcb = Child(g, bc, "bcb", i);
            Node bcba = Child(g, bcb, "bcba", r);
            Node bd = Child(g, b, "bd", r);
            Node bda = Child(g, bd, "bda", i);
            Node bdaa = Child(g, bda, "bdaa", r);
            Node c = NewNode(g, "c", r);
            Node d = NewNode(g, "d", i);
            Node da = Child(g, d, "da", r);
            Node e = NewNode(g, "e", r);
            Node ea = Child(g, e, "ea", i);

            string edgeType = "call";
            List<Edge> edges = new List<Edge>()
            {
                NewEdge(g, a, ba, edgeType),      // lost
                NewEdge(g, a, b, edgeType),       // lost
                NewEdge(g, baa, baaa, edgeType),  // kept
                NewEdge(g, baa, bba, edgeType),   // kept
                NewEdge(g, bb, bba, edgeType),    // kept
                NewEdge(g, bbaa, bba, edgeType),  // kept
                NewEdge(g, bcab, bcba, edgeType), // kept
                NewEdge(g, bdaa, baaa, edgeType), // kept
                NewEdge(g, bdaa, bd, edgeType),   // kept
                NewEdge(g, bdaa, bdaa, edgeType), // kept
                NewEdge(g, c, e, edgeType),       // kept
                NewEdge(g, d, d, edgeType),       // lost
                NewEdge(g, ea, d, edgeType),      // lost
            };

            HashSet<string> relevantNodeTypes = new HashSet<string>() { r };
            Graph subgraph = g.Subgraph(relevantNodeTypes);
            // Nodes in subgraph must have a relevant node type.
            foreach (Node node in subgraph.Nodes())
            {
                Assert.That(relevantNodeTypes.Contains(node.Type));
            }
            // There are 13 nodes with a relevant node type.
            Assert.AreEqual(13, subgraph.NodeCount);

            Assert.IsNull(Pendant(subgraph, a));
            Assert.IsNull(Pendant(subgraph, b));
            Assert.IsNull(Pendant(subgraph, ba));
            Node BAA = Pendant(subgraph, baa);
            Node BB = Pendant(subgraph, bb);
            Assert.IsNull(Pendant(subgraph, bc));
            Node BCA = Pendant(subgraph, bca);
            Assert.IsNull(Pendant(subgraph, bcb));
            Node BCBA = Pendant(subgraph, bcba);
            Node BD = Pendant(subgraph, bd);
            Node C = Pendant(subgraph, c);
            Assert.IsNull(Pendant(subgraph, d));
            Node DA = Pendant(subgraph, da);
            Node E = Pendant(subgraph, e);
            Node BAAAA = Pendant(subgraph, baaaa);
            Node BBA = Pendant(subgraph, bba);
            Node BCAA = Pendant(subgraph, bcaa);
            Node BCAB = Pendant(subgraph, bcab);
            Node BDAA = Pendant(subgraph, bdaa);

            Assert.That(BAA.IsRoot);
            Assert.That(BB.IsRoot);
            Assert.That(BCA.IsRoot);
            Assert.That(BCBA.IsRoot);
            Assert.That(BD.IsRoot);
            Assert.That(C.IsRoot);
            Assert.That(DA.IsRoot);
            Assert.That(E.IsRoot);
            Assert.That(BAAAA.IsLeaf);
            Assert.That(BBA.IsLeaf);
            Assert.That(BCAA.IsLeaf);
            Assert.That(BCAB.IsLeaf);
            Assert.That(BCBA.IsLeaf);
            Assert.That(BDAA.IsLeaf);
            Assert.That(C.IsLeaf);
            Assert.That(E.IsLeaf);

            AssertHasChild(subgraph, baa, baaaa);
            AssertHasChild(subgraph, bb, bba);
            AssertHasChild(subgraph, bca, bcaa);
            AssertHasChild(subgraph, bca, bcab);
            AssertHasChild(subgraph, bd, bdaa);

            // There are 9 edges kept.
            Assert.AreEqual(9, subgraph.EdgeCount);
            Assert.That(HasEdge(BAA, BAA, edgeType));
            Assert.That(HasEdge(BAA, BBA, edgeType));
            Assert.That(HasEdge(BB, BBA, edgeType));
            Assert.That(HasEdge(BBA, BBA, edgeType));
            Assert.That(HasEdge(BCAB, BCBA, edgeType));
            Assert.That(HasEdge(BDAA, BAA, edgeType));
            Assert.That(HasEdge(BDAA, BDAA, edgeType));
            Assert.That(HasEdge(BDAA, BD, edgeType));
            Assert.That(HasEdge(C, E, edgeType));
        }
    }
}