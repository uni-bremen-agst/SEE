using DiffMatchPatch;
using InControl;
using LibGit2Sharp;
using MoreLinq.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;

namespace SEE.Layout.NodeLayouts.RectanglePacking
{
  /// <summary>
  /// A two-dimensional kd-tree.
  /// </summary>
  public class PTree
  {
    /// <summary>
    /// Creates a ptree with a root having the given position and size.
    /// </summary>
    /// <param name="position">position of the rectangle represented by the root</param>
    /// <param name="size">size of the rectangle represented by the root</param>
    public PTree(Vector2 position, Vector2 size)
    {
      Root = new PNode(position, size, null);
      FreeLeaves = new List<PNode>
            {
                Root
            };
      coverec = Vector2.zero;
    }

    /// <summary>
    /// The root of the PTree corresponds to the entire available space, while
    /// each of the other nodes corresponds to a particular partition of the space.
    /// </summary>
    public PNode Root;

    public Vector2 coverec;

    /// <summary>
    /// The leaves of this tree that are not occupied.
    ///
    /// Note: We may want to use a sorted data structure if performance
    /// becomes an issue. Currently, this list will be linearly traversed.
    /// Thus looking up all leaves having a requested size has linear time
    /// complexity with the number of leaves.
    /// </summary>
    public IList<PNode> FreeLeaves;

    private int attempts = 0;

    /// <summary>
    /// Splits the rectangle represented by this node into sub-rectangles, where the left-most upper
    /// rectangle will be occupied by a new rectangle with the given size. More precisely, there
    /// are four different cases (let R be the rectangle represented by this node, let R' be
    /// a sub-rectangle with the requested size allocated within R by this method):
    ///
    /// R' is always positioned at the same left upper corner as R and has the given size.
    ///
    /// 1) size.x = rectangle.size.x && size.y = rectangle.size.y:
    ///    This is a gerfect match and R' = R, that is, R is from now on occupied.
    ///
    /// 2) size.x = rectangle.size.x && size.y < rectangle.size.y:
    ///    R is split into two non-overlapping rectangles R' and S where S is
    ///    positioned right from R' allocating the remaining space R-R'.
    ///
    /// 3) size.x < rectangle.size.x && size.y = rectangle.size.y:
    ///    R is split into two non-overlapping rectangles R' and S where S is
    ///    positioned below R' allocating the remaining space R-R'.
    ///
    /// 4) size.x < rectangle.size.x && size.y < rectangle.size.y:
    ///    R is split into three non-overlapping rectangles R', S, and T
    ///    where T is positioned below R' allocating the space of R
    ///    with the width of R and the height of R' and S is positioned
    ///    right of R' allocating the remaining space R-R'-T.
    ///
    /// In all cases, S and T are considered non-occupied.
    ///
    /// Preconditions:
    ///
    /// 1) node is a free leaf
    /// 2) size.x > rectangle.size.x || size.y > rectangle.size.y
    ///
    /// If the preconditions are not met, an exception is thrown.
    /// </summary>
    /// <param name="node">the node in which the rectangle should be occupied</param>
    /// <param name="size">the requested size of the rectangle to be occupied</param>
    /// <returns>the node that represents the rectangle fitting the requested size</returns>
    public PNode Split(PNode node, Vector2 size, string id = null)
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
          node.Id = id;
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
          node.Left.Id = id;

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
          node.Left.Id = id;

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
          node.Left.Left.Id = id;
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

    /// <summary>
    /// True if <paramref name="sub"/> fits into <paramref name="container"/>.
    /// </summary>
    /// <param name="sub">size of the presumably smaller rectangle</param>
    /// <param name="container">size of the presumably larger rectangle</param>
    /// <returns>true if <paramref name="sub"/> fits into <paramref name="container"/></returns>
    public static bool FitsInto(Vector2 sub, Vector2 container)
    {
      return sub.x <= container.x && sub.y <= container.y;
    }

    public void DeleteMergeRemainLeaves(string id)
    {
      Traverse(Root).ForEach(node =>
      {
        if (node.Id == id)
        {
          DeleteMergeRemainLeaves(node);
        }
      });
    }

