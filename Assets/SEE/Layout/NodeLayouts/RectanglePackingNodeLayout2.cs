using SEE.Game.CityRendering;
using SEE.Layout.NodeLayouts.RectanglePacking;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

namespace SEE.Layout.NodeLayouts
{
  /// <summary>
  /// This layout packs rectangles closely together as a set of nested packed rectangles to decrease
  /// the total area of city. The algorithm is based on the dissertation of Richard Wettel
  /// "Software Systems as Cities" (2010); see page 35.
  /// https://www.inf.usi.ch/lanza/Downloads/PhD/Wett2010b.pdf
  /// </summary>
  public class RectanglePackingNodeLayout2 : NodeLayout, IIncrementalNodeLayout
  {
    static RectanglePackingNodeLayout2()
    {
      Name = "Rectangle Packing2";
    }

    public Dictionary<ILayoutNode, NodeTransform> layoutResult = new();

    public RectanglePackingNodeLayout2 oldLayout;

    public IIncrementalNodeLayout OldLayout
    {
      set
      {
        if (value is RectanglePackingNodeLayout2 layout)
        {
          oldLayout = layout;
        }
        else
        {
          throw new ArgumentException(
              $"Predecessor of {nameof(RectanglePackingNodeLayout2)} was not an {nameof(RectanglePackingNodeLayout2)}.");
        }
      }
    }

    public List<ILayoutNode> sameLeaves = null;
    
    public Dictionary<ILayoutNode,Vector3> sameLeavesChangedSize = null;

    public Dictionary<ILayoutNode, NodeTransform> toBeDeleted = null;

    public List<ILayoutNode> newNodes = null;

    public static PTree tree = null;

    public static Vector2 covrec = Vector2.zero;

    public int globalCallCount = 0;

    public List<ILayoutNode> allThisLayoutNodes = null;

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
      /*
      newNodes = new List<ILayoutNode>();
      var layoutNodeIds = new HashSet<string>(thisLayoutNodes.Select(n => n.ID));
       */
      allThisLayoutNodes = thisLayoutNodes.ToList();
      

      /*
       
      ILayoutNode childLayoutVertex = new LayoutVertex("1");
      rootLayoutVertex.AddChild(childLayoutVertex);
      childLayoutVertex.Parent = rootLayoutVertex;

       */

