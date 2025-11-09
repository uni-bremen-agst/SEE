using LibGit2Sharp;
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
    }

    /// <summary>
    /// The root of the PTree corresponds to the entire available space, while
    /// each of the other nodes corresponds to a particular partition of the space.
    /// </summary>
    public PNode Root;

    /// <summary>
    /// The leaves of this tree that are not occupied.
    ///
    /// Note: We may want to use a sorted data structure if performance
    /// becomes an issue. Currently, this list will be linearly traversed.
    /// Thus looking up all leaves having a requested size has linear time
    /// complexity with the number of leaves.
    /// </summary>
    public IList<PNode> FreeLeaves;

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
    public PNode Split(PNode node, Vector2 size)
    {
      PNode result;

      // Node is no longer a free leaf. As a matter of fact, technically, it may
      // still be a leaf if the requested size perfectly matches the size of node,
      // so that it is actually not split, but it is not free.
      if (!FreeLeaves.Remove(node))
      { 
        throw new Exception("Node to be split is not a free leaf." + node + RectanglePackingNodeLayout.globalCallCount);
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
          node.Left.Rectangle = new PRectangle(node.Rectangle.Position, size);
          node.Left.Occupied = true;

          node.Right = new();
          node.Right.Parent = node;
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
          node.Left.Rectangle = new PRectangle(node.Rectangle.Position, size);
          node.Left.Occupied = true;

          node.Right = new();
          node.Right.Parent = node;
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
          node.Left.Rectangle = new PRectangle(node.Rectangle.Position, new Vector2(node.Rectangle.Size.x, size.y));

          node.Right = new();
          node.Right.Parent = node;
          node.Right.Rectangle = new PRectangle(new Vector2(node.Rectangle.Position.x, node.Rectangle.Position.y + size.y),
                                                new Vector2(node.Rectangle.Size.x, node.Rectangle.Size.y - size.y));
          FreeLeaves.Add(node.Right);

          // The upper enclosed rectangle is split again. Its left rectangle will be the rectangle
          // requested. Its right rectangle is available.
          node.Left.Left = new();
          node.Left.Left.Parent = node.Left;
          // This space is not available anymore.
          node.Left.Left.Occupied = true;
          // The allocated rectangle is added at the left upper corner of left node.
          node.Left.Left.Rectangle = new PRectangle(node.Left.Rectangle.Position, size);

          // The remaining rectangle sits right of the allocated one and occupies
          // the remaining space of left.
          node.Left.Right = new();
          node.Left.Right.Parent = node.Left;
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

    // Change the method signature from static to instance method
    public void MergeFreeLeaves(PNode fitNode)
    {
      if (fitNode.Occupied && !FreeLeaves.Contains(fitNode))
      {
        fitNode.Occupied = false;
        FreeLeaves.Add(fitNode);
      }

      PNode parent = fitNode.Parent;
      if (parent != null)
      {
        PNode sibling = parent.Left == fitNode ? parent.Right : parent.Left;
        if (sibling != null && FreeLeaves.Contains(sibling) && sibling.Occupied == false)
        {
          if (sibling.Occupied == false && fitNode.Occupied == false 
            && sibling.Left == null && sibling.Right == null
            && fitNode.Left == null && fitNode.Right == null)
          {
            parent.Left = null;
            parent.Right = null;
            parent.Occupied = true;
            FreeLeaves.Remove(fitNode);
            FreeLeaves.Remove(sibling);
            MergeFreeLeaves(parent);
          }
        }
      }
      else
      {
        return;
      }
    }
    /*
     */

    //************************************************************************************************************************************
    public void FreeLeavesAdjust() 
    {
      var maxX = FreeLeaves.Max(leaf => leaf.Rectangle.Position.x + leaf.Rectangle.Size.x);
      var maxY = FreeLeaves.Max(leaf => leaf.Rectangle.Position.y + leaf.Rectangle.Size.y);
      foreach (var pNode in Traverse(Root))
      {
        //right lower corner 

        var corner = pNode.Rectangle.Position + pNode.Rectangle.Size;
        if (maxY == corner.y)
        {
          Debug.Log("before y: " + pNode.Rectangle.Size.y);
          pNode.Rectangle.Size.y = Root.Rectangle.Size.y;
          Debug.Log("after y: " + pNode.Rectangle.Size.y);

        }
        if (maxX == corner.x)
        {
          Debug.Log("before x: " + pNode.Rectangle.Size.x);
          pNode.Rectangle.Size.x = Root.Rectangle.Size.x;
          Debug.Log("after x: " + pNode.Rectangle.Size.x);
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
        if (!pNode.Occupied)
          pNode.Rectangle.Size += factor;
      }
    }


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
      if (result.Count == 0)
      {
        Root.Rectangle.Size = Root.Rectangle.Size + 1.1f * new Vector2(1.0f, 1.0f);
        enlargeAllPNodes(1.1f * new Vector2(5.0f, 5.0f));
        // Fix: Cast the recursive call result to List<PNode>
        result = (List<PNode>)GetSufficientlyLargeLeaves(size);
      }
      /*
       */
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
    }
  }
}
