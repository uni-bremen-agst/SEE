using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SEE.Tools.ReflexionAnalysis;

namespace SEE.DataModel.DG
{
    /// <summary>
    /// Unit tests for Graph.
    /// </summary>
    internal class TestGraph : TestGraphBase
    {
        /// <summary>
        /// Tests the following operations:
        ///   graph.AddNode
        ///   graph.AddEdge
        ///   graph.RemoveEdge
        ///   graph.RemoveNode
        ///   node.Outgoings
        ///   node.FromTo
        /// </summary>
        [Test]
        public void AddingRemovingGraphElements()
        {
            Graph g = NewEmptyGraph();

            Node n1 = NewNode(g, "n1");
            Node n2 = NewNode(g, "n2");
            Node n3 = NewNode(g, "n3");

            Assert.That(AsSet(n1.Outgoings), Is.EqualTo(new HashSet<Edge>()));
            Edge call_n1_n1 = NewEdge(g, n1, n1);
            Assert.That(AsSet(n1.Outgoings), Is.EqualTo(new HashSet<Edge> { call_n1_n1 }));
            Edge call_n1_n2 = NewEdge(g, n1, n2);
            Assert.That(AsSet(n1.Outgoings), Is.EqualTo(new HashSet<Edge> { call_n1_n1, call_n1_n2 }));
            Edge call_n1_n3 = NewEdge(g, n1, n3);
            Assert.That(AsSet(n1.Outgoings), Is.EqualTo(new HashSet<Edge> { call_n1_n1, call_n1_n2, call_n1_n3 }));
            Edge use_n1_n3_a = NewEdge(g, n1, n3, "use");
            Assert.That(AsSet(n1.Outgoings), Is.EqualTo(new HashSet<Edge> { call_n1_n1, call_n1_n2, call_n1_n3, use_n1_n3_a }));
            Edge use_n1_n3_b = NewEdge(g, n1, n3, "abuse");
            // We have overridden Equals() for edges so that they are considered the same if
            // they have the same type, same source and target linknames, and same attributes.
            // Based on this comparison, use_n1_n3_a and use_n1_n3_b are equal. To make them different,
            // we set an attribute for the latter.
            use_n1_n3_b.SetToggle("Duplicated");
            Assert.That(AsSet(n1.Outgoings), Is.EqualTo(new HashSet<Edge> { call_n1_n1, call_n1_n2, call_n1_n3, use_n1_n3_a, use_n1_n3_b }));

            Assert.That(AsSet(n1.FromTo(n3, "none")), Is.EqualTo(new HashSet<Edge>()));
            Assert.That(AsSet(n1.FromTo(n3, "call")), Is.EqualTo(new HashSet<Edge> { call_n1_n3 }));
            Assert.That(AsSet(n1.FromTo(n3, "use")), Is.EqualTo(new HashSet<Edge> { use_n1_n3_a }));
            Assert.That(AsSet(n1.FromTo(n3, "abuse")), Is.EqualTo(new HashSet<Edge> { use_n1_n3_b }));

            Edge call_n2_n3 = NewEdge(g, n2, n3);

            Edge call_n2_n2 = NewEdge(g, n2, n2);

            HashSet<Node> nodes = new HashSet<Node> { n1, n2, n3 };
            HashSet<Edge> edges = new HashSet<Edge> { call_n1_n1, call_n1_n2, call_n1_n3, use_n1_n3_a, use_n1_n3_b, call_n2_n3, call_n2_n2 };

            Assert.That(g.NodeCount, Is.EqualTo(nodes.Count));
            Assert.That(g.EdgeCount, Is.EqualTo(edges.Count));

            Assert.That(g.Nodes(), Is.EqualTo(nodes));
            Assert.That(g.Edges(), Is.EqualTo(edges));

            g.RemoveEdge(use_n1_n3_b);
            edges = new HashSet<Edge> { call_n1_n1, call_n1_n2, call_n1_n3, use_n1_n3_a, call_n2_n3, call_n2_n2 };
            Assert.That(g.NodeCount, Is.EqualTo(nodes.Count));
            Assert.That(g.EdgeCount, Is.EqualTo(edges.Count));
            Assert.That(g.Nodes(), Is.EqualTo(nodes));
            Assert.That(g.Edges(), Is.EqualTo(edges));

            Assert.That(AsSet(n1.FromTo(n3, "call")), Is.EqualTo(new HashSet<Edge> { call_n1_n3 }));
            Assert.That(AsSet(n1.FromTo(n3, "use")), Is.EqualTo(new HashSet<Edge> { use_n1_n3_a }));
            Assert.That(AsSet(n1.FromTo(n2, "call")), Is.EqualTo(new HashSet<Edge> { call_n1_n2 }));
            Assert.That(AsSet(n1.FromTo(n1, "call")), Is.EqualTo(new HashSet<Edge> { call_n1_n1 }));
            Assert.That(AsSet(n1.FromTo(n2, "use")), Is.EqualTo(new HashSet<Edge>()));
            Assert.That(AsSet(n1.Outgoings), Is.EqualTo(new HashSet<Edge> { call_n1_n1, call_n1_n2, call_n1_n3, use_n1_n3_a }));
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
            Graph g = NewEmptyGraph();

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

            Assert.That(g.NodeCount, Is.EqualTo(nodes.Count));
            Assert.That(g.EdgeCount, Is.EqualTo(edges.Count));

            Assert.That(g.Nodes(), Is.EqualTo(nodes));
            Assert.That(g.Edges(), Is.EqualTo(edges));

            Assert.That(n1.Outgoings,
                        Is.EquivalentTo(new HashSet<Edge> { call_n1_n2, use_n1_n2, call_n1_n3 }));
            Assert.That(n1.Incomings,
                        Is.EquivalentTo(new HashSet<Edge> { call_n3_n1 }));

            Assert.That(n2.Outgoings,
                        Is.EquivalentTo(new HashSet<Edge> { call_n2_n3, use_n2_n3, call_n2_n2, use_n2_n2 }));
            Assert.That(n2.Incomings,
                        Is.EquivalentTo(new HashSet<Edge> { call_n1_n2, use_n1_n2, call_n2_n2, use_n2_n2 }));

            Assert.That(n3.Outgoings,
                        Is.EquivalentTo(new HashSet<Edge> { call_n3_n1 }));
            Assert.That(n3.Incomings,
                        Is.EquivalentTo(new HashSet<Edge> { call_n2_n3, use_n2_n3, call_n1_n3 }));

            // If a node is removed, all its incoming and outgoing edges must
            // be removed, too, and its successors and predecessors must be adjusted, too.

            g.RemoveNode(n2);
            nodes = new HashSet<Node> { n1, n3 };
            edges = new HashSet<Edge> { call_n1_n3, call_n3_n1 };

            Assert.That(g.NodeCount, Is.EqualTo(nodes.Count));
            Assert.That(g.EdgeCount, Is.EqualTo(edges.Count));

            Assert.That(g.Nodes(), Is.EqualTo(nodes));
            Assert.That(g.Edges(), Is.EqualTo(edges));

            Assert.That(n1.Outgoings,
                        Is.EquivalentTo(new HashSet<Edge> { call_n1_n3 }));
            Assert.That(n1.Incomings,
                        Is.EquivalentTo(new HashSet<Edge> { call_n3_n1 }));

            Assert.That(n2.Outgoings,
                        Is.EquivalentTo(new HashSet<Edge>()));
            Assert.That(n2.Incomings,
                        Is.EquivalentTo(new HashSet<Edge>()));

            Assert.That(n3.Outgoings,
                        Is.EquivalentTo(new HashSet<Edge> { call_n3_n1 }));
            Assert.That(n3.Incomings,
                        Is.EquivalentTo(new HashSet<Edge> { call_n1_n3 }));

            // If an edge is removed, it must be removed from the incoming and outgoing
            // edges of its source and target, respectively.
            g.RemoveEdge(call_n3_n1);
            nodes = new HashSet<Node> { n1, n3 };
            edges = new HashSet<Edge> { call_n1_n3 };

            Assert.That(g.NodeCount, Is.EqualTo(nodes.Count));
            Assert.That(g.EdgeCount, Is.EqualTo(edges.Count));

            Assert.That(g.Nodes(), Is.EqualTo(nodes));
            Assert.That(g.Edges(), Is.EqualTo(edges));

            Assert.That(n1.Outgoings,
                        Is.EquivalentTo(new HashSet<Edge> { call_n1_n3 }));
            Assert.That(n1.Incomings,
                        Is.EquivalentTo(new HashSet<Edge>()));

            Assert.That(n3.Outgoings,
                        Is.EquivalentTo(new HashSet<Edge>()));
            Assert.That(n3.Incomings,
                        Is.EquivalentTo(new HashSet<Edge> { call_n1_n3 }));

            // After removing n3, the graph should have only a single node left.
            g.RemoveNode(n3);
            nodes = new HashSet<Node> { n1 };
            edges = new HashSet<Edge>();

            Assert.That(g.NodeCount, Is.EqualTo(nodes.Count));
            Assert.That(g.EdgeCount, Is.EqualTo(edges.Count));

            Assert.That(g.Nodes(), Is.EqualTo(nodes));
            Assert.That(g.Edges(), Is.EqualTo(edges));

            Assert.That(n1.Outgoings,
                        Is.EquivalentTo(new HashSet<Edge>()));
            Assert.That(n1.Incomings,
                        Is.EquivalentTo(new HashSet<Edge>()));

            Assert.That(n3.Outgoings,
                        Is.EquivalentTo(new HashSet<Edge>()));
            Assert.That(n3.Incomings,
                        Is.EquivalentTo(new HashSet<Edge>()));
        }

