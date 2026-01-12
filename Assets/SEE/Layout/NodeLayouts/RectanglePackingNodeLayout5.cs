
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
  public class RectanglePackingNodeLayout5 : NodeLayout, IIncrementalNodeLayout
  {
    static RectanglePackingNodeLayout5()
    {
      Name = "Rectangle Packing5";
    }

    public Dictionary<ILayoutNode, NodeTransform> layoutResult = new();

    public RectanglePackingNodeLayout5 oldLayout;

    public IIncrementalNodeLayout OldLayout
    {
      set
      {
        if (value is RectanglePackingNodeLayout5 layout)
        {
          oldLayout = layout;
        }
        else
        {
          throw new ArgumentException(
              $"Predecessor of {nameof(RectanglePackingNodeLayout5)} was not an {nameof(RectanglePackingNodeLayout5)}.");
        }
      }
    }

    public Dictionary<ILayoutNode, NodeTransform> toBeDeleted = null;

    public static PTree tree = null;

    public static Vector2 covrec = Vector2.zero;

    public static List<List<string>> rows;

    public List<ILayoutNode> allThisLayoutNodes;

    /*
    public List<ILayoutNode> sameLeaves = null;
    public List<ILayoutNode> newNodes = null;
    public static int globalCallCount = 0;
    public static ILayoutNode rootLayoutVertex = new LayoutVertex("0");
     */

    //***********************************************************************************
    /// <summary>
    /// See <see cref="NodeLayout.Layout"/>.
    /// </summary>
    /// <exception cref="System.Exception">thrown if there is more than one root in
    /// <paramref name="thisLayoutNodes"/></exception>
    protected override Dictionary<ILayoutNode, NodeTransform> Layout
    (IEnumerable<ILayoutNode> thisLayoutNodes,
    Vector3 centerPosition,
    Vector2 rectangle)
    {

      allThisLayoutNodes = thisLayoutNodes.ToList();

      if (oldLayout == null)
      {
        // Initialize static fields only on first layout
        rows = new List<List<string>>();
        SortNodes(allThisLayoutNodes);
        if (allThisLayoutNodes.Count > 0)
        {
          rows.Add(allThisLayoutNodes.Select(n => n.ID).ToList());
        }
      }
      else
      {
        /*
        Vector2 worstCaseSize = Sum(allThisLayoutNodes);
        tree.Root.Rectangle.Size = 1.1f * worstCaseSize;
        tree.Root.Rectangle.Position = Vector2.zero;
         */
        var sameNodes = allThisLayoutNodes
            .Where(n => oldLayout.allThisLayoutNodes.Any(oldNode => oldNode.ID == n.ID))
            .ToList();
      
        var newNodes = allThisLayoutNodes
          .Where(n => !oldLayout.allThisLayoutNodes.Any(oldNode => oldNode.ID == n.ID))
          .ToList();
        if (newNodes.Count > 0)
        {
          SortNodes(newNodes);
          rows.Add(newNodes.Select(n => n.ID).ToList());
        }
      }

      foreach (var list in rows)
      {
        var targetNodes = allThisLayoutNodes
            .Where(n => list.Any(id => id == n.ID))
            .ToList();
        CreateLayout(targetNodes, centerPosition, rectangle);
      }

      return layoutResult;
    }

    //***********************************************************************************
    private void SortNodes(List<ILayoutNode> allThisLayoutNodes)
    {
      allThisLayoutNodes.Sort(delegate (ILayoutNode left, ILayoutNode right)
      { return (right.AbsoluteScale.x * right.AbsoluteScale.z).CompareTo(left.AbsoluteScale.x * left.AbsoluteScale.z); });
    }

    //***********************************************************************************
    public void CreateLayout
        (IEnumerable<ILayoutNode> layoutNodes,
        Vector3 centerPosition,
        Vector2 rectangle)
    {

      IList<ILayoutNode> layoutNodeList = layoutNodes.ToList();
      if (layoutNodeList.Count == 1)
      {
        ILayoutNode layoutNode = layoutNodeList.First();
        layoutResult[layoutNode] = new NodeTransform(0, 0, layoutNode.AbsoluteScale);
      }

      int numberOfLeaves = 0;
      foreach (ILayoutNode node in layoutNodeList)
      {
        if (node.IsLeaf)
        {
          Vector3 scale = node.AbsoluteScale;
          float padding = Padding(scale.x, scale.z);
          scale.x += padding;
          scale.z += padding;
          layoutResult[node] = new NodeTransform(0, 0, scale);
          numberOfLeaves++;
        }
      }
      if (numberOfLeaves == layoutNodeList.Count)
      {
        Debug.Log("here2");
        Pack(layoutResult, layoutNodeList.Cast<ILayoutNode>().ToList(), GroundLevel);
        RemovePadding(layoutResult);
        return;
      }


      ICollection<ILayoutNode> roots = LayoutNodes.GetRoots(layoutNodeList);
      if (roots.Count == 0)
      {
        return;
      }
      else if (roots.Count > 1)
      {
        throw new System.Exception("Graph has more than one root node." );
      }
      else
      {
        Debug.Log("here");
        ILayoutNode root = roots.FirstOrDefault();
        Vector2 area = PlaceNodes(layoutResult, root, GroundLevel);
        layoutResult[root] = new NodeTransform(0, 0, new Vector3(area.x, root.AbsoluteScale.y, area.y));
        RemovePadding(layoutResult);
        MakeContained(layoutResult, root);
      }
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
    public static void MakeContained
        (Dictionary<ILayoutNode, NodeTransform> layout,
         ILayoutNode parent)
    {
      NodeTransform parentTransform = layout[parent];
      Vector3 parentExtent = parentTransform.Scale / 2.0f;
      // The x co-ordinate of the left lower corner of the parent.
      float xCorner = parentTransform.X - parentExtent.x;
      // The z co-ordinate of the left lower corner of the parent.
      float zCorner = parentTransform.Z - parentExtent.z;

      foreach (ILayoutNode child in parent.Children())
      {
        if (!layout.ContainsKey(child)) continue;
        layout[child].MoveBy(xCorner, zCorner);
        MakeContained(layout, child);
      }
    }

    //***********************************************************************************
    /// <summary>
    /// Removes the added padding for all NodeTransforms in <paramref name="layout"/>.
    /// </summary>
    /// <param name="layout">layout containing the NodeTransform.Scale to be adjusted</param>
    public static void RemovePadding(Dictionary<ILayoutNode, NodeTransform> layout)
    {
      // We use a copy of the keys because we will modify layout during the iteration.
      ICollection<ILayoutNode> layoutNodes = new List<ILayoutNode>(layout.Keys);

      foreach (ILayoutNode layoutNode in layoutNodes)
      {
        // We added padding to both inner nodes and leaves, but we want to
        // the restore the original size of the leaves only.
        if (layoutNode.IsLeaf)
        {
          NodeTransform value = layout[layoutNode];
          Vector3 scale = value.Scale;
          float reversePadding = ReversePadding(scale.x, scale.z);
          // We shrink the scale, but the position remains the same since
          // value.Position denotes the center point.
          layout[layoutNode].ExpandBy(-reversePadding, -reversePadding);
        }
      }
    }

    //***********************************************************************************
    /// <summary>
    /// Recursively places the given node and its descendants in nested packed rectangles.
    ///
    /// Precondition: layout has the final scale of all leaves already set.
    /// </summary>
    /// <param name="layout">the current layout; will be updated</param>
    /// <param name="node">node to be laid out (includings all its descendants)</param>
    /// <param name="groundLevel">The y-coordindate of the ground where all nodes will be placed.</param>
    /// <returns>the width and depth of the area covered by the rectangle for <paramref name="node"/></returns>
    public Vector2 PlaceNodes(Dictionary<ILayoutNode, NodeTransform> layout, ILayoutNode node, float groundLevel)
    {
      if (node.IsLeaf)
      {
        /*
        // Leaves maintain their scale, which was already set initially. The position will
        // be adjusted later at a higher level of the node hierarchy when Pack() is
        // applied to this leaf and all its siblings.
         */
        return new Vector2(node.AbsoluteScale.x, node.AbsoluteScale.z);
      }
      else
      {
        // Inner node.
        ICollection<ILayoutNode> children = node.Children();

        // First recurse towards the leaves and determine the sizes of all descendants.
        foreach (ILayoutNode child in children)
        {
          if (!child.IsLeaf)
          {
            Vector2 childArea = PlaceNodes(layout, child, groundLevel);
            /*
            // childArea is the ground area size required for this inner node.
            // The position of this inner node in layout will be below in the call to Pack().
            // The position is relative to the parent of this inner node.
            // We only need to set the scale here.
            // Note: We have already added padding to leaf nodes, but this one here is an
            // inner node. Nevertheless, we do not add padding here, because padding is already
            // included in the returned childArea.
             */
            layout[child] = new NodeTransform(0, 0,
                                              new Vector3(childArea.x, child.AbsoluteScale.y, childArea.y));
          }
        }
        // The scales of all children of the node have now been set. Now
        // let's pack those children.
        if (children.Count > 0)
        {
          Vector2 area = Pack(layout, children.Cast<ILayoutNode>().ToList(), groundLevel, allThisLayoutNodes);
          float padding = Padding(area.x, area.y);
          return new Vector2(area.x + padding, area.y + padding);
        }
        else
        {
          // Can we ever arrive here? That would mean that node is not a leaf
          // and does not have children.
          return new Vector2(node.AbsoluteScale.x, node.AbsoluteScale.z);
        }
      }
    }

    //***********************************************************************************
    /// <summary>
    /// Returns the area size of given <paramref name="node"/>, i.e., its width (x co-ordinate)
    /// multiplied by its depth (z co-ordinate).
    /// </summary>
    /// <param name="node">node whose size is to be returned</param>
    /// <returns>area size of given layout node</returns>
    public static float AreaSize(NodeTransform node)
    {
      Vector3 size = node.Scale;
      return size.x * size.z;
    }

    //***********************************************************************************
    /// <summary>
    /// Returns the ground area size of the given <paramref name="node"/>:
    /// (x -> width, z -> depth).
    /// </summary>
    /// <param name="node">node whose ground area size is requested</param>
    /// <returns>ground area size of the given <paramref name="node"/></returns>
    public static Vector2 GetRectangleSize(NodeTransform node)
    {
      Vector3 size = node.Scale;
      return new Vector2(size.x, size.z);
    }

    //***********************************************************************************
    /// <summary>
    /// Returns the sum of the required ground area over all given <paramref name="nodes"/> including
    /// the padding for each. A node's width is mapped onto the x co-ordinate
    /// and its depth is mapped onto the y co-ordinate of the resulting Vector2.
    /// </summary>
    /// <param name="nodes">nodes whose ground area size is requested</param>
    /// <param name="layoutResult">the currently existing layout information for each node
    /// (its scale is required only)</param>
    /// <param name="padding">the padding to be added to a node's ground area size</param>
    /// <returns>sum of the required ground area over all given <paramref name="nodes"/></returns>
    public static Vector2 Sum(List<ILayoutNode> nodes, Dictionary<ILayoutNode, NodeTransform> layout)
    {
      Vector2 result = Vector2.zero;
      foreach (ILayoutNode element in nodes)
      {
        if (!layout.ContainsKey(element))
        {
          continue;
        }
        
        Vector3 size = layout[element].Scale;
        result.x += size.x;
        result.y += size.z;
        
      }
      return result;
    }

    //***********************************************************************************
    private static Vector2 Sum(List<ILayoutNode> nodes)
    {
      Vector2 result = Vector2.zero;
      foreach (var node in nodes)
      {
        result.x += node.AbsoluteScale.x;
        result.y += node.AbsoluteScale.z;
      }
      return result;
    }

    /*
    private Vector2 Pack(Dictionary<ILayoutNode, NodeTransform> layout, List<ILayoutNode> nodes, float groundLevel)
    {
      if (oldLayout != null)
      {
        return Vector2.zero;
      }
      else
      {
        return CreatePack(layout, nodes, groundLevel);
      }
    }
     
     */

    //***********************************************************************************
    /// <summary>
    /// Places the given <paramref name="nodes"/> in a minimally sized rectangle without
    /// overlapping.
    ///
    /// Allows one to pack smaller rectangles into a single larger rectangle
    /// so that the contained rectangles do not overlap, are as close together
    /// as possible (without padding) and the containing rectangle is as
    /// small as possible (no optimal solution is provided). The containing
    /// rectangle is organized in stripes whose aspect ratio is as close to
    /// one as possible. The layout maintains the size and orientation of
    /// all smaller rectangles. The largest contained rectangle appears at the
    /// left lower corner of the containing rectangle at position (0, groundlevel, 0).
    ///
    /// Precondition: The scales of all <paramref name="nodes"/> are set in
    /// the corresponding NodeTransforms in <paramref name="layout"/>.
    /// </summary>
    /// <param name="layout">the current layout (positions of <paramref name="nodes"/>
    /// will be updated</param>
    /// <param name="nodes">the nodes to be laid out</param>
    /// <param name="groundLevel">The y-coordindate of the ground where all nodes will be placed.</param>
    /// <returns>the width (x) and depth (y) of the outer rectangle in which all
    /// <paramref name="nodes"/> were placed</returns>
    public Vector2 Pack(Dictionary<ILayoutNode, NodeTransform> layout, List<ILayoutNode> nodes, float groundLevel, List<ILayoutNode> allLayoutNodes = null)
    {
      Vector2 worstCaseSize = Sum(nodes, layout);

      PTree tree = new(Vector2.zero, 1.1f * worstCaseSize);

      var covrec = Vector2.zero;

      Dictionary<PNode, float> preservers = new();
      
      Dictionary<PNode, float> expanders = new();

      foreach (ILayoutNode el in nodes)
      {
        if (!layout.ContainsKey(el))
        {
          continue;
        }

        Vector2 requiredSize = GetRectangleSize(layout[el]);

        preservers.Clear();
        expanders.Clear();

        var sufficientLargeLeaves = tree.GetSufficientlyLargeLeaves(requiredSize);
        //tree.Print();

        if (sufficientLargeLeaves.Count == 0)
        {
          tree.Print();
          throw new Exception("No sufficiently large free leaf found for size " + " :" + el.AbsoluteScale + ": " + " :" + RectanglePackingNodeLayout1.globalCallCount + ": ");
        }


        foreach (PNode pnode in sufficientLargeLeaves)
        {
          Vector2 corner = pnode.Rectangle.Position + requiredSize;
          Vector2 expandedCoveRec = new(Mathf.Max(covrec.x, corner.x), Mathf.Max(covrec.y, corner.y));

          if (PTree.FitsInto(expandedCoveRec, covrec))
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

        if (targetNode == null)
        {
          Debug.LogError("targetNode is null!");
          continue;
        }
        PNode fitNode = tree.Split(targetNode, requiredSize);//fixME debug

        Vector3 scale = layout[el].Scale;
        layout[el] = new NodeTransform(fitNode.Rectangle.Position.x + scale.x / 2.0f,
                                       fitNode.Rectangle.Position.y + scale.z / 2.0f,
                                       scale, fitNode);


        {
          Vector2 corner = fitNode.Rectangle.Position + requiredSize;
          Vector2 expandedCoveRec = new(Mathf.Max(covrec.x, corner.x), Mathf.Max(covrec.y, corner.y));

          if (!PTree.FitsInto(expandedCoveRec, covrec))
          {
            covrec = expandedCoveRec;
          }
        }
      }
      return covrec;
    }

    //***********************************************************************************
    // Creates a new ILayoutNode instance of the same runtime type as the given node, if possible.
    // If the type does not have a parameterless constructor, falls back to using MemberwiseClone.
    public static ILayoutNode CreateNewNodeOfSameType(ILayoutNode node)
    {
      var type = node.GetType();
      Debug.Log($"Creating new node of type {type.Name}.");
      switch (type.Name)
      {
        case "LayoutGraphNode":
          LayoutGraphNode graphNode = new LayoutGraphNode(((LayoutGraphNode)node).ItsNode)
          {
            AbsoluteScale = node.AbsoluteScale,
            CenterPosition = node.CenterPosition,
            Rotation = node.Rotation,
            Level = node.Level
          };
          foreach (var child in node.Children())
          {
            var manufacturedChild = CreateNewNodeOfSameType(child);
            graphNode.AddChild(manufacturedChild);
            manufacturedChild.Parent = graphNode;

          }
          Debug.Log($"Creating of {type.Name} successful.");
          return graphNode;
        case "LayoutVertex":
          LayoutVertex layoutVertex = new LayoutVertex(node.ID)
          {
            AbsoluteScale = node.AbsoluteScale,
            CenterPosition = node.CenterPosition,
            Rotation = node.Rotation
          };
          Debug.Log($"Creating of {type.Name} successful.");
          return layoutVertex;
        default:
          throw new NotImplementedException($"Creation of new node of type {type.Name} is not implemented.");
      }

    }

    public bool AllAreLeaves(IEnumerable<ILayoutNode> layoutNodes)
    {
      foreach (ILayoutNode node in layoutNodes)
      {
        if (!node.IsLeaf)
        {
          return false;
        }
      }
      return true;
    }
  }


}
