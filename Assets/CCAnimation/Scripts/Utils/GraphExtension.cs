using SEE.DataModel;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class GraphExtension
{
    public static void Traverse(this Graph graph, Action<Node> rootAction, Action<Node> treeAction, Action<Node> leafAction)
    {
        graph.GetRoots().ForEach(
            rootNode =>
            {
                rootAction(rootNode);
                TraverseTree(rootNode, treeAction, leafAction);
            }
        );
    }

    public static void Traverse(this Graph graph, Action<Node> treeAction, Action<Node> leafAction)
    {
        Traverse(graph, DoNothing, treeAction, leafAction);
        graph.GetRoots().ForEach(rootNode => TraverseTree(rootNode, treeAction, leafAction));
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