        [Test]
        public void RemoveOrphansBecomeChildren()
        {
            Graph g = NewEmptyGraph();

            Node r = NewNode(g, "root");
            Node d = Child(g, r, "toBeDeleted");
            Node o1 = Child(g, d, "orphan1");
            Node o2 = Child(g, d, "orphan2");

            g.RemoveNode(d, orphansBecomeRoots: false);

            AssertHasChild(g, parent: r, child: o1);
            AssertHasChild(g, parent: r, child: o2);
            Assert.That(d.ItsGraph, Is.Null);
            Assert.That(d.Parent, Is.Null);
            Assert.That(d.NumberOfChildren(), Is.EqualTo(0));
        }

        [Test]
        public void RemoveOrphansBecomeRoots()
        {
            Graph g = NewEmptyGraph();

            Node r = NewNode(g, "root");
            Node d = Child(g, r, "toBeDeleted");
            Node o1 = Child(g, d, "orphan1");
            Node o2 = Child(g, d, "orphan2");

            g.RemoveNode(d, orphansBecomeRoots: true);

            Assert.That(r.NumberOfChildren(), Is.EqualTo(0));
            Assert.That(o1.Parent, Is.Null);
            Assert.That(o2.Parent, Is.Null);
            Assert.That(d.ItsGraph, Is.Null);
            Assert.That(d.Parent, Is.Null);
            Assert.That(d.NumberOfChildren(), Is.EqualTo(0));
        }

