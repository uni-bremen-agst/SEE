using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SEE.DataModel
{
    /// <summary>
    /// A graph with nodes and edges representing the data to be visualized 
    /// by way of blocks and connections.
    /// </summary>
    [System.Serializable]
    public class Graph : Attributable
    {
        // The list of graph nodes indexed by their unique linkname
        [SerializeField]
        private StringNodeDictionary nodes = new StringNodeDictionary();

        // The list of graph edges.
        [SerializeField]
        private List<Edge> edges = new List<Edge>();

        // The (view) name of the graph.
        [SerializeField]
        private string viewName = "";

        // The path of the file from which this graph was loaded. Could be the
        /// empty string if the graph was not created by loading it from disk.
        [SerializeField]
        private string path = "";

        /// Adds a node to the graph. 
        /// Preconditions:
        ///   (1) node must not be null
        ///   (2) node.Linkname must be defined.
        ///   (3) a node with node.Linkname must not have been added before
        ///   (4) node must not be contained in another graph
        /// </summary>
        /// <param name="node"></param>
        public void AddNode(Node node)
        {
            if (ReferenceEquals(node, null))
            {
                throw new Exception("node must not be null");
            }
            else if (String.IsNullOrEmpty(node.LinkName))
            {
                throw new Exception("linkname of a node must neither be null nor empty");
            }
            else if (nodes.ContainsKey(node.LinkName))
            {
                throw new Exception("linknames must be unique");
            }
            else if (!ReferenceEquals(node.ItsGraph, null))
            {
                throw new Exception("node " + node.ToString() + " is already in a graph " + node.ItsGraph.Name);
            }
            else
            {
                nodes[node.LinkName] = node;
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
                if (nodes.Remove(node.LinkName))
                {
                    // the edges of node are stored in the node's data structure as well as
                    // in the node's neighbor's data structure
                    foreach (Edge outgoing in node.Outgoings)
                    {
                        Node successor = outgoing.Target;
                        successor.RemoveIncoming(outgoing);
                        edges.Remove(outgoing);
                    }
                    foreach (Edge incoming in node.Incomings)
                    {
                        Node predecessor = incoming.Source;
                        predecessor.RemoveOutgoing(incoming);
                        edges.Remove(incoming);
                    }
                    node.RemoveAllEdges();
                }
                else
                {
                    throw new Exception("node " + node.ToString() + " is not contained in this graph " + Name);
                }
            }
        }

        /// <summary>
        /// Returns true if this graph contains a node with the same unique linkname
        /// as the given node.
        /// Throws an exception if node is null or node has no valid linkname.
        /// </summary>
        /// <param name="node">node to be checked for containment</param>
        /// <returns>true iff there is a node contained in the graph with node.LinkName</returns>
        public bool Contains(Node node)
        {
            if (ReferenceEquals(node, null))
            {
                throw new System.Exception("node must not be null");
            }
            else if (String.IsNullOrEmpty(node.LinkName))
            {
                throw new System.Exception("linkname of a node must neither be null nor empty");
            }
            else
            {
                return nodes.ContainsKey(node.LinkName);
            }
        }

        /// <summary>
        /// Returns the node with the given unique linkname if it exists; otherwise null.
        /// </summary>
        /// <param name="linkname">unique linkname</param>
        /// <returns>node with the given unique linkname if it exists; otherwise null</returns>
        public Node GetNode(string linkname)
        {
            if (String.IsNullOrEmpty(linkname))
            {
                throw new System.Exception("linkname must neither be null nor empty");
            }
            else if (nodes.TryGetValue(linkname, out Node node))
            {
                return node;
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
                    edges.Add(edge);
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
                if (!edges.Remove(edge))
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
            get => viewName;
            set => viewName = value;
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
            return edges;
        }

        /// <summary>
        /// Returns the node with the given unique linkname. If there is no
        /// such node, node will be null and false will be returned; otherwise
        /// true will be returned.
        /// </summary>
        /// <param name="linkname">unique linkname of the searched node</param>
        /// <param name="node">the found node, otherwise null</param>
        /// <returns>true if a node could be found</returns>
        public bool TryGetNode(string linkname, out Node node)
        {
            return nodes.TryGetValue(linkname, out node);
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
        internal void DumpTree()
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
        /// Dumps the hierarchy for given root by adding level many blanks 
        /// as indentation. Used for debugging.
        /// </summary>
        private void DumpTree(Node root, int level)
        {
            string indentation = "";
            for (int i = 0; i < level; i++)
            {
                indentation += "-";
            }            
            Debug.Log(indentation + root.LinkName + "\n");
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
        /// nodes by their names (either Source.Name or Linkname).
        /// </summary>
        public void SortHierarchyByName()
        {
            SortHierarchy(Node.CompareTo);
        }

        /// <summary>
        /// Returns the maximal depth of the graph. Precondition: Graph must be tree.
        /// </summary>
        /// <returns>The maximal depth of the graph.</returns>
        public int GetMaxDepth()
        {
            return GetMaxDepth(GetRoots(), -1);
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

            foreach (Edge edge in this.Edges())
            {
                if (nodes.Contains(edge.Source) && nodes.Contains(edge.Target))
                {
                    result.Add(edge);
                }
            }
            return result;
        }

        private int GetMaxDepth(List<Node> nodes, int currentDepth)
        {
            int max = currentDepth + 1;
            for (int i = 0; i < nodes.Count; i++)
            {
                max = Math.Max(max, GetMaxDepth(nodes[i].Children(), currentDepth + 1));
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
            result += " \"name\": \"" + viewName + "\",\n";
            // its own attributes
            result += base.ToString();
            // its nodes
            foreach (Node node in nodes.Values)
            {
                result += node.ToString() + ",\n";
            }
            foreach (Edge edge in edges)
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
            target.viewName = this.viewName;
            target.path = this.path;
            CopyNodesTo(target);
            CopyEdgesTo(target);
            CopyHierarchy(this, target);
        }

        private void CopyNodesTo(Graph target)
        {
            target.nodes = new StringNodeDictionary();
            foreach (var entry in this.nodes)
            {
                Node node = (Node)entry.Value.Clone();
                target.AddNode(node);
            }
        }

        private void CopyEdgesTo(Graph target)
        {
            target.edges = new List<Edge>();
            foreach (Edge edge in this.edges)
            {
                Edge clone = (Edge)edge.Clone();
                // set corresponding source
                if (target.TryGetNode(edge.Source.LinkName, out Node from))
                {
                    clone.Source = from;
                }
                else
                {
                    throw new Exception("target graph does not have a node with linkname " + edge.Source.LinkName);
                }
                // set corresponding target
                if (target.TryGetNode(edge.Target.LinkName, out Node to))
                {
                    clone.Target = to;
                }
                else
                {
                    throw new Exception("target graph does not have a node with linkname " + edge.Target.LinkName);
                }
                target.AddEdge(clone);
            }
        }

        private static void CopyHierarchy(Graph fromGraph, Graph toGraph)
        {
            foreach (Node fromRoot in fromGraph.GetRoots())
            {
                if (toGraph.TryGetNode(fromRoot.LinkName, out Node toRoot))
                {
                    CopyHierarchy(fromRoot, toRoot, toGraph);
                }
                else
                {
                    throw new Exception("target graph does not have a node with linkname " + fromRoot.LinkName);
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
                if (toGraph.TryGetNode(fromChild.LinkName, out Node toChild))
                {
                    // fromChild is a parent of fromParent and
                    // toChild must become a child of toParent
                    toParent.AddChild(toChild);
                    CopyHierarchy(fromChild, toChild, toGraph);
                }
                else
                {
                    throw new Exception("target graph does not have a node with linkname " + fromChild.LinkName);
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
        /// If an action ist null, it just won't be called.
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
    }
}