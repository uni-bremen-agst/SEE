using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

namespace SEE.DataModel.DG
{
    /// <summary>
    /// A graph with nodes and edges representing the data to be visualized
    /// by way of blocks and connections.
    /// </summary>
    public class Graph : Attributable
    {
        // The list of graph nodes indexed by their unique IDs
        private Dictionary<string, Node> nodes = new Dictionary<string, Node>();

        // The list of graph edges indexed by their unique IDs.
        private Dictionary<string, Edge> edges = new Dictionary<string, Edge>();

        // The (view) name of the graph.

        /// <summary>
        /// Name of the artificial node type used for artificial root nodes added
        /// when we do not have a real node type derived from the input graph.
        /// </summary>
        public const string UnknownType = "";

        /// <summary>
        /// Indicates whether the node hierarchy has changed and, hence,
        /// the node levels and roots need to be recalculated. Rather than
        /// re-calculating the levels and roots each time a new node is
        /// added, we will re-calculate them only on demand, that is,
        /// if the node levels or roots are requested. This may save time.
        ///
        /// Note: This attribute should be set only by <see cref="Node"/>.
        /// </summary>
        public bool NodeHierarchyHasChanged = true;

        private int maxDepth = -1;

        /// <summary>
        /// The maximal depth of the node hierarchy. The maximal depth is the
        /// maximal length of all paths from any of the roots to their leaves
        /// where the length of a path is defined by the number of nodes on this
        /// path. The empty graph has maximal depth 0.
        ///
        /// Important note: This value must be computed by calling FinalizeGraph()
        /// before accessing <see cref="MaxDepth"/>.
        /// </summary>
        public int MaxDepth
        {
            get
            {
                if (NodeHierarchyHasChanged)
                {
                    FinalizeNodeHierarchy();
                }

                return maxDepth;
            }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="name">name of the graph</param>
        public Graph(string name = "")
        {
            this.Name = name;
        }

        /// Adds a node to the graph.
        /// Preconditions:
        ///   (1) node must not be null
        ///   (2) node.ID must be defined.
        ///   (3) a node with node.ID must not have been added before
        ///   (4) node must not be contained in another graph
        /// </summary>
        public void AddNode(Node node)
        {
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            if (string.IsNullOrEmpty(node.ID))
            {
                throw new ArgumentException("ID of a node must neither be null nor empty.");
            }

            if (nodes.ContainsKey(node.ID))
            {
                throw new InvalidOperationException($"ID '{node.ID}' is not unique\n: {node}. \nDuplicate already in graph: {nodes[node.ID]}.");
            }

            if (node.ItsGraph != null)
            {
                throw new InvalidOperationException($"Node {node.ID} is already in a graph {node.ItsGraph.Name}.");
            }

            nodes[node.ID] = node;
            node.ItsGraph = this;
            NodeHierarchyHasChanged = true;
        }

        /// <summary>
        /// Removes the given node from the graph. Its incoming and outgoing edges are removed
        /// along with it.
        ///
        /// If <paramref name="orphansBecomeRoots"/> is true, the children of <paramref name="node"/>
        /// become root nodes. Otherwise they become children of the parent of <paramref name="node"/>
        /// if there is a parent.
        ///
        /// Precondition: node must not be null and must be contained in this graph.
        /// </summary>
        /// <param name="node">node to be removed</param>
        /// <param name="orphansBecomeRoots">if true, the children of <paramref name="node"/> become root nodes;
        /// otherwise they become children of the parent of <paramref name="node"/> (if any)</param>
        public void RemoveNode(Node node, bool orphansBecomeRoots = true)
        {
            if (node == null)
            {
                throw new Exception("A node to be removed from a graph must not be null.");
            }

            if (node.ItsGraph != this)
            {
                if (node.ItsGraph == null)
                {
                    throw new Exception($"Node {node} is not contained in any graph.");
                }

                throw new Exception($"Node {node} is contained in a different graph {node.ItsGraph.Name}.");
            }

            if (nodes.Remove(node.ID))
            {
                // The edges of node are stored in the node's data structure as well as
                // in the node's neighbor's data structure.
                foreach (Edge outgoing in node.Outgoings)
                {
                    Node successor = outgoing.Target;
                    successor.RemoveIncoming(outgoing);
                    edges.Remove(outgoing.ID);
                    outgoing.ItsGraph = null;
                }

                foreach (Edge incoming in node.Incomings)
                {
                    Node predecessor = incoming.Source;
                    predecessor.RemoveOutgoing(incoming);
                    edges.Remove(incoming.ID);
                    incoming.ItsGraph = null;
                }

                // Adjust the node hierarchy.
                if (node.NumberOfChildren() > 0)
                {
                    Reparent(node.Children().ToArray(),
                             orphansBecomeRoots ? null : node.Parent);
                }

                node.Reset();
                NodeHierarchyHasChanged = true;
            }
            else
            {
                throw new Exception($"Node {node} is not contained in this graph {Name}.");
            }

            /// <summary>
            /// Reparents all <paramref name="children"/> to new <paramref name="parent"/>.
            /// </summary>
            /// <param name="children">children to be re-parented</param>
            /// <param name="parent">new parent of <see cref="children"/></param>
            void Reparent(Node[] children, Node parent)
            {
                foreach (Node child in children)
                {
                    child.Reparent(parent);
                }
            }
        }

        /// <summary>
        /// Returns true if this graph contains a node with the same unique ID
        /// as the given node.
        /// Throws an exception if node is null or node has no valid ID.
        /// </summary>
        /// <param name="node">node to be checked for containment</param>
        /// <returns>true iff there is a node contained in the graph with node.ID</returns>
        public bool ContainsNode(Node node)
        {
            if (ReferenceEquals(node, null))
            {
                throw new ArgumentNullException(nameof(node));
            }

            if (string.IsNullOrEmpty(node.ID))
            {
                throw new ArgumentException("ID of a node must neither be null nor empty");
            }

            return nodes.ContainsKey(node.ID);
        }

        /// <summary>
        /// Returns true if this graph contains an edge with the same unique ID
        /// as the given edge.
        /// Throws an exception if <paramref name="edge"/> is null or has no valid ID.
        /// </summary>
        /// <param name="edge">edge to be checked for containment</param>
        /// <returns>true iff there is an edge contained in the graph with the same ID</returns>
        public bool ContainsEdge(Edge edge)
        {
            if (ReferenceEquals(edge, null))
            {
                throw new ArgumentNullException(nameof(edge));
            }

            if (string.IsNullOrEmpty(edge.ID))
            {
                throw new ArgumentException("ID of an edge must neither be null nor empty");
            }

            return edges.ContainsKey(edge.ID);
        }

        /// <summary>
        /// If the graph has only a single root, nothing happens. Otherwise
        /// all current roots become an immediate child of a newly added
        /// root node with given <paramref name="name"/> and <paramref name="type"/>.
        /// </summary>
        /// <param name="name">ID of new root node</param>
        /// <param name="type">type of new root node</param>
        public void AddSingleRoot(string name, string type)
        {
            List<Node> roots = GetRoots();
            if (roots.Count > 0)
            {
                Node newRoot = new Node { SourceName = name, ID = name, Type = type };
                AddNode(newRoot);
                foreach (Node oldRoot in roots)
                {
                    newRoot.AddChild(oldRoot);
                }

                NodeHierarchyHasChanged = true;
            }
        }

        /// <summary>
        /// Returns the node with the given unique ID if it exists; otherwise null.
        /// </summary>
        /// <param name="ID">unique ID</param>
        /// <returns>node with the given unique ID if it exists; otherwise null</returns>
        public Node GetNode(string ID)
        {
            if (string.IsNullOrEmpty(ID))
            {
                throw new ArgumentException("ID must neither be null nor empty");
            }

            if (nodes.TryGetValue(ID, out Node node))
            {
                return node;
            }

            return null;
        }

        /// <summary>
        /// Returns the edge with the given unique ID if it exists; otherwise null.
        /// </summary>
        /// <param name="ID">unique ID</param>
        /// <returns>edge with the given unique ID if it exists; otherwise null</returns>
        public Edge GetEdge(string ID)
        {
            if (string.IsNullOrEmpty(ID))
            {
                throw new ArgumentException("ID must neither be null nor empty");
            }

            if (edges.TryGetValue(ID, out Edge edge))
            {
                return edge;
            }

            return null;
        }

        /// <summary>
        /// Adds a non-hierarchical edge to the graph.
        /// Preconditions:
        /// (1) edge must not be null.
        /// (2) its source and target nodes must be in the graph already
        /// (3) the edge must not be in any other graph
        /// </summary>
        public void AddEdge(Edge edge)
        {
            if (ReferenceEquals(edge, null))
            {
                throw new ArgumentNullException(nameof(edge));
            }

            if (ReferenceEquals(edge.Source, null) || ReferenceEquals(edge.Target, null))
            {
                throw new ArgumentException("Source/target of this edge is null.");
            }

            if (ReferenceEquals(edge.ItsGraph, null))
            {
                if (edge.Source.ItsGraph != this)
                {
                    throw new InvalidOperationException($"Source node {edge.Source} is not in the graph.");
                }

                if (edge.Target.ItsGraph != this)
                {
                    throw new InvalidOperationException($"Target node {edge.Target} is not in the graph.");
                }

                if (edges.ContainsKey(edge.ID))
                {
                    throw new InvalidOperationException($"There is already an edge with the ID {edge.ID}.");
                }

                edge.ItsGraph = this;
                edges[edge.ID] = edge;
                edge.Source.AddOutgoing(edge);
                edge.Target.AddIncoming(edge);
            }
            else
            {
                throw new Exception($"Edge {edge} is already in a graph {edge.ItsGraph.Name}.");
            }
        }

        /// <summary>
        /// Removes the given edge from the graph.
        /// </summary>
        /// <param name="edge">edge to be removed</param>
        public void RemoveEdge(Edge edge)
        {
            if (ReferenceEquals(edge, null))
            {
                throw new ArgumentNullException(nameof(edge));
            }

            if (edge.ItsGraph != this)
            {
                if (ReferenceEquals(edge.ItsGraph, null))
                {
                    throw new ArgumentException($"Edge {edge} is not contained in any graph");
                }

                throw new ArgumentException($"Edge {edge} is contained in a different graph {edge.ItsGraph.Name}.");
            }

            if (!edges.ContainsKey(edge.ID))
            {
                throw new InvalidOperationException($"Edge {edge} is not contained in graph {Name}.");
            }

            edge.Source.RemoveOutgoing(edge);
            edge.Target.RemoveIncoming(edge);
            edges.Remove(edge.ID);
            edge.ItsGraph = null;
        }

        /// <summary>
        /// The number of nodes of the graph.
        /// </summary>
        public int NodeCount => nodes.Count;

        /// <summary>
        /// The number of edges of the graph.
        /// </summary>
        public int EdgeCount => edges.Count;

        /// <summary>
        /// Name of the graph (the view name of the underlying RFG).
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The path of the file from which this graph was loaded. Could be the
        /// empty string if the graph was not created by loading it from disk.
        /// </summary>
        public string Path { get; set; } = "";

        /// <summary>
        /// Returns all nodes of the graph.
        /// </summary>
        /// <returns>all nodes</returns>
        public List<Node> Nodes()
        {
            return nodes.Values.ToList();
        }

        /// <summary>
        /// Returns all non-hierarchical edges of the graph.
        /// </summary>
        /// <returns>all non-hierarchical edges</returns>
        public List<Edge> Edges()
        {
            return edges.Values.ToList();
        }

        /// <summary>
        /// Returns true if a node with the given <paramref name="ID"/> is part of the graph.
        /// </summary>
        /// <param name="ID">unique ID of the node searched</param>
        /// <returns>true if a node with the given <paramref name="ID"/> is part of the graph</returns>
        public bool ContainsNodeID(string ID)
        {
            return nodes.ContainsKey(ID);
        }

        /// <summary>
        /// Returns the node with the given unique ID. If there is no
        /// such node, node will be null and false will be returned; otherwise
        /// true will be returned.
        /// </summary>
        /// <param name="ID">unique ID of the searched node</param>
        /// <param name="node">the found node, otherwise null</param>
        /// <returns>true if a node could be found</returns>
        public bool TryGetNode(string ID, out Node node)
        {
            return nodes.TryGetValue(ID, out node);
        }

        /// <summary>
        /// The list of root nodes of this graph. Must be re-computed
        /// whenever nodeHierarchyHasChanged becomes true.
        /// </summary>
        private readonly List<Node> roots = new List<Node>();

        /// <summary>
        /// Returns the list of nodes without parent.
        /// </summary>
        /// <returns>root nodes of the hierarchy</returns>
        public List<Node> GetRoots()
        {
            if (NodeHierarchyHasChanged)
            {
                FinalizeNodeHierarchy();
            }

            return roots;
#if false
            // FIXME this method is called often, which is why it should be more performant.
            // firstly, the result of this query could be cached to improve performance. this is
            // only possible, if the graph is final and nodes do not change parents anymore.
            // moreover, we could count the number of root nodes, create a list with appropriate
            // capacity and then fill it. then, we could also use an array. this reduces the number
            // of mallocs and potentially improves performance. profile the implementation below

            Node[] values = nodes.Values.ToArray();
            int capacity = 0;
            int count = values.Length;
            for (int i = 0; i < count; i++)
            {
                if (values[i].IsRoot())
                {
                    capacity++;
                }
            }
            List<Node> result = new List<Node>(capacity);
            for (int i = 0; i < count; i++)
            {
                if (values[i].IsRoot())
                {
                    result.Add(values[i]);
                }
            }
            return result;
//#else
            List<Node> result = new List<Node>();
            foreach (Node node in nodes.Values)
            {
                if (node.IsRoot())
                {
                    result.Add(node);
                }
            }
            return result;
#endif
        }

        /// <summary>
        /// Dumps the hierarchy for each root. Used for debugging.
        /// </summary>
        public void DumpTree()
        {
            foreach (Node root in GetRoots())
            {
                DumpTree(root);
            }
        }

        /// <summary>
        /// Dumps the hierarchy for given root. Used for debugging.
        /// </summary>
        internal void DumpTree(Node root)
        {
            DumpTree(root, 0);
        }

        /// <summary>
        /// Dumps the hierarchy for given root by adding level many -
        /// as indentation. Used for debugging.
        /// </summary>
        private static void DumpTree(Node root, int level)
        {
            string indentation = "";
            for (int i = 0; i < level; i++)
            {
                indentation += "-";
            }

            Debug.Log(indentation + root.ID + "\n");
            foreach (Node child in root.Children())
            {
                DumpTree(child, level + 1);
            }
        }

        /// <summary>
        /// Destroys the GameObjects of the graph's nodes and edges including the
        /// associated Node and Edge components as well as the GameObject of the graph
        /// itself (and its Graph component). The graph is unusable afterward.
        /// </summary>
        public void Destroy()
        {
            edges.Clear();
            nodes.Clear();
        }

        /// <summary>
        /// Sorts the list of children of all nodes using the given comparison.
        /// </summary>
        /// <param name="comparison">the comparison used to sort the nodes in the hierarchy</param>
        public void SortHierarchy(Comparison<Node> comparison)
        {
            foreach (Node node in nodes.Values)
            {
                node.SortChildren(comparison);
            }
        }

        /// <summary>
        /// Sorts the list of children of all nodes using Node.CompareTo(), which compares the
        /// nodes by their names (either Source.Name or ID).
        /// </summary>
        public void SortHierarchyByName()
        {
            SortHierarchy(Node.CompareTo);
        }

        /// <summary>
        /// Merges the <paramref name="other"/> graph into this one.
        /// This is done by copying the attributes, nodes, and edges from the other graph into this one.
        /// For the nodes and edges of the other graph, we append the given <paramref name="nodeIdSuffix"/>
        /// or <paramref name="edgeIdSuffix"/> to avoid any collisions. In case a given id suffix is <c>null</c>,
        /// two nodes with the same ID will be merged into one, combining the attributes of them, unless they
        /// both have an attribute whose value differs between them, in which case an exception will be thrown.
        /// The hierarchy of the nodes will be preserved.
        ///
        /// Deep copies are used for nodes, edges, and the graph itself, so that neither this nor the
        /// <paramref name="other"/> graph (nor their nodes and edges) will be changed in any way.
        ///
        /// Iff an attribute from the <paramref name="other"/> graph has the same name as an attribute from
        /// this graph, the other graph's attribute will be ignored and this graph's attribute will be used.
        /// Since we can't differentiate whether a property has been intentionally unset when it's a toggle attribute,
        /// we ignore the toggle attributes of the other graph.
        ///
        /// Pre-conditions:
        /// <ul>
        /// <li> The <paramref name="other"/> graph must not be <c>null</c>.</li>
        /// <li> The <paramref name="nodeIdSuffix"/> must be chosen such that by appending it to each node's ID
        /// from the <paramref name="other"/> graph, no collision with any node's ID from this graph will occur. If
        /// <paramref name="nodeIdSuffix"/> is <c>null</c> and an ID collision occurs, an exception will be thrown if
        /// the two nodes can't be merged into one (i.e., if they have a differing attribute).</li>
        /// <li> The <paramref name="edgeIdSuffix"/> must be chosen such that by appending it to each edge's ID
        /// from the <paramref name="other"/> graph, no ID collision with any edge's ID from this graph will occur. If
        /// <paramref name="edgeIdSuffix"/> is <c>null</c> and an ID collision occurs, an exception will be thrown.</li>
        /// </ul>
        /// </summary>
        /// <param name="other">The graph whose attributes, nodes and edges are to be copied into this one</param>
        /// <param name="nodeIdSuffix">String suffixed to the ID of the <paramref name="other"/> graph's nodes</param>
        /// <param name="edgeIdSuffix">String suffixed to the ID of the <paramref name="other"/> graph's edges</param>
        /// <returns>The result from merging the <paramref name="other"/> graph into this one</returns>
        /// <exception cref="ArgumentNullException">If <paramref name="other"/> is <c>null</c></exception>
        public Graph MergeWith(Graph other, string nodeIdSuffix = null, string edgeIdSuffix = null)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            // We need to create two copies because we'll mess with the graph's nodes and edges,
            // and don't want to leave a mangled mess.
            Graph mergedGraph = Clone() as Graph;
            Assert.IsNotNull(mergedGraph);
            Graph otherGraph = other.Clone() as Graph;
            Assert.IsNotNull(otherGraph);

            // Name and Path are implicitly taken from this graph.
            // We now merge the other's attributes into this graph's attributes (ignoring toggle attributes).
            foreach (KeyValuePair<string, float> attribute in otherGraph.FloatAttributes)
            {
                if (!mergedGraph.FloatAttributes.ContainsKey(attribute.Key))
                {
                    mergedGraph.FloatAttributes[attribute.Key] = attribute.Value;
                }
            }

            foreach (KeyValuePair<string, int> attribute in otherGraph.IntAttributes)
            {
                if (!mergedGraph.IntAttributes.ContainsKey(attribute.Key))
                {
                    mergedGraph.IntAttributes[attribute.Key] = attribute.Value;
                }
            }

            foreach (KeyValuePair<string, string> attribute in otherGraph.StringAttributes)
            {
                if (!mergedGraph.StringAttributes.ContainsKey(attribute.Key))
                {
                    mergedGraph.StringAttributes[attribute.Key] = attribute.Value;
                }
            }

            // Copy edges and nodes along with their hierarchy
            otherGraph.CopyNodesTo(mergedGraph, nodeIdSuffix);
            otherGraph.CopyEdgesTo(mergedGraph, edgeIdSuffix);
            CopyHierarchy(otherGraph, mergedGraph, nodeIdSuffix);

            // Finalize hierarchy now that it's changed
            mergedGraph.NodeHierarchyHasChanged = true;
            mergedGraph.FinalizeNodeHierarchy();

            return mergedGraph;
        }