        [Test]
        public void TestReparent()
        {
            string t = "Routine";

            Graph g = NewEmptyGraph();
            Assert.That(g.MaxDepth, Is.EqualTo(0));

            Node a = NewNode(g, "a", t);
            Node b = NewNode(g, "b", t);

            // hierarchy:
            //  a   b
            Assert.That(a.Level, Is.EqualTo(0));
            Assert.That(b.Level, Is.EqualTo(0));
            Assert.That(a.Depth(), Is.EqualTo(1));
            Assert.That(b.Depth(), Is.EqualTo(1));
            Assert.That(g.MaxDepth, Is.EqualTo(1));

            a.Reparent(null);
            // hierarchy:
            //  a   b
            // no change expected
            Assert.That(a.Level, Is.EqualTo(0));
            Assert.That(b.Level, Is.EqualTo(0));
            Assert.That(a.Depth(), Is.EqualTo(1));
            Assert.That(b.Depth(), Is.EqualTo(1));
            Assert.That(g.MaxDepth, Is.EqualTo(1));

            Node bc = Child(g, b, "bc", t);
            // hierarchy:
            //  a   b
            //      |
            //      bc
            Assert.That(a.Level, Is.EqualTo(0));
            Assert.That(b.Level, Is.EqualTo(0));
            Assert.That(bc.Level, Is.EqualTo(1));
            Assert.That(a.Depth(), Is.EqualTo(1));
            Assert.That(b.Depth(), Is.EqualTo(2));
            Assert.That(bc.Depth(), Is.EqualTo(1));
            Assert.That(g.MaxDepth, Is.EqualTo(2));

            bc.Reparent(null);
            // hierarchy:
            //  a   b  bc
            Assert.That(a.Level, Is.EqualTo(0));
            Assert.That(a.Depth(), Is.EqualTo(1));
            Assert.That(b.Level, Is.EqualTo(0));
            Assert.That(b.Depth(), Is.EqualTo(1));
            Assert.That(bc.Level, Is.EqualTo(0));
            Assert.That(bc.Depth(), Is.EqualTo(1));
            Assert.That(g.MaxDepth, Is.EqualTo(1));

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
            Assert.That(a.Level, Is.EqualTo(0));
            Assert.That(a.Depth(), Is.EqualTo(3));
            Assert.That(ac.Level, Is.EqualTo(1));
            Assert.That(ac.Depth(), Is.EqualTo(2));
            Assert.That(acc.Level, Is.EqualTo(2));
            Assert.That(acc.Depth(), Is.EqualTo(1));

            Assert.That(b.Level, Is.EqualTo(0));
            Assert.That(b.Depth(), Is.EqualTo(3));
            Assert.That(bc.Level, Is.EqualTo(1));
            Assert.That(bc.Depth(), Is.EqualTo(2));
            Assert.That(bcc.Level, Is.EqualTo(2));
            Assert.That(bcc.Depth(), Is.EqualTo(1));

            Assert.That(g.MaxDepth, Is.EqualTo(3));
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
            Assert.That(a.Level, Is.EqualTo(0));
            Assert.That(a.Depth(), Is.EqualTo(4));
            Assert.That(ac.Level, Is.EqualTo(1));
            Assert.That(ac.Depth(), Is.EqualTo(3));
            Assert.That(acc.Level, Is.EqualTo(2));
            Assert.That(acc.Depth(), Is.EqualTo(1));
            Assert.That(bc.Level, Is.EqualTo(2));
            Assert.That(bc.Depth(), Is.EqualTo(2));
            Assert.That(bcc.Level, Is.EqualTo(3));
            Assert.That(bcc.Depth(), Is.EqualTo(1));
            Assert.That(b.Level, Is.EqualTo(0));
            Assert.That(b.Depth(), Is.EqualTo(1));
            Assert.That(g.MaxDepth, Is.EqualTo(4));
        }

