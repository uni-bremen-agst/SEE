using SEE.Layout;
using SEE.Layout.NodeLayouts;
using SEE.Layout.NodeLayouts.RectanglePacking;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Pseudocode plan:
// 1. Create a method Layout that takes nodes, center position, and rectangle size, and returns a dictionary of node transforms.
// 2. Create a method PackRectangles that takes nodes and rectangle size, and returns a list of packed rectangles (with node references).
// 3. In Layout, call PackRectangles, then assign positions/scales to each node based on the packed rectangles and center position.

namespace SEE.Layout.NodeLayouts
{
  public class ZamaLayout : NodeLayout, IIncrementalNodeLayout
  {
    static ZamaLayout()
    {
      Name = "ZamaLayout";
    }

    private readonly Dictionary<ILayoutNode, PTree> packingTrees = new();

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

    /// <summary>
    /// Lays out the nodes by packing their rectangles and assigning positions/scales.
    /// </summary>
    /// <param name="layoutNodes">The nodes to be laid out.</param>
    /// <param name="centerPosition">The center of the rectangle in worldspace.</param>
    /// <param name="rectangle">The size of the rectangle (width, depth).</param>
    /// <returns>A dictionary mapping each node to its NodeTransform.</returns>
    protected override Dictionary<ILayoutNode, NodeTransform> Layout(
        IEnumerable<ILayoutNode> layoutNodes,
        Vector3 centerPosition,
        Vector2 rectangle)
    {

      /*
       
       
      if (oldLayout != null)
      {
        Debug.Log($"Layout called with {layoutNodes.Count()} nodes.");
        // Deep copy: clone each key and value
        this.result = oldLayout.result.ToDictionary(
            kvp => (ILayoutNode)kvp.Key.Clone(),
            kvp => (NodeTransform)kvp.Value.Clone()
        );
      }
      else
      {
        Debug.Log("Layout: No old layout available, starting fresh.");
      }
       */

      var packedRects = PackRectangles(layoutNodes, rectangle);
      

      float minX = packedRects.Min(r => r.x);
      float minZ = packedRects.Min(r => r.z);
      float maxX = packedRects.Max(r => r.x + r.width);
      float maxZ = packedRects.Max(r => r.z + r.depth);
      float centerX = (minX + maxX) / 2f;
      float centerZ = (minZ + maxZ) / 2f;

      foreach (var rect in packedRects)
      {
        // Center the packed layout at centerPosition
        float worldX = rect.x - centerX + centerPosition.x + (rectangle.x / 2f);
        float worldZ = rect.z - centerZ + centerPosition.z + (rectangle.y / 2f);
        Vector3 nodeCenter = new Vector3(worldX + rect.width / 2f, groundLevel, worldZ + rect.depth / 2f);
        result[rect.node] = new NodeTransform(nodeCenter, new Vector3(rect.width, rect.node.AbsoluteScale.y, rect.depth));
      }
      return result;
    }

    /// <summary>
    /// Packs rectangles for the nodes using a simple row-based algorithm.
    /// </summary>
    /// <param name="layoutNodes">The nodes to be packed.</param>
    /// <param name="rectangle">The size of the rectangle (width, depth).</param>
    /// <returns>A list of packed rectangles with node references.</returns>
    private List<(ILayoutNode node, float x, float z, float width, float depth)> PackRectangles(
        IEnumerable<ILayoutNode> layoutNodes,
        Vector2 rectangle)
    {
      var nodes = layoutNodes.ToList();
      var nodeAreas = nodes.Select(n => new
      {
        Node = n,
        Width = Mathf.Max(n.AbsoluteScale.x, 0.01f),
        Depth = Mathf.Max(n.AbsoluteScale.z, 0.01f)
      })
      .OrderByDescending(x => x.Width * x.Depth)
      .ToList();

      float padding = 0.02f;
      float startX = padding;
      float startZ = padding;
      float maxRowDepth = 0f;
      float currentX = startX;
      float currentZ = startZ;
      float rectWidth = rectangle.x - 2 * padding;
      float rectDepth = rectangle.y - 2 * padding;

      List<(ILayoutNode node, float x, float z, float width, float depth)> packed = new();

      foreach (var nodeInfo in nodeAreas)
      {
        float width = nodeInfo.Width;
        float depth = nodeInfo.Depth;

        if (currentX + width > startX + rectWidth)
        {
          currentX = startX;
          currentZ += maxRowDepth + padding;
          maxRowDepth = 0f;
        }

        if (currentZ + depth > startZ + rectDepth)
          break;

        packed.Add((nodeInfo.Node, currentX, currentZ, width, depth));

        currentX += width + padding;
        if (depth > maxRowDepth)
          maxRowDepth = depth;
      }

      return packed;
    }
  }

}