        /// <summary>
        /// Returns all edges of graph whose source and target is contained in
        /// selectedNodes.
        /// </summary>
        /// <param name="graph">graph whose edges are to be filtered</param>
        /// <param name="selectedNodes">source and target nodes of required edges</param>
        /// <returns>all edges of graph whose source and target is contained in selectedNodes</returns>
        public IList<Edge> ConnectingEdges(IEnumerable<Node> selectedNodes)
        {
            HashSet<Node> nodeSet = new HashSet<Node>(selectedNodes);

            return Edges().Where(edge => nodeSet.Contains(edge.Source) && nodeSet.Contains(edge.Target)).ToList();
        }

        /// <summary>
        /// Returns the maximal depth of the node hierarchy among the given <paramref name="nodes"/>
        /// plus the given <paramref name="currentDepth"/>.
        /// </summary>
        /// <param name="nodes">nodes for which to determine the depth</param>
        /// <param name="currentDepth">the current depth of the given <paramref name="nodes"/></param>
        /// <returns></returns>
        private static int CalcMaxDepth(IEnumerable<Node> nodes, int currentDepth)
        {
            return nodes.Select(node => CalcMaxDepth(node.Children(), currentDepth + 1))
                        .Prepend(currentDepth + 1).Max();
        }

        /// <summary>
        /// Returns the graph in a JSON-like format including its attributes and all its nodes
        /// and edges including their attributes.
        /// </summary>
        /// <returns>graph in textual form</returns>
        public override string ToString()
        {
            string result = "{\n";
            result += " \"kind\": graph,\n";
            result += $" \"name\": \"{Name}\",\n";
            // its own attributes
            result += base.ToString();
            // its nodes
            foreach (Node node in nodes.Values)
            {
                result += $"{node},\n";
            }

            foreach (Edge edge in edges.Values)
            {
                result += $"{edge},\n";
            }

            result += "}\n";
            return result;
        }

