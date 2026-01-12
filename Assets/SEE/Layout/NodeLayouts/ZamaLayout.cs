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
          //Debug.Log("ZamaLayout: OldLayout has been set.");
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
    public Dictionary<string, Vector3> positions;
    public static List<List<string>> rows;

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

      padding = 0.02f;
      currentX = -rectangle.x / 2f;
      currentZ = -rectangle.y / 2f;
      rowHeight = 0f;
      maxX = rectangle.x / 2f;

      if (oldLayout == null)
      {
        rows = new List<List<string>>();
        // Initialize static fields only on first layout
        if (layoutNodeList.Count > 0)
        {
          rows.Add(layoutNodeList.Select(n => n.ID).ToList());
        }
        /*
        PlaceNodesInGrid(layoutNodeList, centerPosition, rectangle);

        positions = result.ToDictionary(kvp => kvp.Key.ID, kvp => kvp.Value.CenterPosition);
         */

      }
      else
      {
        var sameNodes = layoutNodeList
            .Where(n => oldLayout.result.Keys.Any(oldNode => oldNode.ID == n.ID))
            .ToList();
        /*
        // Create a dictionary mapping node IDs to their NodeTransform for fast lookup
        var oldPositions = oldLayout.positions;

      //foreach (var node in layoutNodeList)
      //{
      //  if (oldPositions.ContainsKey(node.ID))
      //  {
      //    Vector3 pos = oldPositions[node.ID];
      //    result[node] = new NodeTransform(
      //        pos,
      //        new Vector3(node.AbsoluteScale.x, node.AbsoluteScale.y, node.AbsoluteScale.z)
      //    );
      //  }
          //}
         

        PlaceNodesInGrid(sameNodes, centerPosition, rectangle);
        PlaceNodesInGrid(newNodes, centerPosition, rectangle);
        positions = result.ToDictionary(kvp => kvp.Key.ID, kvp => kvp.Value.CenterPosition);
         */
        var newNodes = layoutNodeList.Except(sameNodes).ToList();
        if (newNodes.Count > 0)
        {
          rows.Add(newNodes.Select(n => n.ID).ToList());
        }
      }

      foreach (var list in rows)
      {
        var sameNodes = layoutNodeList
            .Where(n => list.Any(id => id == n.ID))
            .ToList();
        PlaceNodesInGrid(sameNodes, centerPosition, rectangle);
      }

      //Debug.Log("Debugger2");

      return result;
    }

    private void PlaceNodesInGrid(List<ILayoutNode> nodes, Vector3 centerPosition, Vector2 rectangle)
    {
      //#fixme the positions of each node should be stored in a list with new = done

      

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
            GroundLevel,
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
  }
}