    // Change the method signature from static to instance method
    public void DeleteMergeRemainLeaves(PNode fitNode)
    {
      //#fixme merge what mergable is rest right, rest left, left right, rest left right 
      if (fitNode.Occupied && !FreeLeaves.Contains(fitNode))
      {
        fitNode.Occupied = false;
        FreeLeaves.Add(fitNode);
      }

      PNode parent = fitNode.Parent;
      if (parent != null)
      {
        PNode sibling = parent.Left == fitNode ? parent.Right : parent.Left;
        PNode rest = parent.Rest;
        if (sibling != null && FreeLeaves.Contains(sibling) && sibling.Occupied == false)
        {
          if (sibling.Occupied == false && fitNode.Occupied == false 
            && sibling.Left == null && sibling.Right == null 
            && fitNode.Left == null && fitNode.Right == null)
          {
            Debug.Log("@removing");
            parent.Left = null;
            parent.Right = null;
            
            parent.Occupied = true;
            FreeLeaves.Remove(parent.Left);
            FreeLeaves.Remove(parent.Right);
          }
        }
        if (rest != null && rest.Occupied == false && rest.Left == null && rest.Right == null && FreeLeaves.Contains(rest))
        {
          parent.Rest = null;
          FreeLeaves.Remove(parent.Rest);
        }
        DeleteMergeRemainLeaves(parent);
      }
      else
      {
        return;
      }
    }
    /*
     */

    //************************************************************************************************************************************
    public void FreeLeavesAdjust(Vector2 oldWorstCaseSize) 
    {
      if (oldWorstCaseSize == Vector2.zero)
        return;
      var maxX = FreeLeaves.Max(leaf => leaf.Rectangle.Position.x + leaf.Rectangle.Size.x);
      var maxY = FreeLeaves.Max(leaf => leaf.Rectangle.Position.y + leaf.Rectangle.Size.y);
      foreach (var pNode in Traverse(Root))
      {
        if (pNode.Occupied) continue;

        var corner = pNode.Rectangle.Position + pNode.Rectangle.Size;
        if (maxY == corner.y)
        {
          //Debug.Log("before y: " + pNode.Rectangle.Size.y);
          var diffYDepth = Root.Rectangle.Size.y - oldWorstCaseSize.y;
          pNode.Rectangle.Size.y += diffYDepth;
          //Debug.Log("after y: " + pNode.Rectangle.Size.y);

        }
        if (maxX == corner.x)
        {
          //Debug.Log("before x: " + pNode.Rectangle.Size.x);
          var diffXWidth = Root.Rectangle.Size.x - oldWorstCaseSize.x;
          pNode.Rectangle.Size.x += diffXWidth;
          //Debug.Log("after x: " + pNode.Rectangle.Size.x);
        }
      }
    }
    //************************************************************************************************************************************
    public void FreeLeavesAdjust(Vector2 oldWorstCaseSize, PNode root)
    {
      if (oldWorstCaseSize == Vector2.zero)
        return;
      IList<PNode> freeLeaves = new List<PNode>();
      foreach (var pNode in Traverse(root))
      {
        if (pNode.Occupied == false && pNode.Left==null && pNode.Right==null && pNode.Rest==null) freeLeaves.Add(pNode);
      }
      var maxX = freeLeaves.Max(leaf => leaf.Rectangle.Position.x + leaf.Rectangle.Size.x);
      var maxY = freeLeaves.Max(leaf => leaf.Rectangle.Position.y + leaf.Rectangle.Size.y);
      //Debug.Log("maxX: " + maxX + " maxY: " + maxY);
      foreach (PNode pNode in Traverse(root))
      {
        if (pNode.Occupied) continue;

        var corner = pNode.Rectangle.Position + pNode.Rectangle.Size;
        if (maxY == corner.y)
        {
          //Debug.Log("before y: " + pNode.Rectangle.Size.y);
          var diffYDepth = root.Rectangle.Size.y - oldWorstCaseSize.y;
          pNode.Rectangle.Size.y += diffYDepth;
          //Debug.Log("after y: " + pNode.Rectangle.Size.y + " diffYdepth: " + diffYDepth);

        }
        if (maxX == corner.x)
        {
          //Debug.Log("before x: " + pNode.Rectangle.Size.x);
          var diffXWidth = root.Rectangle.Size.x - oldWorstCaseSize.x;
          pNode.Rectangle.Size.x += diffXWidth;
          //Debug.Log("after x: " + pNode.Rectangle.Size.x + " diffXdepth: " + diffXWidth);
        }
      }
    }