        /// <summary>
        /// Sets the level of each node in the graph. The level of a root node is 0.
        /// For all other nodes, the level is the level of its parent + 1.
        ///
        /// Precondition: <see cref="roots"/> is up to date.
        /// </summary>
        private void CalculateLevels()
        {
            foreach (Node root in roots)
            {
                root.SetLevel(0);
            }
        }

        /// <summary>
        /// Sets the levels of all nodes and the maximal depth of the graph.
        ///
        /// Note: This method should be called only by <see cref="Node"/> and
        /// <see cref="SEE.DataModel.DG.IO.GraphReader"/>.
        /// </summary>
        public void FinalizeNodeHierarchy()
        {
            GatherRoots();
            CalculateLevels();
            maxDepth = CalcMaxDepth(roots, -1);
            NodeHierarchyHasChanged = false;
            /// Note: SetLevelMetric just propagates the newly calculated node levels
            /// to the metric attribute. It can be called when that level is known.
            /// It must be called after NodeHierarchyHasChanged has been set to false,
            /// otherwise we will run into an endless loop because querying the level
            /// attribute will trigger <see cref="FinalizeNodeHierarchy"/> again.
            SetLevelMetric();
        }

        /// <summary>
        /// Recalculates <see cref="roots"/>, that is, clears the set of
        /// <see cref="roots"/> and adds every node without a parent to it.
        /// </summary>
        private void GatherRoots()
        {
            if (NodeHierarchyHasChanged)
            {
                roots.Clear();
                foreach (Node node in nodes.Values)
                {
                    if (node.Parent == null)
                    {
                        roots.Add(node);
                    }
                }
            }
        }

