using SEE;
using SEE.DataModel;
using SEE.Layout;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// A CCALayout uses a given NodeLayout to calculate the Layout and save
/// it for later use.
/// </summary>
public class CCALayout
{
    /// <summary>
    /// The calculated NodeTransforms representing the layout.
    /// </summary>
    public readonly Dictionary<string, NodeTransform> nodeTransforms = new Dictionary<string, NodeTransform>();

    /// <summary>
    /// Returns a NodeTransform for a given node, using the Node.LinkName attribut.
    /// </summary>
    /// <param name="node"></param>
    /// <returns></returns>
    public NodeTransform GetNodeTransform(Node node)
    {
        nodeTransforms.TryGetValue(node.LinkName, out var nodeTransform);
        return nodeTransform;
    }

    /// <summary>
    /// Calculates the layout data using the given NodeLayout, IScale and ObjectManager for the given graph.
    /// </summary>
    /// <param name="objectManager"></param>
    /// <param name="scaler"></param>
    /// <param name="nodeLayout"></param>
    /// <param name="graph"></param>
    /// <param name="graphSettings"></param>
    public void Calculate(AbstractCCAObjectManager objectManager, IScale scaler, NodeLayout nodeLayout, Graph graph, GraphSettings graphSettings)
    {
        var gameObjects = new List<GameObject>();
        graph.Traverse(
            rootNode =>
            {
                objectManager.GetInnerNode(rootNode, out var inner);
                gameObjects.Add(inner);
            },
            innerNode =>
            {
                objectManager.GetInnerNode(innerNode, out var inner);
                gameObjects.Add(inner);
            },
            leafNode =>
            {
                objectManager.GetLeaf(leafNode, out var leaf);
                var size = new Vector3(
                    scaler.GetNormalizedValue(graphSettings.WidthMetric, leafNode),
                    scaler.GetNormalizedValue(graphSettings.HeightMetric, leafNode),
                    scaler.GetNormalizedValue(graphSettings.DepthMetric, leafNode)
                );
                objectManager.NodeFactory.SetSize(leaf, size);
                gameObjects.Add(leaf);
            }
        );

        var layoutData = nodeLayout.Layout(gameObjects);
        layoutData.Keys.ToList().ForEach(key =>
        {
            var node = key.GetComponent<NodeRef>().node;
            var nodeTransform = layoutData[key];
            if (node.IsLeaf())
            {
                var size = new Vector3(
                    scaler.GetNormalizedValue(graphSettings.WidthMetric, node),
                    scaler.GetNormalizedValue(graphSettings.HeightMetric, node),
                    scaler.GetNormalizedValue(graphSettings.DepthMetric, node)
                );
                nodeTransform.scale = size;
            }
            nodeTransforms.Add(node.LinkName, nodeTransform);
        });
    }
}