        /// <summary>
        /// Tests subgraph creation by marking some nodes as relevant and some as irrelevant, then constructing the
        /// subgraph using the given functions.
        /// Note: While for any graph element X, makeRelevant(X) MUST imply isRelevant(x), on the other hand
        /// makeIrrelevant(X) doesn't necessarily have to imply NOT(isRelevant(x)).
        /// For example, SubGraphByNodeType doesn't care about edges, so isRelevant returns true for all edges,
        /// regardless of whether makeIrrelevant had been applied to the edges.
        /// </summary>
        /// <param name="makeRelevant">defines what is relevant</param>
        /// <param name="makeIrrelevant">defines what is irrelevant</param>
        /// <param name="isRelevant">predicate deciding whether a graph element is relevant</param>
        /// <param name="makeSubgraph">function to create the subgraph</param>
        private void TestSubGraphBy(Action<GraphElement> makeRelevant, Action<GraphElement> makeIrrelevant,
                                   Func<GraphElement, bool> isRelevant, Func<Graph, Graph> makeSubgraph)
        {
            // Note: This test is rather imperfect and may be improved in the future.
            Graph g = NewEmptyGraph();

            Node a = NewNode(g, "a");
            makeIrrelevant(a);
            Node b = NewNode(g, "b");
            makeIrrelevant(b);
            Node ba = Child(g, b, "ba");
            makeIrrelevant(ba);
            Node baa = Child(g, ba, "baa");
            makeRelevant(baa);
            Assert.That(isRelevant(baa), Is.True,
                        $"{baa.ID} should be relevant.");
            Node baaa = Child(g, baa, "baaa");
            makeIrrelevant(baaa);
            Node baaaa = Child(g, baaa, "baaaa");
            makeRelevant(baaaa);
            Assert.That(isRelevant(baaaa), Is.True,
                        $"{baaaa.ID} should be relevant.");
            Node bb = Child(g, b, "bb");
            makeRelevant(bb);
            Assert.That(isRelevant(bb), Is.True,
                        $"{bb.ID} should be relevant.");
            Node bba = Child(g, bb, "bba");
            makeRelevant(bba);
            Assert.That(isRelevant(bba), Is.True,
                        $"{bba.ID} should be relevant.");
            Node bbaa = Child(g, bba, "bbaa");
            makeIrrelevant(bbaa);
            Node bc = Child(g, b, "bc");
            makeIrrelevant(bc);
            Node bca = Child(g, bc, "bca");
            makeRelevant(bca);
            Assert.That(isRelevant(bca), Is.True,
                        $"{bca.ID} should be relevant.");
            Node bcaa = Child(g, bca, "bcaa");
            makeRelevant(bcaa);
            Assert.That(isRelevant(bcaa), Is.True,
                        $"{bcaa.ID} should be relevant.");
            Node bcab = Child(g, bca, "bcab");
            makeRelevant(bcab);
            Assert.That(isRelevant(bcab), Is.True,
                        $"{bcab.ID} should be relevant.");
            Node bcb = Child(g, bc, "bcb");
            makeIrrelevant(bcb);
            Node bcba = Child(g, bcb, "bcba");
            makeRelevant(bcba);
            Assert.That(isRelevant(bcba), Is.True,
                        $"{bcba.ID} should be relevant.");
            Node bd = Child(g, b, "bd");
            makeRelevant(bd);
            Assert.That(isRelevant(bd), Is.True,
                        $"{bd.ID} should be relevant.");
            Node bda = Child(g, bd, "bda");
            makeIrrelevant(bda);
            Node bdaa = Child(g, bda, "bdaa");
            makeRelevant(bdaa);
            Assert.That(isRelevant(bdaa), Is.True,
                        $"{bdaa.ID} should be relevant.");
            Node c = NewNode(g, "c");
            makeRelevant(c);
            Assert.That(isRelevant(c), Is.True,
                        $"{c.ID} should be relevant.");
            Node d = NewNode(g, "d");
            makeIrrelevant(d);
            Node da = Child(g, d, "da");
            makeRelevant(da);
            Assert.That(isRelevant(da), Is.True,
                        $"{da.ID} should be relevant.");
            Node e = NewNode(g, "e");
            makeRelevant(e);
            Assert.That(isRelevant(e), Is.True,
                        $"{e.ID} should be relevant.");
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
            Assert.That(isRelevant(e1), Is.True,
                        $"{e1.ID} should be relevant.");
            Edge e2 = NewEdge(g, a, b);
            makeRelevant(e2);
            Assert.That(isRelevant(e2), Is.True,
                        $"{e2.ID} should be relevant.");
            Edge e3 = NewEdge(g, baa, baaa);
            makeRelevant(e3);
            Assert.That(isRelevant(e3), Is.True,
                        $"{e3.ID} should be relevant.");
            Edge e4 = NewEdge(g, baa, bba);
            makeRelevant(e4);
            Assert.That(isRelevant(e4), Is.True,
                        $"{e4.ID} should be relevant.");
            Edge e5 = NewEdge(g, bb, bba);
            makeRelevant(e5);
            Assert.That(isRelevant(e5), Is.True,
                        $"{e5.ID} should be relevant.");
            Edge e6 = NewEdge(g, bbaa, bba);
            makeRelevant(e6);
            Assert.That(isRelevant(e6), Is.True,
                        $"{e6.ID} should be relevant.");
            Edge e7 = NewEdge(g, bcab, bcba);
            makeRelevant(e7);
            Assert.That(isRelevant(e7), Is.True,
                        $"{e7.ID} should be relevant.");
            Edge e8 = NewEdge(g, bdaa, baaa);
            makeRelevant(e8);
            Assert.That(isRelevant(e8), Is.True,
                        $"{e8.ID} should be relevant.");
            Edge e9 = NewEdge(g, bdaa, bd);
            makeRelevant(e9);
            Assert.That(isRelevant(e9), Is.True,
                        $"{e9.ID} should be relevant.");
            Edge e10 = NewEdge(g, bdaa, bdaa);
            makeRelevant(e10);
            Assert.That(isRelevant(e10), Is.True,
                        $"{e10.ID} should be relevant.");
            Edge e11 = NewEdge(g, c, e);
            makeRelevant(e11);
            Assert.That(isRelevant(e11), Is.True,
                        $"{e11.ID} should be relevant.");
            Edge e12 = NewEdge(g, d, d);
            makeRelevant(e12);
            Assert.That(isRelevant(e12), Is.True,
                        $"{e12.ID} should be relevant.");
            Edge e13 = NewEdge(g, ea, d);
            makeRelevant(e13);
            Assert.That(isRelevant(e13), Is.True,
                        $"{e13.ID} should be relevant.");
            Edge e14 = NewEdge(g, bcba, bd);
            makeIrrelevant(e14);

            Graph subgraph = makeSubgraph(g);

            // Nodes in subgraph must be relevant.
            foreach (Node node in subgraph.Nodes())
            {
                Assert.That(isRelevant(node), Is.True,
                            $"{node.ID} should be relevant.");
            }

            foreach (Edge edge in subgraph.Edges())
            {
                Assert.That(isRelevant(edge), Is.True,
                            $"{edge.ID} should be relevant.");
            }

            Assert.That(subgraph.NodeCount, Is.EqualTo(relevantNodes));
            Assert.That(Pendant(subgraph, a), Is.Null);
            Assert.That(Pendant(subgraph, b), Is.Null);
            Assert.That(Pendant(subgraph, ba), Is.Null);
            Node BAA = Pendant(subgraph, baa) as Node;
            Node BB = Pendant(subgraph, bb) as Node;
            Assert.That(Pendant(subgraph, bc), Is.Null);
            Node BCA = Pendant(subgraph, bca) as Node;
            Assert.That(Pendant(subgraph, bcb), Is.Null);
            Node BCBA = Pendant(subgraph, bcba) as Node;
            Node BD = Pendant(subgraph, bd) as Node;
            Node C = Pendant(subgraph, c) as Node;
            Assert.That(Pendant(subgraph, d), Is.Null);
            Node DA = Pendant(subgraph, da) as Node;
            Node E = Pendant(subgraph, e) as Node;
            Node BAAAA = Pendant(subgraph, baaaa) as Node;
            Node BBA = Pendant(subgraph, bba) as Node;
            Node BCAA = Pendant(subgraph, bcaa) as Node;
            Node BCAB = Pendant(subgraph, bcab) as Node;
            Node BDAA = Pendant(subgraph, bdaa) as Node;

            Assert.That(BAA, Is.Not.Null, $"{nameof(BAA)} must not be null.");
            Assert.That(BAA.IsRoot(), Is.True, $"{BAA.ID} should be a root.");

            Assert.That(BB, Is.Not.Null, $"{nameof(BB)} must not be null.");
            Assert.That(BB.IsRoot(), Is.True, $"{BB.ID} should be a root.");

            Assert.That(BCA, Is.Not.Null, $"{nameof(BCA)} must not be null.");
            Assert.That(BCA.IsRoot(), Is.True, $"{BCA.ID} should be a root.");

            Assert.That(BCBA, Is.Not.Null, $"{nameof(BCBA)} must not be null.");
            Assert.That(BCBA.IsRoot(), Is.True, $"{BCBA.ID} should be a root.");

            Assert.That(BD, Is.Not.Null, $"{nameof(BD)} must not be null.");
            Assert.That(BD.IsRoot(), Is.True, $"{BD.ID} should be a root.");

            Assert.That(C, Is.Not.Null, $"{nameof(C)} must not be null.");
            Assert.That(C.IsRoot(), Is.True, $"{C.ID} should be a root.");
            Assert.That(C.IsLeaf(), Is.True, $"{C.ID} should be a leaf.");

            Assert.That(DA, Is.Not.Null, $"{nameof(DA)} must not be null.");
            Assert.That(DA.IsRoot(), Is.True, $"{DA.ID} should be a root.");

            Assert.That(E, Is.Not.Null, $"{nameof(E)} must not be null.");
            Assert.That(E.IsRoot(), Is.True, $"{E.ID} should be a root.");
            Assert.That(E.IsLeaf(), Is.True, $"{E.ID} should be a leaf.");

            Assert.That(BAAAA, Is.Not.Null, $"{nameof(BAAAA)} must not be null.");
            Assert.That(BAAAA.IsLeaf(), Is.True, $"{BAAAA.ID} should be a leaf.");

            Assert.That(BBA, Is.Not.Null, $"{nameof(BBA)} must not be null.");
            Assert.That(BBA.IsLeaf(), Is.True, $"{BBA.ID} should be a leaf.");

            Assert.That(BCAA, Is.Not.Null, $"{nameof(BCAA)} must not be null.");
            Assert.That(BCAA.IsLeaf(), Is.True, $"{BCAA.ID} should be a leaf.");

            Assert.That(BCAB, Is.Not.Null, $"{nameof(BCAB)} must not be null.");
            Assert.That(BCAB.IsLeaf(), Is.True, $"{BCAB.ID} should be a leaf.");

            Assert.That(BCBA, Is.Not.Null, $"{nameof(BCBA)} must not be null.");
            Assert.That(BCBA.IsLeaf(), Is.True, $"{BCBA.ID} should be a leaf.");

            Assert.That(BDAA, Is.Not.Null, $"{nameof(BDAA)} must not be null.");
            Assert.That(BDAA.IsLeaf(), Is.True, $"{BDAA.ID} should be a leaf.");

            AssertHasChild(subgraph, baa, baaaa);
            AssertHasChild(subgraph, bb, bba);
            AssertHasChild(subgraph, bca, bcaa);
            AssertHasChild(subgraph, bca, bcab);
            AssertHasChild(subgraph, bd, bdaa);

            // makeIrrelevant may have no effect, which is why we have to count this way.
            int relevantEdges = new List<Edge> { e0, e1, e2, e3, e4, e5, e6, e7, e8, e9, e10, e11, e12, e13, e14 }.Count(isRelevant);
            // 9 edges are kept.
            // Kept edges: Those for which isRelevant returned true before subgraphing minus four "dangling" ones.
            Assert.That(subgraph.EdgeCount, Is.EqualTo(relevantEdges - 4));
            Assert.That(HasEdge(BAA, BAA), Is.True, $"There must be an edge from {BAA.ID} to {BAA.ID}.");
            Assert.That(HasEdge(BAA, BBA), Is.True, $"There must be an edge from {BAA.ID} to {BBA.ID}.");
            Assert.That(HasEdge(BB, BBA), Is.True, $"There must be an edge from {BB.ID} to {BBA.ID}.");
            Assert.That(HasEdge(BBA, BBA), Is.True, $"There must be an edge from {BBA.ID} to {BBA.ID}.");
            Assert.That(HasEdge(BCAB, BCBA), Is.True, $"There must be an edge from {BCAB.ID} to {BCBA.ID}.");
            Assert.That(HasEdge(BDAA, BAA), Is.True, $"There must be an edge from {BDAA.ID} to {BAA.ID}.");
            Assert.That(HasEdge(BDAA, BDAA), Is.True, $"There must be an edge from {BDAA.ID} to {BDAA.ID}.");
            Assert.That(HasEdge(BDAA, BD), Is.True, $"There must be an edge from {BDAA.ID} to {BD.ID}.");
            Assert.That(HasEdge(C, E), Is.True, $"There must be an edge from {C.ID} to {E.ID}.");
        }

