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
    /// If an action ist null, it just won't be called.
    /// </summary>
    /// <param name="graph">The graph to traverse.</param>
    /// <param name="rootAction">Function that is called on root nodes.</param>
    /// <param name="innerNodeAction">Function that is called when node is not a leaf.</param>
    /// <param name="leafAction">Function that is called when node is a leaf.</param>
    public static void Traverse(this Graph graph, Action<Node> rootAction, Action<Node> innerNodeAction, Action<Node> leafAction)
    {
        graph.AssertNotNull("graph");

        graph.GetRoots().ForEach(
            rootNode =>
            {
                rootAction?.Invoke(rootNode);
                rootNode.Children().ForEach(child => TraverseTree(child, innerNodeAction, leafAction));
            }
        );
    }

    /// <summary>
    /// Traverses a given graph. On every node that is a leaf,
    /// leafAction is called, otherwise innerNodeAction is called.
    /// If an action ist null, it just won't be called.
    /// </summary>
    /// <param name="graph">The graph to traverse.</param>
    /// <param name="innerNodeAction">Function that is called when node is not a leaf.</param>
    /// <param name="leafAction">Function that is called when node is a leaf.</param>
    public static void Traverse(this Graph graph, Action<Node> innerNodeAction, Action<Node> leafAction)
    {
        Traverse(graph, null, innerNodeAction, leafAction);
    }

    /// <summary>
    /// Traverses a given graph recursively. On every node that is a leaf,
    /// leafAction is called.
    /// If an action ist null, it just won't be called.
    /// </summary>
    /// <param name="graph">The graph to traverse.</param>
    /// <param name="leafAction">Function that is called when node is a leaf.</param>
    public static void Traverse(this Graph graph, Action<Node> leafAction)
    {
        Traverse(graph, null, leafAction);
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