        /// <summary>
        /// The name of a node metric that reflects the node's depth within the node hierarchy.
        /// It is equivalent to the node attribute <see cref="Level"/>.
        /// </summary>
        public const string MetricLevel = "Metric.Level";

        /// <summary>
        /// Sets the metric <see cref="MetricLevel"/> of each node to its Level.
        /// </summary>
        private void SetLevelMetric()
        {
            foreach (Node node in nodes.Values)
            {
                node.SetInt(MetricLevel, node.Level);
            }
        }

        /// <summary>
        /// Creates deep copies of attributes where necessary. Is called by
        /// Clone() once the copy is created. Must be extended by every
        /// subclass that adds fields that should be cloned, too.
        ///
        /// IMPORTANT NOTE: Cloning a graph means to create deep copies of
        /// its node and children, too. The hierarchy will be isomorphic.
        /// </summary>
        /// <param name="clone">the clone receiving the copied attributes</param>
        protected override void HandleCloned(object clone)
        {
            base.HandleCloned(clone);
            Graph target = (Graph)clone;
            target.Name = Name;
            target.Path = Path;
            target.nodes = null;
            CopyNodesTo(target);
            target.edges = null;
            CopyEdgesTo(target);
            CopyHierarchy(this, target);
            target.NodeHierarchyHasChanged = true;
            target.FinalizeNodeHierarchy();
        }

