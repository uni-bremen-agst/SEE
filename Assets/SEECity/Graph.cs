using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Implements IGraph.
/// </summary>
public class Graph : Attributable, IGraph
{
    // The list of graph nodes indexed by their unique linkname
    private Dictionary<string, INode> nodes = new Dictionary<string, INode>();

    // The list of graph edges.
    private List<IEdge> edges = new List<IEdge>();

    void IGraph.AddNode(INode node)
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

    void IGraph.AddEdge(IEdge edge)
    {
        edges.Add(edge);
    }

    int IGraph.NodeCount => nodes.Count;

    int IGraph.EdgeCount => edges.Count;

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
        foreach(INode node in nodes.Values)
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

    List<INode> IGraph.Nodes()
    {
        return nodes.Values.ToList();
    }

    List<IEdge> IGraph.Edges()
    {
        return edges;
    }

    bool IGraph.TryGetNode(string linkname, out INode node)
    {
        return nodes.TryGetValue(linkname, out node);
    }
}

