using LibGit2Sharp;
using SEE.DataModel.DG;
using SEE.Game.CityRendering;
using SEE.Game.HolisticMetrics.Metrics;
using SEE.Layout.NodeLayouts.RectanglePacking;
using SEE.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEngine.UI.ContentSizeFitter;

namespace SEE.Layout.NodeLayouts
{
  /// <summary>
  /// Simple rectangle layout that places nodes in a line
  /// and sorts them descending by Z inside the rectangle.
  /// </summary>
  public class IncrementalRectanglePackingLayout : NodeLayout, IIncrementalNodeLayout
  {
    static IncrementalRectanglePackingLayout()
    {
      Name = "Incremental Rectangle Packing Layout";
    }

    public IncrementalRectanglePackingLayout oldLayout;

    public IIncrementalNodeLayout OldLayout
    {
      set
      {
        if (value is IncrementalRectanglePackingLayout layout)
        {
          oldLayout = layout;
        }
        else
        {
          throw new ArgumentException(
              $"Predecessor of {nameof(IncrementalRectanglePackingLayout)} was not an {nameof(IncrementalRectanglePackingLayout)}.");
        }
      }
    }

    //protected override LayoutAnchor Anchor => LayoutAnchor.TopLeft;

    public List<Rec> recs;
    public List<ILayoutNode> leafsNodes;
    public Dictionary<ILayoutNode, NodeTransform> layoutResult;
    LayoutGraphNode rootLayoutNode;
    Graph graph;
    Node rootNode;
    public static Vector2 initialWorstCaseSize;


    public static bool changedOrDeleted = false;
    //public static List<(string, List<(List<(string, Vector2)>, List<(string, Vector2)>, List<(string, Vector2)>)>)> history;
    //                    parentID            sameIDs newSizes        newIDs  newSizes       deletedIDs  newSizes  worstCaseSize coverec
    public static List<(string, List<(List<(string, Vector2)>, List<(string, Vector2)>, List<(string, Vector2)>, Vector2, Vector2)>)> history;