    //************************************************************************************************************************************
    public void resetAllPNodes(Vector2 factor)
    {
      foreach (var pNode in Traverse(Root))
      {
        if (!pNode.Occupied)
        pNode.Rectangle.Size = factor;
      }
    }

    public void enlargeAllPNodes(Vector2 factor)
    {
      foreach (var pNode in Traverse(Root))
      {
        if (!pNode.Occupied && pNode.Parent!=null)
          pNode.Rectangle.Size += factor;
      }
    }

    //************************************************************************************************************************************
    public PNode FindNodeById(string id)
    {
      foreach (var node in Traverse(Root))
      {
        if (node.Id == id)
        {
          return node;
        }
      }
      return null;
    }
    //************************************************************************************************************************************

    public void GrowLeaf(PNode leaf, Vector3 newScale)
    {
      var oldSize = leaf.Rectangle.Size;
      leaf.Rectangle.Size = new Vector2(
          newScale.x + 0.1f,
          newScale.z + 0.1f
      );
      var deltaSize = leaf.Rectangle.Size - oldSize;

      PropagateGrowUp(leaf, deltaSize);
    }

    /*
    public void GrowLeaf(PNode leaf, Vector3 newScale)
    {
      leaf.Rectangle.Size.x = newScale.x + 0.1f;
      leaf.Rectangle.Size.y = newScale.z + 0.1f;

      propagateResizeUp(leaf);
    }
    public void propagateResizeUp(PNode node)
    {
      
      Debug.Log("Propagate resize up from node: " + node + " with size: " + node.Rectangle.Size);
      PNode parent = node.Parent;
      Vector2 leftSize = parent.Left.Rectangle.Size;
      Vector2 rightSize = parent.Right.Rectangle.Size;
      if (parent.Left == node)
      {
        Debug.Log("Left child resized.");
        // Left child resized
        if (leftSize.y > rightSize.y)
        {
          Debug.Log("Updating parent y size from " + parent.Rectangle.Size.y + " to " + leftSize.y);
          parent.Rectangle.Size.y = leftSize.y;
        }
        Debug.Log("Updating parent x size from " + parent.Rectangle.Size.x + " to " + (leftSize.x + rightSize.x));
        parent.Rectangle.Size.x = leftSize.x + rightSize.x;
      }
      else
      {
        Debug.Log("Right child resized.");
        // Right child resized
        if (rightSize.y > leftSize.y)
        {
          Debug.Log("Updating parent y size from " + parent.Rectangle.Size.y + " to " + rightSize.y);
          parent.Rectangle.Size.y = rightSize.y;
        }
        Debug.Log("Updating parent x size from " + parent.Rectangle.Size.x + " to " + (leftSize.x + rightSize.x));
        parent.Rectangle.Size.x = leftSize.x + rightSize.x;
      }

      parent.RecomputeBounds();
      
      if (parent.Parent == null)
        return;
      propagateResizeUp(parent);
    }
     */
    /*
    public void PropagateGrowUp(PNode node, Vector2 delta)
    {
      // Wenn wir bereits an der Wurzel sind → fertig.
      if (node.Parent == null)
        return;

      PNode parent = node.Parent;
      PNode sibling = parent.Left == node ? parent.Right : parent.Left;

      // ------------------------------------------------------------
      // 1) Konflikte zwischen node und seinem sibling lösen
      // ------------------------------------------------------------
      if (sibling != null)
        ResolveConflict(node, sibling);

      // ------------------------------------------------------------
      // 2) Parent vergrößern, sodass beide Kinder hineinpassen
      // ------------------------------------------------------------
      ExpandParentToFitChildren(parent);

      // ------------------------------------------------------------
      // 3) Weiter nach oben propagieren
      // ------------------------------------------------------------
      PropagateGrowUp(parent, delta);
    }
     */
    public void PropagateGrowUp(PNode node, Vector2 delta)
    {
      // delta should be reseted every time we go up by the parents diff of new size and old size 
      if (node.Parent == null)
        return;

      PNode parent = node.Parent;
      PNode left = parent.Left;
      PNode right = parent.Right;
      PNode rest = parent.Rest;

      bool isLeft = (left == node);
      bool isLeftRight = right.Rectangle.Position.x != left.Rectangle.Position.x;
      PNode sibling = isLeft ? right : left;

      /*
       * this is already handled in the resize of sibling subtree
      //reset delta if needed
      if (isLeftRight && delta.y < 0 && !FreeLeaves.Contains(sibling))
      {
        delta.y = 0f;
      }
      else if (!isLeftRight && delta.x < 0 && !FreeLeaves.Contains(sibling))
      {
        delta.x = 0;
      }
       */


      //if sibling is free pnode
      if (FreeLeaves.Contains(sibling))
      {
        if (isLeftRight)
        {
          sibling.Rectangle.Size.x -= delta.x;
          sibling.Rectangle.Size.y += delta.y;
        }
        else
        {
          sibling.Rectangle.Size.x += delta.x;
          sibling.Rectangle.Size.y -= delta.y;
        }
      /*
        if (isLeft && sibling.Rectangle.Size.x <= 0 || sibling.Rectangle.Size.y <= 0)
        {
          parent.Right = null;
          FreeLeaves.Remove(parent.Right);
        }
        else if (!isLeft && sibling.Rectangle.Size.x <= 0 || sibling.Rectangle.Size.y <= 0)
        {
          parent.Left = null;
          FreeLeaves.Remove(parent.Left);
        }
       */
      }

      //if sibling is a subtree
      if (isLeftRight && sibling!=null && !FreeLeaves.Contains(sibling) && sibling.Occupied==false)
      {
        /*
        //resize sibling subtree in y
        // test it with FreeLeavesAdjust 
        Debug.Log("**************************sibling.y before " + sibling.Rectangle.Size.y);
        //Debug.Log("**************************sibling.y after " + sibling.Rectangle.Size.y + " delta.y " + delta.y);
        //delta.y -= diffY;
        //Debug.Log("***************************delta.y after resize sibling: " + diffY + " " + delta.y);
         */
        GetCoverectArea(sibling, out Vector2 sizeOfSibling);
        var diffY = sibling.Rectangle.Size.y - sizeOfSibling.y;
        var oldWorstCaseSize = sibling.Rectangle.Size;
        if (delta.y < 0)
        {
          sibling.Rectangle.Size.y -= Mathf.Min(Mathf.Abs(delta.y),diffY);
          FreeLeavesAdjust(oldWorstCaseSize, sibling);
        }else if (delta.y > 0)
        {
          sibling.Rectangle.Size.y = node.Rectangle.Size.y;
          FreeLeavesAdjust(oldWorstCaseSize, sibling);
        }
      }
      else if (!isLeftRight && sibling != null && !FreeLeaves.Contains(sibling) && sibling.Occupied == false)
      {
        /*
        //resize sibling subtree in x
        // test it with FreeLeavesAdjust 
        //delta.x -= diffX;
        Debug.Log("***************************delta.x after resize sibling: " + diffX);
         */
        GetCoverectArea(sibling, out Vector2 sizeOfSibling);
        var diffX = sibling.Rectangle.Size.x - sizeOfSibling.x;
        var oldWorstCaseSize = sibling.Rectangle.Size;
        if (delta.x < 0)
        {
          sibling.Rectangle.Size.x -= Mathf.Min(Mathf.Abs(delta.x), diffX);
          FreeLeavesAdjust(oldWorstCaseSize, sibling);
        }
        else if (delta.x > 0)
        {
          sibling.Rectangle.Size.x = node.Rectangle.Size.x;
          FreeLeavesAdjust(oldWorstCaseSize, sibling);
        }
      }

      //set parent size, position
      parent.Rectangle.Position = left.Rectangle.Position;
      Vector2 OldParentSize = parent.Rectangle.Size; 
      if (isLeftRight)
      {
        parent.Rectangle.Size.x = left.Rectangle.Size.x + right.Rectangle.Size.x;
        parent.Rectangle.Size.y = Mathf.Max(left.Rectangle.Size.y, right.Rectangle.Size.y);
      } 
      else
      { 
        //Debug.Log("herere: " + right.Rectangle.Position.x + " : " + left.Rectangle.Position.x + left.Rectangle.Size.x);
        parent.Rectangle.Size.x = Mathf.Max(left.Rectangle.Size.x, right.Rectangle.Size.x);
        parent.Rectangle.Size.y = left.Rectangle.Size.y + right.Rectangle.Size.y;
      }
      Vector2 newDeltaParent = parent.Rectangle.Size - OldParentSize;
      /*
      //
      // 3. Weiter nach oben propagieren
      //
       */

      //initialize rest pnode if there is more space in parent
      var restSize = Vector2.zero;
      var restPosition = Vector2.zero;
      if (isLeftRight)
      {
        if (left.Rectangle.Size.y < right.Rectangle.Size.y) 
        {
          restPosition = new Vector2(
              left.Rectangle.Position.x,
              left.Rectangle.Size.y
          );
          restSize.x = left.Rectangle.Size.x;
          restSize.y = Mathf.Abs(left.Rectangle.Size.y - parent.Rectangle.Size.y);
          
        }
        else if(right.Rectangle.Size.y < left.Rectangle.Size.y)
        {
          restPosition = new Vector2(
              right.Rectangle.Position.x,
              right.Rectangle.Size.y
          );
          restSize.x = right.Rectangle.Size.x;
          restSize.y = Mathf.Abs(right.Rectangle.Size.y - parent.Rectangle.Size.y);
        }
      }
      else
      {
        if (left.Rectangle.Size.x < right.Rectangle.Size.x)
        {
          restPosition = new Vector2(
              left.Rectangle.Size.x,
              left.Rectangle.Position.y
          );
          restSize.y = left.Rectangle.Size.y;
          restSize.x = Mathf.Abs(left.Rectangle.Size.x - parent.Rectangle.Size.x);
        }
        else if (right.Rectangle.Size.x < left.Rectangle.Size.x)
        {
          restPosition = new Vector2(
              right.Rectangle.Size.x,
              right.Rectangle.Position.y
          );
          restSize.y = right.Rectangle.Size.y;
          restSize.x = Mathf.Abs(right.Rectangle.Size.x - parent.Rectangle.Size.x);
        }
      }
      if (restSize.x > 0 && restSize.y > 0 && parent.Rest == null && !FreeLeaves.Contains(sibling))
      {
        parent.Rest = new PNode(restPosition, restSize, parent, PNode.SplitDirection.None);
        FreeLeaves.Add(parent.Rest);
      }

      //shift sibling and rest if needed
      if (sibling != null && delta != Vector2.zero)
      {
        if (isLeft && (left.Rectangle.Size.y > right.Rectangle.Size.y))
        {
          ShiftSubtree(sibling, delta.x, delta.y, isLeftRight, parent.Rest);
        }
        else if (isLeft)
        {
          ShiftSubtree(sibling, delta.x, delta.y, isLeftRight);
          /*
          //ShiftSubtree(sibling, -dx, 0f);
           */
        }
      }

      /*
      //testme: enlarge sibling tree if 8,6 -> 9,6 => 4,4.parent.parent gets larger
      //testme: merge free rest with sibling if possible

      //#testme: if parent has only one node then set node = null and go up Freeleaves.remove(node) parent.occupied = true 
      if (left != null && right==null && rest==null && parent.Rectangle.Size == left.Rectangle.Size)
      {
        parent.Left = null;
        parent.Occupied = true;
      }
      else if (left == null && right != null && rest == null && parent.Rectangle.Size == right.Rectangle.Size)
      {
        parent.Right = null;
        parent.Occupied = true;
      }
      else if (left == null && right == null && rest != null && parent.Rectangle.Size == rest.Rectangle.Size)
      {
        parent.Rest = null;
        parent.Occupied = true;
      }
      //MergeRestSibling(rest, sibling)
       */

      PropagateGrowUp(parent, newDeltaParent);
    }

