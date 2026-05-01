using SEE.Layout.NodeLayouts.CirclePacking;
using SEE.Layout.NodeLayouts.RectanglePacking;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SEE.Layout.NodeLayouts
{

  public class CirclePackingNodeLayout1 : NodeLayout, IIncrementalNodeLayout
  {
    static CirclePackingNodeLayout1()
    {
      Name = "Circle Packing1";
    }

    public CirclePackingNodeLayout1 oldLayout;

    public IIncrementalNodeLayout OldLayout
    {
      set
      {
        if (value is CirclePackingNodeLayout1 layout)
        {
          oldLayout = layout;
        }
        else
        {
          throw new ArgumentException(
              $"Predecessor of {nameof(CirclePackingNodeLayout1)} was not an {nameof(CirclePackingNodeLayout1)}.");
        }
      }
    }

    private Dictionary<ILayoutNode, NodeTransform> layoutResult;

    //                    parentID            sameIDs newSizes        newIDs  newSizes       deletedIDs  newSizes  worstCaseSize coverec
    public static List<(string, List<(List<(string, Vector2)>, List<(string, Vector2)>, List<(string, Vector2)>, Vector2, Vector2)>)> history;
    //********************************************************************************************************************************
    protected override Dictionary<ILayoutNode, NodeTransform> Layout(IEnumerable<ILayoutNode> layoutNodes, Vector3 centerPosition, Vector2 rectangle)
    {

      return FirstScenario(layoutNodes, centerPosition, rectangle);

    }
    //********************************************************************************************************************************
    public Dictionary<ILayoutNode, NodeTransform> FirstScenario(IEnumerable<ILayoutNode> layoutNodes, Vector3 centerPosition, Vector2 rectangle)
    {
      layoutResult = new Dictionary<ILayoutNode, NodeTransform>();

      ICollection<ILayoutNode> roots = LayoutNodes.GetRoots(layoutNodes);
      if (roots.Count == 0)
      {
        throw new System.Exception("Graph has no root node.");
      }
      else if (roots.Count > 1)
      {
        throw new System.Exception("Graph has more than one root node.");
      }
      else
      {
        ILayoutNode root = roots.FirstOrDefault();

        //AddToHistory(layoutResult, layoutNodes.ToList(), rectangle, root.ID);

        // exactly one root
        float outRadius = PlaceNodes(root, layoutResult);
        Vector2 position = Vector2.zero;
        layoutResult[root] = new NodeTransform(position.x, position.y, GetScale(root, outRadius));
        MakeGlobal(layoutResult, position, root.Children());
        Debug.Log("**************************************************************************************");
        return layoutResult;
      }
    }
    //********************************************************************************************************************************
    private static void MakeGlobal(Dictionary<ILayoutNode, NodeTransform> layoutResult, Vector2 position, ICollection<ILayoutNode> children)
    {
      foreach (ILayoutNode child in children)
      {
        NodeTransform childTransform = layoutResult[child];
        Vector2 childPosition = new Vector2(childTransform.X, childTransform.Z) + position;
        childTransform.MoveTo(childPosition.x, childPosition.y);
        layoutResult[child] = childTransform;
        MakeGlobal(layoutResult, childPosition, child.Children());
      }
    }

    //********************************************************************************************************************************
    public float PlaceNodes(ILayoutNode parent, Dictionary<ILayoutNode, NodeTransform> layout)
    {
      ICollection<ILayoutNode> children = parent.Children();

      if (children.Count == 0)
      {

        return LeafRadius(parent);
      }
      else
      {
        List<Circle1> circles = new(children.Count);

        int i = 0;
        foreach (ILayoutNode child in children)
        {
          float radius = child.IsLeaf ? LeafRadius(child) : PlaceNodes(child, layout);

          float radians = (i / (float)children.Count) * (2.0f * Mathf.PI);
          circles.Add(new Circle1(child, new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * radius, radius));
          i++;
        }
        ///////////////////////////////////////

        ///////////////////////////////////////
        //CirclePacker.Pack(0.1f, circles, out float outOuterRadius);
        
        CirclePacker1.PackCircles(circles, Vector2.zero, out float outOuterRadius, oldLayout == null, parent.ID);

        if (children.Count == 1 && !children.ElementAt(0).IsLeaf)
        {
          outOuterRadius *= 1.2f;
        }

        foreach (Circle1 circle in circles)
        {

          layout[circle.GameObject]
               = new NodeTransform(circle.Center.x, circle.Center.y,
                                   GetScale(circle.GameObject, circle.Radius));
        }
        return outOuterRadius;
      }
    }


    












    //********************************************************************************************************************************

    /*
    internal class CirclePacker2
    {
      private List<Circle1> placedCircles = new List<Circle1>();

      private Vector2 containerCenter;
      private float containerRadius;

      public CirclePacker2(Vector2 center, float radius)
      {
        containerCenter = center;
        containerRadius = radius;
      }

      public Circle1 FindEmptyPlace(ILayoutNode node, float radius)
      {
        List<Vector2> candidates = new List<Vector2>();

        // 1. Try center
        candidates.Add(containerCenter);

        // 2. Generate candidates around existing circles
        foreach (var c in placedCircles)
        {
          float dist = c.Radius + radius;

          int steps = 20;
          for (int i = 0; i < steps; i++)
          {
            float angle = (i / (float)steps) * Mathf.PI * 2f;
            Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

            Vector2 candidate = c.Center + dir * dist;
            candidates.Add(candidate);
          }
        }

        Vector2 bestPos = Vector2.zero;
        float bestScore = float.MaxValue;
        bool found = false;

        foreach (var pos in candidates)
        {
          if (!IsInsideContainer(pos, radius)) continue;
          if (IsOverlapping(pos, radius)) continue;

          float score = (pos - containerCenter).sqrMagnitude;

          if (score < bestScore)
          {
            bestScore = score;
            bestPos = pos;
            found = true;
          }
        }

        // 3. Fallback (random search)
        if (!found)
        {
          for (int i = 0; i < 100; i++)
          {
            Vector2 randomPos = containerCenter + UnityEngine.Random.insideUnitCircle * (containerRadius - radius);

            if (!IsOverlapping(randomPos, radius))
            {
              return new Circle1(node, randomPos, radius);
            }
          }

          Debug.LogWarning("No valid position found!");
        }

        return new Circle1(node, bestPos, radius);
      }

      public void AddCircle(Circle1 circle)
      {
        placedCircles.Add(circle);
      }

      private bool IsInsideContainer(Vector2 pos, float radius)
      {
        return Vector2.Distance(pos, containerCenter) + radius <= containerRadius;
      }

      private bool IsOverlapping(Vector2 pos, float radius)
      {
        foreach (var c in placedCircles)
        {
          float dist = Vector2.Distance(pos, c.Center);
          if (dist < (radius + c.Radius))
            return true;
        }
        return false;
      }
    }
     */



    //********************************************************************************************************************************
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
          List<(string, Vector2)> deletedTupple = oldTupples.Where(x => x.Item1 == deletedID).ToList();
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
    //********************************************************************************************************************************
    public void PerformHistory(ref Dictionary<ILayoutNode, NodeTransform> layout, ref List<ILayoutNode> nodes, ref PTree tree, string parent)
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
              tree.DeleteMergeRemainLeaves2(id: deletedID);
              //tree.Tighten(tree.Root);
              //ResetCoverec(ref tree);
            }
            // Second, handle resized nodes that are the same
            // set ptree to same nodes with new size
            if (sameIDsNewSizes.Count > 0)
            {
              ResizeNodesInPTree1(sameIDsNewSizes, ref tree);
              //tree.Tighten(tree.Root);
              //ResetCoverec(ref tree);
            }


            // Next, handle new nodes
            PlaceNodesInPTree(ref layout, ref nodes, newNodeIDsSizes, ref tree, worstCaseSize, parent);
            // Finally, update sizes of same nodes
          }
        }
      }
    }
    //********************************************************************************************************************************
    public void PlaceNodesInPTree(ref Dictionary<ILayoutNode, NodeTransform> layout, ref List<ILayoutNode> nodes, List<(string, Vector2)> newNodeIDsSizes, ref PTree tree, Vector2 worstCaseSize, string parent)
    {
      //SortNodesByAreaSize(nodes, layout);
      var oldWorstCaseSize = tree.Root.Rectangle.Size;
      var newWorstCaseSize = 1.1f * worstCaseSize;
      /*
      //tree.Root.Rectangle.Size = new Vector2(Mathf.Max(newWorstCaseSize.x,newWorstCaseSize.y), Mathf.Max(newWorstCaseSize.x, newWorstCaseSize.y));
       */
      tree.Root.Rectangle.Size = newWorstCaseSize;
      //tree.FreeLeavesAdjust1(oldWorstCaseSize);
      tree.Root.Rectangle.Position = Vector2.zero;

      Vector2 coverec = tree.coverec; // fix me each node should have its own coverec and tree which is not defined here u cant simply have one coverec for all nodes in the level because they can be in different subtrees of the root and thus have different coverecs and also when you place a node in the tree it can change the coverec of its subtree but not necessarily the coverec of the whole tree so you need to keep track of coverecs on a more granular level and not just one coverec for the whole tree


      foreach (var (newID, size) in newNodeIDsSizes)
      {
        Vector2 requiredSize = size;


        Dictionary<PNode, float> preservers = new();
        Dictionary<PNode, float> expanders = new();
        tree.FreeLeaves = tree.FindEmpty(tree.Root, tree.Root.Rests);


        var sufficientLargeLeaves = tree.GetSufficientlyLargeLeaves(requiredSize, oldWorstCaseSize);


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

          Debug.Log(expandedCoveRec + " " + coverec);

          if (PTree.FitsInto(expandedCoveRec, coverec))
          {
            float waste = pnode.Rectangle.Size.x * pnode.Rectangle.Size.y - requiredSize.x * requiredSize.y;
            preservers[pnode] = waste;
            Debug.Log("added to preservers");
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
          var minValue = expanders.Values.Min();

          // Filter nodes with that minimum value
          var candidates = expanders
              .Where(kv => kv.Value == minValue);

          // Find the one with the smallest rectangle area
          KeyValuePair<PNode, float>? best = null;

          foreach (var kv in candidates)
          {
            var area = kv.Key.Rectangle.Size.x * kv.Key.Rectangle.Size.y;

            if (best == null)
            {
              best = kv;
            }
            else
            {
              var bestArea = best.Value.Key.Rectangle.Size.x * best.Value.Key.Rectangle.Size.y;

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

            Debug.Log("coverec changed for a new node " + coverec);
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
    //********************************************************************************************************************************
    public void ResizeNodesInPTree1(List<(string, Vector2)> sameIDsNewSizes, ref PTree tree)
    {
      foreach (var (sameID, size) in sameIDsNewSizes)
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
              Debug.Log("coverec changed for a new node -------------------- after resize" + tree.coverec);
            }
            //tree.Print1();
            Debug.Log("--------------------------------------Resized node " + sameID + " to new size " + requiredSize);

          }
          /*
          {
            Vector2 corner = targetPNode.Rectangle.Position + requiredSize;
            Vector2 expandedCoveRec = new(Mathf.Max(tree.coverec.x, corner.x), Mathf.Max(tree.coverec.y, corner.y));
            if (!PTree.FitsInto(expandedCoveRec, tree.coverec))
            {
              tree.coverec = expandedCoveRec;
            }
          }
           */
        }
      }
    }
    //********************************************************************************************************************************
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
    //********************************************************************************************************************************
    private static Vector3 GetScale(ILayoutNode node, float radius)
    {
      return node.IsLeaf ? node.AbsoluteScale
                         : new Vector3(2 * radius, node.AbsoluteScale.y, 2 * radius);
    }

    //********************************************************************************************************************************
    private static float LeafRadius(ILayoutNode block)
    {
      Vector3 extent = block.AbsoluteScale / 2.0f;
      return Mathf.Sqrt(extent.x * extent.x + extent.z * extent.z);
    }
  }
}
