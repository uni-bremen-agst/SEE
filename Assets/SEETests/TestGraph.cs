using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace SEE.DataModel.DG
{
    /// <summary>
    /// Unit tests for Graph.
    /// </summary>
    internal class TestGraph
    {
        private static Node NewNode(Graph graph, string id, string type = "Routine")
        {
            Node result = new Node
            {
                ID = id,
                Type = type
            };
            graph.AddNode(result);
            return result;
        }

        /// <summary>
        /// Unique ID for edges.
        /// </summary>
        private static int edgeID = 1;

        private static Edge NewEdge(Graph graph, Node from, Node to, string type = "call")
        {
            edgeID++;
            Edge result = new Edge(from, to, type);
            graph.AddEdge(result);
            return result;
        }

        private bool HasEdge(Node source, Node target, string edgeType = null)
        {
            return source.Outgoings.Any(outgoing => outgoing.Target == target && (edgeType == null || outgoing.Type == edgeType));
        }

        private void AssertHasChild(Graph subgraph, Node parent, Node child)
        {
            Assert.AreSame(Pendant(subgraph, parent), Pendant(subgraph, child).Parent);
        }

        private Node Pendant(Graph subgraph, Node baa) => subgraph.GetNode(baa.ID);

        private static Node Child(Graph g, Node parent, string id, string nodeType = "Routine")
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
            Edge call_n1_n1 = NewEdge(g, n1, n1);
            Assert.AreEqual(new HashSet<Edge> { call_n1_n1 }, AsSet(n1.Outgoings));
            Edge call_n1_n2 = NewEdge(g, n1, n2);
            Assert.AreEqual(new HashSet<Edge> { call_n1_n1, call_n1_n2 }, AsSet(n1.Outgoings));
            Edge call_n1_n3 = NewEdge(g, n1, n3);
            Assert.AreEqual(new HashSet<Edge> { call_n1_n1, call_n1_n2, call_n1_n3 }, AsSet(n1.Outgoings));
            Edge use_n1_n3_a = NewEdge(g, n1, n3, "use");
            Assert.AreEqual(new HashSet<Edge> { call_n1_n1, call_n1_n2, call_n1_n3, use_n1_n3_a }, AsSet(n1.Outgoings));
            Edge use_n1_n3_b = NewEdge(g, n1, n3, "abuse");
            // We have overridden Equals() for edges so that they are considered the same if
            // they have the same type, same source and target linknames, and same attributes.
            // Based on this comparison, use_n1_n3_a and use_n1_n3_b are equal. To make them different,
            // we set an attribute for the latter.
            use_n1_n3_b.SetToggle("Duplicated");
            Assert.AreEqual(new HashSet<Edge> { call_n1_n1, call_n1_n2, call_n1_n3, use_n1_n3_a, use_n1_n3_b }, AsSet(n1.Outgoings));

            Assert.AreEqual(new HashSet<Edge>(), AsSet(n1.From_To(n3, "none")));
            Assert.AreEqual(new HashSet<Edge> { call_n1_n3 }, AsSet(n1.From_To(n3, "call")));
            Assert.AreEqual(new HashSet<Edge> { use_n1_n3_a }, AsSet(n1.From_To(n3, "use")));
            Assert.AreEqual(new HashSet<Edge> { use_n1_n3_b }, AsSet(n1.From_To(n3, "abuse")));

            Edge call_n2_n3 = NewEdge(g, n2, n3);

            Edge call_n2_n2 = NewEdge(g, n2, n2);

            HashSet<Node> nodes = new HashSet<Node> { n1, n2, n3 };
            HashSet<Edge> edges = new HashSet<Edge> { call_n1_n1, call_n1_n2, call_n1_n3, use_n1_n3_a, use_n1_n3_b, call_n2_n3, call_n2_n2 };

            Assert.AreEqual(nodes.Count, g.NodeCount);
            Assert.AreEqual(edges.Count, g.EdgeCount);

            Assert.AreEqual(nodes, g.Nodes());
            Assert.AreEqual(edges, g.Edges());

            g.RemoveEdge(use_n1_n3_b);
            edges = new HashSet<Edge> { call_n1_n1, call_n1_n2, call_n1_n3, use_n1_n3_a, call_n2_n3, call_n2_n2 };
            Assert.AreEqual(nodes.Count, g.NodeCount);
            Assert.AreEqual(edges.Count, g.EdgeCount);
            Assert.AreEqual(nodes, g.Nodes());
            Assert.AreEqual(edges, g.Edges());

            Assert.AreEqual(new HashSet<Edge> { call_n1_n3 }, AsSet(n1.From_To(n3, "call")));
            Assert.AreEqual(new HashSet<Edge> { use_n1_n3_a }, AsSet(n1.From_To(n3, "use")));
            Assert.AreEqual(new HashSet<Edge> { call_n1_n2 }, AsSet(n1.From_To(n2, "call")));
            Assert.AreEqual(new HashSet<Edge> { call_n1_n1 }, AsSet(n1.From_To(n1, "call")));
            Assert.AreEqual(new HashSet<Edge>(), AsSet(n1.From_To(n2, "use")));
            Assert.AreEqual(new HashSet<Edge> { call_n1_n1, call_n1_n2, call_n1_n3, use_n1_n3_a }, AsSet(n1.Outgoings));
        }

        private static HashSet<Edge> AsSet(IEnumerable<Edge> edges)
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

            Edge call_n1_n2 = NewEdge(g, n1, n2);
            Edge use_n1_n2 = NewEdge(g, n1, n2, "use");

            Edge call_n2_n3 = NewEdge(g, n2, n3);
            Edge use_n2_n3 = NewEdge(g, n2, n3, "use");
            Edge call_n2_n2 = NewEdge(g, n2, n2);
            Edge use_n2_n2 = NewEdge(g, n2, n2, "use");

            Edge call_n1_n3 = NewEdge(g, n1, n3);
            Edge call_n3_n1 = NewEdge(g, n3, n1);

            HashSet<Node> nodes = new HashSet<Node> { n1, n2, n3 };
            HashSet<Edge> edges = new HashSet<Edge>
            {
                call_n1_n2, use_n1_n2, call_n2_n3, use_n2_n3, call_n2_n2, use_n2_n2,
                call_n1_n3, call_n3_n1
            };

            Assert.AreEqual(nodes.Count, g.NodeCount);
            Assert.AreEqual(edges.Count, g.EdgeCount);

            Assert.AreEqual(nodes, g.Nodes());
            Assert.AreEqual(edges, g.Edges());

            Assert.AreEqual(n1.Outgoings,
                            new HashSet<Edge> { call_n1_n2, use_n1_n2, call_n1_n3 });
            Assert.AreEqual(n1.Incomings,
                            new HashSet<Edge> { call_n3_n1 });

            Assert.AreEqual(n2.Outgoings,
                            new HashSet<Edge> { call_n2_n3, use_n2_n3, call_n2_n2, use_n2_n2 });
            Assert.AreEqual(n2.Incomings,
                            new HashSet<Edge> { call_n1_n2, use_n1_n2, call_n2_n2, use_n2_n2 });

            Assert.AreEqual(n3.Outgoings,
                            new HashSet<Edge> { call_n3_n1 });
            Assert.AreEqual(n3.Incomings,
                            new HashSet<Edge> { call_n2_n3, use_n2_n3, call_n1_n3 });

            // If a node is removed, all its incoming and outgoing edges must
            // be removed, too, and its successors and predecessors must be adjusted, too.

            g.RemoveNode(n2);
            nodes = new HashSet<Node> { n1, n3 };
            edges = new HashSet<Edge> { call_n1_n3, call_n3_n1 };

            Assert.AreEqual(nodes.Count, g.NodeCount);
            Assert.AreEqual(edges.Count, g.EdgeCount);

            Assert.AreEqual(nodes, g.Nodes());
            Assert.AreEqual(edges, g.Edges());

            Assert.AreEqual(n1.Outgoings,
                            new HashSet<Edge> { call_n1_n3 });
            Assert.AreEqual(n1.Incomings,
                            new HashSet<Edge> { call_n3_n1 });

            Assert.AreEqual(n2.Outgoings,
                            new HashSet<Edge>());
            Assert.AreEqual(n2.Incomings,
                            new HashSet<Edge>());

            Assert.AreEqual(n3.Outgoings,
                            new HashSet<Edge> { call_n3_n1 });
            Assert.AreEqual(n3.Incomings,
                            new HashSet<Edge> { call_n1_n3 });

            // If an edge is removed, it must be removed from the incoming and outgoing
            // edges of its source and target, respectively.
            g.RemoveEdge(call_n3_n1);
            nodes = new HashSet<Node> { n1, n3 };
            edges = new HashSet<Edge> { call_n1_n3 };

            Assert.AreEqual(nodes.Count, g.NodeCount);
            Assert.AreEqual(edges.Count, g.EdgeCount);

            Assert.AreEqual(nodes, g.Nodes());
            Assert.AreEqual(edges, g.Edges());

            Assert.AreEqual(n1.Outgoings,
                            new HashSet<Edge> { call_n1_n3 });
            Assert.AreEqual(n1.Incomings,
                            new HashSet<Edge>());

            Assert.AreEqual(n3.Outgoings,
                            new HashSet<Edge>());
            Assert.AreEqual(n3.Incomings,
                            new HashSet<Edge> { call_n1_n3 });

            // After removing n3, the graph should have only a single node left.
            g.RemoveNode(n3);
            nodes = new HashSet<Node> { n1 };
            edges = new HashSet<Edge>();

            Assert.AreEqual(nodes.Count, g.NodeCount);
            Assert.AreEqual(edges.Count, g.EdgeCount);

            Assert.AreEqual(nodes, g.Nodes());
            Assert.AreEqual(edges, g.Edges());

            Assert.AreEqual(n1.Outgoings,
                            new HashSet<Edge>());
            Assert.AreEqual(n1.Incomings,
                            new HashSet<Edge>());

            Assert.AreEqual(n3.Outgoings,
                            new HashSet<Edge>());
            Assert.AreEqual(n3.Incomings,
                            new HashSet<Edge>());
        }

        [Test]
        public void RemoveOrphansBecomeChildren()
        {
            Graph g = new Graph();

            Node r = NewNode(g, "root");
            Node d = Child(g, r, "toBeDeleted");
            Node o1 = Child(g, d, "orphan1");
            Node o2 = Child(g, d, "orphan2");

            g.RemoveNode(d, orphansBecomeRoots: false);

            AssertHasChild(g, parent: r, child: o1);
            AssertHasChild(g, parent: r, child: o2);
            Assert.IsNull(d.ItsGraph);
            Assert.IsNull(d.Parent);
            Assert.AreEqual(0, d.NumberOfChildren());
        }

        [Test]
        public void RemoveOrphansBecomeRoots()
        {
            Graph g = new Graph();

            Node r = NewNode(g, "root");
            Node d = Child(g, r, "toBeDeleted");
            Node o1 = Child(g, d, "orphan1");
            Node o2 = Child(g, d, "orphan2");

            g.RemoveNode(d, orphansBecomeRoots: true);

            Assert.AreEqual(0, r.NumberOfChildren());
            Assert.IsNull(o1.Parent);
            Assert.IsNull(o2.Parent);
            Assert.IsNull(d.ItsGraph);
            Assert.IsNull(d.Parent);
            Assert.AreEqual(0, d.NumberOfChildren());
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

        /// <summary>
        /// Tests subgraph creation by marking some nodes as relevant and some as irrelevant, then constructing the
        /// subgraph using the given functions.
        /// Note: While for any graph element X, makeRelevant(X) MUST imply isRelevant(x), on the other hand
        /// makeIrrelevant(X) doesn't necessarily have to imply NOT(isRelevant(x)).
        /// For example, SubGraphByNodeType doesn't care about edges, so isRelevant returns true for all edges,
        /// regardless of whether makeIrrelevant had been applied to the edges.
        /// </summary>
        private void TestSubGraphBy(Action<GraphElement> makeRelevant, Action<GraphElement> makeIrrelevant,
                                   Func<GraphElement, bool> isRelevant, Func<Graph, Graph> makeSubgraph)
        {
            // Note: This test is rather imperfect and may be improved in the future.
            Graph g = new Graph();

            Node a = NewNode(g, "a");
            makeIrrelevant(a);
            Node b = NewNode(g, "b");
            makeIrrelevant(b);
            Node ba = Child(g, b, "ba");
            makeIrrelevant(ba);
            Node baa = Child(g, ba, "baa");
            makeRelevant(baa);
            Assert.IsTrue(isRelevant(baa));
            Node baaa = Child(g, baa, "baaa");
            makeIrrelevant(baaa);
            Node baaaa = Child(g, baaa, "baaaa");
            makeRelevant(baaaa);
            Assert.IsTrue(isRelevant(baaaa));
            Node bb = Child(g, b, "bb");
            makeRelevant(bb);
            Assert.IsTrue(isRelevant(bb));
            Node bba = Child(g, bb, "bba");
            makeRelevant(bba);
            Assert.IsTrue(isRelevant(bba));
            Node bbaa = Child(g, bba, "bbaa");
            makeIrrelevant(bbaa);
            Node bc = Child(g, b, "bc");
            makeIrrelevant(bc);
            Node bca = Child(g, bc, "bca");
            makeRelevant(bca);
            Assert.IsTrue(isRelevant(bca));
            Node bcaa = Child(g, bca, "bcaa");
            makeRelevant(bcaa);
            Assert.IsTrue(isRelevant(bcaa));
            Node bcab = Child(g, bca, "bcab");
            makeRelevant(bcab);
            Assert.IsTrue(isRelevant(bcab));
            Node bcb = Child(g, bc, "bcb");
            makeIrrelevant(bcb);
            Node bcba = Child(g, bcb, "bcba");
            makeRelevant(bcba);
            Assert.IsTrue(isRelevant(bcba));
            Node bd = Child(g, b, "bd");
            makeRelevant(bd);
            Assert.IsTrue(isRelevant(bd));
            Node bda = Child(g, bd, "bda");
            makeIrrelevant(bda);
            Node bdaa = Child(g, bda, "bdaa");
            makeRelevant(bdaa);
            Assert.IsTrue(isRelevant(bdaa));
            Node c = NewNode(g, "c");
            makeRelevant(c);
            Assert.IsTrue(isRelevant(c));
            Node d = NewNode(g, "d");
            makeIrrelevant(d);
            Node da = Child(g, d, "da");
            makeRelevant(da);
            Assert.IsTrue(isRelevant(da));
            Node e = NewNode(g, "e");
            makeRelevant(e);
            Assert.IsTrue(isRelevant(e));
            Node ea = Child(g, e, "ea");
            makeIrrelevant(ea);
            // makeIrrelevant may have no effect, which is why we have to count this way.
            int relevantNodes = new List<Node>
            {
                a, b, ba, baa, baaa, baaaa, bb, bba, bbaa, bc, bca, bcaa, bcab, bcb,
                bcba, bd, bda, bdaa, c, d, da, e, ea
            }.Count(isRelevant);

            // We make irrelevant: BCBA->BD and E->C (these two would be included if not for their irrelevance)
            Edge e0 = NewEdge(g, e, c);
            makeIrrelevant(e0);
            Edge e1 = NewEdge(g, a, ba);
            makeRelevant(e1);
            Assert.IsTrue(isRelevant(e1));
            Edge e2 = NewEdge(g, a, b);
            makeRelevant(e2);
            Assert.IsTrue(isRelevant(e2));
            Edge e3 = NewEdge(g, baa, baaa);
            makeRelevant(e3);
            Assert.IsTrue(isRelevant(e3));
            Edge e4 = NewEdge(g, baa, bba);
            makeRelevant(e4);
            Assert.IsTrue(isRelevant(e4));
            Edge e5 = NewEdge(g, bb, bba);
            makeRelevant(e5);
            Assert.IsTrue(isRelevant(e5));
            Edge e6 = NewEdge(g, bbaa, bba);
            makeRelevant(e6);
            Assert.IsTrue(isRelevant(e6));
            Edge e7 = NewEdge(g, bcab, bcba);
            makeRelevant(e7);
            Assert.IsTrue(isRelevant(e7));
            Edge e8 = NewEdge(g, bdaa, baaa);
            makeRelevant(e8);
            Assert.IsTrue(isRelevant(e8));
            Edge e9 = NewEdge(g, bdaa, bd);
            makeRelevant(e9);
            Assert.IsTrue(isRelevant(e9));
            Edge e10 = NewEdge(g, bdaa, bdaa);
            makeRelevant(e10);
            Assert.IsTrue(isRelevant(e10));
            Edge e11 = NewEdge(g, c, e);
            makeRelevant(e11);
            Assert.IsTrue(isRelevant(e11));
            Edge e12 = NewEdge(g, d, d);
            makeRelevant(e12);
            Assert.IsTrue(isRelevant(e12));
            Edge e13 = NewEdge(g, ea, d);
            makeRelevant(e13);
            Assert.IsTrue(isRelevant(e13));
            Edge e14 = NewEdge(g, bcba, bd);
            makeIrrelevant(e14);
            // makeIrrelevant may have no effect, which is why we have to count this way.
            int relevantEdges = new List<Edge> { e0, e1, e2, e3, e4, e5, e6, e7, e8, e9, e10, e11, e12, e13, e14 }.Count(isRelevant);

            Graph subgraph = makeSubgraph(g);

            // Nodes in subgraph must be relevant.
            foreach (Node node in subgraph.Nodes())
            {
                Assert.That(isRelevant(node));
            }

            foreach (Edge edge in subgraph.Edges())
            {
                Assert.That(isRelevant(edge));
            }

            Assert.AreEqual(relevantNodes, subgraph.NodeCount);
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

            // 9 edges are kept.
            // Kept edges: Those for which isRelevant returned true before subgraphing minus four "dangling" ones.
            Assert.AreEqual(relevantEdges - 4, subgraph.EdgeCount);
            Assert.That(HasEdge(BAA, BAA));
            Assert.That(HasEdge(BAA, BBA));
            Assert.That(HasEdge(BB, BBA));
            Assert.That(HasEdge(BBA, BBA));
            Assert.That(HasEdge(BCAB, BCBA));
            Assert.That(HasEdge(BDAA, BAA));
            Assert.That(HasEdge(BDAA, BDAA));
            Assert.That(HasEdge(BDAA, BD));
            Assert.That(HasEdge(C, E));
        }

        [Test]
        public void TestSubGraphByNodeType()
        {
            const string r = "relevant";
            const string i = "irrelevant";
            HashSet<string> relevantNodeTypes = new HashSet<string> { r };

            TestSubGraphBy(x => x.Type = r, x => x.Type = i,
                           x => !(x is Node) || relevantNodeTypes.Contains(x.Type),
                           g => g.SubgraphByNodeType(relevantNodeTypes));
        }

        [Test]
        public void TestSubGraphByToggleAttribute()
        {
            const string relevantToggleType = "relevant";

            TestSubGraphBy(x => x.SetToggle(relevantToggleType),
                           x => x.UnsetToggle(relevantToggleType),
                           x => x.HasToggle(relevantToggleType),
                           g => g.SubgraphByToggleAttributes(new[] { relevantToggleType }));
        }

        [Test]
        public void TestSubGraphByToggleAttributes()
        {
            IEnumerable<string> relevantToggleTypes = new[] { "relevant", "relevant too", "oh, and me too" };

            TestSubGraphBy(x =>
                           {
                               foreach (string relevantToggleType in relevantToggleTypes)
                               {
                                   x.SetToggle(relevantToggleType);
                               }
                           },
                           // it suffices to unset a single toggle to make the element irrelevant
                           x => x.UnsetToggle(relevantToggleTypes.First()),
                           x => relevantToggleTypes.All(x.HasToggle),
                           g => g.SubgraphByToggleAttributes(relevantToggleTypes));
        }

        /// <summary>
        /// Deleting and restoring a subtree consisting of only a single node.
        /// </summary>
        [Test]
        public void TestDeleteTreeSingleNode()
        {
            Graph g = new Graph();
            Node a = NewNode(g, "a");
            SubgraphMemento subgraph = a.DeleteTree();
            Assert.IsNull(a.ItsGraph);
            Assert.AreEqual(0, g.NodeCount);
            Assert.AreEqual(0, g.EdgeCount);

            subgraph.Restore();
            Assert.AreEqual(g, a.ItsGraph);
            Assert.AreEqual(1, g.NodeCount);
            Assert.AreEqual(0, g.EdgeCount);
        }

        /// <summary>
        /// Deleting and restoring a subtree consisting of only a single node
        /// and a self loop.
        /// </summary>
        [Test]
        public void TestDeleteTreeSingleNodeAndEdge()
        {
            Graph g = new Graph();
            Node a = NewNode(g, "a");
            Edge e = NewEdge(g, a, a);
            SubgraphMemento subgraph = a.DeleteTree();
            Assert.IsNull(a.ItsGraph);
            Assert.IsNull(e.ItsGraph);
            Assert.AreEqual(0, g.NodeCount);
            Assert.AreEqual(0, g.EdgeCount);

            subgraph.Restore();
            Assert.AreEqual(g, a.ItsGraph);
            Assert.AreEqual(g, e.ItsGraph);
            Assert.AreEqual(1, g.NodeCount);
            Assert.AreEqual(1, g.EdgeCount);
        }

        /// <summary>
        /// Deleting and restoring a subtree consisting of multiple nested
        /// nodes and several incoming, outgoing, and internal edges in
        /// the node hierarchy to be deleted.
        /// </summary>
        [Test]
        public void TestDeleteTree()
        {
            Graph g = new Graph();
            Node a = NewNode(g, "a"); // root
            Node b = Child(g, a, "b"); // child of a, but not descendant of c
            Node c = Child(g, a, "c"); // root of subtree to be deleted
            Node d = Child(g, c, "d"); // descendant of c
            Node e = Child(g, c, "e"); // descendant of c
            Node f = Child(g, e, "f"); // descendant of c

            List<Node> subgraphNodes = new List<Node> { c, d, e, f };

            Edge e1 = NewEdge(g, a, b); // outside
            Edge e2 = NewEdge(g, b, a); // outside
            Edge e3 = NewEdge(g, a, e); // incoming
            Edge e4 = NewEdge(g, d, b); // outgoing
            Edge e5 = NewEdge(g, d, e); // internal
            Edge e6 = NewEdge(g, f, d); // internal
            Edge e7 = NewEdge(g, a, c); // incoming
            Edge e8 = NewEdge(g, c, a); // outgoing

            List<Edge> subgraphEdges = new List<Edge> { e3, e4, e5, e6, e7, e8 };

            SubgraphMemento subgraph = c.DeleteTree();

            // a and b are still in the graph, but all other nodes are removed
            Assert.AreEqual(g, a.ItsGraph);
            Assert.AreEqual(g, b.ItsGraph);
            foreach (Node node in subgraphNodes)
            {
                Assert.IsNull(node.ItsGraph);
            }

            // e1 and e2 are still in the graph, but all other edges are removed
            Assert.AreEqual(g, e1.ItsGraph);
            Assert.AreEqual(g, e2.ItsGraph);
            foreach (Edge edge in subgraphEdges)
            {
                Assert.IsNull(edge.ItsGraph);
            }

            Assert.AreEqual(2, g.NodeCount);
            Assert.AreEqual(2, g.EdgeCount);

            subgraph.Restore();
            Assert.AreEqual(g, a.ItsGraph);
            Assert.AreEqual(g, b.ItsGraph);
            foreach (Node node in subgraphNodes)
            {
                Assert.AreEqual(g, node.ItsGraph);
            }

            Assert.AreEqual(g, e1.ItsGraph);
            Assert.AreEqual(g, e2.ItsGraph);
            foreach (Edge edge in subgraphEdges)
            {
                Assert.AreEqual(g, edge.ItsGraph);
            }

            Assert.AreEqual(subgraphNodes.Count + 2, g.NodeCount);
            Assert.AreEqual(subgraphEdges.Count + 2, g.EdgeCount);
        }
    }
}