    private void GetCoverectArea(PNode node, out Vector2 sizeOfSibling)
    {
      float maxX = float.MinValue;
      float maxY = float.MinValue;
      foreach (var n in PTree.Traverse(node))
      {
        if (n.Occupied)
        {
          maxX = Mathf.Max(maxX, n.Rectangle.Position.x + n.Rectangle.Size.x);
          maxY = Mathf.Max(maxY, n.Rectangle.Position.y + n.Rectangle.Size.y);
        }
      }
      sizeOfSibling = new Vector2(maxX, maxY);
    }
    private void ShiftSubtree(PNode node, float dx, float dy, bool isLeftRight, PNode restNode = null)
    {
      foreach (var n in PTree.Traverse(restNode))
      {
        if (isLeftRight)
          n.Rectangle.Position.x += dx;
        else
          n.Rectangle.Position.y += dy;
      }

      foreach (var n in PTree.Traverse(node))
      {
        if (isLeftRight)
          n.Rectangle.Position.x += dx;
        else
          n.Rectangle.Position.y += dy;
      }
    }
    /*
    public void PropagateGrowUp(PNode node, Vector2 delta)
    {
      if (node.Parent == null)
        return;

      PNode parent = node.Parent;
      PNode sibling = parent.Left == node ? parent.Right : parent.Left;

      // 1. Sibling bei Bedarf verschieben (weiterhin 2D)
      if (sibling != null)
        ResolveConflictPositiveGrow(node, sibling);

      // 2. Parent nach +X und +Y vergrößern
      ExpandParentPositive(parent);

      // 3. Weiter nach oben propagieren
      PropagateGrowUp(parent, delta);
    }
    private void ResolveConflictPositiveGrow(PNode grown, PNode sibling)
    {
      var A = grown.Rectangle;
      var B = sibling.Rectangle;

      if (!Overlaps(A, B))
        return;

      float shiftX = 0f;
      float shiftY = 0f;

      // Überlappungen berechnen
      float overlapX = (A.Position.x + A.Size.x) - B.Position.x;
      float overlapY = (A.Position.y + A.Size.y) - B.Position.y;

      // Nur positive Verschiebungen
      if (overlapX > 0)
        shiftX = overlapX;

      if (overlapY > 0)
        shiftY = overlapY;

      // Mindestens eine Richtung muss korrigiert werden
      Vector2 shift = new Vector2(shiftX, shiftY);

      // Rectangle verschieben
      var rect = sibling.Rectangle;
      rect.Position += shift;
      sibling.Rectangle = rect;
    }
     */
    /*
    private void ExpandParentToFitChildren(PNode parent)
    {
      var rect = parent.Rectangle;
      var a = parent.Left?.Rectangle;
      var b = parent.Right?.Rectangle;

      float minX = rect.Position.x;
      float minY = rect.Position.y;
      float maxX = rect.Position.x + rect.Size.x;
      float maxY = rect.Position.y + rect.Size.y;

      if (a != null)
      {
        minX = Mathf.Min(minX, a.Position.x);
        minY = Mathf.Min(minY, a.Position.y);
        maxX = Mathf.Max(maxX, a.Position.x + a.Size.x);
        maxY = Mathf.Max(maxY, a.Position.y + a.Size.y);
      }

      if (b != null)
      {
        minX = Mathf.Min(minX, b.Position.x);
        minY = Mathf.Min(minY, b.Position.y);
        maxX = Mathf.Max(maxX, b.Position.x + b.Size.x);
        maxY = Mathf.Max(maxY, b.Position.y + b.Size.y);
      }

      rect.Position = new Vector2(minX, minY);
      rect.Size = new Vector2(maxX - minX, maxY - minY);

      parent.Rectangle = rect;
    }
    private void ExpandParentPositive(PNode parent)
    {
      var rect = parent.Rectangle;
      float maxX = rect.Position.x + rect.Size.x;
      float maxY = rect.Position.y + rect.Size.y;

      if (parent.Left != null)
      {
        var a = parent.Left.Rectangle;
        maxX = Mathf.Max(maxX, a.Position.x + a.Size.x);
        maxY = Mathf.Max(maxY, a.Position.y + a.Size.y);
      }

      if (parent.Right != null)
      {
        var b = parent.Right.Rectangle;
        maxX = Mathf.Max(maxX, b.Position.x + b.Size.x);
        maxY = Mathf.Max(maxY, b.Position.y + b.Size.y);
      }

      // Position bleibt unverändert — nur Größe wächst nach rechts und oben
      rect.Size = new Vector2(
          maxX - rect.Position.x,
          maxY - rect.Position.y
      );

      parent.Rectangle = rect;
    }
    private void ResolveConflict(PNode grown, PNode sibling)
    {
      var A = grown.Rectangle;
      var B = sibling.Rectangle;

      // Kein Overlap → fertig
      if (!Overlaps(A, B))
        return;

      // Wie stark überlappen die Rechtecke in jede Richtung?
      
      float overlapLeft = (A.Position.x + A.Size.x) - B.Position.x;               // A drückt B nach rechts
      float overlapRight = (B.Position.x + B.Size.x) - A.Position.x;               // A drückt B nach links
      float overlapUp = (A.Position.y + A.Size.y) - B.Position.y;               // A drückt B nach oben
      float overlapDown = (B.Position.y + B.Size.y) - A.Position.y;               // A drückt B nach unten

      // Wir verschieben minimal → kleinstes positive Overlap finden
      float minHorizontal = Mathf.Min(overlapLeft, overlapRight);
      float minVertical = Mathf.Min(overlapUp, overlapDown);

      Vector2 shift = Vector2.zero;

      if (minHorizontal < minVertical)
      {
        // Horizontal lösen
        if (overlapLeft < overlapRight)
          shift.x = overlapLeft;       // sibling wird nach rechts geschoben
        else
          shift.x = -overlapRight;     // sibling wird nach links geschoben
      }
      else
      {
        // Vertikal lösen
        if (overlapUp < overlapDown)
          shift.y = overlapUp;         // sibling nach oben schieben
        else
          shift.y = -overlapDown;      // sibling nach unten schieben
      }

      // Verschieben des sibling
      var rect = sibling.Rectangle;
      rect.Position += shift;
      sibling.Rectangle = rect;
    }
    public void propagateResizeUp(PNode node)
    {
      if (node.Parent == null)
        return;

      PNode parent = node.Parent;

      AdjustSubtreeAfterResize(parent);

      parent.RecomputeBounds();

      // Try shrinking (in case the leaf got smaller!)
      ShrinkParentIfPossible(parent);

      propagateResizeUp(parent);
    }
    private void AdjustSubtreeAfterResize(PNode parent)
    {
      PNode left = parent.Left;
      PNode right = parent.Right;

      if (left == null || right == null)
        return;

      // Right child must always be placed exactly right of left
      float expectedRightX = left.Rectangle.Position.x + left.Rectangle.Size.x;
      float deltaX = expectedRightX - right.Rectangle.Position.x;

      if (Mathf.Abs(deltaX) > 0.0001f)
        ShiftSubtreeX(right, deltaX);

      // Heights are independent; only the parent grows vertically
    }
    private void ShrinkParentIfPossible(PNode parent)
    {
      if (parent.Left == null || parent.Right == null)
        return;

      float minWidth = parent.Left.Rectangle.Size.x + parent.Right.Rectangle.Size.x;
      float minHeight = Mathf.Max(parent.Left.Rectangle.Size.y, parent.Right.Rectangle.Size.y);

      // Shrink width
      if (parent.Rectangle.Size.x > minWidth)
        parent.Rectangle.Size.x = minWidth;

      // Shrink height
      if (parent.Rectangle.Size.y > minHeight)
        parent.Rectangle.Size.y = minHeight;
    }
    private void ShiftSubtreeX(PNode node, float deltaX)
    {
      node.Rectangle.Position.x -= deltaX;

      if (node.Left != null)
        ShiftSubtreeX(node.Left, deltaX);

      if (node.Right != null)
        ShiftSubtreeX(node.Right, deltaX);
    }
    private void ShiftSubtreeY(PNode node, float deltaY)
    {
      node.Rectangle.Position.y += deltaY;

      if (node.Left != null)
        ShiftSubtreeY(node.Left, deltaY);

      if (node.Right != null)
        ShiftSubtreeY(node.Right, deltaY);
    }
    private bool Overlaps(PRectangle a, PRectangle b)
    {
      return !(a.Position.x + a.Size.x <= b.Position.x ||
               b.Position.x + b.Size.x <= a.Position.x ||
               a.Position.y + a.Size.y <= b.Position.y ||
               b.Position.y + b.Size.y <= a.Position.y);
    }
    private bool Overlaps(PNode a, PNode b)
    {
      return !(a.Rectangle.Position.x + a.Rectangle.Size.x <= b.Rectangle.Position.x ||
               b.Rectangle.Position.x + b.Rectangle.Size.x <= a.Rectangle.Position.x ||
               a.Rectangle.Position.y + a.Rectangle.Size.y <= b.Rectangle.Position.y ||
               b.Rectangle.Position.y + b.Rectangle.Size.y <= a.Rectangle.Position.y);
    }
     */

