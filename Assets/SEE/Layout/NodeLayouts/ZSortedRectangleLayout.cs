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

namespace SEE.Layout.NodeLayouts
{
  /// <summary>
  /// Simple rectangle layout that places nodes in a line
  /// and sorts them descending by Z inside the rectangle.
  /// </summary>
  public class ZSortedRectangleLayout : NodeLayout, IIncrementalNodeLayout
  {
    static ZSortedRectangleLayout()
    {
      Name = "ZSortedRectangleLayout";
    }

    public ZSortedRectangleLayout oldLayout;

    public IIncrementalNodeLayout OldLayout
    {
      set
      {
        if (value is ZSortedRectangleLayout layout)
        {
          oldLayout = layout;
        }
        else
        {
          throw new ArgumentException(
              $"Predecessor of {nameof(ZSortedRectangleLayout)} was not an {nameof(ZSortedRectangleLayout)}.");
        }
      }
    }

    //protected override LayoutAnchor Anchor => LayoutAnchor.TopLeft;

    public List<Rec> recs;
    public List<ILayoutNode> leafsNodes;
    public Dictionary<ILayoutNode, NodeTransform> entries;
    LayoutGraphNode rootLayoutNode;
    Graph graph;
    Node rootNode;
    public static Vector2 initialWorstCaseSize;
    //public static List<(string, List<(List<(string, Vector2)>, List<(string, Vector2)>, List<(string, Vector2)>)>)> history;
    //                    parentID            sameIDs newSizes        newIDs  newSizes       deletedIDs  newSizes  worstCaseSize coverec
    public static List<(string, List<(List<(string, Vector2)>, List<(string, Vector2)>, List<(string, Vector2)>, Vector2, Vector2)>)> history;

    /*
    public override Dictionary<ILayoutNode, NodeTransform> Create(
        IEnumerable<ILayoutNode> layoutNodes,
        Vector3 centerPosition,
        Vector2 rectangle)
    {
      var layout = base.Create(layoutNodes, centerPosition, rectangle);

      if (layout.Count == 0)
        return layout;

      var entry = layout.First();
      NodeTransform t = entry.Value;

      float x = centerPosition.x - rectangle.x / 2f + t.Scale.x / 2f;
      float z = centerPosition.z + rectangle.y / 2f - t.Scale.z / 2f;

      t.MoveTo(x, z);

      return layout;
    }
     */
    /*
    what is my task today
    make a rectangle of the size of the node
    place the node in the middle of it
    place the next rectangle next to it until the limit z is reached
    then go to the next row


     */
    protected override Dictionary<ILayoutNode, NodeTransform> Layout(
        IEnumerable<ILayoutNode> layoutNodes,
        Vector3 centerPosition,
        Vector2 rectangle)
    {
      graph = new Graph();
      rootNode = new Node();
      entries = new Dictionary<ILayoutNode, NodeTransform>();
      recs = new List<Rec>();
      leafsNodes = layoutNodes.Where(n => n != null && n.IsLeaf).ToList();

      //FirstScenario(layoutNodes, centerPosition, rectangle);
      //SecondScenario(leafsNodes, centerPosition, rectangle);

      ThirdScenario(leafsNodes, centerPosition, rectangle);


      //PrintDictionary(entries);
      
      return entries;

    }
    public void ThirdScenario(List<ILayoutNode> leafNodes, Vector3 centerPosition, Vector2 rectangle)
    {
      //var oldLeafsIDs = oldLayout != null ? oldLayout.leafsNodes.Select(n => n.ID).ToList() : new List<string>();
      List<ILayoutNode> sameLeafs;

      if (oldLayout == null)
      {
        //                    parentID            sameIDs newSizes        newIDs  newSizes       deletedIDs  newSizes  worstCaseSize
        history = new List<(string, List<(List<(string, Vector2)>, List<(string, Vector2)>, List<(string, Vector2)>, Vector2, Vector2)>)>();
        sameLeafs = leafNodes;
      } 
      else 
      {
        var oldLeafsIDs = oldLayout.leafsNodes.Select(n => n.ID).ToList();
        sameLeafs = leafNodes.Where(n => oldLeafsIDs.Contains(n.ID)).ToList(); 
      }
      foreach (var leafNode in leafNodes)
      {
        entries[leafNode] = new NodeTransform(
            0,
            0,
            leafNode.AbsoluteScale
        );
      }
      /*
      rootLayoutNode =new LayoutGraphNode(
          rootNode
      );
      rootLayoutNode.AbsoluteScale = new Vector3(
          centerPosition.z + rectangle.y / 2f,
          0,
          centerPosition.z + rectangle.y / 2f
      );
       */

      

      Pack(entries, leafNodes.Cast<ILayoutNode>().ToList(), GroundLevel, rootLayoutNode);
    }

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