        [Test]
        public void TestSubGraphByNodeType()
        {
            const string r = "relevant";
            const string i = "irrelevant";
            HashSet<string> relevantNodeTypes = new HashSet<string> { r };

            TestSubGraphBy(x => x.Type = r,
                           x => x.Type = i,
                           x => !(x is Node) || relevantNodeTypes.Contains(x.Type),
                           g => g.SubgraphByNodeType(relevantNodeTypes, false));
        }

        [Test]
        public void TestSubGraphByNodeTypeLiftedEdges()
        {
            const string RootType = "Root";
            const string ChildType = "Child";

            Graph g = NewEmptyGraph();

            Node a = NewNode(g, "a", RootType);
            Node a1 = Child(g, a, "a1", ChildType);
            Node a2 = Child(g, a, "a2", ChildType);

            Node b = NewNode(g, "b", RootType);
            Node b1 = Child(g, b, "b1", ChildType);
            Node b2 = Child(g, b, "b2", ChildType);

            Node c = NewNode(g, "c", RootType);
            Node c1 = Child(g, c, "c1", ChildType);

            Node d = NewNode(g, "d", RootType);
            Node d1 = Child(g, d, "d1", ChildType);

            Node e = NewNode(g, "e", RootType);
            Node e1 = Child(g, e, "e1", ChildType);

            Edge v1 = NewEdge(g, a, a);   // original self loop; must be present
            Edge v2 = NewEdge(g, d1, d);  // implies lifted self loop at d
            Edge v3 = NewEdge(g, e, e1);  // implies lifted self loop at e
            Edge v4 = NewEdge(g, a1, c1); // implies lifted edge from a to c
            Edge v5 = NewEdge(g, b1, a2); // implies lifted edge from b to a
            Edge v6 = NewEdge(g, c1, c1); // implies lifted self loop at c
            Edge v7 = NewEdge(g, b2, b1); // implies lifted self loop at b

            {
                // Lifted self loops are not ignored.
                Graph subgraph = g.SubgraphByNodeType(new List<string> { RootType }, false);
                Assert.That(subgraph.NodeCount, Is.EqualTo(g.Nodes().Where(n => n.Type == RootType).Count()));

                Node A = Pendant(subgraph, a) as Node;
                Node B = Pendant(subgraph, b) as Node;
                Node C = Pendant(subgraph, c) as Node;
                Node D = Pendant(subgraph, d) as Node;
                Node E = Pendant(subgraph, e) as Node;

                Assert.That(HasEdge(A, A), Is.True, $"There must be an edge from {A.ID} to {A.ID}.");
                Assert.That(HasEdge(B, B), Is.True, $"There must be an edge from {B.ID} to {B.ID}.");
                Assert.That(HasEdge(C, C), Is.True, $"There must be an edge from {C.ID} to {C.ID}.");
                Assert.That(HasEdge(D, D), Is.True, $"There must be an edge from {D.ID} to {D.ID}.");
                Assert.That(HasEdge(E, E), Is.True, $"There must be an edge from {E.ID} to {E.ID}.");
                Assert.That(HasEdge(A, C), Is.True, $"There must be an edge from {A.ID} to {C.ID}.");
                Assert.That(HasEdge(B, A), Is.True, $"There must be an edge from {B.ID} to {A.ID}.");
                Assert.That(subgraph.EdgeCount, Is.EqualTo(7));
            }
            {
                // Lifted self loops are ignored.
                Graph subgraph = g.SubgraphByNodeType(new List<string> { RootType }, true);
                Assert.That(subgraph.NodeCount, Is.EqualTo(g.Nodes().Where(n => n.Type == RootType).Count()));

                Node A = Pendant(subgraph, a) as Node;
                Node B = Pendant(subgraph, b) as Node;
                Node C = Pendant(subgraph, c) as Node;
                Node D = Pendant(subgraph, d) as Node;
                Node E = Pendant(subgraph, e) as Node;

                Assert.That(HasEdge(A, A), Is.True, $"There must be an edge from {A.ID} to {A.ID}.");
                Assert.That(HasEdge(A, C), Is.True, $"There must be an edge from {A.ID} to {C.ID}.");
                Assert.That(HasEdge(B, A), Is.True, $"There must be an edge from {B.ID} to {A.ID}.");
                Assert.That(subgraph.EdgeCount, Is.EqualTo(3));
            }
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
            Graph g = NewEmptyGraph();
            Node a = NewNode(g, "a");
            SubgraphMemento subgraph = a.DeleteTree();
            Assert.That(a.ItsGraph, Is.Null);
            Assert.That(g.NodeCount, Is.EqualTo(0));
            Assert.That(g.EdgeCount, Is.EqualTo(0));

            subgraph.Restore();
            Assert.That(a.ItsGraph, Is.SameAs(g));
            Assert.That(g.NodeCount, Is.EqualTo(1));
            Assert.That(g.EdgeCount, Is.EqualTo(0));
        }

