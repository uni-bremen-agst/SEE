using Crosstales.RTVoice.Util;
using SEE.DataModel.DG;
using SEE.Game.CityRendering;
using SEE.Layout.NodeLayouts.RectanglePacking;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEngine.UI.ContentSizeFitter;

namespace SEE.Layout.NodeLayouts
{
  /// <summary>
  /// This layout packs rectangles closely together as a set of nested packed rectangles to decrease
  /// the total area of city. The algorithm is based on the dissertation of Richard Wettel
  /// "Software Systems as Cities" (2010); see page 35.
  /// https://www.inf.usi.ch/lanza/Downloads/PhD/Wett2010b.pdf
  /// </summary>
  public class RectanglePackingNodeLayout6 : NodeLayout, IIncrementalNodeLayout
  {
    static RectanglePackingNodeLayout6()
    {
      Name = "Rectangle Packing5";
    }

    public Dictionary<ILayoutNode, NodeTransform> layoutResult = new();

    public RectanglePackingNodeLayout6 oldLayout;

    public IIncrementalNodeLayout OldLayout
    {
      set
      {
        if (value is RectanglePackingNodeLayout6 layout)
        {
          oldLayout = layout;
        }
        else
        {
          throw new ArgumentException(
              $"Predecessor of {nameof(RectanglePackingNodeLayout6)} was not an {nameof(RectanglePackingNodeLayout6)}.");
        }
      }
    }

    public PTree tree;

    public Vector2 covrec = Vector2.zero;

    public static List<List<string>> rows;

    public List<ILayoutNode> allThisLayoutNodes;

    public Dictionary<string, List<Vector2>> idsAndSizes;

    LayoutGraphNode rootLayoutNode;
    Graph graph;
    Node rootNode;

    public static List<List<List<(string, List<Vector2>)>>> rowss;
    //public static List<(string, List<(Vector2, List<ILayoutNode>)>)> history;
    //public static List<(string, List<(List<Vector2>, List<ILayoutNode>)>)> history;
    //public static List<(string, List<(List<Vector2>, List<string>)>)> history;
    //public static List<(string, List<(List<(string, Vector2)>, List<string>)>)> history;
    //public static List<(string, List<(List<(string, Vector2)>, List<string>, List<string>)>)> history;
    public static List<(string, List<(List<(string, Vector2)>, List<(string, Vector2)>, List<(string, Vector2)>)>)> history;

    /*
    public Dictionary<ILayoutNode, NodeTransform> toBeDeleted = null;
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
    protected override Dictionary<ILayoutNode, NodeTransform> Layout(IEnumerable<ILayoutNode> thisLayoutNodes, Vector3 centerPosition, Vector2 rectangle)
    {

      allThisLayoutNodes = thisLayoutNodes.ToList();
      if (oldLayout == null)
      {
        history = new List<(string, List<(List<(string, Vector2)>, List<(string, Vector2)>, List<(string, Vector2)>)>)>();
        rowss = new List<List<List<(string, List<Vector2>)>>>();
      }

      CreateLayout(allThisLayoutNodes, centerPosition, rectangle);

      return layoutResult;
    }

    //***********************************************************************************
    private void SortNodes(List<ILayoutNode> allThisLayoutNodes)
    {
      allThisLayoutNodes.Sort(delegate (ILayoutNode left, ILayoutNode right)
      { return (right.AbsoluteScale.x * right.AbsoluteScale.z).CompareTo(left.AbsoluteScale.x * left.AbsoluteScale.z); });
    }

    //***********************************************************************************
    public void CreateLayout(IEnumerable<ILayoutNode> layoutNodes, Vector3 centerPosition, Vector2 rectangle)
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
        throw new System.Exception("Graph has more than one root node.");
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

        ICollection<ILayoutNode> children = node.Children();


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
        if (children.Count > 0)
        {
          Vector2 area = Pack(layout, children.Cast<ILayoutNode>().ToList(), groundLevel, node);
          float padding = Padding(area.x, area.y);
          return new Vector2(area.x + padding, area.y + padding);
        }
        else
        {
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
          Debug.LogWarning("Layout does not contain element************************************** " + element.ID);
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
    public Vector2 Pack(Dictionary<ILayoutNode, NodeTransform> layout, List<ILayoutNode> nodes, float groundLevel, ILayoutNode parent = null)
    {
      /*
       let all initial sizes be in layout with padding added
      if (oldLayout == null)
      {
        FullJournal(layout, nodes);
      }
      JournalNodes(layout, nodes);
       */
      /*
      else { 
        // Adjust the root rectangle to the new worst case size.
        tree.Root.Rectangle.Size = 1.1f * worstCaseSize;
        tree.FreeLeavesAdjust(worstCaseSize);
        tree.Root.Rectangle.Position = Vector2.zero;
      }
       */
      string parentID = parent == null ? "dummy" : parent.ID;

