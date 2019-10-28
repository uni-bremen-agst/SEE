using SEE.DataModel;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Extension that simplifies access and usage of Graphs.
/// </summary>
public static class GraphExtension
{
    /// <summary>
    /// Traverses a given graph. On every root node rootAction is called.
    /// On every node that is a leaf, leafAction is called, otherwise innerNodeAction is called.
    /// </summary>
    /// <param name="graph">The graph to traverse.</param>
    /// <param name="rootAction">Function that is called on root nodes.</param>
    /// <param name="innerNodeAction">Function that is called when node is not a leaf.</param>
    /// <param name="leafAction">Function that is called when node is a leaf.</param>
    public static void Traverse(this Graph graph, Action<Node> rootAction, Action<Node> innerNodeAction, Action<Node> leafAction)
    {
        graph.GetRoots().ForEach(
            rootNode =>
            {
                rootAction(rootNode);
                TraverseTree(rootNode, innerNodeAction, leafAction);
            }
        );
    }

    /// <summary>
    /// Traverses a given graph. On every node that is a leaf,
    /// leafAction is called, otherwise innerNodeAction is called.
    /// </summary>
    /// <param name="graph">The graph to traverse.</param>
    /// <param name="innerNodeAction">Function that is called when node is not a leaf.</param>
    /// <param name="leafAction">Function that is called when node is a leaf.</param>
    public static void Traverse(this Graph graph, Action<Node> innerNodeAction, Action<Node> leafAction)
    {
        Traverse(graph, DoNothing, innerNodeAction, leafAction);
    }

    /// <summary>
    /// Traverses a given graph recursively. On every node that is a leaf,
    /// leafAction is called.
    /// </summary>
    /// <param name="graph">The graph to traverse.</param>
    /// <param name="leafAction">Function that is called when node is a leaf.</param>
    public static void Traverse(this Graph graph, Action<Node> leafAction)
    {
        Traverse(graph, DoNothing, leafAction);
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
            leafAction(node);
        }
        else
        {
            innerNodeAction(node);
            node.Children().ForEach(childNode => TraverseTree(childNode, innerNodeAction, leafAction));
        }
    }

    /// <summary>
    /// A dummy function that does nothing and is intended to fill unused parameters.
    /// </summary>
    /// <param name="node">Unused node.</param>
    private static void DoNothing(Node _) { }
}