    protected override Dictionary<ILayoutNode, NodeTransform> Layout(IEnumerable<ILayoutNode> layoutNodes, Vector3 centerPosition, Vector2 rectangle)
    {
      layoutResult = new Dictionary<ILayoutNode, NodeTransform>();
      //sleafsNodes = layoutNodes.Where(n => n != null && n.IsLeaf).ToList();

      ThirdScenario(layoutNodes.ToList(), centerPosition, rectangle);
      
      return layoutResult;

    }
    public void ThirdScenario(List<ILayoutNode> leafNodes, Vector3 centerPosition, Vector2 rectangle)
    {
      
      if (oldLayout == null)
      {
        //                    parentID            sameIDs newSizes        newIDs  newSizes       deletedIDs  newSizes  worstCaseSize  coverec
        history = new List<(string, List<(List<(string, Vector2)>, List<(string, Vector2)>, List<(string, Vector2)>, Vector2, Vector2)>)>();
        
      } 
      
      string rootLayoutNodeID = leafNodes.First().Parent != null ? leafNodes.First().Parent.ID : null;

      IList<ILayoutNode> layoutNodeList = leafNodes.ToList();
      if (layoutNodeList.Count == 1)
      {

        ILayoutNode layoutNode = layoutNodeList.First();
        layoutResult[layoutNode] = new NodeTransform(0, 0, layoutNode.AbsoluteScale);
        return;
      }

      {
        int numberOfLeaves = 0;
        foreach (ILayoutNode node in layoutNodeList)
        {
          if (node.IsLeaf)
          {

            Vector3 scale = node.AbsoluteScale;
            //float padding = Padding(scale.x, scale.z);
            //scale.x += padding;
            //scale.z += padding;
            layoutResult[node] = new NodeTransform(0, 0, scale);
            numberOfLeaves++;
          }
        }
        if (numberOfLeaves == layoutNodeList.Count)
        {
          // There are only leaves.
          Pack(layoutResult, layoutNodeList.Cast<ILayoutNode>().ToList(), GroundLevel);
          //RemovePadding(layoutResult);
          return;
        }
      }


      ICollection<ILayoutNode> roots = LayoutNodes.GetRoots(leafNodes);
      if (roots.Count == 1)
      {
        Debug.Log("only one root");
        ILayoutNode root = roots.FirstOrDefault();
        Vector2 area = PlaceNodes(layoutResult, root, GroundLevel);
        layoutResult[root] = new NodeTransform(0, 0, new Vector3(area.x, root.AbsoluteScale.y, area.y));
        //RemovePadding(layoutResult);
        MakeContained(layoutResult, root);
        return;
      }
      else
      {
        Debug.Log("multiple or zero roots");
        foreach (ILayoutNode leafNode in leafNodes)
        {
          
          layoutResult[leafNode] = new NodeTransform(
              0,
              0,
              leafNode.AbsoluteScale
          );
        }

        Pack(layoutResult, leafNodes.Cast<ILayoutNode>().ToList(), GroundLevel, rootLayoutNodeID);
        //RemovePadding(layoutResult);
      }
    }
    public Vector2 PlaceNodes(Dictionary<ILayoutNode, NodeTransform> layout, ILayoutNode node, float groundLevel)
    {
      if (node.IsLeaf )
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
            //Debug.Log("Placed node " + child.ID + " with area " + childArea + " in PlaceNodes");
            Debug.Log("child absolute scale: " + child.AbsoluteScale + " if child.isLeaf " + child.IsLeaf + " : child.Rests() " + child.Children().Count + " : if child.isLeaf " + child.Children().ToList().First().IsLeaf + " : " + child.Children().ToList().First().AbsoluteScale);
          
          }
          //Debug.Log("Placed node " + node.ID + " with area " + node.AbsoluteScale + " in PlaceNodes");
          //else
          //{
          //  layout[child] = new NodeTransform(0, 0, child.AbsoluteScale);
          //}
          //Debug.Log("Placed node " + child.ID + " with area " + childArea);
        }
        if (children.Count > 0)
        {
          Vector2 area = Pack(layout, children.Cast<ILayoutNode>().ToList(), groundLevel, node.ID);
          //float padding = Padding(area.x, area.y);

          Debug.Log("Packed node " + node.ID + " with area " + area + " in children.Count");
          //return new Vector2(area.x + padding, area.y + padding);

          return new Vector2(area.x, area.y);
        }
        else
        {
          return new Vector2(node.AbsoluteScale.x, node.AbsoluteScale.z);
        }
      }
    }

    private Vector2 PlaceNodes1(Dictionary<ILayoutNode, NodeTransform> layout, ILayoutNode node, float groundLevel)
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
            Vector2 childArea = PlaceNodes1(layout, child, groundLevel);
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
    private Vector2 Pack(Dictionary<ILayoutNode, NodeTransform> layout, List<ILayoutNode> nodes, float groundLevel, string parent = null)
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

      SortNodesByAreaSize(nodes, layout);
      Vector2 worstCaseSize = Sum(nodes, layout);
      /*
       
      Debug.Log("worst case size: " + worstCaseSize);
      Debug.Log("nodes: " + nodes.First().AbsoluteScale);
      Debug.Log("nodes: " + nodes.Last().AbsoluteScale);
      Debug.Log("layout: " + layout[nodes.First()]);
       */

      PTree tree = new(Vector2.zero, Vector2.zero);

      Vector2 covrec = Vector2.zero;

      /*
      if (oldLayout == null)
      {
        tree = new(Vector2.zero, 1.1f * initialWorstCaseSize);
        covrec = Vector2.zero;
      }
      else
      {
        tree = new(Vector2.zero, Vector2.zero);
        covrec = Vector2.zero;
      }
       */

      /*
      Add to history
      perform history 
      set the nodes to last scene


      Vector2 coverec =  UsualProcess(layout, nodes);
      return coverec;
       */

      string parentID = parent == null ? "dummy" : parent;
      AddToHistory(layout, nodes, worstCaseSize, parentID);
      PerformHistory(ref layout, ref nodes, ref tree, parentID);
      tree.Tighten(tree.Root);
      ResetCoverec(ref tree);
      PlaceNodesInLayout(ref layout, ref nodes, parentID, ref tree);
      return tree.coverec;


    }

    private static void MakeContained(Dictionary<ILayoutNode, NodeTransform> layout, ILayoutNode parent)
    {
      /*
      // The x co-ordinate of the left lower corner of the parent.
      // The z co-ordinate of the left lower corner of the parent.
       */
      NodeTransform parentTransform = layout[parent];
      Vector3 parentExtent = parentTransform.Scale / 2.0f;
      float xCorner = parentTransform.X - parentExtent.x;
      float zCorner = parentTransform.Z - parentExtent.z;

      foreach (ILayoutNode child in parent.Children())
      {
        //Debug.Log("Making contained: " + child.ID);
        layout[child].MoveBy(xCorner, zCorner);
        MakeContained(layout, child);
      }
    }
    public void PlaceNodesInLayout(ref Dictionary<ILayoutNode, NodeTransform> layout, ref List<ILayoutNode> nodes, string parent, ref PTree tree)
    {
      /*
       */
      foreach (ILayoutNode el in nodes)
      {
        //Debug.Log(el.Print());
        if (!layout.ContainsKey(el))
        {
          continue;
        }
        PNode fitNode = tree.FindNodeById2(el.ID);

        if (fitNode == null)
        {
          //Debug.Log("fitnode is null" + el.ID);
          continue;

        }
        Vector3 scale = layout[el].Scale;
        layout[el] = new NodeTransform(fitNode.Rectangle.Position.x + scale.x / 2.0f,
                                       fitNode.Rectangle.Position.y + scale.z / 2.0f,
                                       scale, fitNode);
        
        /*
        {
          Vector2 corner = fitNode.Rectangle.Position + new Vector2(scale.x, scale.z);
          Vector2 expandedCoveRec = new(Mathf.Max(covrec.x, corner.x), Mathf.Max(covrec.y, corner.y));
          if (!PTree.FitsInto(expandedCoveRec, covrec))
          {
            covrec = expandedCoveRec;
          }
        }
        Vector2 coverec = covrec;
        AddCoverecToHistory(coverec, parent);
         */
      }

      //PrintHistory();
      tree.Print1();
      Debug.Log("1********************************************************************************************************");
    }

    public void PlaceNodesInPTree(ref Dictionary<ILayoutNode, NodeTransform> layout, ref List<ILayoutNode> nodes, List<(string, Vector2)> newNodeIDsSizes, ref PTree tree, Vector2 worstCaseSize, string parent)
    {
      //SortNodesByAreaSize(nodes, layout);
      Vector2 oldWorstCaseSize = tree.Root.Rectangle.Size;
      Vector2 newWorstCaseSize = 1.1f * worstCaseSize;
      /*
      //tree.Root.Rectangle.Size = new Vector2(Mathf.Max(newWorstCaseSize.x,newWorstCaseSize.y), Mathf.Max(newWorstCaseSize.x, newWorstCaseSize.y));
       */
      tree.Root.Rectangle.Size = newWorstCaseSize;
      //tree.FreeLeavesAdjust1(oldWorstCaseSize);
      tree.Root.Rectangle.Position = Vector2.zero;

      Vector2 coverec = tree.coverec; // fix me each node should have its own coverec and tree which is not defined here u cant simply have one coverec for all nodes in the level because they can be in different subtrees of the root and thus have different coverecs and also when you place a node in the tree it can change the coverec of its subtree but not necessarily the coverec of the whole tree so you need to keep track of coverecs on a more granular level and not just one coverec for the whole tree


      foreach ((string newID, Vector2 size) in newNodeIDsSizes)
      {
        Vector2 requiredSize = size;

        
        Dictionary<PNode, float> preservers = new();
        Dictionary<PNode, float> expanders = new();
        tree.FreeLeaves = tree.FindEmpty(tree.Root, tree.Root.Rests);


        IList<PNode> sufficientLargeLeaves = tree.GetSufficientlyLargeLeaves(requiredSize, oldWorstCaseSize);


        if (sufficientLargeLeaves.Count == 0)
        {
          Debug.Log("--------------------------------------------------------------------------------------------------------------");
          tree.Print1();
          Debug.Log("--------------------------------------------------------------------------------------------------------------");
          if (tree.FreeLeaves.Count == 0) Debug.Log("no free leaves");
          else Debug.Log("free leaves: " + tree.FreeLeaves.Count);
          foreach (PNode freeLeaf in tree.FreeLeaves)
          {
            if (freeLeaf != null) Debug.Log(freeLeaf.ToString1());
            else Debug.Log("free leaf is null");
          }
          Debug.Log("--------------------------------------------------------------------------------------------------------------");

          throw new Exception("No sufficiently large free leaf found for size " + " :" + newID + ": :" + requiredSize + ": " + tree.coverec + " : " + tree.Root.Rectangle.Size + " : " + worstCaseSize);
        }
        foreach (PNode pnode in sufficientLargeLeaves)
        {
          Vector2 corner = pnode.Rectangle.Position + requiredSize;
          Vector2 expandedCoveRec = new(Mathf.Max(coverec.x, corner.x), Mathf.Max(coverec.y, corner.y));

          //Debug.Log(expandedCoveRec + " " + coverec);

          if (PTree.FitsInto(expandedCoveRec, coverec))
          {
            float waste = pnode.Rectangle.Size.x * pnode.Rectangle.Size.y - requiredSize.x * requiredSize.y;
            preservers[pnode] = waste;
            //Debug.Log("added to preservers");
          }
          else
          {
            /*
            float truncatedX = Mathf.Floor(expandedCoveRec.x * 10f) / 10f;
            float truncatedY = Mathf.Floor(expandedCoveRec.y * 10f) / 10f;
            float ratio = truncatedX / truncatedY;

            expanders[pnode] = Mathf.Abs(ratio - 1);
             */
            float ratio = expandedCoveRec.x / expandedCoveRec.y;
            expanders[pnode] = Mathf.Abs(ratio - 1);

            //Debug.Log("added to extenders");
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
          float bestRatio = Mathf.Infinity;
          //float smallestArea = Mathf.Infinity;
          foreach (KeyValuePair<PNode, float> entry in expanders)
          {
            var area = entry.Key.Rectangle.Size.x * entry.Key.Rectangle.Size.y;
            //if (entry.Value < bestRatio && area < smallestArea)
            if (entry.Value < bestRatio)
            {
              //smallestArea = area;
              targetNode = entry.Key;
              bestRatio = entry.Value;
            }
          }
           */
          // Find the minimum value
          Single minValue = expanders.Values.Min();

          // Filter nodes with that minimum value
          IEnumerable<KeyValuePair<PNode, float>> candidates = expanders
              .Where(kv => kv.Value == minValue);

          // Find the one with the smallest rectangle area
          KeyValuePair<PNode, float>? best = null;

          foreach (KeyValuePair<PNode, float> kv in candidates)
          {
            Single area = kv.Key.Rectangle.Size.x * kv.Key.Rectangle.Size.y;

            if (best == null)
            {
              best = kv;
            }
            else
            {
              Single bestArea = best.Value.Key.Rectangle.Size.x * best.Value.Key.Rectangle.Size.y;

              if (area < bestArea)
              {
                best = kv;
              }
            }
          }

          // Final result
          targetNode = best?.Key;
          /*
           */
          /*
          targetNode = expanders
            .Where(kv => kv.Value == expanders.Values.Min())
            .OrderBy(kv => kv.Key.Rectangle.Size.x * kv.Key.Rectangle.Size.y)
            .First()
            .Key;
           */
        }
        if (targetNode == null)
        {
          Debug.LogError("targetNode is null!");
          continue;
        }
        //PrintPreserverExpanders(preservers, expanders);

        PNode fitNode = new PNode(targetNode.Rectangle.Position, requiredSize, newID);
        tree.Root.Rests.Add(fitNode);
        fitNode.Parent = tree.Root;

        {
          //ResetCoverec(ref tree);

          /*
          coverec = new Vector2( Mathf.Max(
              Mathf.Max(tree.coverec.x, fitNode.Rectangle.Position.x + fitNode.Rectangle.Size.x),
              Mathf.Max(tree.coverec.y, fitNode.Rectangle.Position.y + fitNode.Rectangle.Size.y)
          ), Mathf.Max(
              Mathf.Max(tree.coverec.x, fitNode.Rectangle.Position.x + fitNode.Rectangle.Size.x),
              Mathf.Max(tree.coverec.y, fitNode.Rectangle.Position.y + fitNode.Rectangle.Size.y)
          )) ;
          tree.coverec = coverec;
           */



          Vector2 corner = fitNode.Rectangle.Position + size;
          Vector2 expandedCoveRec = new(Mathf.Max(coverec.x, corner.x), Mathf.Max(coverec.y, corner.y));
          if (!PTree.FitsInto(expandedCoveRec, coverec))
          {
            //coverec = new Vector2(Mathf.Max(expandedCoveRec.x, expandedCoveRec.y), Mathf.Max(expandedCoveRec.x, expandedCoveRec.y));
            coverec = expandedCoveRec;
            tree.coverec = coverec;

            //Debug.Log("coverec changed for a new node " + coverec);
          }
          /*
          Debug.Log("...........................");
          tree.Print();
          Debug.Log("...........................");
           */
        }

        /*
         */

      }
    }

    

    
    public void ResizeNodesInPTree1(List<(string, Vector2)> sameIDsNewSizes, ref PTree tree)
    {
      foreach ((string sameID, Vector2 size) in sameIDsNewSizes)
      {
        Vector2 requiredSize = size;
        PNode targetPNode = tree.FindNodeById2(sameID);

        if (targetPNode != null)
        {
          if (targetPNode.Rectangle.Size == requiredSize) continue;
          else
          {
            tree.GrowLeaf2(targetPNode, new Vector3(requiredSize.x, 1, requiredSize.y));

            Vector2 corner = targetPNode.Rectangle.Position + size;
            Vector2 expandedCoveRec = new(Mathf.Max(tree.coverec.x, corner.x), Mathf.Max(tree.coverec.y, corner.y));
            if (!PTree.FitsInto(expandedCoveRec, tree.coverec))
            {
              //tree.coverec = new Vector2(Mathf.Max(expandedCoveRec.x, expandedCoveRec.y), Mathf.Max(expandedCoveRec.x, expandedCoveRec.y));
              tree.coverec = expandedCoveRec;
              //Debug.Log("coverec changed for a new node -------------------- after resize" + tree.coverec);
            }
            //tree.Print1();
            //Debug.Log("--------------------------------------Resized node " + sameID + " to new size " + requiredSize);

          }
        }
      }
    }

    public void PerformHistory(ref Dictionary<ILayoutNode, NodeTransform> layout, ref List<ILayoutNode> nodes, ref PTree tree, string parent)
    {

      if (history.Any(h => h.Item1 == parent || h.Item1 == "dummy"))
      {
        (string, List<(List<(string, Vector2)>, List<(string, Vector2)>, List<(string, Vector2)>, Vector2, Vector2)>) getLine = history.LastOrDefault(h => h.Item1 == parent || h.Item1 == "dummy");

        // Iterate through all events in the history for this parent
        foreach ((List<(string, Vector2)>, List<(string, Vector2)>, List<(string, Vector2)>, Vector2, Vector2) historyEvent in getLine.Item2)
        {
          List<(string, Vector2)> sameIDsNewSizes = historyEvent.Item1;
          List<(string, Vector2)> newNodeIDsSizes = historyEvent.Item2;
          List<(string, Vector2)> deletedNodeIDsSizes = historyEvent.Item3;
          Vector2 worstCaseSize = historyEvent.Item4;
          if (sameIDsNewSizes.Count == 0 && deletedNodeIDsSizes.Count == 0)
          {
            PlaceNodesInPTree(ref layout, ref nodes, newNodeIDsSizes, ref tree, worstCaseSize, parent);
          }
          else
          {
            // First, handle deleted nodes
            foreach ((string deletedID, Vector2 size) in deletedNodeIDsSizes)
            {
              tree.DeleteMergeRemainLeaves2(id: deletedID);
              //tree.Tighten(tree.Root);
              //ResetCoverec(ref tree);
              changedOrDeleted = true;
            }
            // Second, handle resized nodes that are the same
            // set ptree to same nodes with new size
            if (sameIDsNewSizes.Count > 0)
            {
              ResizeNodesInPTree1(sameIDsNewSizes, ref tree);
              //tree.Tighten(tree.Root);
              //ResetCoverec(ref tree);
            }
            if (changedOrDeleted)
            {
              changedOrDeleted = false;
              //tree.Tighten(tree.Root);
            } 

            // Next, handle new nodes
            PlaceNodesInPTree(ref layout, ref nodes, newNodeIDsSizes, ref tree, worstCaseSize, parent);
            // Finally, update sizes of same nodes
          }
        }
      }
    }

    public void ResetCoverec(ref PTree tree)
    {
      List<Vector2> pnodes = tree.Root.Rests
        .Select(n => n.Rectangle.Position + n.Rectangle.Size)
        .ToList();
      Vector2 max = Vector2.zero;
      foreach (Vector2 corner in pnodes)
      {
        max = new Vector2(
            Mathf.Max(max.x, corner.x),
            Mathf.Max(max.y, corner.y)
        );
      }
      //tree.coverec = new Vector2(Mathf.Max(max.x, max.y), Mathf.Max(max.x,max.y));
      tree.coverec = max;

      Debug.Log("ResetCoverec " + tree.coverec);
    }

    #region untested
    public void AddCoverecToHistory(Vector2 coverec, string parent)
    {
      //parentID          sameIDs newSizes        newIDs  newSizes       deletedIDs  newSizes
      //(string, List<(List<(string, Vector2)>, List<(string, Vector2)>, List<(string, Vector2)>, Vector2, Vector2)>) getLine = new();

      //    sameIDs newSizes        newIDs  newSizes       deletedIDs  newSizes
      //(List<(string, Vector2)>, List<(string, Vector2)>, List<(string, Vector2)>, Vector2, Vector2) lastEvent = new();

      // Find the index of the history entry for the parent
      int historyIdx = history.FindLastIndex(h => h.Item1 == parent || h.Item1 == "dummy");
      if (historyIdx != -1)
      {
          var eventList = history[historyIdx].Item2;
          if (eventList.Count > 0)
          {
              // Get the last event, update its Item5 (coverec), and set it back
              var lastEvent = eventList[eventList.Count - 1];
              lastEvent.Item5 = coverec;
              eventList[eventList.Count - 1] = lastEvent;
          }
      }
    }

    public Vector2 GetCoverecFromHistory(string parent)
    {
      if (history.Any(h => h.Item1 == parent || h.Item1 == "dummy"))
      {
        var getLine = history.LastOrDefault(h => h.Item1 == parent || h.Item1 == "dummy");
        var lastEvent = getLine.Item2.LastOrDefault();
        return lastEvent.Item5;
      }
      Debug.LogWarning("No history found for parent: " + parent);
      return Vector2.zero;
    }
    #endregion

    public void AddToHistory(Dictionary<ILayoutNode, NodeTransform> layout, List<ILayoutNode> nodes, Vector2 worstCaseSize, string parent)
    {
    //                    parentID            sameIDs newSizes        newIDs  newSizes       deletedIDs  newSizes  worstCaseSize coverec
    //public static List<(string, List<(List<(string, Vector2)>, List<(string, Vector2)>, List<(string, Vector2)>, Vector2, Vector2)>)> history;

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
      List<(List<(string, Vector2)>, List<(string, Vector2)>, List<(string, Vector2)>, Vector2, Vector2)> listOfHistory = new();
      List<Vector2> newSizes = new();

      //parentID          sameIDs newSizes        newIDs  newSizes       deletedIDs  newSizes
      (string, List<(List<(string, Vector2)>, List<(string, Vector2)>, List<(string, Vector2)>, Vector2, Vector2)>) getLine = new();

      //    sameIDs newSizes        newIDs  newSizes       deletedIDs  newSizes
      (List<(string, Vector2)>, List<(string, Vector2)>, List<(string, Vector2)>, Vector2, Vector2) lastEvent = new();

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
          if (node != null)
          {
            Vector2 size = new Vector2(layout[node].Scale.x, layout[node].Scale.z);
            currentNodeIDsNewSizes.Add((node.ID, size));
          }
        }
        foreach ((string, Vector2) currentNode in currentNodeIDsNewSizes)
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
          List<(string, Vector2)> oldTupples = lastEvent.Item1.Concat(lastEvent.Item2).ToList();
          List<(string,Vector2)> deletedTupple = oldTupples.Where(x => x.Item1 == deletedID).ToList();
          deletedNodeIDsNewSizes.AddRange(deletedTupple);
        }

        int idx = history.FindLastIndex(h => h.Item1 == parent || h.Item1 == "dummy");
        if (idx != -1)
        {
          history[idx].Item2.Add((sameIDsNewSizes, newNodeIDsNewSizes, deletedNodeIDsNewSizes, worstCaseSize, Vector2.zero));
        }
        //PrintHistory();
        //Debug.Log("1");
      }
      else
      {
        newNodeIDsNewSizes = nodes.Select(n => (n.ID, new Vector2(layout[n].Scale.x, layout[n].Scale.z))).ToList();
        listOfHistory.Add((sameIDsNewSizes, newNodeIDsNewSizes, deletedNodeIDsNewSizes, worstCaseSize, Vector2.zero));
        if (!history.Any(h => h.Item1 == parent || h.Item1 == "dummy"))
        {
          history.Add((parent, listOfHistory));
        }
        //PrintHistory();
        //Debug.Log("2");
      }

    }


    private Vector2 UsualProcess(Dictionary<ILayoutNode, NodeTransform> layout, List<ILayoutNode> nodes)
    {

      nodes.Sort(delegate (ILayoutNode left, ILayoutNode right)
      { return AreaSize(layout[right]).CompareTo(AreaSize(layout[left])); });


      Vector2 worstCaseSize = Sum(nodes, layout);

      PTree tree = new(Vector2.zero, 1.1f * worstCaseSize);

      Vector2 covrec = Vector2.zero;

      Dictionary<PNode, float> preservers = new();

      Dictionary<PNode, float> expanders = new();

      foreach (ILayoutNode el in nodes)
      {

        Vector2 requiredSize = GetRectangleSize(layout[el]);

        preservers.Clear();
        expanders.Clear();

        foreach (PNode pnode in tree.GetSufficientlyLargeLeaves(requiredSize))
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

        PNode fitNode = tree.Split(targetNode, requiredSize);

        Vector3 scale = layout[el].Scale;
        layout[el] = new NodeTransform(fitNode.Rectangle.Position.x + scale.x / 2.0f,
                                       fitNode.Rectangle.Position.y + scale.z / 2.0f,
                                       scale);

        {
          Vector2 corner = fitNode.Rectangle.Position + requiredSize;
          Vector2 expandedCoveRec = new(Mathf.Max(covrec.x, corner.x), Mathf.Max(covrec.y, corner.y));

          if (!PTree.FitsInto(expandedCoveRec, covrec))
          {
            covrec = expandedCoveRec;
          }
        }
      }
      Debug.Log("tree.root.size: " + tree.Root.Rectangle.Size);
      //tree.Print();
      return covrec;
    }
    public Vector2 UsualProcess1(ref Dictionary<ILayoutNode, NodeTransform> layout, ref List<ILayoutNode> nodes)
    {
      /*
      */
      SortNodesByAreaSize(nodes, layout);
      Vector2 worstCaseSize = Sum(nodes, layout);
      PTree tree = new(Vector2.zero, 1.1f * worstCaseSize);
      Vector2 covrec = Vector2.zero;

      Dictionary<PNode, float> preservers = new();

      Dictionary<PNode, float> expanders = new();

      foreach (ILayoutNode el in nodes)
      {
        if (!layout.ContainsKey(el))
        {
          Debug.LogWarning("Layout does not contain element************************************** " + el.ID);
          continue;
        }

        Vector2 requiredSize = GetRectangleSize(layout[el]);

        preservers.Clear();

        expanders.Clear();

        var sufficientLargeLeaves = tree.GetSufficientlyLargeLeaves(requiredSize);
        //tree.Print();

        if (sufficientLargeLeaves.Count == 0)
        {
          tree.Print1();
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
          /*
          targetNode = expanders
            .Where(kv => kv.Value == expanders.Values.Min())
            .OrderBy(kv => kv.Key.Rectangle.Size.x * kv.Key.Rectangle.Size.y)
            .First()
            .Key;
        }
           */

          if (targetNode == null)
          {
            Debug.LogError("targetNode is null!");
            continue;
          }
          PNode fitNode = tree.Split1(targetNode, requiredSize, el.ID);

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
        tree.Print1();
        Debug.Log("********************************************************************************************************");
      }
        return covrec;
    }

    public static Vector2 GetRectangleSize(NodeTransform node)
    {
      Vector3 size = node.Scale;
      return new Vector2(size.x, size.z);
    }


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

    private static void SortNodesByAreaSize(List<ILayoutNode> nodes, Dictionary<ILayoutNode, NodeTransform> layout)
    {
      nodes.Sort(delegate (ILayoutNode left, ILayoutNode right)
      { return AreaSize(layout[right]).CompareTo(AreaSize(layout[left])); });
    }

    public static float AreaSize(NodeTransform node)
    {
      Vector3 size = node.Scale;
      return size.x * size.z;
    }
    public void SecondScenario(IEnumerable<ILayoutNode> layoutNodes, Vector3 centerPosition, Vector2 rectangle)
    {
      SetRootLayoutNode(rectangle);
      float zStart = centerPosition.z + rectangle.y / 2f;
      float xStart = centerPosition.z + rectangle.y / 2f;

      float zPointer = zStart;
      float xPointer = xStart;

      float zScalePointer = zStart;
      float xScalePointer = xStart;

      float limitZ = -zStart;
      float limitX = -xStart;

      NodeTransform parentTransform = new NodeTransform(
          0,
          0,
          rootLayoutNode.AbsoluteScale
      );

      layoutResult[rootLayoutNode] = parentTransform;

      foreach (var leafNode in leafsNodes)
      {
        Vector3 nodeScale = leafNode.AbsoluteScale;
        if (zPointer + nodeScale.x > limitZ)
        {
          // Move to next row
          zPointer = zStart;
          xPointer -= xScalePointer;
        }
        NodeTransform nodeTransform = new NodeTransform(
            zPointer,
            xPointer,
            nodeScale
        );
        if (nodeScale.z > xScalePointer) xScalePointer = nodeScale.z;
        layoutResult[leafNode] = nodeTransform;
        xPointer -= nodeScale.x + .1f;

        rootLayoutNode.AddChild(leafNode);
        
      }
    }
    public void PlaceNodesInRecs()
    {
      foreach (var leafNode in leafsNodes)
      {
        Rec nodeRec = new Rec(0, 0, leafNode.AbsoluteScale.x, leafNode.AbsoluteScale.z);
      }
    }
    public void PrintDictionary(Dictionary<ILayoutNode, NodeTransform> dict)
      {
        foreach (var entry in dict)
        {
          Debug.Log($"Node: {entry.Key.Print()}, Transform: {entry.Value}");
        }
      }
    public void FirstScenario(IEnumerable<ILayoutNode> layoutNodes, Vector3 centerPosition, Vector2 rectangle)
    {
      ILayoutNode firstNode = layoutNodes.FirstOrDefault(n => n != null && n.IsLeaf);

      SetRootLayoutNode(rectangle);

      Debug.Log(rectangle + " " + centerPosition);

      /*
      for (int i = 0; i < count; i++)
      {
        ILayoutNode node = nodes[i];

        float z = startZ - i * spacing;

      }

      float xx = x - rootLayoutNode.AbsoluteScale.x / 2f;
      float zz = z - rootLayoutNode.AbsoluteScale.z / 2f;
       */
      float x = centerPosition.x - rectangle.x / 2f;
      float z = centerPosition.z + rectangle.y / 2f;


      NodeTransform parentTransform = new NodeTransform(
          0,
          0,
          rootLayoutNode.AbsoluteScale
      );

      NodeTransform firstNodeTransform = new NodeTransform(
          -z,
          -z,
          firstNode.AbsoluteScale
      );

      layoutResult[rootLayoutNode] = parentTransform;
      layoutResult[firstNode] = firstNodeTransform;

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

    private void PrintPreserverExpanders(Dictionary<PNode, float> preservers, Dictionary<PNode, float> expanders)
    {
      Debug.Log("--------------------------------------------");
      Debug.Log("preservers----------------------------------");
      foreach (var entry in preservers)
      {
        Debug.Log($"PNode ID: {entry.Key.ToString()}, Waste: {entry.Value}");
      }
      Debug.Log("--------------------------------------------");
      Debug.Log("expanders-----------------------------------");
      foreach (var entry in expanders)
      {
        Debug.Log($"PNode ID: {entry.Key.ToString()}, Ratio Difference: {entry.Value}");
      }
      Debug.Log("--------------------------------------------");
    }

    private static void RemovePadding(Dictionary<ILayoutNode, NodeTransform> layout)
    {
      // We use a copy of the keys because we will modify layout during the iteration.
      ICollection<ILayoutNode> layoutNodes = new List<ILayoutNode>(layout.Keys);

      foreach (ILayoutNode layoutNode in layoutNodes)
      {
        // We added padding to both inner nodes and leaves, but we want to
        // restore the original size of the leaves only.
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

    //public float Padding(float width, float depth)
    //{
    //  return Mathf.Clamp(Mathf.Min(width, depth) * paddingFactor, minimimalAbsolutePadding, maximalAbsolutePadding);
    //}

  }
  public class Rec
  {
    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="x">X co-ordinate at corner.</param>
    /// <param name="z">Z co-ordinate at corner.</param>
    /// <param name="width">Width of the rectangle.</param>
    /// <param name="depth">Depth (breadth) of the rectangle.</param>
    public Rec(float x, float z, float width, float depth)
    {
      X = x;
      Z = z;
      Width = width;
      Depth = depth;
    }
    /// <summary>
    /// X co-ordinate at corner.
    /// </summary>
    public float X;
    /// <summary>
    /// Z co-ordinate at corner.
    /// </summary>
    public float Z;
    /// <summary>
    /// Width of the rectangle.
    /// </summary>
    public float Width;
    /// <summary>
    /// Depth (breadth) of the rectangle.
    /// </summary>
    public float Depth;

    public Vector2 Center()
    {
      return new Vector2(X + Width / 2f, Z + Depth / 2f);
    }

    public Vector2 position
    {
      get { return new Vector2(X, Z); }
    }
  }
  public class NodeSize
  {
    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="gameNode">Layout node this node size corresponds to.</param>
    /// <param name="size">Size of the node.</param>
    public NodeSize(ILayoutNode gameNode, float size)
    {
      GameNode = gameNode;
      Size = size;
    }
    /// <summary>
    /// The layout node this node size corresponds to.
    /// </summary>
    public ILayoutNode GameNode;
    /// <summary>
    /// The size of the node.
    /// </summary>
    public float Size;
  }
}
