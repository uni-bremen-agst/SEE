﻿using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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
        private string name = "";

        // The path of the file from which this graph was loaded. Could be the
        /// empty string if the graph was not created by loading it from disk.
        private string path = "";

        private int maxDepth = -1;
        /// <summary>
        /// The maximal depth of the node hierarchy. This value must be computed
        /// by calling FinalizeGraph() before accessing <see cref="MaxDepth"/>.
        /// </summary>
        public int MaxDepth 
        { 
            get
            {
                if (maxDepth < 0)
                {
                    Debug.LogErrorFormat("Forgotten call to FinalizeGraph() for graph {0}\n", name);
                    FinalizeGraph();
                }
                return maxDepth;
            }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="name">name of the graph</param>
        public Graph(string name = "") : base()
        {
            this.name = name;
        }

        /// Adds a node to the graph. 
        /// Preconditions:
        ///   (1) node must not be null
        ///   (2) node.ID must be defined.
        ///   (3) a node with node.ID must not have been added before
        ///   (4) node must not be contained in another graph
        /// </summary>
        /// <param name="node"></param>
        public void AddNode(Node node)
        {
            if (ReferenceEquals(node, null))
            {
                throw new Exception("node must not be null");
            }
            else if (String.IsNullOrEmpty(node.ID))
            {
                throw new Exception("ID of a node must neither be null nor empty");
            }
            else if (nodes.ContainsKey(node.ID))
            {
                throw new Exception("ID '" + node.ID + "' is not unique:\n"
                                    + node.ToString()
                                    + ".\nDuplicate already in graph: "
                                    + nodes[node.ID].ToString());
            }
            else if (!ReferenceEquals(node.ItsGraph, null))
            {
                throw new Exception("node " + node.ToString() + " is already in a graph " + node.ItsGraph.Name);
            }
            else
            {
                nodes[node.ID] = node;
                node.ItsGraph = this;
            }
        }

        /// <summary>
        /// Removes the given node from the graph. Its incoming and outgoing edges are removed,
        /// along with it.
        /// 
        /// Precondition: node must not be null and must be contained in this graph.
        /// </summary>
        /// <param name="node">node to be removed</param>
        public void RemoveNode(Node node)
        {
            if (ReferenceEquals(node, null))
            {
                throw new System.Exception("node must not be null");
            }
            else if (node.ItsGraph != this)
            {
                if (ReferenceEquals(node.ItsGraph, null))
                {
                    throw new Exception("node " + node.ToString() + " is not contained in any graph");
                }
                else
                {
                    throw new Exception("node " + node.ToString() + " is contained in a different graph " + node.ItsGraph.Name);
                }
            }
            else
            {
                if (nodes.Remove(node.ID))
                {
                    // The edges of node are stored in the node's data structure as well as
                    // in the node's neighbor's data structure.
                    foreach (Edge outgoing in node.Outgoings)
                    {
                        Node successor = outgoing.Target;
                        successor.RemoveIncoming(outgoing);
                        edges.Remove(outgoing.ID);
                    }
                    foreach (Edge incoming in node.Incomings)
                    {
                        Node predecessor = incoming.Source;
                        predecessor.RemoveOutgoing(incoming);
                        edges.Remove(incoming.ID);
                    }
                    node.RemoveAllEdges();
                    // Adjust the node hierarchy.
                    if (node.NumberOfChildren() > 0)
                    {
                        if (node.Parent == null)
                        {
                            // All children of node become roots now.
                            foreach (Node child in node.Children())
                            {
                                child.Parent = null;
                            }
                        }
                        else
                        {
                            // The father of node now becomes the father of all children of node.
                            foreach (Node child in node.Children())
                            {
                                child.Parent = null;
                                node.Parent.AddChild(child);
                            }
                        }
                        // Because the node hierarchy has changed, we need to re-calcuate
                        // the levels. Note: We could do that incrementally if we wanted to
                        // by traversing only the children of node instead of all nodes in 
                        // the graph.
                        CalculateLevels();
                    }
                    node.ItsGraph = null;
                }
                else
                {
                    throw new Exception("node " + node.ToString() + " is not contained in this graph " + Name);
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
        public bool Contains(Node node)
        {
            if (ReferenceEquals(node, null))
            {
                throw new System.Exception("node must not be null");
            }
            else if (String.IsNullOrEmpty(node.ID))
            {
                throw new System.Exception("ID of a node must neither be null nor empty");
            }
            else
            {
                return nodes.ContainsKey(node.ID);
            }
        }

        /// <summary>
        /// Returns the node with the given unique ID if it exists; otherwise null.
        /// </summary>
        /// <param name="ID">unique ID</param>
        /// <returns>node with the given unique ID if it exists; otherwise null</returns>
        public Node GetNode(string ID)
        {
            if (String.IsNullOrEmpty(ID))
            {
                throw new System.Exception("ID must neither be null nor empty");
            }
            else if (nodes.TryGetValue(ID, out Node node))
            {
                return node;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Returns the edge with the given unique ID if it exists; otherwise null.
        /// </summary>
        /// <param name="ID">unique ID</param>
        /// <returns>edge with the given unique ID if it exists; otherwise null</returns>
        public Edge GetEdge(string ID)
        {
            if (String.IsNullOrEmpty(ID))
            {
                throw new System.Exception("ID must neither be null nor empty");
            }
            else if (edges.TryGetValue(ID, out Edge edge))
            {
                return edge;
            }
            else
            {
                return null;
            }
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
                throw new Exception("edge must not be null");
            }
            else if (ReferenceEquals(edge.Source, null) || ReferenceEquals(edge.Target, null))
            {
                throw new Exception("source/target of this node is null");
            }
            else if (ReferenceEquals(edge.ItsGraph, null))
            {
                if (edge.Source.ItsGraph != this)
                {
                    throw new Exception("source node " + edge.Source.ToString() + " is not in the graph");
                }
                else if (edge.Target.ItsGraph != this)
                {
                    throw new Exception("target node " + edge.Target.ToString() + " is not in the graph");
                }
                else
                {
                    edge.ItsGraph = this;
                    edges[edge.ID] = edge;
                    edge.Source.AddOutgoing(edge);
                    edge.Target.AddIncoming(edge);
                }
            }
            else
            {
                throw new Exception("edge " + edge.ToString() + " is already in a graph " + edge.ItsGraph.Name);
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
                throw new Exception("edge must not be null");
            }
            else if (edge.ItsGraph != this)
            {
                if (ReferenceEquals(edge.ItsGraph, null))
                {
                    throw new Exception("edge " + edge.ToString() + " is not contained in any graph");
                }
                else
                {
                    throw new Exception("edge " + edge.ToString() + " is contained in a different graph " + edge.ItsGraph.Name);
                }
            }
            else
            {
                if (!edges.ContainsKey(edge.ID))
                {
                    throw new Exception("edge " + edge.ToString() + " is not contained in graph " + Name);
                }
                else
                {
                    edge.Source.RemoveOutgoing(edge);
                    edge.Target.RemoveIncoming(edge);
                    edge.ItsGraph = null;
                }
            }
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
        public string Name
        {
            get => name;
            set => name = value;
        }

        /// <summary>
        /// The path of the file from which this graph was loaded. Could be the
        /// empty string if the graph was not created by loading it from disk.
        /// </summary>
        public string Path
        {
            get => path;
            set => path = value;
        }

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
        /// Returns the list of nodes without parent.
        /// </summary>
        /// <returns>root nodes of the hierarchy</returns>
        public List<Node> GetRoots()
        {
            List<Node> result = new List<Node>();
            foreach (Node node in nodes.Values)
            {
                if (node.IsRoot())
                {
                    result.Add(node);
                }
            }
            return result;
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
        private void DumpTree(Node root, int level)
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
        /// Returns all edges of graph whose source and target is contained in 
        /// selectedNodes.
        /// </summary>
        /// <param name="graph">graph whose edges are to be filtered</param>
        /// <param name="selectedNodes">source and target nodes of required edges</param>
        /// <returns>all edges of graph whose source and target is contained in selectedNodes</returns>
        public IList<Edge> ConnectingEdges(ICollection<Node> selectedNodes)
        {
            IList<Edge> result = new List<Edge>();
            HashSet<Node> nodes = new HashSet<Node>(selectedNodes);

            foreach (Edge edge in Edges())
            {
                if (nodes.Contains(edge.Source) && nodes.Contains(edge.Target))
                {
                    result.Add(edge);
                }
            }
            return result;
        }

        /// <summary>
        /// Returns the maximal depth of the node hierarchy among the given <paramref name="nodes"/>
        /// plus the given <paramref name="currentDepth"/>.
        /// </summary>
        /// <param name="nodes">nodes for which to determine the depth</param>
        /// <param name="currentDepth">the current depth of the given <paramref name="nodes"/></param>
        /// <returns></returns>
        private int CalcMaxDepth(List<Node> nodes, int currentDepth)
        {
            int max = currentDepth + 1;
            for (int i = 0; i < nodes.Count; i++)
            {
                max = Math.Max(max, CalcMaxDepth(nodes[i].Children(), currentDepth + 1));
            }
            return max;
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
            result += " \"name\": \"" + name + "\",\n";
            // its own attributes
            result += base.ToString();
            // its nodes
            foreach (Node node in nodes.Values)
            {
                result += node.ToString() + ",\n";
            }
            foreach (Edge edge in edges.Values)
            {
                result += edge.ToString() + ",\n";
            }
            result += "}\n";
            return result;
        }

        /// <summary>
        /// Sets the level of each node in the graph. The level of a root node is 0.
        /// For all other nodes, the level is the level of its parent + 1.
        /// </summary>
        public void CalculateLevels()
        {
            foreach (Node root in GetRoots())
            {
                root.SetLevel(0);
            }
        }

        /// <summary>
        /// Sets the maximal depth of the graph. This method must be called
        /// after the graph has been fully loaded and before any client is 
        /// accessing MaxDepth.
        /// </summary>
        public void FinalizeGraph()
        {
            maxDepth = CalcMaxDepth(GetRoots(), -1);
        }

        /// <summary>
        /// Creates deep copies of attributes where necessary. Is called by
        /// Clone() once the copy is created. Must be extended by every 
        /// subclass that adds fields that should be cloned, too.
        /// 
        /// IMPORTANT NOTE: Cloning a graph means to create deep copies of
        /// its node and children, too. The hierarchy will be isomporph.
        /// </summary>
        /// <param name="clone">the clone receiving the copied attributes</param>
        protected override void HandleCloned(object clone)
        {
            base.HandleCloned(clone);
            Graph target = (Graph)clone;
            target.name = name;
            target.path = path;
            CopyNodesTo(target);
            CopyEdgesTo(target);
            CopyHierarchy(this, target);
        }

        private void CopyNodesTo(Graph target)
        {
            target.nodes = new Dictionary<string, Node>();
            foreach (KeyValuePair<string, Node> entry in nodes)
            {
                Node node = (Node)entry.Value.Clone();
                target.AddNode(node);
            }
        }

        private void CopyEdgesTo(Graph target)
        {
            target.edges = new Dictionary<string, Edge>();
            foreach (KeyValuePair<string, Edge> entry in edges)
            {
                Edge edge = entry.Value;
                Edge clone = (Edge)edge.Clone();
                // set corresponding source
                if (target.TryGetNode(edge.Source.ID, out Node from))
                {
                    clone.Source = from;
                }
                else
                {
                    throw new Exception("target graph does not have a node with ID " + edge.Source.ID);
                }
                // set corresponding target
                if (target.TryGetNode(edge.Target.ID, out Node to))
                {
                    clone.Target = to;
                }
                else
                {
                    throw new Exception("target graph does not have a node with ID " + edge.Target.ID);
                }
                target.AddEdge(clone);
            }
        }

        private static void CopyHierarchy(Graph fromGraph, Graph toGraph)
        {
            foreach (Node fromRoot in fromGraph.GetRoots())
            {
                if (toGraph.TryGetNode(fromRoot.ID, out Node toRoot))
                {
                    CopyHierarchy(fromRoot, toRoot, toGraph);
                }
                else
                {
                    throw new Exception("target graph does not have a node with ID " + fromRoot.ID);
                }
            }
            toGraph.CalculateLevels();
        }

        /// <summary>
        /// Adds the children to toParent corresponding to the children of fromParent in toGraph.
        /// </summary>
        /// <param name="fromParent">a parent node in the original graph</param>
        /// <param name="toParent">a parent node in copied graph (toGraph) whose children are to be added</param>
        /// <param name="toGraph">the graph copy containing toParent and its children</param>
        private static void CopyHierarchy(Node fromParent, Node toParent, Graph toGraph)
        {
            foreach (Node fromChild in fromParent.Children())
            {
                // Get the node in toGraph corresponding to fromChild.
                if (toGraph.TryGetNode(fromChild.ID, out Node toChild))
                {
                    // fromChild is a parent of fromParent and
                    // toChild must become a child of toParent
                    toParent.AddChild(toChild);
                    CopyHierarchy(fromChild, toChild, toGraph);
                }
                else
                {
                    throw new Exception("target graph does not have a node with ID " + fromChild.ID);
                }
            }
        }

        /// <summary>
        /// Yields a subgraph of given graph that contains only nodes with one of the given
        /// <paramref name="nodeTypes"/>. The edges of this graph are "lifted" in the subgraph.
        /// More precisely, let mapsTo be a mapping of nodes from this graph onto nodes in 
        /// in the resulting subgraph defined as follows (for every node N in this graph):
        /// (A) if N has a type in <paramref name="nodeTypes"/>,
        ///     then N has a clone N' in the subgraph and mapsTo[N] = N'
        /// (B) if N has no type in <paramref name="nodeTypes"/>,
        ///     then N has no clone in subgraph and
        ///     (1) if N has a nearest ancestor A that has a type in <paramref name="nodeTypes"/>,
        ///         then mapsTo[N] = mapsTo[A]
        ///     or     
        ///     (2) none of the ancestors of N has a type in <paramref name="nodeTypes"/>,
        ///         then mapsTo[N] = null
        /// 
        /// Given this mapping mapsTo, edges in the resulting subgraph are present as follows.
        /// For every edge E in this graph, there is a cloned edge E' in the resulting subgraph
        /// if and only if mapsTo[E.Source] != null and mapsTo[E.Target] != null where
        /// E'.Source = mapsTo[E.Source] and E'.Target = mapsTo[E.Target] and there is
        /// not already an edge (of any type) from mapsTo[E.Source] to mapsTo[E.Target]
        /// (i.e., not mapsTo[E.Source].HasSuccessor(mapsTo[E.Target]). Every propatated
        /// edge is marked with the toggle attribute Edge.IsLiftedToggle.
        /// 
        /// Notes: 
        /// 
        /// The result is a new graph, that is, the nodes and edges are copies; they are not
        /// shared. Graph elements in the resulting subgraph can be mapped onto their original
        /// corresponding graph elements in this graph by way of their ID.
        /// 
        /// The resulting subgraph may have fewer edges: if an edge of this graph has a 
        /// source or target, N, for which mapsTo[N] = null, it will be lost.
        /// 
        /// An edge, E, is not propagated to a pair of nodes that already have an edge, E', 
        /// independent of the types of E and E'. As a consequence, there can only be one
        /// propagated edge from one node to another node. Because the edge types are
        /// neglected, we loose information. On the other hand, we reduce the number of edges.
        /// </summary>
        /// <param name="nodeTypes">the node types that should be kept</param>
        /// <returns>subgraph containing only nodes with given <paramref name="nodeTypes"/></returns>
        public Graph Subgraph(ICollection<string> nodeTypes)
        {
            Graph subgraph = new Graph();
            HashSet<string> relevantTypes = new HashSet<string>(nodeTypes);
            Dictionary<Node, Node> mapsTo = AddNodesToSubgraph(subgraph, relevantTypes);
            AddEdgesToSubgraph(subgraph, mapsTo);
            return subgraph;
        }

        /// <summary>
        /// Recursively adds all nodes to <paramref name="subgraph"/> if their type is one of
        /// <paramref name="relevantTypes"/>. Starts at the roots and traverses all nodes in this graph.
        /// </summary>
        /// <param name="subgraph">subgraph where to add the nodes</param>
        /// <param name="relevantTypes">the node types that should be kept</param>
        /// <returns>a mapping of nodes from this graph onto the subgraph's nodes</returns>
        private Dictionary<Node, Node> AddNodesToSubgraph(Graph subgraph, HashSet<string> relevantTypes)
        {
            Dictionary<Node, Node> mapsTo = new Dictionary<Node, Node>();
            foreach (Node root in GetRoots())
            {
                // the node that corresponds to root in subgraph (may be null if
                // there is no corresponding node)
                Node rootInSubgraph = null;
                if (relevantTypes.Contains(root.Type))
                {
                    // root must be kept => a corresponding node is added to subgraph
                    // and root is mapped onto that node
                    rootInSubgraph = (Node)root.Clone();
                    subgraph.AddNode(rootInSubgraph);
                    mapsTo[root] = rootInSubgraph;
                }
                else
                {
                    mapsTo[root] = null;
                }
                AddToSubGraph(subgraph, relevantTypes, mapsTo, root);
            }
            return mapsTo;
        }

        /// <summary>
        /// Adds all ancestors of <paramref name="parent"/> to <paramref name="subgraph"/> if their 
        /// type is one of <paramref name="relevantTypes"/>. The mapping <paramref name="mapsTo"/>
        /// is updated accordingly.
        /// </summary>
        /// <param name="subgraph">subgraph where to add the nodes</param>
        /// <param name="relevantTypes">the node types that should be kept</param>
        /// <param name="mapsTo">mapping from nodes of this graph onto nodes in <paramref name="subgraph"/></param>
        /// <param name="parent">root of a subtree to be mapped; is a node in this graph</param>
        private void AddToSubGraph
            (Graph subgraph,
             HashSet<string> relevantTypes,
             Dictionary<Node, Node> mapsTo,
             Node parent)
        {
            foreach (Node child in parent.Children())
            {
                if (relevantTypes.Contains(child.Type))
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
                    // a root with a type to be ignored or if all its ascendants
                    // have a type to be ignored.
                    Node parentInSubgraph = mapsTo[parent];
                    if (parentInSubgraph != null)
                    {
                        parentInSubgraph.AddChild(childInSubgraph);
                    }
                    AddToSubGraph(subgraph, relevantTypes, mapsTo, child);
                }
                else
                {
                    // The child has a type to be ignored. Hence, no corresponding node
                    // is added in the subgraph for it. That means, it must be mapped
                    // onto mapsTo[parent]. There are cases in which mapsTo[parent]
                    // may be null, but that is OK: We allow null values in the mapping.
                    mapsTo[child] = mapsTo[parent];
                    AddToSubGraph(subgraph, relevantTypes, mapsTo, child);
                }
            }
        }

        /// <summary>
        /// Propagates edge from this graph onto <paramref name="subgraph"/> as follows:
        /// For every edge E in this graph, there is a cloned edge E' in the resulting subgraph
        /// if and only if mapsTo[E.Source] != null and mapsTo[E.Target] != null where
        /// E'.Source = mapsTo[E.Source] and E'.Target = mapsTo[E.Target] and there is
        /// not already an edge (of any type) from mapsTo[E.Source] to mapsTo[E.Target]
        /// (i.e., not mapsTo[E.Source].HasSuccessor(mapsTo[E.Target]). Every propatated
        /// edge is marked with the toggle attribute Edge.IsLiftedToggle.
        /// </summary>
        /// <param name="subgraph">graph where to propagate the edges too</param>
        /// <param name="mapsTo">mapping from nodes of this graph onto nodes in <paramref name="subgraph"/></param>
        private void AddEdgesToSubgraph(Graph subgraph, Dictionary<Node, Node> mapsTo)
        {
            foreach (Edge edge in Edges())
            {
                Node sourceInSubgraph = mapsTo[edge.Source];
                Node targetInSubgraph = mapsTo[edge.Target];

                if (sourceInSubgraph != null && targetInSubgraph != null
                    && !sourceInSubgraph.HasSuccessor(targetInSubgraph, edge.Type))
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
        /// If an action ist null, it just won't be called.
        /// </summary>
        /// <param name="rootAction">Function that is called on root nodes.</param>
        /// <param name="innerNodeAction">Function that is called when node is not a leaf.</param>
        /// <param name="leafAction">Function that is called when node is a leaf.</param>
        public void Traverse(Action<Node> rootAction, Action<Node> innerNodeAction, Action<Node> leafAction)
        {
            GetRoots().ForEach(
                rootNode =>
                {
                    rootAction?.Invoke(rootNode);
                    rootNode.Children().ForEach(child => TraverseTree(child, innerNodeAction, leafAction));
                }
            );
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
        public override bool Equals(System.Object other)
        {
            if (!base.Equals(other))
            {
                Graph otherNode = other as Graph;
                if (other != null)
                {
                    Report("Graphs " + name + " " + otherNode.name + " have differences");
                }
                return false;
            }
            else
            {
                Graph otherGraph = other as Graph;
                if (path != otherGraph.path)
                {
                    Report("Graph paths are different");
                    return false;
                }
                else if (name != otherGraph.name)
                {
                    Report("Graph names are different");
                    return false;
                }
                else if (NodeCount != otherGraph.NodeCount)
                {
                    Report("Number of nodes are different");
                    return false;
                }
                else if (!AreEqual(nodes, otherGraph.nodes))
                {
                    // Note: because the Equals operation for nodes checks also the ID
                    // of the node's parents and children, we will also implicitly check the
                    // node hierarchy.
                    Report("Graph nodes are different");
                    return false;
                }
                else if (edges.Count != otherGraph.edges.Count)
                {
                    Report("Number of edges are different");
                    return false;
                }
                else
                {
                    bool equal = AreEqual(edges, otherGraph.edges);
                    if (!equal)
                    {
                        Report("Graph edges are different");
                    }
                    return true;
                }
            }
        }

        /// <summary>
        /// Returns a hash code.
        /// </summary>
        /// <returns>hash code</returns>
        public override int GetHashCode()
        {
            // we are using the viewName which is intended to be unique
            return name.GetHashCode();
        }
    }
}