        /// <summary>
        /// Deleting and restoring a subtree consisting of only a single node
        /// and a self loop.
        /// </summary>
        [Test]
        public void TestDeleteTreeSingleNodeAndEdge()
        {
            Graph g = NewEmptyGraph();
            Node a = NewNode(g, "a");
            Edge e = NewEdge(g, a, a);
            SubgraphMemento subgraph = a.DeleteTree();
            Assert.That(a.ItsGraph, Is.Null);
            Assert.That(e.ItsGraph, Is.Null);
            Assert.That(g.NodeCount, Is.EqualTo(0));
            Assert.That(g.EdgeCount, Is.EqualTo(0));

            subgraph.Restore();
            Assert.That(a.ItsGraph, Is.SameAs(g));
            Assert.That(e.ItsGraph, Is.SameAs(g));
            Assert.That(g.NodeCount, Is.EqualTo(1));
            Assert.That(g.EdgeCount, Is.EqualTo(1));
        }

        /// <summary>
        /// Deleting and restoring a subtree consisting of multiple nested
        /// nodes and several incoming, outgoing, and internal edges in
        /// the node hierarchy to be deleted.
        /// </summary>
        [Test]
        public void TestDeleteTree()
        {
            Graph g = NewEmptyGraph();
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
            Assert.That(a.ItsGraph, Is.SameAs(g));
            Assert.That(b.ItsGraph, Is.SameAs(g));
            foreach (Node node in subgraphNodes)
            {
                Assert.That(node.ItsGraph, Is.Null);
            }

            // e1 and e2 are still in the graph, but all other edges are removed
            Assert.That(e1.ItsGraph, Is.SameAs(g));
            Assert.That(e2.ItsGraph, Is.SameAs(g));
            foreach (Edge edge in subgraphEdges)
            {
                Assert.That(edge.ItsGraph, Is.Null);
            }

            Assert.That(g.NodeCount, Is.EqualTo(2));
            Assert.That(g.EdgeCount, Is.EqualTo(2));

            subgraph.Restore();
            Assert.That(a.ItsGraph, Is.SameAs(g));
            Assert.That(b.ItsGraph, Is.SameAs(g));
            foreach (Node node in subgraphNodes)
            {
                Assert.That(node.ItsGraph, Is.SameAs(g));
            }

            Assert.That(e1.ItsGraph, Is.SameAs(g));
            Assert.That(e2.ItsGraph, Is.SameAs(g));
            foreach (Edge edge in subgraphEdges)
            {
                Assert.That(edge.ItsGraph, Is.SameAs(g));
            }

            Assert.That(g.NodeCount, Is.EqualTo(subgraphNodes.Count + 2));
            Assert.That(g.EdgeCount, Is.EqualTo(subgraphEdges.Count + 2));
        }

