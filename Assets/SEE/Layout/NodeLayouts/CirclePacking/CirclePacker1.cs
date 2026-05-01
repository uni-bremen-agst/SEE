using LibGit2Sharp;
using Markdig.Helpers;
using SEE.Layout.NodeLayouts.RectanglePacking;
using Sirenix.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SEE.Layout.NodeLayouts.CirclePacking
{
  internal class Circle1
  {
    public Vector2 Center;
    public float X
    {
      get => Center.x;
      set => Center.x = value;
    }
    public float Y
    {
      get => Center.y;
      set => Center.y = value;
    }
    public float Radius;
    public ILayoutNode GameObject;
    public List<Circle1> Children;
    public List<(string, Vector2, float)> idRadCenter;
    public string ID;

    public float PrevX { get; set; }
    public float PrevY { get; set; }

    public Circle1(ILayoutNode gameObject, Vector2 center, float radius)
    {
      this.GameObject = gameObject;
      this.Center = center;
      this.Radius = radius;
      Children = new List<Circle1>();
      ID = gameObject != null ? gameObject.ID : null;
      idRadCenter = new List<(string, Vector2, float)>();
      X = center.x;
      Y = center.y;
    }

    public Circle1()
    {
      this.GameObject = null;
      this.Center = Vector2.zero;
      this.Radius = 0f;
      Children = new List<Circle1>();
      ID = null;
      idRadCenter = new List<(string, Vector2, float)>();
      X = 0f;
      Y = 0f;
    }

    public Circle1(Vector2 center, float radius, string id)
    {
      this.GameObject = null;
      this.Center = center;
      this.Radius = radius;
      Children = new List<Circle1>();
      ID = id;
      idRadCenter = new List<(string, Vector2, float)>();
      X = center.x;
      Y = center.y;
    }
    public override string ToString()
    {
      return "(ID=" + ID + " center= " + Center.ToString() + ", radius=" + Radius + ")";
    }
  }

  /// <summary>
  /// This class holds a list of <see cref="Circle"/> objects and packs them closely.
  /// The original source can be found
  /// <see href="https://www.codeproject.com/Articles/42067/D-Circle-Packing-Algorithm-Ported-to-Csharp">HERE</see>.
  /// </summary>
  /// 

  
  
  public static class CirclePacker1
  {
    //                    parentID         placedIDs newSizes        newIDs  newSizes    deletedIDs  deletedSizes  worstCaseSize coverec
    public static List<(string, List<(List<(string, float)>, List<(string, float)>, List<(string, float)>)>)> history;

    /*
     default stuff not needed

    public const float DefaultMinimalSeparation = 0.1f;

    private static readonly float minimalSeparation = DefaultMinimalSeparation;
    //********************************************************************************************************************************
    private static int DescendingRadiusComparator(Circle1 c1, Circle1 c2)
    {
      float r1 = c1.Radius;
      float r2 = c2.Radius;
      if (r1 < r2)
      {
        return 1;
      }
      else if (r1 > r2)
      {
        return -1;
      }
      else
      {
        return 0;
      }
    }

    //********************************************************************************************************************************
    internal static void Pack(float relMinDist, List<Circle1> circles, out float outOuterRadius, ILayoutNode parent = null)
    {
      outOuterRadius = 0.0f;

#if UNITY_EDITOR
      if (relMinDist < 0.0f)
      {
        Debug.LogWarning("Relative min distance is negative and will be treated as zero!");
      }
#endif
      if (relMinDist > 0.0f)
      {
        for (int i = 0; i < circles.Count; i++)
        {
          circles[i].Radius *= 1.0f + relMinDist;
        }
      }

      // Sort circles descendingly based on radius
      circles.Sort(DescendingRadiusComparator);

      Vector2 center = Vector2.zero;
      float lastOutRadius = Mathf.Infinity;
      float minSeparationSq = minimalSeparation * minimalSeparation;
      int maxIterations = circles.Count; // FIXME: What would be a suitable number of maximal iterations? mCircles.Count?
      for (int iterations = 1; iterations <= maxIterations; iterations++)
      {
        // Each step draws all pairs of circles closer together.
        for (int i = 0; i < circles.Count - 1; i++)
        {
          for (int j = i + 1; j < circles.Count; j++)
          {
            if (i == j)
            {
              continue;
            }

            // vector between the two centers
            Vector2 ab = circles[j].Center - circles[i].Center;
            // the minimal distance between the two centers so
            // that the circles don't overlap
            float r = circles[i].Radius + circles[j].Radius;

            // Length squared = (dx * dx) + (dy * dy);
            float d = ab.SqrMagnitude() - minSeparationSq;
            float minSepSq = Math.Min(d, minSeparationSq);
            d -= minSepSq;

            if (d < (r * r) - 0.01)
            {
              ab.Normalize();

              ab *= (float)((r - Math.Sqrt(d)) * 0.5f);

              circles[j].Center += ab;
              circles[i].Center -= ab;
            }
          }
        }
        SmallestEnclosingCircle(circles, out center, out outOuterRadius);

        
        float ratio = outOuterRadius / lastOutRadius;
        if (lastOutRadius != Mathf.Infinity && !(outOuterRadius > lastOutRadius || ratio < 0.99f))
        {
          
          break;
        }
        lastOutRadius = outOuterRadius;
      }

      if (relMinDist > 0.0f)
      {
        for (int i = 0; i < circles.Count; i++)
        {
          circles[i].Radius *= 1.0f / (1.0f + relMinDist);
        }
        SmallestEnclosingCircle(circles, out center, out outOuterRadius);
        outOuterRadius *= 1.0f + relMinDist;
      }
      
      var parentID = parent != null ? parent.ID : "null";
      Debug.Log(parentID + ": " + outOuterRadius + "-----------------------------------------------------------");
      for (int i = 0; i < circles.Count; i++)
      {
        circles[i].Center -= center;
        Debug.Log(circles[i].ToString());
      }
      Debug.Log("++++++++++++++++++++++++++++++++++++++++++");
    }
     */


    //********************************************************************************************************************************

    internal static void PackCircles(List<Circle1> circles, Vector2 containerCenter, out float containerRadius, bool useOldLayout, string parentID)
    {
      if (useOldLayout)
      {
        //                 parentID         placedIDs newSizes        newIDs  newSizes    deletedIDs  deletedSizes  worstCaseSize coverec
        history = new List<(string, List<(List<(string, float)>, List<(string, float)>, List<(string, float)>)>)>();
      }
      //circles.Sort((a, b) => b.Radius.CompareTo(a.Radius));

      Circle1 rootCircle = new Circle1();

      /*
      //case2
      //containerRadius = FindMinimalRadius(circles, ref rootCircle);
       
       */
      /*
       
      //case3
      //containerRadius = AddCirclesIncrementally(new List<Circle1>(), circles, 0f);
       */
      /*
      //case4
      //containerRadius = Pack1(circles, out float bigRadius);
       
       */
      /*
      //case5
      AddToHistory 
      PerformHistory
      return the radius 

      AddToHistory1(circles, parentID);
      PerformHistory1(circles, parentID, containerCenter, out containerRadius, rootCircle);
      for (int i = 0; i < circles.Count; i++)
      {
        Debug.Log(circles[i].ToString());
      }
      Debug.Log("containerRadius " + containerRadius + "containerCenter " + containerCenter + "++++++++++++++++++++++++++++++++++++++++++");
      containerRadius = ComputeSurroundingCircle11ResetCircles(circles).Radius;


       */
      /*
      //case6
      PackCircles1(circles, containerCenter, out containerRadius);
      containerRadius = rootCircle.Radius;
       
       */

      //case7
      AddToHistory(circles, parentID);
      PerformHistory(circles, parentID, containerCenter, out containerRadius, rootCircle);
      /*
       
       */



      var maxCircleDiame = 0f;
      foreach (var circle in circles) 
      {
        if (circle.Radius * 2f > maxCircleDiame)
        {
          maxCircleDiame = circle.Radius * 2f;
        }
      }

      CirclePacker2 packer = new CirclePacker2(maxCircleDiame);
      //packer.GravityStrength = 5.0f; // Move 1 unit per step
      packer.PbdIterations = 10;
      packer.ComputePacking(500, circles);

      for (int i = 0; i < circles.Count; i++)
      {
        Debug.Log(circles[i].ToString());
      }
      Debug.Log("containerRadius " + containerRadius + "containerCenter " + containerCenter + "++++++++++++++++++++++++++++++++++++++++++");
      containerRadius = ComputeSurroundingCircle11ResetCircles(circles).Radius;
      //containerRadius = 1f;

      /*
       */


      /*
       
      FindMinimalRadius(circles, ref rootCircle);

      containerRadius = rootCircle.Radius;
       */
    }

    private static void AddToHistory(List<Circle1> circles, string parent)
    {
      //                    parentID         placedIDs newSizes        newIDs  newSizes       deletedIDs  deletedSizes 
      //public static List<(string, List<(List<(string, float)>, List<(string, float)>, List<(string, float)>)>)> history;

      List<string> newNodeIDs = new();
      List<string> sameNodeIDs = new();
      List<string> deletedNodeIDs = new();
      List<string> oldNodeIDs = new();
      List<string> currentNodeIDs = new();

      List<(string, float)> sameIDsNewSizes = new();
      List<(string, float)> sameIDsOldSizes = new();
      List<(string, float)> newNodeIDsNewSizes = new();
      List<(string, float)> deletedNodeIDsNewSizes = new();
      List<(string, float)> currentNodeIDsNewSizes = new();
      List<(string, float)> oldNodeIDsOldSizes = new();

      //         placedIDs newSizes        newIDs  newSizes       deletedIDs  deletedSizes
      List<(List<(string, float)>, List<(string, float)>, List<(string, float)>)> listOfHistory = new();
      List<float> newSizes = new();
      //parentID          placedIDs newSizes        newIDs  newSizes       deletedIDs  deletedSizes
      (string, List<(List<(string, float)>, List<(string, float)>, List<(string, float)>)>) getLine = new();

      //    placedIDs newSizes        newIDs  newSizes       deletedIDs  deletedSizes
      (List<(string, float)>, List<(string, float)>, List<(string, float)>) lastEvent = new();

      if (history.Any(h => h.Item1 == parent || h.Item1 == "dummy"))
      {
        getLine = history.LastOrDefault(h => h.Item1 == parent || h.Item1 == "dummy");

        lastEvent = getLine.Item2.LastOrDefault();

        oldNodeIDs = lastEvent.Item1.Select(x => x.Item1).Concat(lastEvent.Item2.Select(x => x.Item1)).ToList();
        oldNodeIDsOldSizes = lastEvent.Item1.Concat(lastEvent.Item2).ToList();

        currentNodeIDs = circles.Select(n => n.ID).ToList();

        sameNodeIDs = oldNodeIDs.Intersect(currentNodeIDs).ToList();
        newNodeIDs = currentNodeIDs.Except(oldNodeIDs).ToList();
        deletedNodeIDs = oldNodeIDs.Except(currentNodeIDs).ToList();
        sameIDsOldSizes = lastEvent.Item1.Where(x => sameNodeIDs.Contains(x.Item1)).ToList();

        foreach (Circle1 circle in circles)
        {
          if (circle != null)
          {
            float size = circle.Radius;
            currentNodeIDsNewSizes.Add((circle.ID, size));
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
          List<(string, float)> oldTupples = lastEvent.Item1.Concat(lastEvent.Item2).ToList();
          List<(string, float)> deletedTupple = oldTupples.Where(x => x.Item1 == deletedID).ToList();
          deletedNodeIDsNewSizes.AddRange(deletedTupple);
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
        newNodeIDsNewSizes = circles.Select(n => (n.ID, n.Radius)).ToList();
        listOfHistory.Add((sameIDsNewSizes, newNodeIDsNewSizes, deletedNodeIDsNewSizes));
        if (!history.Any(h => h.Item1 == parent || h.Item1 == "dummy"))
        {
          history.Add((parent, listOfHistory));
        }
        //PrintHistory();
        //Debug.Log("2");
      }

    }
    private static void PerformHistory(List<Circle1> circles, string parent, Vector2 containerCenter, out float containerRadius, Circle1 rootCircle)
    {
      containerRadius = 0f;
      //CirclePacker2 packer = new CirclePacker2(maxCircleDiameter: 0.1f);
      ////packer.GravityStrength = 5.0f; // Move 1 unit per step
      //packer.PbdIterations = 100;
      List<(string, float)> OLDplacedIDsNewSizes = new List<(string, float)> ();
      List<(string, float)> placedIDsNewSizes = new List<(string, float)>();
      List<(string, float)> newNodeIDsSizes = new List<(string, float)>();
      List<(string, float)> deletedNodeIDsSizes = new List<(string, float)>();

      if (history.Any(h => h.Item1 == parent || h.Item1 == "dummy"))
      {
        var getLine = history.LastOrDefault(h => h.Item1 == parent || h.Item1 == "dummy");

        // Iterate through all events in the history for this parent
        for (int i = 0; i < getLine.Item2.Count; i++)
        {
          if (i >= 1)
            OLDplacedIDsNewSizes = getLine.Item2[i - 1].Item1.Concat(getLine.Item2[i - 1].Item2).ToList();
          placedIDsNewSizes = getLine.Item2[i].Item1;
          newNodeIDsSizes = getLine.Item2[i].Item2;
          deletedNodeIDsSizes = getLine.Item2[i].Item3;
          if (placedIDsNewSizes.Count == 0 && deletedNodeIDsSizes.Count == 0)
          {
            var dealingCircles = circles.Where(c => newNodeIDsSizes.Any(n => n.Item1 == c.ID)).ToList();
            foreach (var c in circles)
            {
              var tupple = newNodeIDsSizes.FirstOrDefault(n => n.Item1 == c.ID);
              c.Radius = tupple != default ? tupple.Item2 : c.Radius;
              //var dealingCircles = circles.Where(c => newNodeIDsSizes.Any(n => n.Item1 == c.ID)).ToList();
              //c.Center = Vector2.zero;

              //Debug.Log("Circle " + c.ID + " radius set to " + c.Radius);
            }
            PackCircles1(dealingCircles, containerCenter, out containerRadius, newNodeIDsSizes, rootCircle);
            Debug.Log("3");

            
          }
          else
          {
            var dealingCircles = circles.Where(c => placedIDsNewSizes.Concat(newNodeIDsSizes).Any(n => n.Item1 == c.ID)).ToList();
            foreach (var c in dealingCircles)
            {
              var tupple = placedIDsNewSizes.Concat(newNodeIDsSizes).FirstOrDefault(n => n.Item1 == c.ID);
              c.Radius = tupple != default ? tupple.Item2 : c.Radius;

            }

            // First, handle deleted circles
            foreach (var (deletedID, size) in deletedNodeIDsSizes)
            {
              
              //Debug.Log("Deleted circle with ID " + deletedID + " and size " + size);
            }
            
            if (placedIDsNewSizes.Count > 0 )
            {
              foreach (var (id, size) in placedIDsNewSizes)
              {

                var match = OLDplacedIDsNewSizes.FirstOrDefault(n => n.Item1 == id && n.Item2 != size);
                if (match != default)
                {
                  Debug.Log("Circle with ID " + id + " changed size from " + match.Item2 + " to " + size);
                  //var placedCircles = circles.Where(c => placedIDsNewSizes.Any(n => n.Item1 == c.ID)).ToList();

                  ResizeNodesInCircle(OLDplacedIDsNewSizes, placedIDsNewSizes, dealingCircles.Where(c => placedIDsNewSizes.Any(n => n.Item1 == c.ID)).ToList());
                }
              }

              //foreach(var c in dealingCircles)
              //{
              //  Debug.Log("Dealing circle " + c.ID + " with radius " + c.Radius + " and center " + c.Center);
              //}
              Debug.Log("4");

              /*
              //foreach (var (placedID, placedSize) in placedIDsNewSizes)
              //{
              //  var match = rootCircle.Children.FirstOrDefault(n => n.ID == placedID);
              //  if (match != null)
              //  {
              //    match.Radius = placedSize;
              //  }
              //  RelaxPacking(rootCircle.Children, 25, rootCircle);
              //Debug.Log("Relaxed packing for same size circles with ID " + placedID + " and new size " + placedSize);
              //}
              //ResizeNodesInPTree1(placedIDsNewSizes, ref tree);
              //tree.Tighten(tree.Root);
              //ResetCoverec(ref tree);
               */
            }


            if (newNodeIDsSizes.Count > 0)
            {
              PackCircles1(dealingCircles, containerCenter, out containerRadius, newNodeIDsSizes, rootCircle);
              Debug.Log("5");
            }

          }
        }

      }
    }



    //********************************************************************************************************************************
    /*
     //case2
     //this case works very well 
     // maybe we will needed
     
    private static float FindMinimalRadius(List<Circle1> circles, ref Circle1 rootCircle, List<(string, float)> newNodeIDsSizes = null)
    {
      float low = 0f;
      float high = EstimateUpperBound(circles); // e.g. sum of radii

      float best = high;

      if (newNodeIDsSizes != null)
      {
        // Update circles with new circles to be placed
        var newIDs = newNodeIDsSizes.Select(n => n.Item1).ToHashSet();
        var existingIDs = rootCircle.Children.Select(c => c.ID).ToHashSet();

        var existingCircles = circles.Where(c => existingIDs.Contains(c.ID)).ToList();
        circles = circles.Where(c => newIDs.Contains(c.ID)).ToList();

        //handle the existing circles with same size and ids 
        foreach (var existing in existingCircles)
        {
          var match = rootCircle.Children.FirstOrDefault(n => n.ID == existing.ID);
          if (match != null)
          {
            existing.Radius = match.Radius;
          }
        }


      }
      
      for (int i = 0; i < 20; i++) // precision iterations
      {
        float mid = (low + high) * 0.5f;
        bool success = TryPack(circles, mid, ref rootCircle);
        if (success)
        {
          best = mid;
          high = mid; // try smaller
        }
        else
        {
          low = mid; // need bigger
        }
        if (low == mid && i == 19 && !success)
        {
          Debug.Log("Warning: Binary search reached precision limit at iteration " + i);
          while (!TryPack(circles, high, ref rootCircle))
          {
            high *= 1.5f; // expand until it works
            best = high; 
          } 

        }
      }

      //rootCircle.Radius = best;
      //rootCircle.Children.AddRange(circles);
      return best;
    }

    private static float EstimateUpperBound(List<Circle1> circles)
    {
      float maxRadius = 0f;
      float sum = 0f;

      foreach (var c in circles)
      {
        sum += c.Radius;
        if (c.Radius > maxRadius)
          maxRadius = c.Radius;
      }

      return Mathf.Max(sum * 0.5f, maxRadius * 2f);
    }

    private static bool TryPack(List<Circle1> circles, float bigRadius, ref Circle1 rootCircle)
    {
      // Sort biggest first → critical
      
      List<Circle1> placed = new();

      foreach (var circle in circles)
      {
        bool placedSuccessfully = false;

        float maxR = bigRadius - circle.Radius;

        // OUTSIDE → INSIDE
        for (float r = maxR; r >= 0 && !placedSuccessfully; r -= 0.5f)
        {
          for (float angle = 0; angle < Mathf.PI * 2; angle += 0.15f)
          {
            Vector2 pos = new Vector2(
                Mathf.Cos(angle),
                Mathf.Sin(angle)
            ) * r;

            if (IsValidPosition(pos, circle, placed, bigRadius))
            {
              circle.Center = pos;
              placed.Add(circle);
              placedSuccessfully = true;
              break;
            }
          }
        }

        if (!placedSuccessfully)
          return false; // packing failed for this R
      }

      //rootCircle.Children.AddRange(placed);
      return true;
    }

    private static bool IsValidPosition(Vector2 pos, Circle1 circle, List<Circle1> placed, float bigRadius)
    {
      if (pos.magnitude + circle.Radius > bigRadius)
        return false;

      foreach (var other in placed)
      {
        float dist = Vector2.Distance(pos, other.Center);
        if (dist < circle.Radius + other.Radius)
          return false;
      }

      return true;
    }
     */

    /*
    private static bool CanPlaceNewCircle(List<Circle1> circles, Circle1 newCircle, float bigRadius)
    {
      float maxR = bigRadius - newCircle.Radius;

      for (float r = maxR; r >= 0; r -= 0.25f)
      {
        for (float angle = 0; angle < Mathf.PI * 2; angle += 0.05f)
        {
          Vector2 pos = new Vector2(
              Mathf.Cos(angle),
              Mathf.Sin(angle)
          ) * r;

          bool overlaps = false;

          foreach (var c in circles)
          {
            float d = Vector2.Distance(pos, c.Center);

            if (d < c.Radius + newCircle.Radius)
            {
              overlaps = true;
              break;
            }
          }

          if (!overlaps)
          {
            newCircle.Center = pos;
            return true;
          }
        }
      }

      return false;
    }
     */

    //********************************************************************************************************************************
    #region case5 helper methods
    /*
     case5 maybe needed
     */
    private static void AddToHistory1(List<Circle1> circles, string parent)
    {
      //                    parentID         placedIDs newSizes        newIDs  newSizes       deletedIDs  deletedSizes 
      //public static List<(string, List<(List<(string, float)>, List<(string, float)>, List<(string, float)>)>)> history;

      List<string> newNodeIDs = new();
      List<string> sameNodeIDs = new();
      List<string> deletedNodeIDs = new();
      List<string> oldNodeIDs = new();
      List<string> currentNodeIDs = new();

      List<(string, float)> sameIDsNewSizes = new();
      List<(string, float)> sameIDsOldSizes = new();
      List<(string, float)> newNodeIDsNewSizes = new();
      List<(string, float)> deletedNodeIDsNewSizes = new();
      List<(string, float)> currentNodeIDsNewSizes = new();
      List<(string, float)> oldNodeIDsOldSizes = new();

      //         placedIDs newSizes        newIDs  newSizes       deletedIDs  deletedSizes
      List<(List<(string, float)>, List<(string, float)>, List<(string, float)>)> listOfHistory = new();
      List<float> newSizes = new();
      //parentID          placedIDs newSizes        newIDs  newSizes       deletedIDs  deletedSizes
      (string, List<(List<(string, float)>, List<(string, float)>, List<(string, float)>)>) getLine = new();

      //    placedIDs newSizes        newIDs  newSizes       deletedIDs  deletedSizes
      (List<(string, float)>, List<(string, float)>, List<(string, float)>) lastEvent = new();

      if (history.Any(h => h.Item1 == parent || h.Item1 == "dummy"))
      {
        getLine = history.LastOrDefault(h => h.Item1 == parent || h.Item1 == "dummy");

        lastEvent = getLine.Item2.LastOrDefault();

        oldNodeIDs = lastEvent.Item1.Select(x => x.Item1).Concat(lastEvent.Item2.Select(x => x.Item1)).ToList();
        oldNodeIDsOldSizes = lastEvent.Item1.Concat(lastEvent.Item2).ToList();

        currentNodeIDs = circles.Select(n => n.ID).ToList();

        sameNodeIDs = oldNodeIDs.Intersect(currentNodeIDs).ToList();
        newNodeIDs = currentNodeIDs.Except(oldNodeIDs).ToList();
        deletedNodeIDs = oldNodeIDs.Except(currentNodeIDs).ToList();
        sameIDsOldSizes = lastEvent.Item1.Where(x => sameNodeIDs.Contains(x.Item1)).ToList();

        foreach (Circle1 circle in circles)
        {
          if (circle != null)
          {
            float size = circle.Radius;
            currentNodeIDsNewSizes.Add((circle.ID, size));
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
          List<(string, float)> oldTupples = lastEvent.Item1.Concat(lastEvent.Item2).ToList();
          List<(string, float)> deletedTupple = oldTupples.Where(x => x.Item1 == deletedID).ToList();
          deletedNodeIDsNewSizes.AddRange(deletedTupple);
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
        newNodeIDsNewSizes = circles.Select(n => (n.ID, n.Radius)).ToList();
        listOfHistory.Add((sameIDsNewSizes, newNodeIDsNewSizes, deletedNodeIDsNewSizes));
        if (!history.Any(h => h.Item1 == parent || h.Item1 == "dummy"))
        {
          history.Add((parent, listOfHistory));
        }
        //PrintHistory();
        //Debug.Log("2");
      }

    }
    private static void PerformHistory1(List<Circle1> circles, string parent, Vector2 containerCenter, out float containerRadius, Circle1 rootCircle)
    {
      containerRadius = 0f;
      List<(string, float)> placedIDsNewSizes = new List<(string, float)>();
      List < (string, float) > OLDplacedIDsNewSizes = new List<(string, float)>();
      if (history.Any(h => h.Item1 == parent || h.Item1 == "dummy"))
      {
        var getLine = history.LastOrDefault(h => h.Item1 == parent || h.Item1 == "dummy");

        // Iterate through all events in the history for this parent
        foreach (var historyEvent in getLine.Item2)
        {
          placedIDsNewSizes = historyEvent.Item1;
          List<(string, float)> newNodeIDsSizes = historyEvent.Item2;
          List<(string, float)> deletedNodeIDsSizes = historyEvent.Item3;
          if (placedIDsNewSizes.Count == 0 && deletedNodeIDsSizes.Count == 0)
          {
            foreach(var c in circles)
            {
              var tupple = newNodeIDsSizes.FirstOrDefault(n => n.Item1 == c.ID);
              c.Radius = tupple != default ? tupple.Item2 : c.Radius;
              //Debug.Log("Circle " + c.ID + " radius set to " + c.Radius);
            }
            Debug.Log("3");
            PackCircles1(circles, containerCenter, out containerRadius, newNodeIDsSizes, rootCircle);
            //foreach (var c in rootCircle.c)
            //{
            //  Debug.Log("c in rootCircle: " + c.Item1 + " radius " + c.Item3);
            //}
          }
          else
          {
            // First, handle deleted circles
            foreach (var (deletedID, size) in deletedNodeIDsSizes)
            {
              //rootCircle.Children.RemoveAll(c => c.ID == deletedID);
              //tree.DeleteMergeRemainLeaves2(id: deletedID);
              //tree.Tighten(tree.Root);
              //ResetCoverec(ref tree);
              Debug.Log("Deleted circle with ID " + deletedID + " and size " + size);
            }
            // Second, handle resized circles that are the same
            // set ptree to same circles with new size


            if (placedIDsNewSizes.Count > 0)
            {
              //foreach(var c in rootCircle.Children)
              //{
              //  Debug.Log("children in rootCircle: " + c.ID + " radius " + c.Radius);
              //}

              Debug.Log("4");

              ResizeNodesInCircle(OLDplacedIDsNewSizes, placedIDsNewSizes, circles);

              /*
              //foreach (var (placedID, placedSize) in placedIDsNewSizes)
              //{
              //  var match = rootCircle.Children.FirstOrDefault(n => n.ID == placedID);
              //  if (match != null)
              //  {
              //    match.Radius = placedSize;
              //  }
              //  RelaxPacking(rootCircle.Children, 25, rootCircle);
              //Debug.Log("Relaxed packing for same size circles with ID " + placedID + " and new size " + placedSize);
              //}
              //ResizeNodesInPTree1(placedIDsNewSizes, ref tree);
              //tree.Tighten(tree.Root);
              //ResetCoverec(ref tree);
               */
            }




            // Next, handle new circles
            //foreach (var c in circles)
            //{
            //  var tupple = newNodeIDsSizes.Concat(placedIDsNewSizes).FirstOrDefault(n => n.Item1 == c.ID);
            //  c.Radius = tupple != default ? tupple.Item2 : c.Radius;
            //  Debug.Log("Circle " + c.ID + " radius set to " + c.Radius);
            //}
            Debug.Log("5");

            PackCircles1(circles, containerCenter, out containerRadius, newNodeIDsSizes, rootCircle);
            //foreach (var c in rootCircle.c)
            //{
            //  Debug.Log("c in rootCircle: " + c.Item1 + " radius " + c.Item3);
            //}
            // Finally, update sizes of same circles
          }
        }

        OLDplacedIDsNewSizes = placedIDsNewSizes;
      }
    }

    private static void PrintHistory()
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

    private static void ResizeNodesInCircle(List<(string, float)> OLDplacedIDsNewSizes, List<(string, float)> placedIDsNewSizes, List<Circle1> circles)
    {
      //Debug.Log("Resizing nodes in circle for same IDs with new sizes:");
      bool flag = false;
      foreach (var (placedID, placedSize) in placedIDsNewSizes)
      {
        var match = OLDplacedIDsNewSizes.FirstOrDefault(n => n.Item1 == placedID && n.Item2 != placedSize);
        //var match2 = circles.FirstOrDefault(c => c.ID == placedID);
        if (match != default)
        {
          Debug.Log("Found circle with ID " + placedID + " that changed size from " + match.Item2 + " to " + placedSize);
          var circleMatch = circles.FirstOrDefault(c => c.ID == placedID);
          if (circleMatch == default) continue;
          if (match.Item2 < circleMatch.Radius)
          {
            Debug.Log("No size change for circle with ID " + placedID + " and size " + placedSize + " == " + match.Item2);
            circleMatch.Radius = match.Item2;
          }
          else if (match.Item2 > circleMatch.Radius)
          {
            return;
            //Debug.Log("Circle with ID " + placedID + " changed size from " + match.Item2 + " to " + placedSize);
            //circleMatch.Radius = match.Item2;
          }
          if (circleMatch.Radius == placedSize)
          {
            //Debug.Log("No size change for circle with ID " + placedID + " and size " + placedSize + " == " + circleMatch.Radius);
            continue;
          }
          else if (circleMatch.Radius < placedSize)
          {
            //match.Radius = placedSize;
            ExpandFromCircleA(circles, circleMatch, placedSize);
            //flag = true;
            Debug.Log("Enlarged circle with ID " + placedID + " to new size " + placedSize);
          }
          else
          {
            //match.Radius = placedSize;
            //flag = true;
            Debug.Log("Shrunk circle with ID " + placedID + " to new size " + placedSize);
          }
          //if (flag)
          //{
          //  flag = false;
          //  ExpandFromCircleA(circles, match, placedSize);
          //}
        }
      }
    }
    
    internal static void ExpandFromCircleA(List<Circle1> circles, Circle1 A, float newRadius)
    {
      float oldRadius = A.Radius;
      float rem = newRadius - oldRadius;

      // Update A first
      A.Radius = newRadius;

      // If no growth, nothing to do
      if (rem <= 0f)
        return;

      Vector2 centerA = A.Center;

      foreach (var c in circles)
      {
        if (c == A)
          continue;

        Vector2 dir = c.Center - centerA;
        float dist = dir.magnitude;

        Debug.Log("Expanding from circle " + A.ID + " to circle " + c.ID + " with rem " + rem + " and dist " + dist + " " + dir);

        // If exactly at center, choose fixed direction (deterministic)
        if (dist == 0f)
        {
          dir = new Vector2(1f, 0f);
        }
        else
        {
          dir /= dist; // normalize
        }

        // Move outward by rem
        c.Center += dir * rem;
      }
    }

    internal static void RelaxPacking(List<Circle1> circles, int iterations = 20, Circle1 rootCircle = null)
    {
      if (circles == null || circles.Count == 0)
      {
        Debug.LogWarning("RelaxPacking called with empty circle list.");
        return;
      }

      Circle1 bigCircle = ComputeSurroundingCircle11(circles);
      //Circle1 bigCircle = rootCircle;


      for (int iter = 0; iter < iterations; iter++)
      {
        // --- 1. Resolve overlaps (weighted push)
        for (int i = 0; i < circles.Count; i++)
        {
          for (int j = i + 1; j < circles.Count; j++)
          {
            var a = circles[i];
            var b = circles[j];

            Vector2 delta = b.Center - a.Center;
            float dist = delta.magnitude;

            if (dist == 0f)
              delta = new Vector2(0.001f, 0f);

            float minDist = a.Radius + b.Radius;

            if (dist < minDist)
            {
              Vector2 dir = delta.normalized;

              float overlap = minDist - dist;

              // Weighted push (better stability)
              float total = a.Radius + b.Radius;

              float pushA = (b.Radius / total) * overlap;
              float pushB = (a.Radius / total) * overlap;

              a.Center -= dir * pushA;
              b.Center += dir * pushB;
            }
          }
        }

        // --- 2. Recompute enclosing circle
        bigCircle = ComputeSurroundingCircle11(circles);

        // --- 3. Keep circles inside container
        foreach (var c in circles)
        {
          Vector2 dir = c.Center - bigCircle.Center;
          float dist = dir.magnitude;

          float maxDist = bigCircle.Radius - c.Radius;

          if (dist > maxDist)
          {
            if (dist > 0f)
              c.Center = bigCircle.Center + dir.normalized * maxDist;
            else
              c.Center = bigCircle.Center;
          }
        }

        // --- 4. Recompute again after clamping
        bigCircle = ComputeSurroundingCircle11(circles);
      }

      // --- 5. Recenter everything to (0,0)
      Vector2 offset = bigCircle.Center;

      foreach (var c in circles)
        c.Center -= offset;

      bigCircle.Center = Vector2.zero;

      //rootCircle.Center = bigCircle.Center;
      //rootCircle.Radius = bigCircle.Radius;
      //rootCircle.Children = circles;

    }

    internal static void PackCircles1(List<Circle1> circles, Vector2 containerCenter, out float containerRadius, List<(string, float)> newNodeIDsSizes = null, Circle1 rootCircle = null)
    {
      List<Circle1> placed = new List<Circle1>();
      containerRadius = 0f;

      var newIDs = newNodeIDsSizes.Select(n => n.Item1).ToList();

      var iDs = circles.Select(c => c.ID).ToHashSet();
      var existingCircles = circles.Where(c => !newIDs.Contains(c.ID)).ToList();


      if (newNodeIDsSizes != null)
      {
        
        if (existingCircles.Count > 0)
          placed.AddRange(existingCircles);
        /*
        //handle the existing circles with same size and ids 
        
        //string newIDsStr = "";
        ////Debug.Log("New IDs to place: " + newIDsStr);
        //foreach (var (id, size) in newNodeIDsSizes)
        //{
        //  newIDsStr += id + " ";
        //}
        //Debug.Log("newIDs: " + newIDsStr);
         */
        /*
        //Debug.Log("........$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$" );
        //Debug.Log("Existing circles in rootCircle: " + rootCircle.Children.Count);
         */
        /*
        //Debug.Log("Matched existing circle with ID " + existing.ID + " to new circle with ID " + match.ID + " and radius " + match.Radius + " and center " + match.Center);
        //existing.Radius = match.Radius;
        //existing.Center = match.Center;//fix this shit center is not saved in the history. but only id and radius
         */
        /*
        //Debug.Log("$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$........");
         */
        
        /*
        if (rootCircle.Children.Count > 0)
        {
          foreach (var existing in rootCircle.Children)
          {
            var match = circles.FirstOrDefault(n => n.ID == existing.ID);
            if (match != null)
            {
              match.Radius = existing.Radius;
              match.Center = existing.Center;
              placed.Add(match);
            }
          }
          //placed.AddRange(rootCircle.Children);
        }
         */

      }
      
      circles = circles.Where(c => newIDs.Contains(c.ID)).ToList();

      foreach (var circle in circles)
      {
        Vector2 pos = FindEmptyPlace12(
            placed,
            circle,
            containerCenter
        );

        circle.Center = pos;
        placed.Add(circle);

        // update container radius
        //float dist = Vector2.Distance(containerCenter, pos) + circle.Radius;
        //if (dist > containerRadius)
        //  containerRadius = dist;
      }
      //rootCircle.Children.Clear();

      //foreach(var c in circles)
      //{
      //  rootCircle.c.Add((c.ID, c.Center, c.Radius));
      //}
      //rootCircle.Children.AddRange(circles);

    }
    private static Vector2 FindEmptyPlace12(List<Circle1> placedCircles, Circle1 circle, Vector2 containerCenter)
    {
      List<Vector2> candidates = new List<Vector2>();

      // 1. Try center
      candidates.Add(containerCenter);

      // 2. Generate candidates around existing circles
      foreach (var c in placedCircles)
      {
        float dist = c.Radius + circle.Radius;

        int steps = 20;
        for (int i = 0; i < steps; i++)
        {
          float angle = (i / (float)steps) * Mathf.PI * 2f;
          Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

          candidates.Add(c.Center + dir * dist);
        }
      }

      // 3. Try normal candidates
      Vector2 bestPos = Vector2.zero;
      float bestScore = float.MaxValue;
      bool found = false;

      foreach (var pos in candidates)
      {
        if (IsOverlapping(pos, circle.Radius, placedCircles))
          continue;

        
        circle.Center = pos; // temporarily set for scoring
        float score = ComputeSurroundingCircle11(placedCircles.Concat(new[] { circle }).ToList()).Radius;
        //float score = (pos - containerCenter).sqrMagnitude;

        if (score < bestScore)
        {
          bestScore = score;
          bestPos = pos;
          found = true;
        }
      }

      if (found)
        return bestPos;

      // 4. Deterministic fallback (expanding rings)
      float stepSize = circle.Radius * 0.5f;
      int ringSteps = 24;

      for (int ring = 1; ring < 100; ring++)
      {
        float dist = ring * stepSize;

        for (int i = 0; i < ringSteps; i++)
        {
          float angle = (i / (float)ringSteps) * Mathf.PI * 2f;
          Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

          Vector2 pos = containerCenter + dir * dist;

          if (!IsOverlapping(pos, circle.Radius, placedCircles))
          {
            Debug.Log("found deterministic fallback");
            return pos;

          }
        }
      }

      Debug.LogError("No valid position found even after expanding!");
      return containerCenter; // fallback fallback
    }
    private static bool IsOverlapping(Vector2 pos, float radius, List<Circle1> placedCircles)
    {
      foreach (var c in placedCircles)
      {
        float dist = Vector2.Distance(pos, c.Center);
        if (dist < (radius + c.Radius))
          return true;
      }
      return false;
    }
    internal static Circle1 ComputeSurroundingCircle11(List<Circle1> circles)
    {
      if (circles.Count == 0)
        return new Circle1(null, Vector2.zero, 0);

      if (circles.Count == 1)
        return new Circle1(
            null,
            circles[0].Center,
            circles[0].Radius
        );

      Circle1 best = null;
      float largestRadius = 0f;

      // Check every pair
      for (int i = 0; i < circles.Count; i++)
      {
        for (int j = i + 1; j < circles.Count; j++)
        {
          Circle1 a = circles[i];
          Circle1 b = circles[j];

          float d = Vector2.Distance(a.Center, b.Center);

          // one circle fully contains the other
          if (d + Mathf.Min(a.Radius, b.Radius) <= Mathf.Max(a.Radius, b.Radius))
          {
            Circle1 larger =
                a.Radius > b.Radius ? a : b;

            if (larger.Radius > largestRadius)
            {
              largestRadius = larger.Radius;
              best = new Circle1(
                  null,
                  larger.Center,
                  larger.Radius
              );
            }

            continue;
          }

          float R = (d + a.Radius + b.Radius) / 2f;

          Vector2 dir = (b.Center - a.Center).normalized;

          Vector2 center =
              a.Center +
              dir * (R - a.Radius);

          if (R > largestRadius)
          {
            largestRadius = R;
            best = new Circle1(
                null,
                center,
                R
            );
          }
        }
      }

      return best;
    }
    internal static Circle1 ComputeSurroundingCircle11ResetCircles(List<Circle1> circles)
    {
      if (circles.Count == 0)
        return new Circle1(null, Vector2.zero, 0);

      if (circles.Count == 1)
        return new Circle1(
            null,
            circles[0].Center,
            circles[0].Radius
        );

      Circle1 best = null;
      float largestRadius = 0f;

      // Check every pair
      for (int i = 0; i < circles.Count; i++)
      {
        for (int j = i + 1; j < circles.Count; j++)
        {
          Circle1 a = circles[i];
          Circle1 b = circles[j];

          float d = Vector2.Distance(a.Center, b.Center);

          // one circle fully contains the other
          if (d + Mathf.Min(a.Radius, b.Radius) <= Mathf.Max(a.Radius, b.Radius))
          {
            Circle1 larger =
                a.Radius > b.Radius ? a : b;

            if (larger.Radius > largestRadius)
            {
              largestRadius = larger.Radius;
              best = new Circle1(
                  null,
                  larger.Center,
                  larger.Radius
              );
            }

            continue;
          }

          float R = (d + a.Radius + b.Radius) / 2f;

          Vector2 dir = (b.Center - a.Center).normalized;

          Vector2 center =
              a.Center +
              dir * (R - a.Radius);

          if (R > largestRadius)
          {
            largestRadius = R;
            best = new Circle1(
                null,
                center,
                R
            );
          }
        }
      }

      if (best != null)
      {
        Vector2 offset = best.Center;

        foreach (var c in circles)
        {
          c.Center -= offset;
        }

        best.Center = Vector2.zero;
      }

      return best;
    }
    #endregion
    //********************************************************************************************************************************
    #region not needed helper methods
    /*
    private static void PackCircles(List<Circle1> circles)
    {
      float angle = 0f;
      float step = 0.5f;

      circles[0].Center = Vector2.zero;

      for (int i = 1; i < circles.Count; i++)
      {
        float radius = 0f;

        while (true)
        {
          Vector2 pos = new Vector2(
              Mathf.Cos(angle),
              Mathf.Sin(angle)
          ) * radius;

          circles[i].Center = pos;

          bool overlaps = false;
          for (int j = 0; j < i; j++)
          {
            if (Overlaps(circles[i], circles[j]))
            {
              overlaps = true;
              break;
            }
          }

          if (!overlaps)
            break;

          angle += 0.1f;
          radius += step * 0.1f;
        }
      }
    }

    private static float ComputeBoundingRadius(List<Circle1> circles)
    {
      float max = 0f;
      foreach (var c in circles)
      {
        float dist = c.Center.magnitude + c.Radius;
        if (dist > max)
          max = dist;
      }
      return max;
    }

    private static bool Overlaps(Circle1 a, Circle1 b)
    {
      float dist = Vector2.Distance(a.Center, b.Center);
      return dist < (a.Radius + b.Radius);
    }
     */

    /*
     */
    /*
     not needed
    private static float ComputeContainerRadius1(List<Circle1> circles)
    {
      float maxRadius = 0f;

      for (int i = 0; i < circles.Count; i++)
      {
        for (int j = i + 1; j < circles.Count; j++)
        {
          var a = circles[i];
          var b = circles[j];

          float dist = Vector2.Distance(a.Center, b.Center);

          float requiredRadius = (dist + a.Radius + b.Radius) * 0.5f;

          if (requiredRadius > maxRadius)
            maxRadius = requiredRadius;
        }
      }

      // Also handle single circle case
      foreach (var c in circles)
      {
        if (c.Radius > maxRadius)
          maxRadius = c.Radius;
      }

      return maxRadius;
    }

    private static void ComputeBoundingCircle11(List<Circle1> circles, out Vector2 center, out float radius)
    {
      // Start with average center
      center = Vector2.zero;
      foreach (var c in circles)
        center += c.Center;

      center /= circles.Count;

      radius = 0f; //  FIX: initialize radius

      for (int iter = 0; iter < 20; iter++)
      {
        float maxDist = 0f;
        Circle1 farthest = default;

        foreach (var c in circles)
        {
          float dist = Vector2.Distance(center, c.Center) + c.Radius;

          if (dist > maxDist)
          {
            maxDist = dist;
            farthest = c;
          }
        }

        // Move center toward the worst-fitting circle
        Vector2 dir = (farthest.Center - center).normalized;
        center += dir * (maxDist - radius) * 0.5f;

        radius = maxDist;
      }
    }
     */

    //********************************************************************************************************************************
    /*
     not needed 
    private static Vector2 FindEmptyPlace(List<Circle1> placedCircles, float radius, Vector2 containerCenter)
    {
      // 1. First circle
      if (placedCircles.Count == 0)
        return containerCenter;

      List<Vector2> candidates = new List<Vector2>();

      // 2. Tangent to ONE circle (like before)
      foreach (var c in placedCircles)
      {
        float dist = c.Radius + radius;

        int steps = 16;
        for (int i = 0; i < steps; i++)
        {
          float angle = (i / (float)steps) * Mathf.PI * 2f;
          Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

          candidates.Add(c.Center + dir * dist);
        }
      }

      //  3. Tangent to TWO circles (IMPORTANT)
      for (int i = 0; i < placedCircles.Count; i++)
      {
        for (int j = i + 1; j < placedCircles.Count; j++)
        {
          var a = placedCircles[i];
          var b = placedCircles[j];

          TryAddTangentPositions(a, b, radius, candidates);
        }
      }

      // 4. Pick best valid position (closest to center)
      Vector2 bestPos = Vector2.zero;
      float bestScore = float.MaxValue;
      bool found = false;

      foreach (var pos in candidates)
      {
        if (IsOverlapping(pos, radius, placedCircles))
          continue;

        float score = pos.sqrMagnitude; // tighter cluster

        if (score < bestScore)
        {
          bestScore = score;
          bestPos = pos;
          found = true;
        }
      }

      if (found)
        return bestPos;

      // NO random, NO expanding
      Debug.LogError("No valid position found (should be rare)");
      return placedCircles[0].Center; // fallback fallback
    }

    private static void TryAddTangentPositions(Circle1 a, Circle1 b, float radius, List<Vector2> results)
    {
      float r1 = a.Radius + radius;
      float r2 = b.Radius + radius;

      Vector2 p1 = a.Center;
      Vector2 p2 = b.Center;

      float d = Vector2.Distance(p1, p2);

      if (d <= 0.0001f)
        return;

      // No solution if circles too far apart
      if (d > r1 + r2)
        return;

      // Law of cosines
      float x = (d * d - r2 * r2 + r1 * r1) / (2f * d);
      float ySq = r1 * r1 - x * x;

      if (ySq < 0f)
        return;

      float y = Mathf.Sqrt(ySq);

      Vector2 dir = (p2 - p1).normalized;
      Vector2 perp = new Vector2(-dir.y, dir.x);

      Vector2 basePoint = p1 + dir * x;

      // Two possible solutions
      results.Add(basePoint + perp * y);
      results.Add(basePoint - perp * y);
    }

    //********************************************************************************************************************************
    private static Vector2 FindEmptyPlace1(List<Circle1> placedCircles, float radius, Vector2 containerCenter)
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

          candidates.Add(c.Center + dir * dist);
        }
      }

      // 3. Try normal candidates
      Vector2 bestPos = Vector2.zero;
      float bestScore = float.MaxValue;
      bool found = false;

      foreach (var pos in candidates)
      {
        if (IsOverlapping(pos, radius, placedCircles))
          continue;

        ////////////
        
        ////////////

        float score = (pos - containerCenter).sqrMagnitude;

        if (score < bestScore)
        {
          bestScore = score;
          bestPos = pos;
          found = true;
        }
      }

      if (found)
        return bestPos;

      // 4. Deterministic fallback (expanding rings)
      float stepSize = radius * 0.5f;
      int ringSteps = 24;

      for (int ring = 1; ring < 100; ring++)
      {
        float dist = ring * stepSize;

        for (int i = 0; i < ringSteps; i++)
        {
          float angle = (i / (float)ringSteps) * Mathf.PI * 2f;
          Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

          Vector2 pos = containerCenter + dir * dist;

          if (!IsOverlapping(pos, radius, placedCircles))
          {
            Debug.Log("found deterministic fallback");
            return pos;

          }
        }
      }

      Debug.LogError("No valid position found even after expanding!");
      return containerCenter; // fallback fallback
    }
     */


    //********************************************************************************************************************************
    /*
    //correct
    internal static Circle1 ComputeSurroundingCircle(List<Circle1> circles)
    {
      // Start from average center
      Vector2 center = Vector2.zero;

      foreach (var c in circles)
        center += c.Center;

      center /= circles.Count;

      // Hill-climb / shrink search
      float step = 10f;

      for (int iter = 0; iter < 200; iter++)
      {
        float best = MaxDistance(center, circles);
        bool improved = false;

        Vector2[] dirs =
        {
            Vector2.right,
            Vector2.left,
            Vector2.up,
            Vector2.down
        };

        foreach (var d in dirs)
        {
          Vector2 test = center + d * step;

          float val = MaxDistance(test, circles);

          if (val < best)
          {
            best = val;
            center = test;
            improved = true;
          }
        }

        if (!improved)
          step *= 0.5f;

        if (step < 0.0001f)
          break;
      }

      float radius = MaxDistance(center, circles);

      return new Circle1(null, center, radius);
    }

    private static float MaxDistance(Vector2 center, List<Circle1> circles)
    {
      float max = 0f;

      foreach (var c in circles)
      {
        float d =
            Vector2.Distance(center, c.Center)
            + c.Radius;

        if (d > max)
          max = d;
      }

      return max;
    }
     */

    //********************************************************************************************************************************
    /*
    // compute a true circle around the given circles
    // same results as ComputeContainerRadius1 
    //this is correct
    internal static Circle1 ComputeSurroundingCircle1(List<Circle1> circles)
    {
      if (circles.Count == 0)
        return new Circle1(null, Vector2.zero, 0);

      Circle1 best = null;
      float bestRadius = float.MaxValue;

      // --- Single circle candidates
      foreach (var c in circles)
      {
        if (ContainsAll(c, circles))
        {
          if (c.Radius < bestRadius)
          {
            best = new Circle1(null, c.Center, c.Radius);
            bestRadius = c.Radius;
          }
        }
      }

      // --- Pair candidates
      for (int i = 0; i < circles.Count; i++)
      {
        for (int j = i + 1; j < circles.Count; j++)
        {
          Circle1 candidate =
              CircleFromTwoCircles(circles[i], circles[j]);

          if (ContainsAll(candidate, circles))
          {
            if (candidate.Radius < bestRadius)
            {
              best = candidate;
              bestRadius = candidate.Radius;
            }
          }
        }
      }

      // --- Triple candidates
      for (int i = 0; i < circles.Count; i++)
      {
        for (int j = i + 1; j < circles.Count; j++)
        {
          for (int k = j + 1; k < circles.Count; k++)
          {
            Circle1 candidate =
                CircleFromThreeCenters(
                    circles[i],
                    circles[j],
                    circles[k]);

            if (candidate == null)
              continue;

            if (ContainsAll(candidate, circles))
            {
              if (candidate.Radius < bestRadius)
              {
                best = candidate;
                bestRadius = candidate.Radius;
              }
            }
          }
        }
      }

      return best;
    }

    private static bool ContainsAll(Circle1 outer, List<Circle1> circles)
    {
      foreach (var c in circles)
      {
        float d =
            Vector2.Distance(
                outer.Center,
                c.Center);

        if (d + c.Radius > outer.Radius)
          return false;
      }

      return true;
    }

    private static Circle1 CircleFromTwoCircles(Circle1 a, Circle1 b)
    {
      float d = Vector2.Distance(a.Center, b.Center);

      if (d + Mathf.Min(a.Radius, b.Radius)
          <= Mathf.Max(a.Radius, b.Radius))
      {
        return
            a.Radius > b.Radius ? a : b;
      }

      float R =
          (d + a.Radius + b.Radius) / 2f;

      Vector2 dir =
          (b.Center - a.Center).normalized;

      Vector2 center =
          a.Center +
          dir * (R - a.Radius);

      return new Circle1(null, center, R);
    }

    private static Circle1 CircleFromThreeCenters(Circle1 a, Circle1 b, Circle1 c)
    {
      Vector2 A = a.Center;
      Vector2 B = b.Center;
      Vector2 C = c.Center;

      float d =
          2 * (
          A.x * (B.y - C.y) +
          B.x * (C.y - A.y) +
          C.x * (A.y - B.y));

      if (Mathf.Abs(d) < 0.0001f)
        return null;

      float ux =
        (
        (A.sqrMagnitude) * (B.y - C.y) +
        (B.sqrMagnitude) * (C.y - A.y) +
        (C.sqrMagnitude) * (A.y - B.y)
        ) / d;

      float uy =
        (
        (A.sqrMagnitude) * (C.x - B.x) +
        (B.sqrMagnitude) * (A.x - C.x) +
        (C.sqrMagnitude) * (B.x - A.x)
        ) / d;

      Vector2 center =
          new Vector2(ux, uy);

      float radius = 0f;

      radius = Mathf.Max(
          radius,
          Vector2.Distance(center, A) + a.Radius);

      radius = Mathf.Max(
          radius,
          Vector2.Distance(center, B) + b.Radius);

      radius = Mathf.Max(
          radius,
          Vector2.Distance(center, C) + c.Radius);

      return new Circle1(null, center, radius);
    }
     */

    //********************************************************************************************************************************
    // old version of ComputeSurroundingCircle works autonom

    //********************************************************************************************************************************
    /*
     not needed
    private static Vector2 FindEmptyPlaces11(List<Circle1> placedCircles, float radius, Vector2 containerCenter)
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
        if (IsOverlapping(pos, radius, placedCircles))
          continue;

        float score = (pos - containerCenter).sqrMagnitude;

        if (score < bestScore)
        {
          bestScore = score;
          bestPos = pos;
          found = true;
        }
      }

      // Fallback
      if (!found)
      {
        Debug.Log("Fallback ...");


        for (int i = 0; i < 100; i++)
        {
          Vector2 randomPos = containerCenter + UnityEngine.Random.insideUnitCircle * 1.1f;

          if (!IsOverlapping(randomPos, radius, placedCircles))
            return randomPos;
        }

        Debug.LogWarning("No valid position found!");
      }


      return bestPos;
    }

    private static Vector2 FindEmptyPlaces112(List<Circle1> placedCircles, float radius, Vector2 containerCenter)
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
        if (IsOverlapping(pos, radius, placedCircles))
          continue;

        float score = (pos - containerCenter).sqrMagnitude;

        if (score < bestScore)
        {
          bestScore = score;
          bestPos = pos;
          found = true;
        }
      }

      // Fallback
      if (!found)
      {
        Debug.Log("Fallback ...");


        for (int i = 0; i < 100; i++)
        {
          Vector2 randomPos = containerCenter + UnityEngine.Random.insideUnitCircle * 1.1f;

          if (!IsOverlapping(randomPos, radius, placedCircles))
            return randomPos;
        }

        Debug.LogWarning("No valid position found!");
      }



      return bestPos;
    }
     */
    //********************************************************************************************************************************
    //********************************************************************************************************************************

    //********************************************************************************************************************************
    /*
    internal static void PackCircles1(List<Circle1> circles, Vector2 containerCenter, float containerRadius)
    {
      List<Circle1> placed = new List<Circle1>();

      foreach (var circle in circles)
      {
        Vector2 pos = FindEmptyPlace(
            placed,
            circle.Radius,
            containerCenter,
            containerRadius
        );

        circle.Center = pos;
        placed.Add(circle);
      }
    }

    private static Vector2 FindEmptyPlace(List<Circle1> placedCircles, float radius, Vector2 containerCenter, float containerRadius)
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
        if (!IsInsideContainer(pos, radius, containerCenter, containerRadius))
          continue;

        if (IsOverlapping(pos, radius, placedCircles))
          continue;

        float score = (pos - containerCenter).sqrMagnitude;

        if (score < bestScore)
        {
          bestScore = score;
          bestPos = pos;
          found = true;
        }
      }

      // Fallback
      if (!found)
      {
        for (int i = 0; i < 100; i++)
        {
          Vector2 randomPos = containerCenter + UnityEngine.Random.insideUnitCircle * (containerRadius - radius);

          if (!IsOverlapping(randomPos, radius, placedCircles))
            return randomPos;
        }

        Debug.LogWarning("No valid position found!");
      }

      return bestPos;
    }

    private static bool IsInsideContainer(Vector2 pos, float radius, Vector2 containerCenter, float containerRadius)
    {
      return Vector2.Distance(pos, containerCenter) + radius <= containerRadius;
    }

    private static bool IsOverlapping(Vector2 pos, float radius, List<Circle1> placedCircles)
    {
      foreach (var c in placedCircles)
      {
        float dist = Vector2.Distance(pos, c.Center);
        if (dist < (radius + c.Radius))
          return true;
      }
      return false;
    }
     */

    //********************************************************************************************************************************
    /*
     not needed
    private static void SmallestEnclosingCircle(List<Circle1> circles, out Vector2 outCenter, out float outRadius)
    {
      SmallestEnclosingCircleImpl(new List<Circle1>(circles), new List<Circle1>(), out Vector2 center, out float radius);
      outCenter = center;
      outRadius = radius;
    }

    //********************************************************************************************************************************
    private static void SmallestEnclosingCircleImpl(List<Circle1> circles, List<Circle1> borderCircles, out Vector2 outCenter, out float outRadius)
    {
      outCenter = Vector2.zero;
      outRadius = 0.0f;

      if (circles.Count == 0 || borderCircles.Count > 0 && borderCircles.Count > 3)
      {
        switch (borderCircles.Count)
        {
          case 1:
            {
              outCenter = borderCircles[0].Center;
              outRadius = borderCircles[0].Radius;
              break;
            }
          case 2:
            {
              CircleIntersectingTwoCircles(borderCircles[0], borderCircles[1], out Vector2 outCenterTrivial, out float outRadiusTrivial);
              outCenter = outCenterTrivial;
              outRadius = outRadiusTrivial;
              break;
            }
          case 3:
            {
              CircleIntersectingThreeCircles(borderCircles[0], borderCircles[1], borderCircles[2], out Vector2 outCenterTrivial, out float outRadiusTrivial);
              outCenter = outCenterTrivial;
              outRadius = outRadiusTrivial;
              break;
            }
        }
        if (circles.Count == 0)
        {
          return;
        }
      }

      // This is the smallest circle, if circles are sorted by descending radius
      int smallestCircleIndex = circles.Count - 1;
      Circle1 smallestCircle = circles[smallestCircleIndex];

      List<Circle1> cmc = new List<Circle1>(circles);
      cmc.RemoveAt(smallestCircleIndex);

      SmallestEnclosingCircleImpl(cmc, borderCircles, out Vector2 outCenterCmc, out float outRadiusCmc);

      if (!CircleContainsCircle(outCenterCmc, outRadiusCmc, smallestCircle))
      {
        List<Circle1> bcpc = new List<Circle1>(borderCircles);
        bcpc.Add(smallestCircle);

        SmallestEnclosingCircleImpl(cmc, bcpc, out Vector2 outCenterCmcBcpc, out float outRadiusCmcBcpc);

        outCenter = outCenterCmcBcpc;
        outRadius = outRadiusCmcBcpc;
      }
      else
      {
        outCenter = outCenterCmc;
        outRadius = outRadiusCmc;
      }
    }

    //********************************************************************************************************************************
    private static bool CircleContainsCircle(Vector2 position, float radius, Circle1 circle)
    {
      float xc0 = position.x - circle.Center.x;
      float yc0 = position.y - circle.Center.y;
      return Mathf.Sqrt(xc0 * xc0 + yc0 * yc0) < radius - circle.Radius + float.Epsilon;
    }

    //********************************************************************************************************************************
    private static void CircleIntersectingTwoCircles(Circle1 c1, Circle1 c2, out Vector2 outCenter, out float outRadius)
    {
      Vector2 c12 = c2.Center - c1.Center;
      float r12 = c2.Radius - c1.Radius;
      float l = c12.magnitude;
      outCenter = (c1.Center + c2.Center + c12 / l * r12) / 2.0f;
      outRadius = (l + c1.Radius + c2.Radius) / 2.0f;
    }
    //********************************************************************************************************************************
    private static void CircleIntersectingThreeCircles(Circle1 c1, Circle1 c2, Circle1 c3, out Vector2 outCenter, out float outRadius)
    {
      Vector2 p0 = c1.Center;
      Vector2 p1 = c2.Center;
      Vector2 p2 = c3.Center;

      float r0 = c1.Radius;
      float r1 = c2.Radius;
      float r2 = c3.Radius;

      Vector2 a0 = 2.0f * (p0 - p1);
      float a1 = 2.0f * (r1 - r0);
      float a2 = p0.SqrMagnitude() - r0 * r0 - p1.SqrMagnitude() + r1 * r1;

      Vector2 b0 = 2.0f * (p0 - p2);
      float b1 = 2.0f * (r2 - r0);
      float b2 = p0.SqrMagnitude() - r0 * r0 - p2.SqrMagnitude() + r2 * r2;

      float det = b0.x * a0.y - a0.x * b0.y;

      float cx = (a0.y * b2 - b0.y * a2) / det - p1.x;
      float cy = -(a0.x * b2 - b0.x * a2) / det - p1.y;
      float dx = (b0.y * a1 - a0.y * b1) / det;
      float dy = -(b0.x * a1 - a0.x * b1) / det;

      float e1 = dx * dx + dy * dy - 1.0f;
      float e2 = 2.0f * (cx * dx + cy * dy + r1);
      float e3 = cx * cx + cy * cy - r1 * r1;

      outRadius = (-e2 - Mathf.Sqrt(e2 * e2 - 4.0f * e1 * e3)) / (2.0f * e1);
      outCenter = new Vector2(cx + dx * outRadius + p1.x, cy + dy * outRadius + p1.y);
    }
     */
    /*
     case4 maybe needed
    private static float Pack1(List<Circle1> circles, out float bigRadius)
    {
      // Sort biggest first → critical

      List<Circle1> placed = new();

      Dictionary<Vector2,float> preserver = new Dictionary<Vector2,float>();
      Dictionary<Vector2, float> extender = new Dictionary<Vector2, float>();

      List<Vector2> bestPlaces = new();
      bigRadius = 0f;


      foreach (var circle in circles)
      {
        if (placed.Count == 0)
        {
          circle.Center = Vector2.zero;
          placed.Add(circle);
          bigRadius = circle.Radius;
          continue;
        }

        bigRadius = bigRadius + circle.Radius * 2 ;


        bool placedSuccessfully = false;

        float maxR = bigRadius - circle.Radius;

        // OUTSIDE → INSIDE
        for (float r = maxR; r >= 0 && !placedSuccessfully; r -= 0.5f)
        {
          for (float angle = 0; angle < Mathf.PI * 2; angle += 0.15f)
          {
            Vector2 pos = new Vector2(
                Mathf.Cos(angle),
                Mathf.Sin(angle)
            ) * r;

            if (IsValidPosition(pos, circle, placed, bigRadius))
            {
              circle.Center = pos;
              placed.Add(circle);
              placedSuccessfully = true;
              break;
            }
          }
        }

        if (!placedSuccessfully)
          return 0f; // packing failed for this R
      }

      //rootCircle.Children.AddRange(placed);
      return bigRadius;
    }
     */

    /*
     
    private static float AddCirclesIncrementally(List<Circle1> packedCircles, List<Circle1> newCircles, float currentBigRadius)
    {
      float bigRadius = currentBigRadius;

      foreach (var newCircle in newCircles)
      {
        // Find smallest expanded radius needed
        bigRadius = FindMinimalExpandedRadius(
            packedCircles,
            newCircle,
            bigRadius);

        // Add newly placed circle to fixed set
        packedCircles.Add(newCircle);
      }

      return bigRadius;
    }

    private static float FindMinimalExpandedRadius(List<Circle1> existing, Circle1 newCircle, float oldRadius)
    {
      float low = oldRadius;

      // Safe upper bound
      float high = oldRadius + newCircle.Radius * 4f;

      while (!TryPlaceIncremental(existing, newCircle, high))
        high *= 2f;

      // Binary search
      for (int i = 0; i < 25; i++)
      {
        float mid = (low + high) * 0.5f;

        if (TryPlaceIncremental(existing, newCircle, mid))
          high = mid;
        else
          low = mid;
      }

      // Final exact placement at smallest radius
      TryPlaceIncremental(existing, newCircle, high);

      return high;
    }

    private static bool TryPlaceIncremental(List<Circle1> existing, Circle1 newCircle, float bigRadius)
    {
      float maxR = bigRadius - newCircle.Radius;

      // Search OUTSIDE -> INSIDE
      for (float r = maxR; r >= 0; r -= 0.25f)
      {
        for (float angle = 0; angle < Mathf.PI * 2; angle += 0.05f)
        {
          Vector2 pos = new Vector2(
              Mathf.Cos(angle),
              Mathf.Sin(angle)
          ) * r;

          if (IsValidPosition(pos, newCircle, existing, bigRadius))
          {
            newCircle.Center = pos;
            return true;
          }
        }
      }

      return false;
    }

    private static bool IsValidPosition(Vector2 pos, Circle1 newCircle, List<Circle1> existing, float bigRadius)
    {
      // Must stay inside enclosing circle
      if (pos.magnitude + newCircle.Radius > bigRadius)
        return false;

      // Must not overlap any existing circle
      foreach (var c in existing)
      {
        float d = Vector2.Distance(pos, c.Center);

        if (d < c.Radius + newCircle.Radius)
          return false;
      }

      return true;
    }

    private static bool Overlaps(Circle1 a, Circle1 b)
    {
      float d = Vector2.Distance(a.Center, b.Center);
      return d < (a.Radius + b.Radius);
    }
     */
    #endregion
  }


  internal class SpatialHashGrid
  {
    public float _cellSize;
    public Dictionary<Tuple<int, int>, List<Circle1>> _cells;

    public SpatialHashGrid(float cellSize)
    {
      _cellSize = cellSize;
      _cells = new Dictionary<Tuple<int, int>, List<Circle1>>();
    }

    public void Clear()
    {
      _cells.Clear();
    }

    public void Insert(Circle1 circle)
    {
      int cellX = (int)MathF.Floor(circle.X / _cellSize);
      int cellY = (int)MathF.Floor(circle.Y / _cellSize);
      var key = new Tuple<int, int>(cellX, cellY);

      if (!_cells.ContainsKey(key))
      {
        _cells[key] = new List<Circle1>();
      }
      _cells[key].Add(circle);
    }

    // Retrieves circles in the target cell and the 8 surrounding cells
    public List<Circle1> GetNearby(Circle1  circle)
    {
      List<Circle1> nearby = new List<Circle1>();
      int cellX = (int)MathF.Floor(circle.X / _cellSize);
      int cellY = (int)MathF.Floor(circle.Y / _cellSize);

      for (int i = -1; i <= 1; i++)
      {
        for (int j = -1; j <= 1; j++)
        {
          var key = new Tuple<int, int>(cellX + i, cellY + j);
          if (_cells.TryGetValue(key, out List<Circle1> cellCircles))
          {
            nearby.AddRange(cellCircles);
          }
        }
      }
      return nearby;
    }
  }

  internal class CirclePacker2
  {
    public List<Circle1> Circles { get; private set; }
    private SpatialHashGrid _grid;

    // Simulation parameters
    public float CenterX { get; set; } = 0f;
    public float CenterY { get; set; } = 0f;
    public float GravityStrength { get; set; } = 0.01f;
    public int PbdIterations { get; set; } = 10; // Higher = stiffer, more accurate packing
    public float BoundingRadius { get; private set; }

    public CirclePacker2(float maxCircleDiameter)
    {
      //Circles = new List<Circle1>();
      // Initialize grid with cell size equal to max diameter
      _grid = new SpatialHashGrid(maxCircleDiameter);
    }

    public void AddCircle(Circle1 c)
    {
      Circles.Add(c);
    }

    public void RemoveCircle(string id)
    {
      Circles.RemoveAll(c => c.ID == id);
    }

    public void ResizeCircle(string id, float newRadius)
    {
      var circle = Circles.Find(c => c.ID == id);
      if (circle != null)
      {
        circle.Radius = newRadius;

        // Safety check for the Spatial Hash Grid (if it gets too big)
        if (newRadius * 2f > _grid._cellSize)
        {
          _grid = new SpatialHashGrid(newRadius * 2f);
        }
      }
    }

    // Call this method every frame
    /*
     */
    public void Update(List<Circle1> circles)
    {
      if (circles.Count == 0) return;

      // 1. Apply Gravity (Shrinking force)
      foreach (var circle in circles)
      {
        float dx = CenterX - circle.X;
        float dy = CenterY - circle.Y;
        circle.X += dx * GravityStrength;
        circle.Y += dy * GravityStrength;
      }

      // 2. Resolve Collisions (PBD)
      // We run this multiple times per frame to ensure a rigid packing
      for (int i = 0; i < PbdIterations; i++)
      {
        RebuildGrid(circles);
        ResolveCollisions(circles);
      }

      // 3. Update Bounding Circle C
      UpdateBoundingCircle(circles);
    }

    public void RebuildGrid(List<Circle1> circles)
    {
      _grid.Clear();
      foreach (var circle in circles)
      {
        _grid.Insert(circle);
      }
    }

    // Runs the packing algorithm until the circles settle into place

    public void ComputePacking(int maxSteps = 5, List<Circle1> circles = null) // Increased max steps drastically
    {
      if (circles == null || circles.Count == 0) return;

      // Microscopic thresholds
      float stopMovementThreshold = 0.001f;
      float stopMovementSq = stopMovementThreshold * stopMovementThreshold;
      float allowedOverlapTolerance = 0.00000001f; // Tighter tolerance

      bool settledSuccessfully = false;

      for (int step = 0; step < maxSteps; step++)
      {
        float maxMovementSq = 0f;
        float maxOverlapThisStep = 0f;

        foreach (var c in circles)
        {
          c.PrevX = c.X;
          c.PrevY = c.Y;
        }

        /*
        for (int i = 0; i < 100; i++)
        {
        }
         */
        // 1. Apply Gravity
        foreach (var c in circles)
        {
          float dx = CenterX - c.X;
          float dy = CenterY - c.Y;
          float dist = MathF.Sqrt(dx * dx + dy * dy);

          if (dist > 0)
          {
            c.X += (dx / dist) * GravityStrength;
            c.Y += (dy / dist) * GravityStrength;
          }
        }


        // 2. Resolve Collisions
        for (int i = 0; i < PbdIterations; i++)
        {
          RebuildGrid(circles);
          float currentOverlap = ResolveCollisions(circles);

          if (currentOverlap > maxOverlapThisStep)
          {
            maxOverlapThisStep = currentOverlap;
          }
        }

        // 3. Check movements
        foreach (var c in circles)
        {
          float moveX = c.X - c.PrevX;
          float moveY = c.Y - c.PrevY;
          float moveSq = (moveX * moveX) + (moveY * moveY);

          if (moveSq > maxMovementSq)
          {
            maxMovementSq = moveSq;
          }
        }

        // 4. Strict Exit Condition
        if (maxMovementSq <= stopMovementSq && maxOverlapThisStep <= allowedOverlapTolerance)
        {
          Debug.Log($"[SUCCESS] Packing settled perfectly in {step} steps. Max Overlap remaining: {maxOverlapThisStep}");
          settledSuccessfully = true;
          break;
        }
      }

      if (!settledSuccessfully)
      {
        // If you see this in your console, your Gravity is too high, or Iterations are too low!
        Debug.Log($"[WARNING] Reached {maxSteps} limit WITHOUT settling perfectly. Overlaps likely remain.");
      }

      UpdateBoundingCircle(circles);
    }
    /*
    public void ComputePacking1(int maxSteps = 2000)
    {
      if (Circles.Count == 0) return;

      // 3. Shrink the thresholds to microscopic levels
      // A movement of 0.001 is small enough that the eye won't see it jitter
      float stopMovementThreshold = 0.001f;
      float stopMovementSq = stopMovementThreshold * stopMovementThreshold;

      // An overlap of 0.0001 guarantees they are perfectly touching without crossing
      float allowedOverlapTolerance = 0.0001f;

      for (int step = 0; step < maxSteps; step++)
      {
        float maxMovementSq = 0f;
        float maxOverlapThisStep = 0f;

        foreach (var c in Circles)
        {
          c.PrevX = c.X;
          c.PrevY = c.Y;
        }

        // Apply Gravity (Now scaled to 0.01f)
        foreach (var c in Circles)
        {
          float dx = CenterX - c.X;
          float dy = CenterY - c.Y;
          float dist = MathF.Sqrt(dx * dx + dy * dy);

          if (dist > 0)
          {
            c.X += (dx / dist) * GravityStrength;
            c.Y += (dy / dist) * GravityStrength;
          }
        }

        // Resolve Collisions (Logic remains exactly the same!)
        for (int i = 0; i < PbdIterations; i++)
        {
          RebuildGrid();
          float currentOverlap = ResolveCollisions(Circles);

          if (currentOverlap > maxOverlapThisStep)
          {
            maxOverlapThisStep = currentOverlap;
          }
        }

        // Check movements
        foreach (var c in Circles)
        {
          float moveX = c.X - c.PrevX;
          float moveY = c.Y - c.PrevY;
          float moveSq = (moveX * moveX) + (moveY * moveY);

          if (moveSq > maxMovementSq)
          {
            maxMovementSq = moveSq;
          }
        }

        // The Strict Exit Condition (Now checking against microscopic tolerances)
        if (maxMovementSq < stopMovementSq && maxOverlapThisStep <= allowedOverlapTolerance)
        {
          Console.WriteLine($"Packing settled perfectly in {step} steps.");
          break;
        }
      }

      UpdateBoundingCircle();
    }
    public void ComputePacking11(int maxSteps = 1000)
    {
      if (Circles.Count == 0) return;

      // The threshold for "stopped moving". If the maximum distance any 
      // circle moves in a step is less than this, we consider it packed.
      float stopThreshold = 0.05f;
      float stopThresholdSq = stopThreshold * stopThreshold;

      for (int step = 0; step < maxSteps; step++)
      {
        float maxMovementSq = 0f;

        // Save current positions to measure how far they move this step
        foreach (var c in Circles)
        {
          c.PrevX = c.X;
          c.PrevY = c.Y;
        }

        // 1. Apply Gravity (Pull to 0,0)
        foreach (var c in Circles)
        {
          float dx = 0f - c.X;
          float dy = 0f - c.Y;
          float dist = MathF.Sqrt(dx * dx + dy * dy);

          if (dist > 0)
          {
            // Pull inward slightly. 
            c.X += (dx / dist) * GravityStrength;
            c.Y += (dy / dist) * GravityStrength;
          }
        }

        // 2. Resolve Collisions (Strict non-overlap)
        for (int i = 0; i < PbdIterations; i++)
        {
          RebuildGrid();
          ResolveCollisions(Circles); // (Use the same method from the previous response)
        }

        // 3. Check if settled
        foreach (var c in Circles)
        {
          float moveX = c.X - c.PrevX;
          float moveY = c.Y - c.PrevY;
          float moveSq = (moveX * moveX) + (moveY * moveY);

          if (moveSq > maxMovementSq)
          {
            maxMovementSq = moveSq;
          }
        }

        // If no circle moved more than the threshold, the packing is complete!
        if (maxMovementSq < stopThresholdSq)
        {
          Console.WriteLine($"Packing settled in {step} steps.");
          break;
        }
      }

      UpdateBoundingCircle();
    }
     */

    public float ResolveCollisions(List<Circle1> circles)
    {
      float maxOverlapFound = 0f;
      //Debug.Log("grid celll size: " + _grid._cellSize);

      foreach (var c1 in circles)
      {
        var neighbors = _grid.GetNearby(c1);
        //Debug.Log($"Circle {c1.ID} has {neighbors.Count} neighbors to check for collisions.");

        foreach (var c2 in neighbors)
        {
          //if (string.CompareOrdinal(c1.ID, c2.ID) >= 0) continue;
          if (c1.ID == c2.ID) continue;

          //Debug.Log($"Comparing {c1.ID} to {c2.ID}");

          // Use DOUBLE precision internally for tiny coordinate math
          double dx = c2.X - c1.X;
          double dy = c2.Y - c1.Y;
          double distSq = dx * dx + dy * dy;
          double radSum = c1.Radius + c2.Radius;

          //Debug.Log($"löadskjf: {dx}, {dy}, {distSq}, {radSum} ");

          if (distSq < radSum * radSum)
          {
            double dist = Math.Sqrt(distSq);

            // Handle the edge case of exact same coordinates perfectly without epsilon
            if (dist < 0.0000001)
            {
              // Push them apart slightly on a random axis so the next pass can catch them
              c1.X -= 0.001f;
              c2.X += 0.001f;
              continue;
            }

            double overlap = radSum - dist;

            if (overlap > maxOverlapFound)
            {
              maxOverlapFound = (float)overlap;
            }

            double nx = dx / dist;
            double ny = dy / dist;

            // Mass calculations
            double mass1 = c1.Radius * c1.Radius;
            double mass2 = c2.Radius * c2.Radius;
            double totalMass = mass1 + mass2;

            double ratio1 = mass2 / totalMass;
            double ratio2 = mass1 / totalMass;

            c1.X -= (float)(nx * overlap * ratio1);
            c1.Y -= (float)(ny * overlap * ratio1);

            //Debug.Log($"After resolving {c1.ID} and {c2.ID}, positions are: {c1.X}, {c1.Y} and {c2.X}, {c2.Y}");

            c2.X += (float)(nx * overlap * ratio2);
            c2.Y += (float)(ny * overlap * ratio2);

            //Debug.Log($"After resolving {c1.ID} and {c2.ID}, positions are: {c1.X}, {c1.Y} and {c2.X}, {c2.Y}");
          }
        }
      }

      //Debug.Log($"G");
      //foreach (var c in circles)
      //{
      //  Debug.Log($"Circle {c.ID} final position: {c.Center}");
      //}

      return maxOverlapFound;
    }
    private float ResolveCollisions1()
    {
      float maxOverlapFound = 0f;

      foreach (var c1 in Circles)
      {
        var neighbors = _grid.GetNearby(c1);

        foreach (var c2 in neighbors)
        {
          if (string.CompareOrdinal(c1.ID, c2.ID) >= 0) continue;

          float dx = c2.X - c1.X;
          float dy = c2.Y - c1.Y;
          float distSq = dx * dx + dy * dy;
          float radSum = c1.Radius + c2.Radius;

          if (distSq < radSum * radSum)
          {
            float dist = MathF.Sqrt(distSq) + 0.0001f;
            float overlap = radSum - dist;

            // TRACK THE WORST OVERLAP
            if (overlap > maxOverlapFound)
            {
              maxOverlapFound = overlap;
            }

            float nx = dx / dist;
            float ny = dy / dist;

            float mass1 = c1.Radius * c1.Radius;
            float mass2 = c2.Radius * c2.Radius;
            float totalMass = mass1 + mass2;

            float ratio1 = mass2 / totalMass;
            float ratio2 = mass1 / totalMass;

            c1.X -= nx * overlap * ratio1;
            c1.Y -= ny * overlap * ratio1;

            c2.X += nx * overlap * ratio2;
            c2.Y += ny * overlap * ratio2;
          }
        }
      }

      return maxOverlapFound;
    }

    private void ResolveCollisions11()
    {
      foreach (var c1 in Circles)
      {
        var neighbors = _grid.GetNearby(c1);

        foreach (var c2 in neighbors)
        {
          // Prevent comparing a circle to itself, and prevent double-resolving pairs
          if (c1.ID == c2.ID) continue;

          float dx = c2.X - c1.X;
          float dy = c2.Y - c1.Y;
          float distSq = dx * dx + dy * dy;
          float radSum = c1.Radius + c2.Radius;

          // Check if overlapping
          if (distSq < radSum * radSum)
          {
            // Add a tiny value to prevent division by zero if centers are exactly the same
            float dist = MathF.Sqrt(distSq) + 0.0001f;
            float overlap = radSum - dist;

            // Normalize direction vector
            float nx = dx / dist;
            float ny = dy / dist;

            // Calculate mass ratios (larger radius = harder to move)
            // Using Area (Radius^2) makes large circles even more solid
            float mass1 = c1.Radius * c1.Radius;
            float mass2 = c2.Radius * c2.Radius;
            float totalMass = mass1 + mass2;

            float ratio1 = mass2 / totalMass; // c1 moves proportionally to c2's mass
            float ratio2 = mass1 / totalMass; // c2 moves proportionally to c1's mass

            // Push circles apart directly modifying positions
            c1.X -= nx * overlap * ratio1;
            c1.Y -= ny * overlap * ratio1;

            c2.X += nx * overlap * ratio2;
            c2.Y += ny * overlap * ratio2;
          }
        }
      }
    }

    private void UpdateBoundingCircle(List<Circle1> circles)
    {
      float maxDistSq = 0f;
      foreach (var c in circles)
      {
        // Distance from center to the outer edge of the circle
        float dx = c.X - CenterX;
        float dy = c.Y - CenterY;
        float dist = MathF.Sqrt(dx * dx + dy * dy) + c.Radius;

        if (dist > maxDistSq)
        {
          maxDistSq = dist;
        }
      }
      BoundingRadius = maxDistSq;
    }
  }
}