      if (oldLayout == null)
      {
        globalCallCount++;
        /*
        ICollection<ILayoutNode> roots = LayoutNodes.GetRoots(allLayoutNodes);
        foreach (var root in roots)
        {
          childLayoutVertex.AddChild(root);
          root.Parent = childLayoutVertex;
        }
         
        allThisLayoutNodes.Sort(delegate (ILayoutNode left, ILayoutNode right)
        { return (right.AbsoluteScale.x * right.AbsoluteScale.z).CompareTo(left.AbsoluteScale.x * left.AbsoluteScale.z); });
         */
        SortNodes(allThisLayoutNodes);

        CreateLayout(allThisLayoutNodes, centerPosition, rectangle);
        return layoutResult;
      }
      else
      {
        var copiedLayout = CopyOldLayout(oldLayout.layoutResult);
        globalCallCount = oldLayout.globalCallCount + 1;
        layoutResult = copiedLayout;

        var oldIds = new HashSet<string>(oldLayout.layoutResult.Keys.Select(n => n.ID));
        var newIds = new HashSet<string>(thisLayoutNodes.Select(n => n.ID));

        //tree.Print();
        UpdateLastNodeSize(layoutResult);

        newNodes = thisLayoutNodes
          .Where(node => !oldIds.Contains(node.ID))
          .ToList();

        toBeDeleted = oldLayout.layoutResult
          .Where(entry => !newIds.Contains(entry.Key.ID))
          .ToDictionary(entry => entry.Key, entry => entry.Value);

        if (toBeDeleted != null)
        {
          foreach (var entry in toBeDeleted)
          {
            if (entry.Value.fitNode != null)
            {
              layoutResult.Remove(entry.Key);
              entry.Value.fitNode.Occupied = false;
              tree.MergeFreeLeaves(entry.Value.fitNode);
            }
          }
        }

        SortNodes(newNodes);
        CreateLayout(newNodes, centerPosition, rectangle);

          //.Where(node => newIds.Contains(node.ID) && thisLayoutNodes.Where(newNode => node == newNode && node.AbsoluteScale != newNode.AbsoluteScale))
        
        /*
        var productNodes = oldNodes.Concat(newNodes).ToList();
        newNodes.Sort(delegate (ILayoutNode left, ILayoutNode right)
        { return (right.AbsoluteScale.x * right.AbsoluteScale.z).CompareTo(left.AbsoluteScale.x * left.AbsoluteScale.z); }); 
         */
        /*
        sameLeaves = oldLayout.allThisLayoutNodes
          .Where(node => newIds.Contains(node.ID))
          .ToList();
        oldNodes.Sort(delegate (ILayoutNode left, ILayoutNode right)
        { return (right.AbsoluteScale.x * right.AbsoluteScale.z).CompareTo(left.AbsoluteScale.x * left.AbsoluteScale.z); });
        allThisLayoutNodes = oldNodes.Concat(newNodes).ToList();


         */
        /*
      if (newNodes.Count > 0)
      {
      }
        foreach (var newNode in newNodes)
        {
          if(newNode.IsLeaf)
          {
            childLayoutVertex.AddChild(newNode);
            newNode.Parent = childLayoutVertex;
          }

        }
        foreach (var newNode in newNodes)
        {
          layoutResult.TryAdd(newNode, new NodeTransform(0, 0, newNode.AbsoluteScale));
        }
         */
        /*
        sameNodes = thisLayoutNodes
          .Where(node => oldIds.Contains(node.ID))
          .ToDictionary(
            node => node
           , node => oldLayout.layoutResult
              .Where(entry => entry.Key.ID == node.ID).First().Value);
        layoutResult = sameNodes;
        sameLeaves = allLayoutNodes.Where(node => oldIds.Contains(node.ID) && node.IsLeaf).ToList();  

        toBeDeleted = oldLayout.layoutResult
          .Where(entry => !layoutNodeIds.Contains(entry.Key.ID))
          .ToDictionary(entry => entry.Key, entry => entry.Value);


        if (toBeDeleted != null)
        {
          foreach (var entry in toBeDeleted)
          {
            if (entry.Value.fitNode != null)
            {
              tree.MergeFreeLeaves(entry.Value.fitNode);
            }
          }
        }
        // Create layout for new nodes only and merge; CreateLayout updates layoutResult in place
        if (newNodes.Count > 0)
        {
        }
         */

        Debug.Log("TestDebugPointer");

        return layoutResult;
      }
    }

    //***********************************************************************************
    public void UpdateLastNodeSize(Dictionary<ILayoutNode, NodeTransform> layout)
    {
      /*
      sameLeavesChangedSize = layout
          .Select(entry =>
          {
            var newNode = allThisLayoutNodes.First(n => n.ID == entry.Key.ID && n.AbsoluteScale != entry.Key.AbsoluteScale);
            return new KeyValuePair<ILayoutNode, Vector3>(entry.Key, newNode.AbsoluteScale);
          })
          .ToDictionary
          (
            entry => entry.Key,
            entry => entry.Value
          );
       */
      
      foreach (var entry in layout)
      {
        var node = entry.Key;
        var transform = entry.Value;
        Debug.Log("node.id: " + node.ID + "old absolutescale: " + node.AbsoluteScale + "transform absolutescale : " + transform.Scale);
      }
      foreach (var node in allThisLayoutNodes)
      {
        Debug.Log("allThisLayoutNodes node.id: " + node.ID + "absolutescale: " + node.AbsoluteScale);
      }


      foreach (var entry in layout)
      {
        var node = entry.Key;
        var transform = entry.Value;
        var newNodeScale = allThisLayoutNodes
          .FirstOrDefault(n => n.ID == node.ID && n.AbsoluteScale != node.AbsoluteScale);

        if (newNodeScale != null)
        {
          Debug.Log("NEwNodeScale" + newNodeScale.ID + "old absolutescale: " + node.AbsoluteScale + "New Node absolutescale : " + newNodeScale.AbsoluteScale);
          var newNodeScaleVector = newNodeScale.AbsoluteScale;
          var oldScale = node.AbsoluteScale;
          var newScale = newNodeScale;
          transform.Scale = newNodeScaleVector;
          tree.GrowLeaf(transform.fitNode, oldScale); 
        }
        else
        {
          continue;
          // No size change
        }
      } 
      /*
       */
    }