        private void CopyNodesTo(Graph target, string nodeIdSuffix = null)
        {
            target.nodes ??= new Dictionary<string, Node>();
            foreach (KeyValuePair<string, Node> entry in nodes)
            {
                Node node = (Node)entry.Value.Clone();
                if (nodeIdSuffix != null)
                {
                    node.ID += nodeIdSuffix;
                }

                if (target.nodes.ContainsKey(node.ID))
                {
                    MergeNodeAttributes(target.nodes[node.ID], node);
                }
                else
                {
                    target.AddNode(node);
                }
            }

            void MergeNodeAttributes(Node targetNode, Node sourceNode)
            {
                foreach (KeyValuePair<string, float> attribute in sourceNode.FloatAttributes)
                {
                    if (!targetNode.TryGetFloat(attribute.Key, out float value))
                    {
                        targetNode.SetFloat(attribute.Key, attribute.Value);
                    }
                    else if (!Mathf.Approximately(value, attribute.Value))
                    {
                        throw new InvalidOperationException($"Node attribute {attribute.Key} differs in nodes "
                                                            + $"{targetNode} and {sourceNode}");
                    }
                }

                foreach (KeyValuePair<string, int> attribute in sourceNode.IntAttributes)
                {
                    if (!targetNode.TryGetInt(attribute.Key, out int value))
                    {
                        targetNode.SetInt(attribute.Key, attribute.Value);
                    }
                    // Level may change when merging two graphs into one
                    else if (value != attribute.Value && attribute.Key != "Metric.Level")
                    {
                        throw new InvalidOperationException($"Node attribute {attribute.Key} differs in nodes "
                                                            + $"{targetNode} and {sourceNode}");
                    }
                }

                foreach (KeyValuePair<string, string> attribute in sourceNode.StringAttributes)
                {
                    if (!targetNode.TryGetString(attribute.Key, out string value))
                    {
                        targetNode.SetString(attribute.Key, attribute.Value);
                    }
                    else if (value != attribute.Value)
                    {
                        throw new InvalidOperationException($"Node attribute {attribute.Key} differs in nodes "
                                                            + $"{targetNode} and {sourceNode}");
                    }
                }

                foreach (string attribute in sourceNode.ToggleAttributes)
                {
                    targetNode.SetToggle(attribute);
                }
            }
        }

        /// <summary>
        /// Copies the edges from this graph to the <paramref name="target"/> graph, matching nodes by
        /// ID + <paramref name="nodeIdSuffix"/>, if the latter is present.
        ///
        /// The nodes in this graph which have edges attached to them must also exist in the <paramref name="target"/>
        /// graph, i.e. nodes with the same ID must exist.
        /// However, if a non-null <paramref name="nodeIdSuffix"/> is given, then instead nodes with the nodeIdSuffix
        /// appended to the same ID must exist.
        ///
        /// </summary>
        /// <param name="target">The graph to which the edges of this graph shall be copied.</param>
        /// <param name="nodeIdSuffix">Suffix which node IDs in the <paramref name="target"/>
        /// graph are required to have.</param>
        /// <param name="edgeIdSuffix">Suffix to append to the new edge IDs</param>
        /// <exception cref="InvalidOperationException">When edge-attached nodes couldn't be found in the target graph.
        /// </exception>
        private void CopyEdgesTo(Graph target, string nodeIdSuffix = null, string edgeIdSuffix = null)
        {
            target.edges ??= new Dictionary<string, Edge>();
            foreach (KeyValuePair<string, Edge> entry in edges)
            {
                Edge edge = entry.Value;
                Edge clone = (Edge)edge.Clone();
                // set corresponding source
                if (target.TryGetNode(edge.Source.ID + (nodeIdSuffix ?? ""), out Node from))
                {
                    clone.Source = from;
                }
                else
                {
                    throw new InvalidOperationException($"Target graph does not have a node with ID {edge.Source.ID}");
                }

                // set corresponding target
                if (target.TryGetNode(edge.Target.ID + (nodeIdSuffix ?? ""), out Node to))
                {
                    clone.Target = to;
                }
                else
                {
                    throw new InvalidOperationException($"Target graph does not have a node with ID {edge.Target.ID}");
                }

                if (edgeIdSuffix != null)
                {
                    clone.ID += edgeIdSuffix;
                }

                target.AddEdge(clone);
            }
        }

        /// <summary>
        /// Copies the hierarchy from <paramref name="fromGraph"/> to <paramref name="toGraph"/>.
        /// </summary>
        /// <param name="fromGraph">The graph to copy the hierarchy from.</param>
        /// <param name="toGraph">The graph to apply the hierarchy to.</param>
        /// <param name="nodeIdSuffix">If non-null, all nodes in <paramref name="toGraph"/> are assumed to
        /// have this string appended to their IDs</param>
        private static void CopyHierarchy(Graph fromGraph, Graph toGraph, string nodeIdSuffix = null)
        {
            foreach (Node fromRoot in fromGraph.GetRoots())
            {
                if (toGraph.TryGetNode(fromRoot.ID + (nodeIdSuffix ?? ""), out Node toRoot))
                {
                    CopyHierarchy(fromRoot, toRoot, toGraph, nodeIdSuffix);
                }
                else
                {
                    throw new InvalidOperationException($"Target graph does not have a node with ID {fromRoot.ID}");
                }
            }
        }

        /// <summary>
        /// Adds the children to toParent corresponding to the children of fromParent in toGraph.
        /// </summary>
        /// <param name="fromParent">a parent node in the original graph</param>
        /// <param name="toParent">a parent node in copied graph (toGraph) whose children are to be added</param>
        /// <param name="toGraph">the graph copy containing toParent and its children</param>
        /// <param name="nodeIdSuffix">If non-null, all nodes in <paramref name="toGraph"/> are assumed to
        /// have this string appended to their IDs</param>
        private static void CopyHierarchy(Node fromParent, Node toParent, Graph toGraph, string nodeIdSuffix = null)
        {
            foreach (Node fromChild in fromParent.Children())
            {
                // Get the node in toGraph corresponding to fromChild.
                if (toGraph.TryGetNode(fromChild.ID + (nodeIdSuffix ?? ""), out Node toChild))
                {
                    // fromChild is a parent of fromParent and
                    // toChild must become a child of toParent
                    toParent.AddChild(toChild);
                    CopyHierarchy(fromChild, toChild, toGraph, nodeIdSuffix);
                }
                else
                {
                    throw new InvalidOperationException($"Target graph does not have a node with ID {fromChild.ID}");
                }
            }
        }

