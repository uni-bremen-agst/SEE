using SEE;
using SEE.DataModel;
using SEE.Layout;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// TODO DOKU
/// </summary>
public class AbstractCCALayout
{
    /// <summary>
    /// TODO DOKU
    /// </summary>
    public readonly Dictionary<string, NodeTransform> nodeTransforms = new Dictionary<string, NodeTransform>();

    /// <summary>
    /// TODO DOKU
    /// </summary>
    /// <param name="node"></param>
    /// <returns></returns>
    public NodeTransform GetNodeTransform(Node node)
    {
        nodeTransforms.TryGetValue(node.LinkName, out var nodeTransform);
        return nodeTransform;
    }

    /// <summary>
    /// TODO noderef muss gesetzt sein wegen check und in COnstructor auslagern
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
