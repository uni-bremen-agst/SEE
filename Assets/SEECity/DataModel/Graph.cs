using System;
using System.Collections.Generic;
using System.Linq;

namespace SEE
{
    /// <summary>
    /// Implements IGraph.
    /// </summary>
    public class Graph : Attributable, IGraph
    {
        // The list of graph nodes indexed by their unique linkname
        private Dictionary<string, INode> nodes = new Dictionary<string, INode>();

        // The list of graph edges.
        private List<IEdge> edges = new List<IEdge>();

        public void AddNode(INode node)
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

        public void AddEdge(IEdge edge)
        {
            edges.Add(edge);
        }

        public int NodeCount => nodes.Count;

        public int EdgeCount => edges.Count;

        private string viewName = "";

        /// <summary>
        /// Name of the graph (the view name of the underlying RFG).
        /// </summary>
        public string Name
        {
            get => viewName;
            set => viewName = value;
        }

        public override string ToString()
        {
            string result = "{\n";
            result += " \"kind\": graph,\n";
            result += " \"name\": \"" + viewName + "\",\n";
            // its own attributes
            result += base.ToString();
            // its nodes
            foreach (INode node in nodes.Values)
            {
                result += node.ToString() + ",\n";
            }
            foreach (IEdge edge in edges)
            {
                result += edge.ToString() + ",\n";
            }
            result += "}\n";
            return result;
        }

        public List<INode> Nodes()
        {
            return nodes.Values.ToList();
        }

        public List<IEdge> Edges()
        {
            return edges;
        }

        public bool TryGetNode(string linkname, out INode node)
        {
            return nodes.TryGetValue(linkname, out node);
        }

        public List<INode> GetRoots()
        {
            List<INode> result = new List<INode>();
            foreach (INode node in nodes.Values)
            {
                if (node.Parent == null)
                {
                    result.Add(node);
                }
            }
            return result;
        }
    }
}