      SortNodesByAreaSize(nodes, layout);
      Vector2 worstCaseSize = Sum(nodes, layout);

      PTree tree = new(Vector2.zero, 1.1f * worstCaseSize);

      Vector2 covrec = Vector2.zero;

      Dictionary<PNode, float> preservers = new();

      Dictionary<PNode, float> expanders = new();

      /*
      Add to history
      perform history 
      set the nodes to last scene
      AddToHistory(layout, nodes, parentID);
      PerformHistory(ref tree, ref covrec, parentID);
      PlaceNodes(ref layout, ref nodes, parentID, ref tree, ref covrec);
       */



      UsualProcess(ref layout, ref nodes, ref preservers, ref expanders, ref tree, ref covrec);
      /*
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
        PNode fitNode = tree.Split(targetNode, requiredSize, el.ID);

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
       */


      /*
      resize all nodes in tree according to new sizes of elements in layout
      look if there are new nodes in this level and let them pack in free leaves with their initial sizes with padding added
      see if the new nodes grow then grow the tree accordingly
      look if there are new new nodes in this level and let them pack in free leaves with their initial sizes with padding added
      see if the new nodes grow then grow the tree accordingly
      so on and so forth until no new nodes are left on this level


      Helper(newNodes);
       
       */
      SortNodesByAreaSize(nodes, layout);
      return covrec;
    }
    //***********************************************************************************
    public void PlaceNodes(ref Dictionary<ILayoutNode, NodeTransform> layout, ref List<ILayoutNode> nodes, string parent, ref PTree tree, ref Vector2 covrec)
    {
      //tree.Print();
      //Debug.Log("********************************************************************************************************");
      foreach (ILayoutNode el in nodes)
      {
        if (!layout.ContainsKey(el))
        {
          continue;
        }
        PNode fitNode = tree.FindNodeById(el.ID);
        
        if (fitNode == null)
        {
          //PrintHistory();
          Debug.Log("fitnode is null"+ el.ID);
          continue;
          
        }
        Vector3 scale = layout[el].Scale;
        layout[el] = new NodeTransform(fitNode.Rectangle.Position.x + scale.x / 2.0f,
                                       fitNode.Rectangle.Position.y + scale.z / 2.0f,
                                       scale, fitNode);
      }
    }
    //***********************************************************************************
    public void PerformHistory(ref PTree tree, ref Vector2 covrec, string parent)
    {
      
      if (history.Any(h => h.Item1 == parent || h.Item1 == "dummy"))
      {
        var getLine = history.LastOrDefault(h => h.Item1 == parent || h.Item1 == "dummy");

        // Iterate through all events in the history for this parent
        foreach (var historyEvent in getLine.Item2)
        {
          List<(string, Vector2)> sameIDsNewSizes = historyEvent.Item1;
          List<(string, Vector2)> newNodeIDsSizes = historyEvent.Item2;
          List<(string, Vector2)> deletedNodeIDsSizes = historyEvent.Item3;
          if (sameIDsNewSizes.Count == 0 && deletedNodeIDsSizes.Count == 0)
          {
            PlaceNodesInPTree(newNodeIDsSizes, ref tree, ref covrec);
          }
          else
          {
            // First, handle deleted nodes
            foreach (var (deletedID, size) in deletedNodeIDsSizes)
            {
              tree.DeleteMergeRemainLeaves(id: deletedID);
            }
            // Second, handle resized nodes that are the same
            // set ptree to same nodes with new size
            ResizeNodesInPTree(sameIDsNewSizes, ref tree, ref covrec);


            // Next, handle new nodes
            PlaceNodesInPTree(newNodeIDsSizes, ref tree, ref covrec);
            // Finally, update sizes of same nodes
          }

        }
      }
    }

