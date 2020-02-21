//Copyright 2020 Florian Garbade

//Permission is hereby granted, free of charge, to any person obtaining a
//copy of this software and associated documentation files (the "Software"),
//to deal in the Software without restriction, including without limitation
//the rights to use, copy, modify, merge, publish, distribute, sublicense,
//and/or sell copies of the Software, and to permit persons to whom the Software
//is furnished to do so, subject to the following conditions:

//The above copyright notice and this permission notice shall be included in
//all copies or substantial portions of the Software.

//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
//INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
//PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
//LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
//TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE
//USE OR OTHER DEALINGS IN THE SOFTWARE.
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