      //SortNodesByAreaSize(nodes, layout);
      Vector2 worstCaseSize = Sum(nodes, layout);

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

      var coverec =  UsualProcess(ref layout, ref nodes);
      return coverec;

       */
      string parentID = parent == null ? "dummy" : parent.ID;
      AddToHistory(layout, nodes, worstCaseSize, parentID);
      PerformHistory(ref layout, ref nodes, ref tree, ref covrec, parentID);
      PlaceNodes(ref layout, ref nodes, parentID, ref tree, ref covrec);
      return covrec;

      /*
      foreach(var node in nodes)
      {
        parent.AddChild(node);
      }
      layout[rootLayoutNode] = new NodeTransform(
            0,
            0,
            new Vector3(covrec.x, 0,covrec.y)
        );

      MakeContained(layout, parent);
       */

      //List nodes = leafsNodes


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
    }

    private static void MakeContained(Dictionary<ILayoutNode, NodeTransform> layout, ILayoutNode parent)
    {
      NodeTransform parentTransform = layout[parent];
      Vector3 parentExtent = parentTransform.Scale / 2.0f;
      // The x co-ordinate of the left lower corner of the parent.
      float xCorner = parentTransform.X - parentExtent.x;
      // The z co-ordinate of the left lower corner of the parent.
      float zCorner = parentTransform.Z - parentExtent.z;

      foreach (ILayoutNode child in parent.Children())
      {
        Debug.Log("Making contained: " + child.ID);
        layout[child].MoveBy(xCorner, zCorner);
        MakeContained(layout, child);
      }
    }
    public void PlaceNodes(ref Dictionary<ILayoutNode, NodeTransform> layout, ref List<ILayoutNode> nodes, string parent, ref PTree tree, ref Vector2 covrec)
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
        PNode fitNode = tree.FindNodeById(el.ID);

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
      tree.Print();
      Debug.Log("********************************************************************************************************");
    }

    public void PlaceNodesInPTree(ref Dictionary<ILayoutNode, NodeTransform> layout, ref List<ILayoutNode> nodes, List<(string, Vector2)> newNodeIDsSizes, ref PTree tree, Vector2 worstCaseSize, string parent)
    {
      var oldWorstCaseSize = tree.Root.Rectangle.Size;
      tree.Root.Rectangle.Size = 1.1f * worstCaseSize;
      tree.FreeLeavesAdjust(oldWorstCaseSize);
      tree.Root.Rectangle.Position = Vector2.zero;

      Vector2 coverec = tree.coverec;


      foreach (var (newID, size) in newNodeIDsSizes)
      {
        Vector2 requiredSize = size;

        if (tree.FindNodeById(newID) != null)
        {
          Debug.Log("node already inside " + newID);
          PNode presentFitNode = tree.FindNodeById(newID);
          {
            Vector2 corner = presentFitNode.Rectangle.Position + size;
            Vector2 expandedCoveRec = new(Mathf.Max(coverec.x, corner.x), Mathf.Max(coverec.y, corner.y));
            if (!PTree.FitsInto(expandedCoveRec, coverec))
            {
              coverec = expandedCoveRec;
              Debug.Log("coverec changed for a node already inside " + coverec);
            }
          }
          continue;
        }

        Dictionary<PNode, float> preservers = new();
        Dictionary<PNode, float> expanders = new();
        var sufficientLargeLeaves = tree.GetSufficientlyLargeLeaves(requiredSize);
        if (sufficientLargeLeaves.Count == 0)
        {
          tree.Print();
          throw new Exception("No sufficiently large free leaf found for size " + " :" + newID + ": ");
        }


        foreach (PNode pnode in sufficientLargeLeaves)
        {
          Vector2 corner = pnode.Rectangle.Position + requiredSize;
          Vector2 expandedCoveRec = new(Mathf.Max(coverec.x, corner.x), Mathf.Max(coverec.y, corner.y));

          Debug.Log(expandedCoveRec + " " + coverec);

          if (PTree.FitsInto(expandedCoveRec, coverec))
          {
            float waste = pnode.Rectangle.Size.x * pnode.Rectangle.Size.y - requiredSize.x * requiredSize.y;
            preservers[pnode] = waste;
            Debug.Log("added to preservers");
          }
          else
          {
            float truncatedX = Mathf.Floor(expandedCoveRec.x * 10f) / 10f;
            float truncatedY = Mathf.Floor(expandedCoveRec.y * 10f) / 10f;
            float ratio = truncatedX / truncatedY;

            expanders[pnode] = Mathf.Abs(ratio - 1);
            Debug.Log("added to extenders");
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
          targetNode = expanders
            .Where(kv => kv.Value == expanders.Values.Min())
            .OrderBy(kv => kv.Key.Rectangle.Size.x * kv.Key.Rectangle.Size.y)
            .First()
            .Key;
        }
        if (targetNode == null)
        {
          Debug.LogError("targetNode is null!");
          continue;
        }
        PrintPreserverExpanders(preservers, expanders);

        PNode fitNode = tree.Split(targetNode, requiredSize, newID);

        {
          Vector2 corner = fitNode.Rectangle.Position + size;
          Vector2 expandedCoveRec = new(Mathf.Max(coverec.x, corner.x), Mathf.Max(coverec.y, corner.y));
          if (!PTree.FitsInto(expandedCoveRec, coverec))
          {
            coverec = expandedCoveRec;
            tree.coverec = coverec;
            Debug.Log("coverec changed for a new node " + coverec);
          }
        }

        /*
         */

      }
    }

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

          /*
          {
            Vector2 corner = targetPNode.Rectangle.Position + requiredSize;
            Vector2 expandedCoveRec = new(Mathf.Max(covrec.x, corner.x), Mathf.Max(covrec.y, corner.y));
            if (!PTree.FitsInto(expandedCoveRec, covrec))
            {
              covrec = expandedCoveRec;
            }
          }
           */
        }
      }
    }

    public void PerformHistory(ref Dictionary<ILayoutNode, NodeTransform> layout, ref List<ILayoutNode> nodes, ref PTree tree, ref Vector2 covrec, string parent)
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
          Vector2 worstCaseSize = historyEvent.Item4;
          if (sameIDsNewSizes.Count == 0 && deletedNodeIDsSizes.Count == 0)
          {
            PlaceNodesInPTree(ref layout, ref nodes, newNodeIDsSizes, ref tree, worstCaseSize, parent);
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
            PlaceNodesInPTree(ref layout, ref nodes, newNodeIDsSizes, ref tree, worstCaseSize, parent);
            // Finally, update sizes of same nodes
          }
        }
      }
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
    //                    parentID            sameIDs newSizes        newIDs  newSizes       deletedIDs  newSizes  worstCaseSize
    //public static List<(string, List<(List<(string, Vector2)>, List<(string, Vector2)>, List<(string, Vector2)>, Vector2)>)> history;

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
            Vector2 size = new Vector2(node.AbsoluteScale.x, node.AbsoluteScale.z);
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

    public Vector2 UsualProcess(ref Dictionary<ILayoutNode, NodeTransform> layout, ref List<ILayoutNode> nodes)
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
            float truncatedX = Mathf.Floor(expandedCoveRec.x * 10f) / 10f;
            float truncatedY = Mathf.Floor(expandedCoveRec.y * 10f) / 10f;
            float ratio = truncatedX / truncatedY;

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
          targetNode = expanders
            .Where(kv => kv.Value == expanders.Values.Min())
            .OrderBy(kv => kv.Key.Rectangle.Size.x * kv.Key.Rectangle.Size.y)
            .First()
            .Key;
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
      tree.Print();
      Debug.Log("********************************************************************************************************");
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

      entries[rootLayoutNode] = parentTransform;

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
        entries[leafNode] = nodeTransform;
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

      entries[rootLayoutNode] = parentTransform;
      entries[firstNode] = firstNodeTransform;

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
