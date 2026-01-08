using SEE.Layout;
using SEE.Layout.NodeLayouts;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SEE.Layout.NodeLayouts
{
  public class ZamaLayout : NodeLayout, IIncrementalNodeLayout
  {
    static ZamaLayout()
    {
      Name = "ZamaLayout";
    }

    private ZamaLayout oldLayout;
    public IIncrementalNodeLayout OldLayout
    {
      set
      {
        if (value is ZamaLayout layout)
        {
          oldLayout = layout;
          Debug.Log("ZamaLayout: OldLayout has been set.");
        }
        else
        {
          throw new ArgumentException(
              $"Predecessor of {nameof(ZamaLayout)} was not an {nameof(ZamaLayout)}.");
        }
      }
    }

    private Dictionary<ILayoutNode, NodeTransform> result = new();

    Vector3 centerPosition;

    Vector2 rectangle;

    public float padding;
    public float currentX;
    public float currentZ;
    public float rowHeight;
    public float maxX;

    protected override Dictionary<ILayoutNode, NodeTransform> Layout(
        IEnumerable<ILayoutNode> layoutNodes,
        Vector3 centerPosition,
        Vector2 rectangle)
    {
      this.centerPosition = centerPosition;
      this.rectangle = rectangle;

      result = new Dictionary<ILayoutNode, NodeTransform>();

      var layoutNodeList = layoutNodes.ToList();
      
      if (layoutNodeList.Count == 0)
      {
        return result;
      }

      if (layoutNodeList.Count == 1)
      {
        ILayoutNode layoutNode = layoutNodeList.First();
        result[layoutNode] = new NodeTransform(
            centerPosition.x, 
            centerPosition.z,
            new Vector3(rectangle.x, layoutNode.AbsoluteScale.y, rectangle.y));
        return result;
      }

      /*
      var sortedNodes = layoutNodeList
          .OrderByDescending(n => n.AbsoluteScale.x * n.AbsoluteScale.z)
          .ToList();
       */

      if (oldLayout == null)
      {
        PlaceNodesInGrid(layoutNodeList, centerPosition, rectangle);
      }else
      {
        var sameNodes = layoutNodeList
            .Where(n => oldLayout.result.Keys.Any(oldNode => oldNode.ID == n.ID))
            .ToList();
        // Create a dictionary mapping node IDs to their NodeTransform for fast lookup
        var oldPositions = oldLayout.result.ToDictionary(kvp => kvp.Key.ID, kvp => kvp.Value);
        var nodesToPlace = new List<ILayoutNode>();
        foreach (var node in layoutNodeList)
        {
          if (oldPositions.ContainsKey(node.ID))
          {
            NodeTransform transform = oldPositions[node.ID];
            result[node] = new NodeTransform(
                transform.CenterPosition,
                new Vector3(node.AbsoluteScale.x, node.AbsoluteScale.y, node.AbsoluteScale.z)
            );
          }
          else
          {
            nodesToPlace.Add(node);
          }
        }

        var newNodes = layoutNodeList.Except(sameNodes).ToList();
        //PlaceNodesInGrid(nodesToPlace, centerPosition, rectangle);
        PlaceNodesInGrid(newNodes, centerPosition, rectangle);
      }

      return result;
    }

    private void PlaceNodesInGrid(List<ILayoutNode> nodes, Vector3 centerPosition, Vector2 rectangle)
    {
      if (oldLayout == null)
      {
        padding = 0.02f;
        currentX = -rectangle.x / 2f;
        currentZ = -rectangle.y / 2f;
        rowHeight = 0f;
        maxX = rectangle.x / 2f;

        foreach (var node in nodes)
        {
          float nodeWidth = node.AbsoluteScale.x + padding;
          float nodeDepth = node.AbsoluteScale.z + padding;

          if (currentX + nodeWidth > maxX && currentX > -rectangle.x / 2f)
          {
            currentX = -rectangle.x / 2f;
            currentZ += rowHeight + padding;
            rowHeight = 0f;
          }

          Vector3 nodePosition = new Vector3(
              centerPosition.x + currentX + nodeWidth / 2f,
              groundLevel,
              centerPosition.z + currentZ + nodeDepth / 2f
          );

          result[node] = new NodeTransform(
              nodePosition,
              new Vector3(node.AbsoluteScale.x, node.AbsoluteScale.y, node.AbsoluteScale.z)
          );

          currentX += nodeWidth;
          rowHeight = Mathf.Max(rowHeight, nodeDepth);
        }
      }
      else 
      {
        foreach (var node in nodes)
        {
          float nodeWidth = node.AbsoluteScale.x + oldLayout.padding;
          float nodeDepth = node.AbsoluteScale.z + oldLayout.padding;

          if (oldLayout.currentX + nodeWidth > oldLayout.maxX && oldLayout.currentX > -rectangle.x / 2f)
          {
            oldLayout.currentX = -rectangle.x / 2f;
            oldLayout.currentZ += oldLayout.rowHeight + oldLayout.padding;
            oldLayout.rowHeight = 0f;
          }

          Vector3 nodePosition = new Vector3(
              centerPosition.x + oldLayout.currentX + nodeWidth / 2f,
              groundLevel,
              centerPosition.z + oldLayout.currentZ + nodeDepth / 2f
          );

          result[node] = new NodeTransform(
              nodePosition,
              new Vector3(node.AbsoluteScale.x, node.AbsoluteScale.y, node.AbsoluteScale.z)
          );

          oldLayout.currentX += nodeWidth;
          oldLayout.rowHeight = Mathf.Max(oldLayout.rowHeight, nodeDepth);
        }
      }


      
    }
  }
}