    /*
     */
    //************************************************************************************************************************************
    public static IEnumerable<PNode> Traverse(PNode node)
    {
      if (node == null)
        yield break;
      yield return node;
      foreach (var n in Traverse(node.Left))
        yield return n;
      foreach (var n in Traverse(node.Right))
        yield return n;
    }

    /// <summary>
    /// Returns all free leaves having at least the requested size.
    /// </summary>
    /// <param name="size">requested size of the rectangle</param>
    /// <returns>all free leaves having at least the requested size</returns>
    public IList<PNode> GetSufficientlyLargeLeaves(Vector2 size, Vector2 oldWorstCaseSize)
    {
      if (++attempts > 1000)
        throw new InvalidOperationException("No sufficiently large leaves possible.");
      List<PNode> result = new();
      foreach (PNode leaf in FreeLeaves)
      {
        if (FitsInto(size, leaf.Rectangle.Size))
        {
          result.Add(leaf);
        }
      }
      if (result.Count == 0)
      {
        //Debug.Log("No sufficiently large leaves found. Enlarging all PNodes.");
        //Print();
        Debug.Log("//////////////////////////////////////////////////////////////////////////////enlarged");
        Root.Rectangle.Size = Root.Rectangle.Size + 5.1f * size;
        FreeLeavesAdjust(oldWorstCaseSize);
        result = (List<PNode>)GetSufficientlyLargeLeaves(size, oldWorstCaseSize);
      }
      /*
        //Root.Rectangle.Size = Root.Rectangle.Size + 1.1f * new Vector2(1.0f, 1.0f);
        //enlargeAllPNodes(1.1f * new Vector2(2.0f, 2.0f));
        // Fix: Cast the recursive call result to List<PNode>
       */
      return result;
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

    /// <summary>
    /// Prints the tree to the console. Can be used for debugging.
    /// </summary>
    public void Print()
    {
      Print(Root, "", true);
    }

    /// <summary>
    /// Prints the tree rooted by <paramref name="node"/> to the console. Can be used for debugging.
    /// </summary>
    /// <param name="node">the root of the tree to be printed</param>
    /// <param name="indent">indentation before the node is printed</param>
    /// <param name="last">whether this is the last node to be printed</param>
    public void Print(PNode node, string indent, bool last)
    {

      if (node == null)
      {
        return;
      }
      string output = indent;
      if (last)
      {
        output += "└─";
        indent += "  ";
      }
      else
      {
        output += "├─";
        indent += "| ";
      }
      Debug.Log(output + " " + node + " :" + node.Rectangle.Size + ": " + "\n");

      Print(node.Left, indent, false);
      Print(node.Right, indent, true);
      if (node.Rest != null)
        Print(node.Rest, indent, true);
    }
  }
}
