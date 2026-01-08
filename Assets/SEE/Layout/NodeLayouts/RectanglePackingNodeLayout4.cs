using SEE.Layout.NodeLayouts.RectanglePacking;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SEE.Layout.NodeLayouts
{
  public class RectanglePackingNodeLayout4 : NodeLayout
  {
    static RectanglePackingNodeLayout4()
    {
      Name = "Rectangle Packing4";
    }

    private static ATree packingTree;
    private static bool isInitialized = false;


    protected override Dictionary<ILayoutNode, NodeTransform> Layout(
    IEnumerable<ILayoutNode> layoutNodes,
    Vector3 centerPosition,
    Vector2 rectangle)
    {
      // Initialize tree on first use or after reset
      if (!isInitialized || packingTree == null)
      {
        packingTree = new ATree(rectangle);
        isInitialized = true;
      }

      Dictionary<ILayoutNode, NodeTransform> layoutResult = new();

      IList<ILayoutNode> layoutNodeList = layoutNodes.ToList();

      // Fast path: single node
      if (layoutNodeList.Count == 1)
      {
        ILayoutNode node = layoutNodeList[0];
        layoutResult[node] = new NodeTransform(0, 0, node.AbsoluteScale);
        return layoutResult;
      }

      // -------------------------------------------------
      // 1. Initialize leaf transforms and collect new ones
      // -------------------------------------------------
      List<ILayoutNode> newLeaves = new();

      foreach (ILayoutNode node in layoutNodeList)
      {
        if (!node.IsLeaf)
          continue;

        Vector3 scale = node.AbsoluteScale;
        float padding = Padding(scale.x, scale.z);
        scale.x += padding;
        scale.z += padding;

        // Only add if not already packed
        if (!layoutResult.ContainsKey(node))
        {
          layoutResult[node] = new NodeTransform(0, 0, scale);
          newLeaves.Add(node);
        }
      }

      // -------------------------------------------------
      // 2. Leaf-only incremental packing
      // -------------------------------------------------
      if (newLeaves.Count > 0 && newLeaves.Count == layoutNodeList.Count)
      {
        Pack(layoutResult, newLeaves, groundLevel);
        RemovePadding(layoutResult);
        return layoutResult;
      }

      // -------------------------------------------------
      // 3. Hierarchical layout
      // -------------------------------------------------
      ICollection<ILayoutNode> roots = LayoutNodes.GetRoots(layoutNodeList);

      if (roots.Count == 0)
        return layoutResult;

      if (roots.Count > 1)
        throw new System.Exception("Graph has more than one root node.");

      ILayoutNode root = roots.First();

      Vector2 area = PlaceNodes(layoutResult, root, groundLevel);

      layoutResult[root] = new NodeTransform(
          0,
          0,
          new Vector3(area.x, root.AbsoluteScale.y, area.y)
      );

      RemovePadding(layoutResult);

      return layoutResult;
    }

    /// <summary>
    /// Resets the static packing tree. Call this when you need a fresh layout.
    /// </summary>
    public static void ResetPackingTree()
    {
      packingTree = null;
      isInitialized = false;
    }

    private static void MakeContained
        (Dictionary<ILayoutNode, NodeTransform> layout,
         ILayoutNode parent)
    {
      NodeTransform parentTransform = layout[parent];
      Vector3 parentExtent = parentTransform.Scale / 2.0f;
      float xCorner = parentTransform.X - parentExtent.x;
      float zCorner = parentTransform.Z - parentExtent.z;

      foreach (ILayoutNode child in parent.Children())
      {
        layout[child].MoveBy(xCorner, zCorner);
        MakeContained(layout, child);
      }
    }

    private static void RemovePadding(Dictionary<ILayoutNode, NodeTransform> layout)
    {
      ICollection<ILayoutNode> layoutNodes = new List<ILayoutNode>(layout.Keys);

      foreach (ILayoutNode layoutNode in layoutNodes)
      {
        if (layoutNode.IsLeaf)
        {
          NodeTransform value = layout[layoutNode];
          Vector3 scale = value.Scale;
          float reversePadding = ReversePadding(scale.x, scale.z);
          layout[layoutNode].ExpandBy(-reversePadding, -reversePadding);
        }
      }
    }

    private Vector2 PlaceNodes(Dictionary<ILayoutNode, NodeTransform> layout, ILayoutNode node, float groundLevel)
    {
      if (node.IsLeaf)
      {
        return new Vector2(node.AbsoluteScale.x, node.AbsoluteScale.z);
      }
      else
      {
        ICollection<ILayoutNode> children = node.Children();

        foreach (ILayoutNode child in children)
        {
          if (!child.IsLeaf)
          {
            Vector2 childArea = PlaceNodes(layout, child, groundLevel);
            layout[child] = new NodeTransform(0, 0,
                                              new Vector3(childArea.x, child.AbsoluteScale.y, childArea.y));
          }
        }
        if (children.Count > 0)
        {
          Vector2 area = Pack(layout, children.Cast<ILayoutNode>().ToList(), groundLevel);
          float padding = Padding(area.x, area.y);
          return new Vector2(area.x + padding, area.y + padding);
        }
        else
        {
          return new Vector2(node.AbsoluteScale.x, node.AbsoluteScale.z);
        }
      }
    }

    private static float AreaSize(NodeTransform node)
    {
      Vector3 size = node.Scale;
      return size.x * size.z;
    }

    private static Vector2 GetRectangleSize(NodeTransform node)
    {
      Vector3 size = node.Scale;
      return new Vector2(size.x, size.z);
    }

    private static Vector2 Sum(List<ILayoutNode> nodes, Dictionary<ILayoutNode, NodeTransform> layoutResult)
    {
      Vector2 result = Vector2.zero;
      foreach (ILayoutNode element in nodes)
      {
        Vector3 size = layoutResult[element].Scale;
        result.x += size.x;
        result.y += size.z;
      }
      return result;
    }

    private Vector2 Pack(
     Dictionary<ILayoutNode, NodeTransform> layout,
     List<ILayoutNode> nodes,
     float groundLevel)
    {
      // Still sort so larger rectangles get better placements
      nodes.Sort((a, b) =>
          AreaSize(layout[b]).CompareTo(AreaSize(layout[a]))
      );

      foreach (ILayoutNode el in nodes)
      {
        Vector2 requiredSize = GetRectangleSize(layout[el]);

        // Incremental insert into persistent tree
        PNode fitNode = packingTree.Insert(requiredSize);

        Vector3 scale = layout[el].Scale;

        layout[el] = new NodeTransform(
            fitNode.Rectangle.Position.x + scale.x * 0.5f,
            fitNode.Rectangle.Position.y + scale.z * 0.5f,
            scale
        );
      }

      // The tree tracks the occupied bounding rectangle
      return packingTree.Coverage;
    }
  }

  class ATree
  {
    private PNode _root;
    private Vector2 _coverage;
    public IList<PNode> FreeLeaves;


    public ATree(Vector2 initialSize)
    {
      _root = new PNode(Vector2.zero, initialSize, null);
      _coverage = Vector2.zero;
      FreeLeaves = new List<PNode>
            {
                _root
            };
    }



    public void EnsureFits(Vector2 requiredCorner)
    {
      if (FitsInto(requiredCorner, _root.Rectangle.Size))
        return;

      Vector2 oldSize = _root.Rectangle.Size;

      Vector2 newSize = new Vector2(
        Mathf.Max(oldSize.x, requiredCorner.x) * 1.5f,
        Mathf.Max(oldSize.y, requiredCorner.y) * 1.5f
      );

      // Expand root by creating a new parent that contains the old root
      PNode newRoot = new PNode(Vector2.zero, newSize, null);
      newRoot.Left = _root;
      
      // Create a new free space node for the expanded area
      newRoot.Right = new PNode(
        new Vector2(_root.Rectangle.Size.x, 0),
        new Vector2(newSize.x - _root.Rectangle.Size.x, newSize.y),
        newRoot
      );
      
      // Add the new free space to available leaves
      FreeLeaves.Add(newRoot.Right);

      _root = newRoot;
    }
    public static bool FitsInto(Vector2 sub, Vector2 container)
    {
      return sub.x <= container.x && sub.y <= container.y;
    }

    public void UpdateCoverage(Vector2 corner)
    {
      _coverage.x = Mathf.Max(_coverage.x, corner.x);
      _coverage.y = Mathf.Max(_coverage.y, corner.y);
    }

    public Vector2 Coverage => _coverage;


    public PNode Insert(Vector2 size)
    {
      IList<PNode> candidates = GetSufficientlyLargeLeaves(size);
      
      if (candidates.Count > 0)
      {
        // Found a suitable leaf
        PNode leaf = candidates[0];
        Vector2 corner = leaf.Rectangle.Position + size;
        EnsureFits(corner);

        PNode fit = Split(leaf, size);
        UpdateCoverage(corner);
        return fit;
      }

      // No leaf fits ? grow tree and try once more
      Vector2 requiredSize = _coverage + size;
      EnsureFits(requiredSize);
      
      // After growing, try to find a leaf again
      candidates = GetSufficientlyLargeLeaves(size);
      if (candidates.Count > 0)
      {
        PNode leaf = candidates[0];
        Vector2 corner = leaf.Rectangle.Position + size;
        PNode fit = Split(leaf, size);
        UpdateCoverage(corner);
        return fit;
      }

      // If still no space, create emergency fallback
      // This should rarely happen if tree expansion works correctly
      UnityEngine.Debug.LogWarning($"ATree: Could not fit rectangle of size {size}. Creating fallback node.");
      PNode fallback = new PNode(_coverage, size, null);
      fallback.Occupied = true;
      UpdateCoverage(_coverage + size);
      return fallback;
    }

    public IList<PNode> GetSufficientlyLargeLeaves(Vector2 size)
    {
      List<PNode> result = new();
      foreach (PNode leaf in FreeLeaves)
      {
        if (FitsInto(size, leaf.Rectangle.Size))
        {
          result.Add(leaf);
        }
      }
      return result;
    }
    public PNode Split(PNode node, Vector2 size)
    {
      PNode result;

      // Node is no longer a free leaf. As a matter of fact, technically, it may
      // still be a leaf if the requested size perfectly matches the size of node,
      // so that it is actually not split, but it is not free.
      if (!FreeLeaves.Remove(node))
      {
        throw new Exception("Node to be split is not a free leaf." + node + RectanglePackingNodeLayout1.globalCallCount);
      }
      else if (size.x > node.Rectangle.Size.x || size.y > node.Rectangle.Size.y)
      {
        throw new Exception("Requested size does not fit into this rectangle.");
      }
      else if (size.x == node.Rectangle.Size.x)
      {
        if (size.y == node.Rectangle.Size.y)
        {
          // size.x = rectangle.size.x && size.y = rectangle.size.y. Perfect match.
          node.Occupied = true;
          result = node;
          result.Parent = node.Parent;
        }
        else
        {
          // size.x = rectangle.size.x && size.y < rectangle.size.y
          node.Left = new();
          node.Left.Parent = node;
          node.Left.Direction = PNode.SplitDirection.Left;
          node.Left.Rectangle = new PRectangle(node.Rectangle.Position, size);
          node.Left.Occupied = true;

          node.Right = new();
          node.Right.Parent = node;
          node.Right.Direction = PNode.SplitDirection.Right;
          node.Right.Rectangle = new PRectangle(new Vector2(node.Rectangle.Position.x, node.Rectangle.Position.y + size.y),
                                                new Vector2(node.Rectangle.Size.x, node.Rectangle.Size.y - size.y));
          FreeLeaves.Add(node.Right);
          result = node.Left;
          result.Parent = node;
        }
      }
      else
      {
        // size.x < rectangle.size.x
        if (size.y == node.Rectangle.Size.y)
        {
          // size.x < rectangle.size.x && size.y = rectangle.size.y
          node.Left = new();
          node.Left.Parent = node;
          node.Left.Direction = PNode.SplitDirection.Left;
          node.Left.Rectangle = new PRectangle(node.Rectangle.Position, size);
          node.Left.Occupied = true;

          node.Right = new();
          node.Right.Parent = node;
          node.Right.Direction = PNode.SplitDirection.Right;
          node.Right.Rectangle = new PRectangle(new Vector2(node.Rectangle.Position.x + size.x, node.Rectangle.Position.y),
                                                new Vector2(node.Rectangle.Size.x - size.x, size.y));
          FreeLeaves.Add(node.Right);
          result = node.Left;
          result.Parent = node;
        }
        else
        {
          // size.x < rectangle.size.x && size.y < rectangle.size.y
          // The node will be split vertically into two sub-rectangles. The upper rectangle is
          // left and the lower rectangle is right.
          // The origin of left is the origin of the enclosing rectangle. Its width is the width
          // of the enclosing rectangle. Its depth is the size of the requested rectangle.

          node.Left = new();
          node.Left.Parent = node;
          node.Left.Direction = PNode.SplitDirection.Left;
          node.Left.Rectangle = new PRectangle(node.Rectangle.Position, new Vector2(node.Rectangle.Size.x, size.y));

          node.Right = new();
          node.Right.Parent = node;
          node.Right.Direction = PNode.SplitDirection.Right;
          node.Right.Rectangle = new PRectangle(new Vector2(node.Rectangle.Position.x, node.Rectangle.Position.y + size.y),
                                                new Vector2(node.Rectangle.Size.x, node.Rectangle.Size.y - size.y));
          FreeLeaves.Add(node.Right);

          // The upper enclosed rectangle is split again. Its left rectangle will be the rectangle
          // requested. Its right rectangle is available.
          node.Left.Left = new();
          node.Left.Left.Parent = node.Left;
          node.Left.Left.Direction = PNode.SplitDirection.Left;
          // This space is not available anymore.
          node.Left.Left.Occupied = true;
          // The allocated rectangle is added at the left upper corner of left node.
          node.Left.Left.Rectangle = new PRectangle(node.Left.Rectangle.Position, size);

          // The remaining rectangle sits right of the allocated one and occupies
          // the remaining space of left.
          node.Left.Right = new();
          node.Left.Right.Parent = node.Left;
          node.Left.Right.Direction = PNode.SplitDirection.Right;
          node.Left.Right.Rectangle = new PRectangle(new Vector2(node.Left.Rectangle.Position.x + size.x, node.Left.Rectangle.Position.y),
                                                     new Vector2(node.Left.Rectangle.Size.x - size.x, node.Left.Rectangle.Size.y));
          FreeLeaves.Add(node.Left.Right);
          result = node.Left.Left;
          result.Parent = node.Left;
        }
      }
      return result;
    }

  }
}