    //***********************************************************************************
    public void ResizeNodesInPTree(List<(string, Vector2)> sameIDsNewSizes, ref PTree tree, ref Vector2 covrec)
    {
      foreach (var (sameID, size) in sameIDsNewSizes)
      {
        Vector2 requiredSize = size;
        PNode targetPNode = tree.FindNodeById(sameID);

        if (targetPNode != null)
        {
          if (targetPNode.Rectangle.Size == requiredSize) continue;
          else tree.GrowLeaf(targetPNode, new Vector3(requiredSize.x, 1, requiredSize.y));

          {
            Vector2 corner = targetPNode.Rectangle.Position + requiredSize;
            Vector2 expandedCoveRec = new(Mathf.Max(covrec.x, corner.x), Mathf.Max(covrec.y, corner.y));
            if (!PTree.FitsInto(expandedCoveRec, covrec))
            {
              covrec = expandedCoveRec;
            }
          }
        }
      }
    }
    //***********************************************************************************
    public void PlaceNodesInPTree(List<(string, Vector2)> newNodeIDsNewSizes, ref PTree tree, ref Vector2 covrec)
    {
      foreach (var (newID, size) in newNodeIDsNewSizes)
      {
        Vector2 requiredSize = size;
        Dictionary<PNode, float> preservers = new();
        Dictionary<PNode, float> expanders = new();
        var sufficientLargeLeaves = tree.GetSufficientlyLargeLeaves(requiredSize);
        if (sufficientLargeLeaves.Count == 0)
        {
          //tree.Print();
          throw new Exception("No sufficiently large free leaf found for size " + " :" + newID + ": ");
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
        PNode fitNode = tree.Split(targetNode, requiredSize, newID);

        {
          Vector2 corner = fitNode.Rectangle.Position + requiredSize;
          Vector2 expandedCoveRec = new(Mathf.Max(covrec.x, corner.x), Mathf.Max(covrec.y, corner.y));

          if (!PTree.FitsInto(expandedCoveRec, covrec))
          {
            covrec = expandedCoveRec;
          }
        }
      }
    }
    //***********************************************************************************
    public void AddToHistory(Dictionary<ILayoutNode, NodeTransform> layout, List<ILayoutNode> nodes, string parent)
    {
      //                    parentID            sameIDs newSizes        newIDs  newSizes       deletedIDs  newSizes
      //public static List<(string, List<(List<(string, Vector2)>, List<(string, Vector2)>, List<(string, Vector2)>)>)> history;

      List<string> newNodeIDs = new();
      List<string> sameNodeIDs = new();
      List<string> deletedNodeIDs = new();
      List<string> oldNodeIDs = new();
      List<string> currentNodeIDs = new();

      List<(string, Vector2)> sameIDsNewSizes = new();
      List<(string, Vector2)> newNodeIDsNewSizes = new();
      List<(string, Vector2)> deletedNodeIDsNewSizes = new();
      List<(string, Vector2)> currentNodeIDsNewSizes = new();

      //         sameIDs newSizes        newIDs  newSizes       deletedIDs  newSizes
      List<(List<(string, Vector2)>, List<(string, Vector2)>, List<(string, Vector2)>)> listOfHistory = new();
      List<Vector2> newSizes = new();

      //parentID          sameIDs newSizes        newIDs  newSizes       deletedIDs  newSizes
      (string, List<(List<(string, Vector2)>, List<(string, Vector2)>, List<(string, Vector2)>)>) getLine = new();

      //    sameIDs newSizes        newIDs  newSizes       deletedIDs  newSizes
      (List<(string, Vector2)>, List<(string, Vector2)>, List<(string, Vector2)>) lastEvent = new();

      if (history.Any(h => h.Item1 == parent || h.Item1 == "dummy"))
      {
        getLine = history.LastOrDefault(h => h.Item1 == parent || h.Item1 == "dummy");

        lastEvent = getLine.Item2.LastOrDefault();

        oldNodeIDs = lastEvent.Item1.Select(x => x.Item1).Concat(lastEvent.Item2.Select(x => x.Item1)).ToList();
        currentNodeIDs = nodes.Select(n => n.ID).ToList();
        sameNodeIDs = oldNodeIDs.Intersect(currentNodeIDs).ToList();
        newNodeIDs = currentNodeIDs.Except(oldNodeIDs).ToList();
        deletedNodeIDs = oldNodeIDs.Except(currentNodeIDs).ToList();

        foreach (ILayoutNode node in nodes)
        {
          if (node != null && layout.ContainsKey(node))
          {
            Vector2 size = new Vector2(layout[node].Scale.x, layout[node].Scale.z);
            currentNodeIDsNewSizes.Add((node.ID, size));
          }
        }
        foreach (var currentNode in currentNodeIDsNewSizes)
        {
          if (sameNodeIDs.Contains(currentNode.Item1))
          {
            sameIDsNewSizes.Add(currentNode);
          }
          if (newNodeIDs.Contains(currentNode.Item1))
          {
            newNodeIDsNewSizes.Add(currentNode);
          }
        }
        foreach (string deletedID in deletedNodeIDs)
        {
          var deletedTupple = lastEvent.Item1.FirstOrDefault(x => x.Item1 == deletedID);
          deletedNodeIDsNewSizes.Add(deletedTupple);
        }
        int idx = history.FindLastIndex(h => h.Item1 == parent || h.Item1 == "dummy");
        if (idx != -1)
        {
            history[idx].Item2.Add((sameIDsNewSizes, newNodeIDsNewSizes, deletedNodeIDsNewSizes));
        }
        //PrintHistory();
        //Debug.Log("1");
      }
      else
      {
        newNodeIDsNewSizes = nodes.Select(n => (n.ID, new Vector2(layout[n].Scale.x, layout[n].Scale.z))).ToList();
        listOfHistory.Add((sameIDsNewSizes, newNodeIDsNewSizes, deletedNodeIDsNewSizes));
        if (!history.Any(h => h.Item1 == parent || h.Item1 == "dummy"))
        {
          history.Add((parent, listOfHistory));
        }
        
        //Debug.Log("2");
      }
      
    }

    //***********************************************************************************
    public void UsualProcess(ref Dictionary<ILayoutNode, NodeTransform> layout, ref List<ILayoutNode> nodes, ref Dictionary<PNode, float> preservers, ref Dictionary<PNode, float> expanders, ref PTree tree, ref Vector2 covrec)
    {
      /*
      SortNodesByAreaSize(nodes, layout);
      Vector2 worstCaseSize = Sum(nodes, layout);
      PTree tree = new(Vector2.zero, 1.1f * worstCaseSize);
      Vector2 covrec = Vector2.zero;
      */
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
        PNode fitNode = tree.Split(targetNode, requiredSize, el.ID);

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
    }
    //***********************************************************************************
    private static void SortNodesByAreaSize(List<ILayoutNode> nodes, Dictionary<ILayoutNode, NodeTransform> layout)
    {
      nodes.Sort(delegate (ILayoutNode left, ILayoutNode right)
      { return AreaSize(layout[right]).CompareTo(AreaSize(layout[left])); });
    }

    private void PrintHistory()
    {
      Debug.Log("Printing History:");
      foreach (var line in history)
      {
        Debug.Log($"Parent ID: {line.Item1}");
        foreach (var eventItem in line.Item2)
        {
          Debug.Log("  Event:");
          Debug.Log("    Same IDs and New Sizes:");
          foreach (var (id, size) in eventItem.Item1)
          {
            Debug.Log($"      ID: {id}, Size: {size}");
          }
          Debug.Log("    New IDs and Sizes:");
          foreach (var (id, size) in eventItem.Item2)
          {
            Debug.Log($"      ID: {id}, Size: {size}");
          }
          Debug.Log("    Deleted IDs and Sizes:");
          foreach (var (id, size) in eventItem.Item3)
          {
            Debug.Log($"      ID: {id}, Size: {size}");
          }
        }
      }
    }

    public void SetRootLayoutNode(Vector2 rectangle)
    {
      rootNode.ID = "1";
      graph.AddNode(rootNode);
      rootNode.ItsGraph = graph;
      rootLayoutNode = new LayoutGraphNode(rootNode);
      //rootLayoutNode.AddChild(firstNode);
      rootLayoutNode.Parent = null;

      rootLayoutNode.AbsoluteScale = new Vector3(rectangle.x * 2f, 0, rectangle.y * 2f);
    }

    #region GarbageMethods
    //Garbage methods 
    //***********************************************************************************
    public void FullJournal(Dictionary<ILayoutNode, NodeTransform> layout, List<ILayoutNode> nodes)
    {
      List<(string, List<Vector2>)> listIdsAndSizes = new();
      Debug.Log("Nodes to be packed:");
      foreach (ILayoutNode node in nodes)
      {
        (string, List<Vector2>) itemIdsAndSizes = new();
        if (!layout.ContainsKey(node))
        {
          Debug.LogWarning("Layout does not contain node************************************** " + node.ID);
          continue;
        }
        Vector2 scale = new Vector2(layout[node].Scale.x, layout[node].Scale.x);
        Debug.Log($"Node ID: {node.ID}, Scale: {scale}");
        itemIdsAndSizes.Item1 = node.ID;
        itemIdsAndSizes.Item2.Add(scale);
        listIdsAndSizes.Add(itemIdsAndSizes);
      }
      foreach (var row in rowss)
      {
        row.Add(listIdsAndSizes);
      }

    }
    //***********************************************************************************
    public void JournalNodes(Dictionary<ILayoutNode, NodeTransform> layout, List<ILayoutNode> nodes)
    {
      foreach (var rows in rowss)
      {
        foreach (var row in rows)
        {
        }
      }


      Dictionary<string, List<Vector2>> idsAndSizes = new();
      Debug.Log("Nodes to be packed:");
      foreach (ILayoutNode node in nodes)
      {
        if (!layout.ContainsKey(node))
        {
          Debug.LogWarning("Layout does not contain node************************************** " + node.ID);
          continue;
        }
        Vector2 scale = new Vector2(layout[node].Scale.x, layout[node].Scale.x);
        Debug.Log($"Node ID: {node.ID}, Scale: {scale}");

        idsAndSizes[node.ID].Add(scale);


      }
    }

    //***********************************************************************************
    /*
    // Creates a new ILayoutNode instance of the same runtime type as the given node, if possible.
    // If the type does not have a parameterless constructor, falls back to using MemberwiseClone.
     */
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

    #endregion
  }

}
