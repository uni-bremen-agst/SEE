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

        /// Adds a node to the graph. 
        /// Preconditions:
        ///   (1) node must not be null
        ///   (2) node.Linkname must be defined.
        ///   (3) a node with node.Linkname must not have been added before
        /// </summary>
        /// <param name="node"></param>
        public void AddNode(Node node)
        {
            if (node == null)
            {
                throw new System.Exception("node must not be null");
            }
            if (String.IsNullOrEmpty(node.LinkName))
            {
                throw new System.Exception("linkname of a node must neither be null nor empty");
            }
            if (nodes.ContainsKey(node.LinkName))
            {
                throw new System.Exception("linknames must be unique");
            }
            nodes[node.LinkName] = node;
        }

        /// <summary>
        /// Adds a non-hierarchical edge to the graph.
        /// Precondition: edge must not be null.
        /// </summary>
        public void AddEdge(Edge edge)
        {
            edges.Add(edge);
        }

        /// <summary>
        /// The number of nodes of the graph.
        /// </summary>
        public int NodeCount => nodes.Count;

        /// <summary>
        /// The number of edges of the graph.
        /// </summary>
        public int EdgeCount => edges.Count;

        private void Awake()
        {
            Debug.Log("Graph " + name + " with " + NodeCount + " nodes and " + EdgeCount + " edges.\n");
        }

        [SerializeField]
        private string viewName = "";

        /// <summary>
        /// Name of the graph (the view name of the underlying RFG).
        /// </summary>
        public string Name
        {
            get => viewName;
            set => viewName = value;
        }

        [SerializeField]
        private string path = "";

        /// <summary>
        /// The path of the file from which this graph was loaded. Could be the
        /// empty string if the graph was created by loading it from disk.
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
                if (node.Parent == null)
                {
                    result.Add(node);
                }
            }
            return result;
        }

        /// <summary>
        /// Returns the game object representing the graph in the scene.
        /// </summary>
        /// <returns>game object representing the graph in the scene</returns>
        public GameObject GetGraph()
        {
            return this.gameObject;
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
        /// Returns all game objects representing the nodes of the graph in the scene.
        /// </summary>
        /// <returns>all node game objects</returns>
        public List<GameObject> GetNodes()
        {
            List<GameObject> result = new List<GameObject>();
            foreach (Node node in nodes.Values)
            {
                result.Add(node.gameObject);
            }
            return result;
        }

        /// <summary>
        /// Returns all game objects representing the edges of the graph in the scene.
        /// </summary>
        /// <returns>all edge game objects</returns>
        public List<GameObject> GetEdges()
        {
            List<GameObject> result = new List<GameObject>();
            foreach (Edge edge in edges)
            {
                result.Add(edge.gameObject);
            }
            return result;
        }

        /// <summary>
        /// Destroys the GameObjects of the graph's nodes and edges including the
        /// associated Node and Edge components as well as the GameObject of the graph 
        /// itself (and its Graph component). The graph is unusable afterward.
        /// </summary>
        public void Destroy()
        {
            foreach (Edge edge in edges)
            {
                Destroyer.DestroyGameObject(edge.gameObject);
            }
            edges.Clear();
            foreach (Node node in nodes.Values)
            {
                Destroyer.DestroyGameObject(node.gameObject);
            }
            nodes.Clear();
            // TODO: Will this actually work and not crash?
            Destroyer.DestroyGameObject(this.gameObject);
        }

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
    }
}