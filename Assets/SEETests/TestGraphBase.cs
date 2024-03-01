using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace SEE.DataModel.DG
{
    /// <summary>
    /// Super class of tests for <see cref="Graph"/>.
    /// </summary>
    internal abstract class TestGraphBase
    {
        /// <summary>
        /// Name of the toggle attribute.
        /// </summary>
        protected const string ToggleAttribute = "Toggle";
        /// <summary>
        /// Name of the float attribute.
        /// </summary>
        protected const string FloatAttribute = "Float";
        /// <summary>
        /// Name of the int attribute.
        /// </summary>
        protected const string IntAttribute = "Int";
        /// <summary>
        /// Name of the string attribute.
        /// </summary>
        protected const string StringAttribute = "String";

        /// <summary>
        /// Creates and returns a new node to <paramref name="graph"/>.
        /// </summary>
        /// <param name="graph">where to add the node</param>
        /// <param name="id">unique ID of the new node</param>
        /// <param name="type">type of the new node</param>
        /// <returns>a new node added to <paramref name="graph"/></returns>
        protected static Node NewNode(Graph graph, string id, string type = "Routine",
            string directory = null, string filename = null, int? line = null, int? length = null)
        {
            Node result = new()
            {
                SourceName = id,
                ID = id,
                Type = type,
                Directory = directory,
                Filename = filename,
                SourceLine = line,
                SourceLength = length
            };

            graph.AddNode(result);
            return result;
        }

        /// <summary>
        /// Creates and returns a new node to <paramref name="graph"/> as a child of <paramref name="parent"/>.
        /// </summary>
        /// <param name="graph">where to add the node</param>
        /// <param name="parent">the parent of the new node; must not be null</param>
        /// <param name="id">unique ID of the new node</param>
        /// <param name="type">type of the new node</param>
        /// <returns>a new node added to <paramref name="graph"/></returns>
        protected static Node Child(Graph graph, Node parent, string id, string type = "Routine",
            string directory = null, string filename = null, int? line = null, int? length = null)
        {
            Node child = NewNode(graph, id, type, directory, filename, line, length);
            parent.AddChild(child);
            return child;
        }

        /// <summary>
        /// Creates and returns a new edge to <paramref name="graph"/>.
        /// </summary>
        /// <param name="graph">where to add the edge</param>
        /// <param name="from">source of the new edge</param>
        /// <param name="to">target of the new edge</param>
        /// <param name="type">type of the new edge</param>
        /// <returns>a new edge added to <paramref name="graph"/></returns>
        protected static Edge NewEdge(Graph graph, Node from, Node to, string type = "call")
        {
            Edge result = new(from, to, type);
            graph.AddEdge(result);
            return result;
        }

        /// <summary>
        /// Returns the node in <paramref name="graph"/> that has the same ID as <paramref name="node"/>.
        /// May be null if there is no such node.
        ///
        /// This function allows to relate "logically identical" nodes, that is, nodes that
        /// are contained in two graphs having the same ID.
        /// </summary>
        /// <param name="graph">graph where to look up the ID of <paramref name="node"/></param>
        /// <param name="node">node whose counterpart in <paramref name="graph"/> is requested</param>
        /// <returns>node in <paramref name="graph"/> that has the same ID as <paramref name="node"/> or null</returns>
        protected GraphElement Pendant(Graph graph, GraphElement node) => node is Node ? graph.GetNode(node.ID) : graph.GetEdge(node.ID);

        /// <summary>
        /// Returns true if <paramref name="source"/> has an outgoing edge with given <paramref name="target"/>
        /// and <paramref name="edgeType"/>.
        /// </summary>
        /// <param name="source">source of the checked edge</param>
        /// <param name="target">target of the checked edge</param>
        /// <param name="edgeType">edge type of the checked edge</param>
        /// <returns></returns>
        protected bool HasEdge(Node source, Node target, string edgeType = null)
        {
            return source.Outgoings.Any(outgoing => outgoing.Target == target && (edgeType == null || outgoing.Type == edgeType));
        }

        /// <summary>
        /// Checks whether the <see cref="Pendant"/> of <paramref name="parent"/>
        /// in <paramref name="graph"/> and the parent of the <see cref="Pendant"/>
        /// of <paramref name="child"/> are the same, in other words, whether
        /// <paramref name="graph"/> has the equivalent parentship.
        /// </summary>
        /// <param name="graph">graph in which to look up the <see cref="Pendant"/>s</param>
        /// <param name="parent">parent node</param>
        /// <param name="child">child node</param>
        protected void AssertHasChild(Graph graph, Node parent, Node child)
        {
            Assert.AreSame(Pendant(graph, parent), (Pendant(graph, child) as Node).Parent);
        }

        /// <summary>
        /// Creates a new graph with default basepath and graph name.
        /// It will have no nodes or edges.
        /// </summary>
        /// <param name="viewName">the name of the graph</param>
        /// <param name="basePath">the basepath of the graph for looking up the source code files</param>
        /// <returns>new graph</returns>
        protected static Graph NewEmptyGraph(string viewName = "CodeFacts", string basePath = "DUMMYBASEPATH")
        {
            return new Graph(basePath, viewName);
        }

        /// <summary>
        /// Returns <paramref name="edges"/> as a set.
        /// </summary>
        /// <param name="edges">edges to be returned</param>
        /// <returns><paramref name="edges"/> as a set</returns>
        protected static ISet<Edge> AsSet(IEnumerable<Edge> edges)
        {
            return new HashSet<Edge>(edges);
        }
    }
}