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

    /// <summary>
    /// Adds the given node to the internal node mapping using linkname as 
    /// unique key. Only then, the node can be derived from this graph.
    /// If the node was added previously with a linkname, it will be 
    /// remapped with the new linkname.
    /// </summary>
    /// <param name="node">the node to be added (must not be null)</param>
    /// <param name="linkName">the unique linkname for this node; must neither be null nor empty </param>
    void IGraph.SetLinkname(INode node, string linkName)
    {
        if (node == null)
        {
            throw new System.Exception("node must not be null");
        }
        if (String.IsNullOrEmpty(linkName))
        {
            throw new System.Exception("linkname of a node must neither be null nor empty");
        }

        string oldLinkName = node.LinkName;

        if (!String.IsNullOrEmpty(oldLinkName))
        {
            // the node has had a linkname before; we may need to remove the old key;
            // Remove will do nothing if the key is not contained
            nodes.Remove(oldLinkName);
        }
        // this will also work if oldLinkName == linkName
        nodes[linkName] = node;
        node.LinkName = linkName;
    }

    IEdge IGraph.NewEdge()
    {
        IEdge result = new Edge();
        edges.Add(result);
        return result;
    }

    /// <summary>
    /// Creates and returns a new plain node. 
    /// Note: This method only creates a new node but does not actually add it to
    /// the graph. In order to add it, you need to call SetLinkname later when the
    /// node has a linkname.
    /// </summary>
    /// <returns>a new node (not yet part of the graph)</returns>
    INode IGraph.NewNode()
    {
        return new Node();
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

    INode[] IGraph.Nodes()
    {
        return nodes.Values.ToArray();
    }
}

