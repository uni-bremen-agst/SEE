using SEE.DataModel.DG;
using SEE.Game.CityRendering;
using SEE.Layout.NodeLayouts.RectanglePacking;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SEE.Layout.NodeLayouts
{
  /// <summary>
  /// This layout packs rectangles closely together as a set of nested packed rectangles to decrease
  /// the total area of city. The algorithm is based on the dissertation of Richard Wettel
  /// "Software Systems as Cities" (2010); see page 35.
  /// https://www.inf.usi.ch/lanza/Downloads/PhD/Wett2010b.pdf
  /// </summary>
  public class RectanglePackingNodeLayout3 : NodeLayout , IIncrementalNodeLayout
  {
    static RectanglePackingNodeLayout3()
    {
      Name = "Rectangle Packing3";
    }

    public RectanglePackingNodeLayout3 oldLayout;

    public IIncrementalNodeLayout OldLayout
    {
      set
      {
        if (value is RectanglePackingNodeLayout3 layout)
        {
          oldLayout = layout;
        }
        else
        {
          throw new ArgumentException(
              $"Predecessor of {nameof(RectanglePackingNodeLayout3)} was not an {nameof(RectanglePackingNodeLayout3)}.");
        }
      }
    }

    public Dictionary<ILayoutNode, NodeTransform> layoutResult;
    
    // Static tree for incremental packing
    private static PTree staticTree;
    private static bool isInitialized = false;
    private static Vector2 staticCoverage = Vector2.zero;

    /// <summary>
    /// Resets the static packing tree. Call this when you need a fresh layout.
    /// </summary>
    public static void ResetPackingTree()
    {
      staticTree = null;
      isInitialized = false;
      staticCoverage = Vector2.zero;
    }

    /// <summary>
    /// See <see cref="NodeLayout.Layout"/>.
    /// </summary>
    /// <exception cref="System.Exception">thrown if there is more than one root in
    /// <paramref name="layoutNodes"/></exception>
    protected override Dictionary<ILayoutNode, NodeTransform> Layout
        (IEnumerable<ILayoutNode> layoutNodes,
        Vector3 centerPosition,
        Vector2 rectangle)
    {
      layoutResult = new();

      IList<ILayoutNode> layoutNodeList = layoutNodes.ToList();
      if (layoutNodeList.Count == 1)
      {
        ILayoutNode layoutNode = layoutNodeList.First();
        layoutResult[layoutNode] = new NodeTransform(0, 0, layoutNode.AbsoluteScale);
        
        return layoutResult;
      }

      var nodesIds = layoutNodeList.Select(n => n.ID).ToList();

      // Copy existing nodes from oldLayout if available
      if (oldLayout != null && oldLayout.layoutResult != null)
      {
        foreach (var entry in oldLayout.layoutResult)
        {
          // Check if this node still exists in the new layout
          if (nodesIds.Contains(entry.Key.ID))
          {
            ILayoutNode existingNode = layoutNodeList.First(n => n.ID == entry.Key.ID);
            layoutResult[existingNode] = new NodeTransform(
              entry.Value.X,
              entry.Value.Z,
              entry.Value.Scale,
              entry.Value.fitNode
            );
          }
        }
      }

      {
        int numberOfLeaves = 0;
        List<ILayoutNode> newLeaves = new();
        
        foreach (ILayoutNode node in layoutNodeList)
        {
          if (node.IsLeaf)
          {
            numberOfLeaves++;
            
            // Only process new nodes that aren't in layoutResult yet
            if (!layoutResult.ContainsKey(node))
            {
              Vector3 scale = node.AbsoluteScale;
              float padding = Padding(scale.x, scale.z);
              scale.x += padding;
              scale.z += padding;
              layoutResult[node] = new NodeTransform(0, 0, scale);
              newLeaves.Add(node);
            }
          }
        }
        
        if (numberOfLeaves == layoutNodeList.Count)
        {
          // Only pack new leaves
          if (newLeaves.Count > 0)
          {
            Pack(layoutResult, layoutNodeList.ToList(), groundLevel, rectangle);
          }
          RemovePadding(layoutResult);
          
          return layoutResult;
        }
      }
      
      ICollection<ILayoutNode> roots = LayoutNodes.GetRoots(layoutNodeList);
      if (roots.Count == 0)
      {
        
        return layoutResult;
      }
      else if (roots.Count > 1)
      {
        throw new System.Exception("Graph has more than one root node.");
      }
      else
      {
        ILayoutNode root = roots.FirstOrDefault();
        Vector2 area = PlaceNodes(layoutResult, root, groundLevel, rectangle);
        
        layoutResult[root] = new NodeTransform(0, 0, new Vector3(area.x, root.AbsoluteScale.y, area.y));
        RemovePadding(layoutResult);
        
        MakeContained(layoutResult, root);

        
        return layoutResult;
      }
    }

    //////////////////////////////////////////////////////////////////////////////////////////////
    public ILayoutNode CreateNewNodeOfSameType(ILayoutNode layoutNode)
    {
      Type type = layoutNode.GetType();
      switch (type.Name)
      {
        case "LayoutGraphNode":
          Node node = ((LayoutGraphNode)layoutNode).ItsNode;
          LayoutGraphNode graphNode = new LayoutGraphNode(node)
          {
            AbsoluteScale = layoutNode.AbsoluteScale,
            CenterPosition = layoutNode.CenterPosition,
            Rotation = layoutNode.Rotation,
            Level = layoutNode.Level
          };
          return graphNode;
        case "LayoutVertex":
          LayoutVertex layoutVertex = new LayoutVertex(layoutNode.ID)
          {
            AbsoluteScale = layoutNode.AbsoluteScale,
            CenterPosition = layoutNode.CenterPosition,
            Rotation = layoutNode.Rotation
          };
          Debug.Log($"Creating of {type.Name} successful.");
          return layoutVertex;
        default:
          throw new NotImplementedException($"Creation of new node of type {type.Name} is not implemented.");
      }

    }

    //***********************************************************************************
    public Dictionary<ILayoutNode, NodeTransform> CopyOldLayout(Dictionary<ILayoutNode, NodeTransform> layoutRes)
    {
      Dictionary<ILayoutNode, NodeTransform> copiedLayout = new();
      foreach (var entry in layoutRes)
      {
        copiedLayout[entry.Key] = new NodeTransform(
          entry.Value.X,
          entry.Value.Z,
          entry.Value.Scale,
          entry.Value.fitNode
        );
      }
      return copiedLayout;
    }
    //***********************************************************************************
    public Dictionary<ILayoutNode, NodeTransform> CopyOldLayout1(Dictionary<ILayoutNode, NodeTransform> layoutRes)
    {
      Dictionary<ILayoutNode, NodeTransform> copiedLayout = new();
      foreach (var entry in layoutRes)
      {
        var oldNode = entry.Key;
        var oldTransform = entry.Value;
        var newNode = CreateNewNodeOfSameType(oldNode);
        var newTransform = new NodeTransform(oldTransform.X, oldTransform.Z, oldTransform.Scale, oldTransform.fitNode);
        copiedLayout[newNode] = newTransform;
      }
      return copiedLayout;
      

    }
    
    //***********************************************************************************


    /// <summary>
    /// Adjusts the layout so that all rectangles are truly nested. This is necessary
    /// because the origin of the rectangle packing layout is different from the
    /// Unity's co-ordinate system. The rectangle packing layout's origin is upper left
    /// and grows to the right and *down*, while the X/Z plane in unity grows to the
    /// right and *up*.
    /// </summary>
    /// <param name="layout">the layout to be adjusted</param>
    /// <param name="parent">the parent node whose children are to be adjusted</param>
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

    private Vector2 PlaceNodes(Dictionary<ILayoutNode, NodeTransform> layout, ILayoutNode node, float groundLevel, Vector2 rectangle)
    {
      if (node.IsLeaf)
      {
        return new Vector2(node.AbsoluteScale.x, node.AbsoluteScale.z);
      }
      else
      {
        ICollection<ILayoutNode> children = node.Children();
        List<ILayoutNode> newChildren = new();

        foreach (ILayoutNode child in children)
        {
          // Check if this is a new child that needs layout
          if (!layout.ContainsKey(child))
          {
            if (!child.IsLeaf)
            {
              Vector2 childArea = PlaceNodes(layout, child, groundLevel, rectangle);
              layout[child] = new NodeTransform(0, 0,
                                                new Vector3(childArea.x, child.AbsoluteScale.y, childArea.y));
            }
            newChildren.Add(child);
          }
          else if (!child.IsLeaf)
          {
            // Update inner node sizes even if they exist
            Vector2 childArea = PlaceNodes(layout, child, groundLevel, rectangle);
            Vector3 currentScale = layout[child].Scale;
            layout[child] = new NodeTransform(
              layout[child].X,
              layout[child].Z,
              new Vector3(childArea.x, currentScale.y, childArea.y)
            );
          }
        }
        
        if (children.Count > 0)
        {
          // Only pack new children
          if (newChildren.Count > 0)
          {
            Vector2 area = Pack(layout, newChildren, groundLevel, rectangle);
          }
          
          // Calculate total area including existing children
          float maxX = 0;
          float maxY = 0;
          foreach (ILayoutNode child in children)
          {
            if (layout.ContainsKey(child))
            {
              Vector3 scale = layout[child].Scale;
              NodeTransform transform = layout[child];
              maxX = Mathf.Max(maxX, transform.X + scale.x / 2.0f);
              maxY = Mathf.Max(maxY, transform.Z + scale.z / 2.0f);
            }
          }
          
          float padding = Padding(maxX, maxY);
          return new Vector2(maxX + padding, maxY + padding);
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

    private Vector2 Pack(Dictionary<ILayoutNode, NodeTransform> layout, List<ILayoutNode> nodes, float groundLevel, Vector2 rectangle)
    {
      // Sort nodes by area (largest first)
      nodes.Sort(delegate (ILayoutNode left, ILayoutNode right)
      { return AreaSize(layout[right]).CompareTo(AreaSize(layout[left])); });

      Vector2 worstCaseSize = Sum(nodes, layout);
      
      // Initialize static tree on first use or if not initialized
      if (!isInitialized || staticTree == null)
      {
        staticTree = new PTree(Vector2.zero, 1.1f * worstCaseSize);
        isInitialized = true;
        staticCoverage = Vector2.zero;
      }
      else
      {
        // Calculate required size considering static coverage and new nodes
        Vector2 requiredSize = new Vector2(
          Mathf.Max(staticCoverage.x, worstCaseSize.x),
          Mathf.Max(staticCoverage.y, worstCaseSize.y)
        );
        
        Vector2 currentSize = staticTree.Root.Rectangle.Size;
        
        if (!PTree.FitsInto(requiredSize, currentSize))
        {
          // Need to expand the tree
          staticTree = new PTree(Vector2.zero, 1.1f * requiredSize);
          
          // Re-mark existing nodes' fitNodes as occupied in the new tree
          // Don't repack them - use their existing fitNode positions
          foreach (var entry in layout)
          {
            if (!nodes.Contains(entry.Key) && entry.Key.IsLeaf && entry.Value.fitNode != null)
            {
              // Get the existing fitNode's rectangle
              PRectangle existingRect = entry.Value.fitNode.Rectangle;
              Vector2 existingSize = existingRect.Size;
              Vector2 existingPos = existingRect.Position;
              
              // Find the corresponding node in the new tree and mark it as occupied
              PNode nodeToSplit = FindNodeAtPosition(staticTree, existingPos, existingSize);
              if (nodeToSplit != null)
              {
                staticTree.Split(nodeToSplit, existingSize);
              }
            }
          }
        }
      }

      Dictionary<PNode, float> preservers = new();
      Dictionary<PNode, float> expanders = new();

      foreach (ILayoutNode el in nodes)
      {
        Vector2 requiredSize = GetRectangleSize(layout[el]);

        preservers.Clear();
        expanders.Clear();

        foreach (PNode pnode in staticTree.GetSufficientlyLargeLeaves(requiredSize))
        {
          Vector2 corner = pnode.Rectangle.Position + requiredSize;
          Vector2 expandedCoveRec = new(Mathf.Max(staticCoverage.x, corner.x), Mathf.Max(staticCoverage.y, corner.y));

          if (PTree.FitsInto(expandedCoveRec, staticCoverage))
          {
            float waste = pnode.Rectangle.Size.x * pnode.Rectangle.Size.y - requiredSize.x * requiredSize.y;
            preservers[pnode] = waste;
          }
          else
          {
            float ratio = expandedCoveRec.x / expandedCoveRec.y;
            expanders[pnode] = Mathf.Abs(ratio - 1);
          }
        }

        PNode targetNode = null;
        if (preservers.Count > 0)
        {
          float lowestWaste = Mathf.Infinity;
          foreach (KeyValuePair<PNode, float> entry in preservers)
          {
            if (entry.Value < lowestWaste)
            {
              targetNode = entry.Key;
              lowestWaste = entry.Value;
            }
          }
        }
        else
        {
          float bestRatio = Mathf.Infinity;
          foreach (KeyValuePair<PNode, float> entry in expanders)
          {
            if (entry.Value < bestRatio)
            {
              targetNode = entry.Key;
              bestRatio = entry.Value;
            }
          }
        }

        if (targetNode != null)
        {
          PNode fitNode = staticTree.Split(targetNode, requiredSize);

          Vector3 scale = layout[el].Scale;
          layout[el] = new NodeTransform(fitNode.Rectangle.Position.x + scale.x / 2.0f,
                                         fitNode.Rectangle.Position.y + scale.z / 2.0f,
                                         scale,
                                         fitNode);

          // Update static coverage
          Vector2 corner = fitNode.Rectangle.Position + requiredSize;
          Vector2 expandedCoveRec = new(Mathf.Max(staticCoverage.x, corner.x), Mathf.Max(staticCoverage.y, corner.y));

          if (!PTree.FitsInto(expandedCoveRec, staticCoverage))
          {
            staticCoverage = expandedCoveRec;
          }
        }
      }
      
      return staticCoverage;
    }
    
    /// <summary>
    /// Finds a node in the tree that can accommodate a rectangle at the specified position and size.
    /// This is used when re-creating the tree to mark existing nodes' positions as occupied.
    /// </summary>
    private PNode FindNodeAtPosition(PTree tree, Vector2 position, Vector2 size)
    {
      // Try to find a leaf that contains this position and can fit this size
      var candidates = tree.GetSufficientlyLargeLeaves(size);
      
      foreach (PNode candidate in candidates)
      {
        PRectangle rect = candidate.Rectangle;
        
        // Check if this candidate can accommodate a rectangle at the desired position
        if (position.x >= rect.Position.x && 
            position.y >= rect.Position.y &&
            position.x + size.x <= rect.Position.x + rect.Size.x &&
            position.y + size.y <= rect.Position.y + rect.Size.y)
        {
          return candidate;
        }
      }
      
      return null;
    }
    
    /// <summary>
    /// Finds the best fit node for a given size in the tree.
    /// </summary>
    private PNode FindBestFit(PTree tree, Vector2 size)
    {
      var candidates = tree.GetSufficientlyLargeLeaves(size);
      
      if (candidates.Count == 0)
        return null;
        
      // Find the node with least waste
      PNode bestNode = null;
      float minWaste = float.MaxValue;
      
      foreach (PNode node in candidates)
      {
        float waste = node.Rectangle.Size.x * node.Rectangle.Size.y - size.x * size.y;
        if (waste < minWaste)
        {
          minWaste = waste;
          bestNode = node;
        }
      }
      
      return bestNode;
    }
  }
}