    //***********************************************************************************
    public void CreateLayout(IEnumerable<ILayoutNode> layoutNodes, Vector3 centerPosition,Vector2 rectangle)
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
        Pack(layoutResult, layoutNodeList.Cast<ILayoutNode>().ToList(), groundLevel);
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

        throw new System.Exception("Graph has more than one root node." + globalCallCount);
      }
      else
      {
        ILayoutNode root = roots.FirstOrDefault();
        Vector2 area = PlaceNodes(layoutResult, root, groundLevel);
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
    public static void MakeContained(Dictionary<ILayoutNode, NodeTransform> layout, ILayoutNode parent)
    {
      NodeTransform parentTransform = layout[parent];
      Vector3 parentExtent = parentTransform.Scale / 2.0f;
      // The x co-ordinate of the left lower corner of the parent.
      float xCorner = parentTransform.X - parentExtent.x;
      // The z co-ordinate of the left lower corner of the parent.
      float zCorner = parentTransform.Z - parentExtent.z;

      foreach (ILayoutNode child in parent.Children())
      {
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
    public static Vector2 Sum(List<ILayoutNode> nodes, Dictionary<ILayoutNode, NodeTransform> layoutResult)
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

    //***********************************************************************************
    private static Vector2 Sum1(List<ILayoutNode> nodes)
    {
      Vector2 result = Vector2.zero;
      foreach (var node in nodes)
      {
        if (node.IsLeaf)
        {
          result.x += node.AbsoluteScale.x;
          result.y += node.AbsoluteScale.z;
        }
      }
      return result;
    }

    //***********************************************************************************
    private static Vector2 Sum2(Dictionary<ILayoutNode, NodeTransform> layoutResult)
    {
      Vector2 result = Vector2.zero;
      var nodes = layoutResult.Keys.ToList();
      Assert.IsFalse(nodes.Count == 0, "No nodes in layoutResult!");
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
    public Vector2 Pack(Dictionary<ILayoutNode, NodeTransform> layout, List<ILayoutNode> nodes, float groundLevel, IList<ILayoutNode> allLayoutNodes = null)
    {
      /*
      // To increase the efficiency of the space usage, we order the elements by one of the sizes.
      // Elements must be sorted by size, descending
      nodes.Sort(delegate (ILayoutNode left, ILayoutNode right)
      { return AreaSize(layout[right]).CompareTo(AreaSize(layout[left])); });
       */
      /*
      // Since we initially do not know how much space we need, we assign a space of the
      // worst case to the root. Note that we want to add padding in between the nodes,
      // so we need to increase the required size accordingly.
       */
      Vector2 worstCaseSize = Sum(nodes, layout);
      /*
      // The worst-case size is increased slightly to circumvent potential
      // imprecisions of floating-point arithmetics.
       */
      Vector2 oldWorstCaseSize = Vector2.zero;

      if (tree == null)
      {
        tree = new(Vector2.zero, 1.1f * (worstCaseSize) );
        covrec = Vector2.zero;
      }
      else
      {
        Debug.Log("Parazitttttttttttttttttttttttttttttttttttttttt");
        var summableList = layoutResult.Keys.ToList();
        Assert.IsNotNull(summableList);
        //Debug.Log("summableList count: " + summableList.Count);
        var newWorstCaseSize = Sum1(summableList);
        //Debug.Log("newWorstCaseSize: " + newWorstCaseSize);
        oldWorstCaseSize = tree.Root.Rectangle.Size;
        tree.Root.Rectangle.Size = 1.1f * newWorstCaseSize;
        //tree.resetAllPNodes(1.1f * newWorstCaseSize);
      }
      /*
        //tree.Print();
        //var enlargementFactor = newWorstCaseSize - tree.Root.Rectangle.Size;
        //tree.Root.Rectangle.Position = Vector2.zero;
        //tree.FreeLeavesAdjust();
       */
      /*
      else
      {
        tree = new (tree.Root.Rectangle.Position, 1.1f * worstCaseSize);
      }
       */
      /*
      // Keeps track of the area currently covered by elements. It is the bounding
      // box containing all rectangles placed so far.
      // Initially, there are no placed elements yet, and therefore the covered
      // area is initialized to (0, 0).
       */
      /*
      // All nodes in pnodes that preserve the size of coverec. The
      // value is the amount of remaining space if the node were split to
      // place el.
       */
      /*
      // All nodes in pnodes that do not preserve the size of coverec.
      // The value is the absolute difference of the aspect ratio of coverec from 1
      // (1 being the perfect ratio) if the node were used to place el.
       */

      
      Dictionary<PNode, float> preservers = new();
      Dictionary<PNode, float> expanders = new();

      foreach (ILayoutNode el in nodes)
      {
        /*
        // We assume that the scale of all nodes in elements have already been set.

        // The size we need to place el plus the padding between nodes.
         */
        Vector2 requiredSize = GetRectangleSize(layout[el]);

        preservers.Clear();
        expanders.Clear();

        var sufficientLargeLeaves = tree.GetSufficientlyLargeLeaves(requiredSize, oldWorstCaseSize);
        
        if (sufficientLargeLeaves.Count == 0)
        {
          //tree.Print();
          throw new Exception("No sufficiently large free leaf found for size " + " :" + el.AbsoluteScale + ": " + " :" + RectanglePackingNodeLayout.globalCallCount + ": ");
        }

        /*
        if (sufficientLargeLeaves.Count == 0)
        {
          var currentRootSize = tree.Root.Rectangle.Size;
          var currentRootPosition = tree.Root.Rectangle.Position; 
          var currentRootCorner = tree.Root.Rectangle.Position + currentRootSize;
          Debug.Log("Current root size: " + currentRootSize);
          Debug.Log("corner down right: " + currentRootCorner);
          tree.Root.Rectangle.Size = 5.0f * tree.Root.Rectangle.Size;
          Debug.Log("New root size: " + tree.Root.Rectangle.Size);
          foreach (var leaf in tree.FreeLeaves)
          {
            Debug.Log(leaf.Rectangle.Position + leaf.Rectangle.Size + " : " + leaf.Rectangle.Position + " : " + leaf.Rectangle.Size);
          }
          var maxX = tree.FreeLeaves.Max(leaf => leaf.Rectangle.Position.x + leaf.Rectangle.Size.x);
          var maxY = tree.FreeLeaves.Max(leaf => leaf.Rectangle.Position.y + leaf.Rectangle.Size.y);
          foreach (var leaf in tree.FreeLeaves)
          {
            //right lower corner 
            
            var corner = leaf.Rectangle.Position + leaf.Rectangle.Size;
            if (maxY == corner.y)
            {
              Debug.Log("before y: " + leaf.Rectangle.Size.y);
              leaf.Rectangle.Size.y = tree.Root.Rectangle.Size.y;
              Debug.Log("after y: " + leaf.Rectangle.Size.y);

            }
            if (maxX == corner.x)
            {
              Debug.Log("before x: " + leaf.Rectangle.Size.x);
              leaf.Rectangle.Size.x = tree.Root.Rectangle.Size.x;
              Debug.Log("after x: " + leaf.Rectangle.Size.x);
            }
          }
          tree.Print();
          
          sufficientLargeLeaves = tree.GetSufficientlyLargeLeaves(requiredSize);


          if (sufficientLargeLeaves.Count == 0)
          {
            Debug.Log("kos after");
            throw new Exception("No sufficiently large free leaf found for size " + " :" + el.AbsoluteScale + ": " + " :" + RectanglePackingNodeLayout.globalCallCount + ": ");

          }
        }
         
         */
        /*
          // No leaf is large enough to host el; we need to expand the root rectangle.
          // We double the size of the root rectangle in both dimensions.
          Debug.Log(tree.Root.Rectangle.Size + ": before root enlargement");
          tree.Print();
          tree.Root.Rectangle.Size = 100.0f * tree.Root.Rectangle.Size;

            foreach (var leaf in tree.FreeLeaves)
            {
              Debug.Log(leaf.Rectangle.Size);
            }
            tree.Print();
          if (sufficientLargeLeaves.Count == 0)
          {
            Debug.Log("kos");
            //throw new Exception("No sufficiently large free leaf found for size " + " :" + el.AbsoluteScale + ": " + " :" + RectanglePackingNodeLayout.globalCallCount + ": ");

          }

          Debug.Log(tree.Root.Rectangle.Size + ": after root enlargement");
          tree.Print();

           */
        /*
           */

        foreach (PNode pnode in sufficientLargeLeaves)
        {
          /*
          // Right lower corner of new rectangle
          // Expanded covrec.
          // If placing el in pnode would preserve the size of coverec
            // The remaining area of pnode if el were placed into it.
            // The aspect ratio of coverec if pnode were used to place el.
          // targetNode is the node with the lowest waste in preservers
           */
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
          /*
          // If there are more potential candidates, all large enough to host the
          // element and all of them boundary expanders, we need to chose the one
          // that expands the boundaries such that the resulting covered area has
          // an aspect ratio closer to a square.

          // targetNode is the node with the aspect ratio closest to 1
           */
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
        /*
        // Place el into targetNode.
        // The free leaf node that has the requested size allocated within targetNode.
         */

        if (targetNode == null)
        {
          Debug.LogError("targetNode is null!");
          continue;
        }
        PNode fitNode = tree.Split(targetNode, requiredSize);//fixME debug

        /*
        // The size of the node remains unchanged. We set only the position.
        // The x and y co-ordinates of the rectangle denote the left front corner. The layout
        // position returned must be the center. The y co-ordinate is the ground level.
         */
        Vector3 scale = layout[el].Scale;
        layout[el] = new NodeTransform(fitNode.Rectangle.Position.x + scale.x / 2.0f,
                                       fitNode.Rectangle.Position.y + scale.z / 2.0f,
                                       scale, fitNode);

        /*
        // If fitNode is a boundary expander, then we need to expand covrec to the
        // newly covered area.
          // Right lower corner of fitNode
          // Expanded covrec.
          // If placing fitNode does not preserve the size of coverec
         */
        {
          Vector2 corner = fitNode.Rectangle.Position + requiredSize;
          Vector2 expandedCoveRec = new(Mathf.Max(covrec.x, corner.x), Mathf.Max(covrec.y, corner.y));

          if (!PTree.FitsInto(expandedCoveRec, covrec))
          {
            covrec = expandedCoveRec;
          }
        }
      }
      /*
      tree.Root.Rectangle.Size = 1.1f * covrec;
      Vector2 dif = tree.Root.Rectangle.Size - covrec;
      tree.enlargeAllPNodes(dif);
       */
      return covrec;
    }

    //***********************************************************************************
    // Creates a new ILayoutNode instance of the same runtime type as the given node, if possible.
    // If the type does not have a parameterless constructor, falls back to using MemberwiseClone.
    public ILayoutNode CreateNewNodeOfSameType(ILayoutNode node)
    {
      Type type = node.GetType();
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
          if (node.Parent != null) 
          {
            foreach (var child in node.Children())
            {
              var manufacturedChild = CreateNewNodeOfSameType(child);
              graphNode.AddChild(manufacturedChild);
              manufacturedChild.Parent = graphNode;

            }
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

    //***********************************************************************************
    public Dictionary<ILayoutNode, NodeTransform> CopyOldLayout(Dictionary<ILayoutNode, NodeTransform> layoutRes)
    {
      foreach (var entry in layoutRes)
      {
        Debug.Log("Old layout node id: " + entry.Key.ID + " with scale: " + entry.Value.Scale);
      }
      Dictionary<ILayoutNode, NodeTransform> copiedLayout = new();
      foreach (var entry in layoutRes)
      {
        var oldNode = entry.Key;
        var oldTransform = entry.Value;
        var newNode = CreateNewNodeOfSameType(oldNode);
        var newTransform = new NodeTransform(oldTransform.X, oldTransform.Z, oldTransform.Scale, oldTransform.fitNode);
        copiedLayout[newNode] = newTransform;
      }
      foreach (var entry in copiedLayout)
      {
        Debug.Log("Copied layout node id: " + entry.Key.ID + " with scale: " + entry.Value.Scale);
      }
      /*
       */
      return copiedLayout;
      /*
      Type type = node.GetType();
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
       */

    }
    //***********************************************************************************

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

    //***********************************************************************************
    private List<ILayoutNode> SortNodes(List<ILayoutNode> list)
    {
      list.Sort(delegate (ILayoutNode left, ILayoutNode right)
      { return (right.AbsoluteScale.x * right.AbsoluteScale.z).CompareTo(left.AbsoluteScale.x * left.AbsoluteScale.z); });
      return list;
    }



  }



}
