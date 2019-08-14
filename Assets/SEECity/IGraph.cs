using System.Collections.Generic;

public interface IGraph : IAttributable
{
    /// <summary>
    /// The number of edges of the graph.
    /// </summary>
    int EdgeCount { get; }

    /// <summary>
    /// The number of nodes of the graph.
    /// </summary>
    int NodeCount { get; }

    /// <summary>
    /// The name of the graph.
    /// </summary>
    string Name { get; set; }

    /// <summary>
    /// Returns a new node. The node may or may not be part of the node.
    /// This can be decided by the implementation.
    /// </summary>
    /// <returns></returns>
    INode NewNode();

    /// <summary>
    /// Returns a new edge. The edge may or may not be part of the node.
    /// This can be decided by the implementation.
    /// </summary>
    /// <returns></returns>
    IEdge NewEdge();

    /// <summary>
    /// 
    /// </summary>
    /// <param name="node"></param>
    /// <param name="linkName"></param>
    void SetLinkname(INode node, string linkName);

    /// <summary>
    /// Returns all nodes of the graph.
    /// </summary>
    /// <returns>all nodes</returns>
    INode[] Nodes();

    /// <summary>
    /// Returns all edges of the graph.
    /// </summary>
    /// <returns>all edges</returns>
    IEdge[] Edges();

    /// <summary>
    /// Returns the node with the given unique linkname. If there is no
    /// such node, node will be null and false will be returned; otherwise
    /// true will be returned.
    /// </summary>
    /// <param name="linkname">unique linkname of the searched node</param>
    /// <param name="node">the found node, otherwise null</param>
    /// <returns>true if a node could be found</returns>
    bool TryGetNode(string linkname, out INode node);
}