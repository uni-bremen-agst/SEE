using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace SEE.DataModel
{
    /// <summary>
    /// Unit tests for Graph.
    /// </summary>
    internal class TestGraph
    {
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
            Edge use_n1_n3_a  = NewEdge(g, n1, n3, "use");
            Assert.AreEqual(new HashSet<Edge>() { call_n1_n1, call_n1_n2, call_n1_n3, use_n1_n3_a }, AsSet(n1.Outgoings));
            Edge use_n1_n3_b = NewEdge(g, n1, n3, "use");
            Assert.AreEqual(new HashSet<Edge>() { call_n1_n1, call_n1_n2, call_n1_n3, use_n1_n3_a, use_n1_n3_b }, AsSet(n1.Outgoings));

            Assert.AreEqual(new HashSet<Edge>(), AsSet(n1.From_To(n3, "none")));
            Assert.AreEqual(new HashSet<Edge>() { call_n1_n3 }, AsSet(n1.From_To(n3, "call")));
            Assert.AreEqual(new HashSet<Edge>() { use_n1_n3_a, use_n1_n3_b }, AsSet(n1.From_To(n3, "use")));

            Edge call_n2_n3 = NewEdge(g, n2, n3, "call");

            Edge call_n2_n2 = NewEdge(g, n3, n2, "call");

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
            HashSet<Edge> result = new HashSet<Edge>(edges);
            return result;
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
            Edge use_n1_n2  = NewEdge(g, n1, n2, "use");

            Edge call_n2_n3 = NewEdge(g, n2, n3, "call");
            Edge use_n2_n3  = NewEdge(g, n2, n3, "use");
            Edge call_n2_n2 = NewEdge(g, n2, n2, "call");
            Edge use_n2_n2  = NewEdge(g, n2, n2, "use");

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
            edges = new HashSet<Edge>() { call_n1_n3, call_n3_n1};

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
    }
}