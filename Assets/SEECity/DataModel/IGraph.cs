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
    /// Adds a non-hierarchical edge to the graph.
    /// Precondition: edge must not be null.
    /// </summary>
    /// <param name="edge"></param>
    void AddEdge(IEdge edge);

    /// <summary>
    /// Returns the list of nodes without parent.
    /// </summary>
    /// <returns>root nodes of the hierarchy</returns>
    List<INode> GetRoots();

    /// <summary>
    /// Adds a node to the graph. 
    /// Preconditions:
    ///   (1) node must not be null
    ///   (2) node.Linkname must be defined.
    ///   (3) a node with node.Linkname must not have been added before
    /// </summary>
    /// <param name="node"></param>
    void AddNode(INode node);

    /// <summary>
    /// Returns all nodes of the graph.
    /// </summary>
    /// <returns>all nodes</returns>
    List<INode> Nodes();

    /// <summary>
    /// Returns all non-hierarchical edges of the graph.
    /// </summary>
    /// <returns>all non-hierarchical edges</returns>
    List<IEdge> Edges();

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