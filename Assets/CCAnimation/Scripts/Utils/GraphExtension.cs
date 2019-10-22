using SEE.DataModel;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class GraphExtension
{
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

    public static void Traverse(this Graph graph, Action<Node> innerNodeAction, Action<Node> leafAction)
    {
        Traverse(graph, DoNothing, innerNodeAction, leafAction);
    }

    public static void Traverse(this Graph graph, Action<Node> leafAction)
    {
        Traverse(graph, DoNothing, leafAction);
    }

    private static void TraverseTree(Node node, Action<Node> treeAction, Action<Node> leafAction)
    {
        if (node.IsLeaf())
        {
            leafAction(node);
        }
        else
        {
            treeAction(node);
            node.Children().ForEach(childNode => TraverseTree(childNode, treeAction, leafAction));
        }
    }

    private static void DoNothing(Node node) { }
}