        /// <summary>
        /// Yields a subgraph of given graph that contains only nodes with one of the given
        /// <paramref name="nodeTypes"/>. The edges of this graph are "lifted" in the subgraph.
        /// For more precise information on what this means, consult the documentation of <see cref="SubgraphBy"/>.
        /// </summary>
        /// <param name="nodeTypes">the node types that should be kept</param>
        /// <returns>subgraph containing only nodes with given <paramref name="nodeTypes"/></returns>
        public Graph SubgraphByNodeType(IEnumerable<string> nodeTypes)
        {
            HashSet<string> relevantTypes = new HashSet<string>(nodeTypes);
            return SubgraphBy(element =>
            {
                if (element is Node node)
                {
                    return relevantTypes.Contains(node.Type);
                }
                else
                {
                    // Edges (attached to these nodes) shall be included
                    return true;
                }
            });
        }

        /// <summary>
        /// Yields a subgraph of given graph that contains only nodes and edges which have all the given
        /// <paramref name="toggleAttributes"/>. The edges of this graph are "lifted" in the subgraph.
        /// For more precise information on what this means, consult the documentation of <see cref="SubgraphBy"/>.
        /// </summary>
        /// <param name="toggleAttributes">Toggle attribute a node or edge must have to be kept</param>
        /// <returns>
        /// subgraph containing only nodes and edges which have all the given <paramref name="toggleAttributes"/>
        /// </returns>
        /// <seealso cref="SubgraphBy"/>
        public Graph SubgraphByToggleAttributes(IEnumerable<string> toggleAttributes) =>
            SubgraphBy(x => x.ToggleAttributes.Overlaps(toggleAttributes));

        /// <summary>
        /// Yields a subgraph of given graph that contains only edges for which <paramref name="includeEdge"/> returns
        /// true and only nodes which are connected to those edges.
        /// For more on how the subgraph is constructed, consult the documentation of <see cref="SubgraphBy"/>.
        /// </summary>
        /// <param name="includeEdge">function returning true if edge shall be added</param>
        /// <returns>Subgraph containing only edges for which <paramref name="includeEdge"/> returns true
        /// and nodes connected to those edges.</returns>
        /// <seealso cref="SubgraphBy"/>
        public Graph SubgraphByEdges(Func<Edge, bool> includeEdge)
        {
            ISet<Edge> keptEdges = new HashSet<Edge>(edges.Select(x => x.Value).Where(includeEdge));

            // We need to identify all nodes we want to keep, so all nodes which are attached to a kept edge.
            ISet<Node> keptNodes = new HashSet<Node>(nodes.Select(x => x.Value)
                                                          .Where(x => keptEdges.Overlaps(x.Incomings.Concat(x.Outgoings))));

            return SubgraphBy(x => x is Node && keptNodes.Contains(x) || x is Edge && keptEdges.Contains(x));
        }

        /// <summary>
        /// Yields a subgraph of given graph that contains only nodes and edges for which
        /// <paramref name="includeElement"/> returns true. The edges of this graph are "lifted" in the subgraph.
        /// More precisely, let mapsTo be a mapping of nodes from this graph onto nodes in
        /// in the resulting subgraph defined as follows (for every node N in this graph):
        /// (A) if <paramref name="includeElement"/>(N) == true,
        ///     then N has a clone N' in the subgraph and mapsTo[N] = N'
        /// (B) if <paramref name="includeElement"/>(N) == false,
        ///     then N has no clone in subgraph and
        ///     (1) if N has a nearest ancestor A for which <paramref name="includeElement"/>(A) == true,
        ///         then mapsTo[N] = mapsTo[A]
        ///     or
        ///     (2) none of the ancestors of N fulfill this condition,
        ///         then mapsTo[N] = null
        ///
        /// Given this mapping mapsTo, edges in the resulting subgraph are present as follows.
        /// For every edge E in this graph, there is a cloned edge E' in the resulting subgraph
        /// if and only if <paramref name="includeElement"/>(E) == true and
        /// mapsTo[E.Source] != null and mapsTo[E.Target] != null where
        /// E'.Source = mapsTo[E.Source] and E'.Target = mapsTo[E.Target] and there is
        /// not already an edge (of any type) from mapsTo[E.Source] to mapsTo[E.Target]
        /// (i.e., not mapsTo[E.Source].HasSuccessor(mapsTo[E.Target]). Every propagated
        /// edge is marked with the toggle attribute Edge.IsLiftedToggle.
        ///
        /// Notes:
        ///
        /// The result is a new graph, that is, the nodes and edges are copies; they are not
        /// shared. Graph elements in the resulting subgraph can be mapped onto their original
        /// corresponding graph elements in this graph by way of their ID.
        ///
        /// The resulting subgraph may have fewer edges even if <paramref name="includeElement"/>
        /// returns true for all edges: if an edge of this graph has a
        /// source or target, N, for which mapsTo[N] = null, it will be lost.
        ///
        /// An edge, E, is not propagated to a pair of nodes that already have an edge, E',
        /// independent of the types of E and E'. As a consequence, there can only be one
        /// propagated edge from one node to another node. Because the edge types are
        /// neglected, we lose information. On the other hand, we reduce the number of edges.
        /// </summary>
        /// <param name="includeElement">function determining whether a given node or edge shall be kept</param>
        /// <returns>
        /// subgraph containing only nodes and edges for which <paramref name="includeElement"/> returns true.
        /// </returns>
        public Graph SubgraphBy(Func<GraphElement, bool> includeElement)
        {
            Graph subgraph = new Graph();
            Dictionary<Node, Node> mapsTo = AddNodesToSubgraph(subgraph, includeElement);
            AddEdgesToSubgraph(subgraph, mapsTo, includeElement);
            return subgraph;
        }

        /// <summary>
        /// Recursively adds all nodes to <paramref name="subgraph"/> if <paramref name="includeElement"/> returns
        /// <c>true</c> for this element. Starts at the roots and traverses all nodes in this graph.
        /// </summary>
        /// <param name="subgraph">subgraph where to add the nodes</param>
        /// <param name="includeElement">function returning true if node shall be added</param>
        /// <returns>a mapping of nodes from this graph onto the subgraph's nodes</returns>
        private Dictionary<Node, Node> AddNodesToSubgraph(Graph subgraph, Func<GraphElement, bool> includeElement)
        {
            Dictionary<Node, Node> mapsTo = new Dictionary<Node, Node>();
            foreach (Node root in GetRoots())
            {
                // the node that corresponds to root in subgraph (may be null if
                // there is no corresponding node)
                if (includeElement(root))
                {
                    // root must be kept => a corresponding node is added to subgraph
                    // and root is mapped onto that node
                    Node rootInSubgraph = (Node)root.Clone();
                    subgraph.AddNode(rootInSubgraph);
                    mapsTo[root] = rootInSubgraph;
                }
                else
                {
                    mapsTo[root] = null;
                }

                AddToSubGraph(subgraph, includeElement, mapsTo, root);
            }

            return mapsTo;
        }

