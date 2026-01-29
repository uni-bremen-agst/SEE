using SEE.Layout;
using SEE.Layout.NodeLayouts;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SEE.Layout.NodeLayouts
{
  public class ZamaLayout : NodeLayout
  {
    static ZamaLayout()
    {
      Name = "ZamaLayout";
    }

    private Dictionary<ILayoutNode, NodeTransform> result = new();

    protected override Dictionary<ILayoutNode, NodeTransform> Layout(
        IEnumerable<ILayoutNode> layoutNodes,
        Vector3 centerPosition,
        Vector2 rectangle)
    {
      result = new Dictionary<ILayoutNode, NodeTransform>();
      var layoutNodeList = layoutNodes.ToList();
      
      if (layoutNodeList.Count == 0)
        return result;

      if (layoutNodeList.Count == 1)
      {
        ILayoutNode layoutNode = layoutNodeList.First();
        result[layoutNode] = new NodeTransform(
            centerPosition.x, 
            centerPosition.z,
            new Vector3(rectangle.x, layoutNode.AbsoluteScale.y, rectangle.y));
        return result;
      }

      PlaceNodesHorizontally(layoutNodeList, centerPosition, rectangle);

      return result;
    }

    private void PlaceNodesHorizontally(List<ILayoutNode> nodes, Vector3 centerPosition, Vector2 rectangle)
    {
      if (nodes.Count == 0)
        return;

      float padding = 0.02f;
      
      // Calculate total width needed
      float totalWidth = 0f;
      foreach (var node in nodes)
      {
        totalWidth += node.AbsoluteScale.x + padding;
      }
      totalWidth -= padding; // Remove last padding

      // Scale down if needed
      float scaleFactor = 1f;
      if (totalWidth > rectangle.x)
      {
        scaleFactor = rectangle.x / totalWidth;
      }

      // Start from the left edge
      float currentX = centerPosition.x - rectangle.x / 2f;
      float zPosition = centerPosition.z;

      nodes.Sort((a, b) => b.AbsoluteScale.x.CompareTo(a.AbsoluteScale.x));

      foreach (var node in nodes)
      {
        float nodeWidth = node.AbsoluteScale.x * scaleFactor;
        float nodeDepth = node.AbsoluteScale.z * scaleFactor;
        float nodeHeight = node.AbsoluteScale.y;

        // Clamp depth to rectangle bounds
        if (nodeDepth > rectangle.y)
        {
          float depthRatio = rectangle.y / nodeDepth;
          nodeDepth = rectangle.y;
          nodeWidth *= depthRatio;
        }

        Vector3 nodePosition = new Vector3(
            currentX + nodeWidth / 2f,
            GroundLevel,
            zPosition
        );

        result[node] = new NodeTransform(
            nodePosition,
            new Vector3(nodeWidth, nodeHeight, nodeDepth)
        );

        currentX += nodeWidth + (padding * scaleFactor);
      }
    }
  }
}
