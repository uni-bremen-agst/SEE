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
    }
}