        /// <summary>
        /// Adds all ancestors of <paramref name="parent"/> to <paramref name="subgraph"/> if
        /// <paramref name="includeElement"/> returns true for the ancestor. The mapping <paramref name="mapsTo"/>
        /// is updated accordingly.
        /// </summary>
        /// <param name="subgraph">subgraph where to add the nodes</param>
        /// <param name="includeElement">function returning true if node shall be added</param>
        /// <param name="mapsTo">mapping from nodes of this graph onto nodes in <paramref name="subgraph"/></param>
        /// <param name="parent">root of a subtree to be mapped; is a node in this graph</param>
        private static void AddToSubGraph(Graph subgraph, Func<GraphElement, bool> includeElement,
                                          IDictionary<Node, Node> mapsTo, Node parent)
        {
            foreach (Node child in parent.Children())
            {
                if (includeElement(child))
                {
                    // The child must be kept => a corresponding node is created
                    // in the subgraph and child is mapped onto that node
                    Node childInSubgraph = (Node)child.Clone();
                    subgraph.AddNode(childInSubgraph);
                    mapsTo[child] = childInSubgraph;
                    // The child in the subgraph must become a child of the node
                    // corresponding to the parent (i.e., mapsTo[parent]) if
                    // one exists; it may happen that parent has no corresponding
                    // node in the subgraph if and only if either the parent is
                    // a root to be ignored (i.e., due to includeElement returning false)
                    // or if all its ascendants are to be ignored.
                    Node parentInSubgraph = mapsTo[parent];
                    parentInSubgraph?.AddChild(childInSubgraph);
                    AddToSubGraph(subgraph, includeElement, mapsTo, child);
                }
                else
                {
                    // The child is to be ignored. Hence, no corresponding node
                    // is added in the subgraph for it. That means, it must be mapped
                    // onto mapsTo[parent]. There are cases in which mapsTo[parent]
                    // may be null, but that is OK: We allow null values in the mapping.
                    mapsTo[child] = mapsTo[parent];
                    AddToSubGraph(subgraph, includeElement, mapsTo, child);
                }
            }
        }

        /// <summary>
        /// Propagates edge from this graph onto <paramref name="subgraph"/> as follows:
        /// For every edge E in this graph, there is a cloned edge E' in the resulting subgraph
        /// if and only if <paramref name="includeElement"/>(E) == true
        /// and mapsTo[E.Source] != null and mapsTo[E.Target] != null where
        /// E'.Source = mapsTo[E.Source] and E'.Target = mapsTo[E.Target] and there is
        /// not already an edge (of any type) from mapsTo[E.Source] to mapsTo[E.Target]
        /// (i.e., not mapsTo[E.Source].HasSuccessor(mapsTo[E.Target]). Every propagated
        /// edge is marked with the toggle attribute Edge.IsLiftedToggle.
        /// </summary>
        /// <param name="subgraph">graph to propagate the edges to</param>
        /// <param name="mapsTo">mapping from nodes of this graph onto nodes in <paramref name="subgraph"/></param>
        /// <param name="includeElement">function determining whether a respective edge shall be kept</param>
        private void AddEdgesToSubgraph(Graph subgraph, IDictionary<Node, Node> mapsTo,
                                        Func<GraphElement, bool> includeElement)
        {
            foreach (Edge edge in Edges())
            {
                Node sourceInSubgraph = mapsTo[edge.Source];
                Node targetInSubgraph = mapsTo[edge.Target];

                if (sourceInSubgraph != null && targetInSubgraph != null
                                             && !sourceInSubgraph.HasSuccessor(targetInSubgraph, edge.Type) && includeElement(edge))
                {
                    Edge edgeInSubgraph = (Edge)edge.Clone();
                    edgeInSubgraph.Source = sourceInSubgraph;
                    edgeInSubgraph.Target = targetInSubgraph;
                    edgeInSubgraph.SetToggle(Edge.IsLiftedToggle);
                    subgraph.AddEdge(edgeInSubgraph);
                }
            }
        }

        /// <summary>
        /// Traverses the given graph. On every root node rootAction is called.
        /// On every node that is a leaf, leafAction is called, otherwise innerNodeAction is called.
        /// If an action is null, it just won't be called.
        /// </summary>
        /// <param name="rootAction">Function that is called on root nodes.</param>
        /// <param name="innerNodeAction">Function that is called when node is not a leaf.</param>
        /// <param name="leafAction">Function that is called when node is a leaf.</param>
        public void Traverse(Action<Node> rootAction, Action<Node> innerNodeAction, Action<Node> leafAction)
        {
            GetRoots().ForEach(rootNode =>
            {
                rootAction?.Invoke(rootNode);
                rootNode.Children().ForEach(child => TraverseTree(child, innerNodeAction, leafAction));
            });
        }

        /// <summary>
        /// Traverses the given graph. On every node that is a leaf,
        /// leafAction is called, otherwise innerNodeAction is called.
        /// If an action is null, it just won't be called.
        /// </summary>
        /// <param name="innerNodeAction">Function that is called when node is not a leaf.</param>
        /// <param name="leafAction">Function that is called when node is a leaf.</param>
        public void Traverse(Action<Node> innerNodeAction, Action<Node> leafAction)
        {
            Traverse(null, innerNodeAction, leafAction);
        }

        /// <summary>
        /// Traverses the given graph recursively. On every node that is a leaf,
        /// leafAction is called.
        /// If an action ist null, it just won't be called.
        /// </summary>
        /// <param name="leafAction">Function that is called when node is a leaf.</param>
        public void Traverse(Action<Node> leafAction)
        {
            Traverse(null, leafAction);
        }