        /// <summary>
        /// Tests <see cref="Graph.SubgraphBy"/>.
        /// </summary>
        private void TestSubgraph<T>() where T : Graph, new()
        {
            const string floatAttribute = "float";
            const string intAttribute = "int";
            const string stringAttribute = "string";
            const string toggleAttribute = "toggle";

            const float floatAttributeValue = 2.0f;
            const int intAttributeValue = 1;
            const string stringAttributeValue = "hello, world";
            const bool toggleAttributeValue = true;

            Graph g = new T();
            g.SetFloat(floatAttribute, floatAttributeValue);
            g.SetInt(intAttribute, intAttributeValue);
            g.SetString(stringAttribute, stringAttributeValue);
            g.SetToggle(toggleAttribute, toggleAttributeValue);
            Node a = NewNode(g, "a", "Routine");
            Node b = NewNode(g, "b", "Field");
            Edge e1 = NewEdge(g, a, b, "call");
            Edge e2 = NewEdge(g, a, b, "set");

            Graph sg = g.SubgraphBy(x => x is Node || (x is Edge e && e.Type == "set"));
            Assert.That(sg.GetFloat(floatAttribute), Is.EqualTo(floatAttributeValue));
            Assert.That(sg.GetInt(intAttribute), Is.EqualTo(intAttributeValue));
            Assert.That(sg.GetString(stringAttribute), Is.EqualTo(stringAttributeValue));
            Assert.That(sg.HasToggle(toggleAttribute), Is.True,
                        $"{sg.Name} has no toggle {toggleAttribute}.");

            Assert.That(sg.NodeCount, Is.EqualTo(2));
            Assert.That(sg.EdgeCount, Is.EqualTo(1));
        }

        [Test]
        public void TestSubgraphGraph()
        {
            TestSubgraph<Graph>();
        }

        [Test]
        public void TestSubgraphReflexionGraph()
        {
            TestSubgraph<ReflexionGraph>();
        }
    }
}