        /// <summary>
        /// Traverses a given node recursively. On every node that is a leaf,
        /// leafAction is called, otherwise innerNodeAction is called.
        /// </summary>
        /// <param name="node">The node to traverse.</param>
        /// <param name="innerNodeAction">Function that is called when node is not a leaf.</param>
        /// <param name="leafAction">Function that is called when node is a leaf.</param>
        private static void TraverseTree(Node node, Action<Node> innerNodeAction, Action<Node> leafAction)
        {
            if (node.IsLeaf())
            {
                leafAction?.Invoke(node);
            }
            else
            {
                innerNodeAction?.Invoke(node);
                node.Children().ForEach(childNode => TraverseTree(childNode, innerNodeAction, leafAction));
            }
        }

        /// <summary>
        /// Returns true if <paramref name="other"/> meets all of the following
        /// conditions:
        ///  (1) is not null
        ///  (2) has exactly the same C# type
        ///  (3) has exactly the same attributes with exactly the same values
        ///  (4) has the same path
        ///  (5) has the same graph name
        ///  (6) has the same number of nodes and the sets of nodes are equal
        ///  (7) has the same number of edges and the sets of edges are equal
        ///  (8) has the same node hierarchy
        ///
        /// Note: This node and the other node may or may not be in the same graph.
        /// </summary>
        /// <param name="other">to be compared to</param>
        /// <returns>true if equal</returns>
        public override bool Equals(object other)
        {
            if (!base.Equals(other))
            {
                Graph otherNode = other as Graph;
                if (other != null)
                {
                    Report($"Graphs {Name} {otherNode?.Name} have differences");
                }

                return false;
            }

            Graph otherGraph = other as Graph;
            Assert.IsNotNull(otherGraph);
            if (Path != otherGraph.Path)
            {
                Report("Graph paths are different");
                return false;
            }

            if (Name != otherGraph.Name)
            {
                Report("Graph names are different");
                return false;
            }

            if (NodeCount != otherGraph.NodeCount)
            {
                Report("Number of nodes are different");
                return false;
            }

            if (!AreEqual(nodes, otherGraph.nodes))
            {
                // Note: because the Equals operation for nodes checks also the ID
                // of the node's parents and children, we will also implicitly check the
                // node hierarchy.
                Report("Graph nodes are different");
                return false;
            }

            if (edges.Count != otherGraph.edges.Count)
            {
                Report("Number of edges are different");
                return false;
            }

            bool equal = AreEqual(edges, otherGraph.edges);
            if (!equal)
            {
                Report("Graph edges are different");
            }

            return true;
        }

        /// <summary>
        /// Returns a hash code.
        /// </summary>
        /// <returns>hash code</returns>
        public override int GetHashCode()
        {
            // we are using the viewName which is intended to be unique
            return Name.GetHashCode();
        }

        /// <summary>
        /// All names of integer attributes of all nodes in the graph.
        /// </summary>
        /// <returns>names of integer node attributes</returns>
        public List<string> AllIntNodeAttributes()
        {
            return AllNodeAttributes(AllIntAttributeNames);
        }

        /// <summary>
        /// All names of float attributes of all nodes in the graph.
        /// </summary>
        /// <returns>names of float node attributes</returns>
        public List<string> AllFloatNodeAttributes()
        {
            return AllNodeAttributes(AllFloatAttributeNames);
        }

        /// <summary>
        /// Returns the concatenation of <see cref="AllFloatNodeAttributes()"/>
        /// and <see cref="AllIntNodeAttributes()"/>.
        /// </summary>
        /// <returns>names of all numeric (int or float) node attributes</returns>
        public List<string> AllNumericNodeAttributes()
        {
            List<string> result = AllIntNodeAttributes();
            result.AddRange(AllFloatNodeAttributes());
            return result;
        }

        /// <summary>
        /// All names of toggle attributes of all nodes in the graph.
        /// </summary>
        /// <returns>names of toggle node attributes</returns>
        public List<string> AllToggleNodeAttributes()
        {
            return AllNodeAttributes(AllToggleAttributeNames);
        }

        /// <summary>
        /// All names of string attributes of all nodes in the graph.
        /// </summary>
        /// <returns>names of string node attributes</returns>
        public List<string> AllStringNodeAttributes()
        {
            return AllNodeAttributes(AllStringAttributeNames);
        }

        /// <summary>
        /// Returns the attribute names of given <paramref name="node"/>.
        /// </summary>
        /// <param name="node">the node whose attribute names are to be retrieved</param>
        /// <returns>attribute names of a particular type</returns>
        private delegate ICollection<string> AllAttributeNames(Node node);

        /// <summary>
        /// Yields all string attribute names of given <paramref name="node"/>.
        /// </summary>
        /// <param name="node">node whose string attributes are to be retrieved</param>
        /// <returns>all string attribute names</returns>
        private static ICollection<string> AllStringAttributeNames(Node node)
        {
            return node.StringAttributes.Keys;
        }

        /// <summary>
        /// Yields all toggle attribute names of given <paramref name="node"/>.
        /// </summary>
        /// <param name="node">node whose toggle attributes are to be retrieved</param>
        /// <returns>all toggle attribute names</returns>
        private static ICollection<string> AllToggleAttributeNames(Node node)
        {
            return node.ToggleAttributes;
        }

        /// <summary>
        /// Yields all float attribute names of given <paramref name="node"/>.
        /// </summary>
        /// <param name="node">node whose float attributes are to be retrieved</param>
        /// <returns>all float attribute names</returns>
        private static ICollection<string> AllFloatAttributeNames(Node node)
        {
            return node.FloatAttributes.Keys;
        }

        /// <summary>
        /// Yields all integer attribute names of given <paramref name="node"/>.
        /// </summary>
        /// <param name="node">node whose integer attributes are to be retrieved</param>
        /// <returns>all integer attribute names</returns>
        private static ICollection<string> AllIntAttributeNames(Node node)
        {
            return node.IntAttributes.Keys;
        }

        /// <summary>
        /// Returns all node attribute names collected via given <paramref name="attributeNames"/>
        /// over all nodes in the graph.
        /// </summary>
        /// <param name="attributeNames">yields the node attribute names to collect</param>
        /// <returns>all node attribute names collected via <paramref name="attributeNames"/></returns>
        private List<string> AllNodeAttributes(AllAttributeNames attributeNames)
        {
            HashSet<string> result = new HashSet<string>();
            foreach (Node node in Nodes())
            {
                foreach (string name in attributeNames(node))
                {
                    result.Add(name);
                }
            }

            return result.ToList();
        }

        public static implicit operator bool(Graph graph)
        {
            return graph != null;
        }
    }
}