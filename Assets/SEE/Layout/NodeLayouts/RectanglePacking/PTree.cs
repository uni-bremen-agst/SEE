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
      Root = new PNode(position, size);
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

    public PNode Split1(PNode node, Vector2 size, string id = null)
    {
      //currently not in use
      PNode result;

      // Node is no longer a free leaf. As a matter of fact, technically, it may
      // still be a leaf if the requested size perfectly matches the size of node,
      // so that it is actually not split, but it is not free.
      if (!FreeLeaves.Remove(node))
      {
        if (node == null) throw new Exception("PNode is null");
        if (node.Id == null) node.Id = " null ";
        throw new Exception("Node to be split is not a free leaf." + node.Id + RectanglePackingNodeLayout1.globalCallCount);
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
          node.Rests.Add(new());
          node.Rests[0].Parent = node;
          node.Rests[0].Rectangle = new PRectangle(node.Rectangle.Position, size);
          node.Rests[0].Occupied = true;
          node.Rests[0].Id = id;

          node.Rests.Add(new());
          node.Rests[1].Parent = node;
          //node.Rests[1].Direction = PNode.SplitDirection.Right;
          node.Rests[1].Rectangle = new PRectangle(new Vector2(node.Rectangle.Position.x, node.Rectangle.Position.y + size.y),
                                                new Vector2(node.Rectangle.Size.x, node.Rectangle.Size.y - size.y));
          FreeLeaves.Add(node.Rests[1]);
          result = node.Rests[0];
          result.Parent = node;
        }
      }
      else
      {
        // size.x < rectangle.size.x
        if (size.y == node.Rectangle.Size.y)
        {
          // size.x < rectangle.size.x && size.y = rectangle.size.y
          node.Rests.Add(new());
          node.Rests[0].Parent = node;
          //node.Rests[0].Direction = PNode.SplitDirection.Left;
          node.Rests[0].Rectangle = new PRectangle(node.Rectangle.Position, size);
          node.Rests[0].Occupied = true;
          node.Rests[0].Id = id;

          node.Rests.Add(new());
          node.Rests[1].Parent = node;
          //node.Rests[1].Direction = PNode.SplitDirection.Right;
          node.Rests[1].Rectangle = new PRectangle(new Vector2(node.Rectangle.Position.x + size.x, node.Rectangle.Position.y),
                                                new Vector2(node.Rectangle.Size.x - size.x, size.y));
          FreeLeaves.Add(node.Rests[1]);
          result = node.Rests[0];
          result.Parent = node;
        }
        else
        {
          // size.x < rectangle.size.x && size.y < rectangle.size.y
          // The node will be split vertically into two sub-rectangles. The upper rectangle is
          // left and the lower rectangle is right.
          // The origin of left is the origin of the enclosing rectangle. Its width is the width
          // of the enclosing rectangle. Its depth is the size of the requested rectangle.

          if (node.Rests == null) Debug.Log("node rests is null");
          node.Rests = new List<PNode>();
          node.Rests.Add(new PNode());
          
          node.Rests[0].Parent = new PNode();
          node.Rests[0].Parent = node;  
          //node.Rests[0].Direction = PNode.SplitDirection.Left;
          node.Rests[0].Rectangle = new PRectangle(node.Rectangle.Position, new Vector2(node.Rectangle.Size.x, size.y));

          node.Rests.Add(new PNode());
          node.Rests[1].Parent = node;
          //node.Rests[1].Direction = PNode.SplitDirection.Right;
          node.Rests[1].Rectangle = new PRectangle(new Vector2(node.Rectangle.Position.x, node.Rectangle.Position.y + size.y),
                                                new Vector2(node.Rectangle.Size.x, node.Rectangle.Size.y - size.y));
          FreeLeaves.Add(node.Rests[1]);

          // The upper enclosed rectangle is split again. Its left rectangle will be the rectangle
          // requested. Its right rectangle is available.
          node.Rests[0].Rests.Add(new());
          node.Rests[0].Rests[0].Parent = node.Rests[0];
          //node.Rests[0].Rests[0].Direction = PNode.SplitDirection.Left;
          // This space is not available anymore.
          node.Rests[0].Rests[0].Occupied = true;
          node.Rests[0].Rests[0].Id = id;
          // The allocated rectangle is added at the left upper corner of left node.
          node.Rests[0].Rests[0].Rectangle = new PRectangle(node.Rests[0].Rectangle.Position, size);

          // The remaining rectangle sits right of the allocated one and occupies
          // the remaining space of left.
          node.Rests[0].Rests.Add(new());
          node.Rests[0].Rests[1].Parent = node.Rests[0];
          //node.Rests[0].Rests[1].Direction = PNode.SplitDirection.Right;
          node.Rests[0].Rests[1].Rectangle = new PRectangle(new Vector2(node.Rests[0].Rectangle.Position.x + size.x, node.Rests[0].Rectangle.Position.y),
                                                     new Vector2(node.Rests[0].Rectangle.Size.x - size.x, node.Rests[0].Rectangle.Size.y));
          FreeLeaves.Add(node.Rests[0].Rests[1]);
          result = node.Rests[0].Rests[0];
          result.Parent = node.Rests[0];
        }
      }
      return result;
    }


    public PNode Split11(PNode node, Vector2 size, string id = null)
    {
      if (!FreeLeaves.Remove(node))
      {
        if (node == null) throw new Exception("PNode is null");
        throw new Exception("Node is not a free leaf: " + node.Id);
      }

      if (size.x > node.Rectangle.Size.x || size.y > node.Rectangle.Size.y)
      {
        throw new Exception("Requested size does not fit.");
      }

      // Reset children (flat structure)
      node.Rests = new List<PNode>();

      Vector2 pos = node.Rectangle.Position;
      Vector2 full = node.Rectangle.Size;

      float remainingWidth = full.x - size.x;
      float remainingHeight = full.y - size.y;

      // 1. Placed rectangle (inside same node)
      PNode placed = new PNode
      {
        Parent = node,
        Rectangle = new PRectangle(pos, size),
        Occupied = true,
        Id = id
      };
      node.Rests.Add(placed);

      // 2. Right remainder
      if (remainingWidth > 0)
      {
        PNode right = new PNode
        {
          Parent = node,
          Rectangle = new PRectangle(
                new Vector2(pos.x + size.x, pos.y),
                new Vector2(remainingWidth, size.y)
            ),
          Occupied = false
        };

        node.Rests.Add(right);
        FreeLeaves.Add(right);
      }

      // 3. Bottom remainder
      if (remainingHeight > 0)
      {
        PNode bottom = new PNode
        {
          Parent = node,
          Rectangle = new PRectangle(
                new Vector2(pos.x, pos.y + size.y),
                new Vector2(full.x, remainingHeight)
            ),
          Occupied = false
        };

        node.Rests.Add(bottom);
        FreeLeaves.Add(bottom);
      }

      return placed;
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


    //************************************************************************************************************************************
    public void DeleteMergeRemainLeaves2(string id)
    {
      Root.Rests = Root.Rests.Where(r => r.Id != id).ToList();
    }

    
    //************************************************************************************************************************************
    public void DeleteMergeRemainLeaves1(string id)
    {
      Traverse1(Root).ForEach(node =>
      {
        if (node.Id == id)
        {
          DeleteMergeRemainLeaves1(node);
        }
      });
    }

    // Change the method signature from static to instance method
    public void DeleteMergeRemainLeaves1(PNode fitNode)
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
        List< PNode > siblings = parent.Rests.Except(new[] { fitNode }).ToList();
        bool hasOccupiedSiblingNode = siblings.Any(sibling => Traverse1(sibling).Any(node => node.Occupied));

        if (!hasOccupiedSiblingNode)
        {
          Debug.Log("@removing");
          foreach (var pnode in Traverse1(parent))
          {
            FreeLeaves.Remove(pnode);
          }
          parent.Rests = new();
          parent.Rests.Add(new());
          parent.Rests.Add(new());
          parent.Rests[0] = null;
          parent.Rests[1] = null;

          parent.Occupied = true;
        
        }
        DeleteMergeRemainLeaves1(parent);
      }
    }
    //************************************************************************************************************************************
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
    public void FreeLeavesAdjust1(Vector2 oldWorstCaseSize)
    {
      if (oldWorstCaseSize == Vector2.zero)
        return;

      if (FreeLeaves == null)
        return;
      float maxX = FreeLeaves.Where(leaf => leaf != null).Max(leaf => leaf.Rectangle.Position.x + leaf.Rectangle.Size.x);
      float maxY = FreeLeaves.Where(leaf => leaf != null).Max(leaf => leaf.Rectangle.Position.y + leaf.Rectangle.Size.y);
      foreach (var pNode in Traverse1(Root))
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
    public void FreeLeavesAdjust1(Vector2 oldWorstCaseSize, PNode root)
    {
      if (oldWorstCaseSize == Vector2.zero)
        return;
      IList<PNode> freeLeaves = new List<PNode>();
      foreach (var pNode in Traverse1(root))
      {
        if (pNode.Occupied == false && pNode.Rests.Count == 0) freeLeaves.Add(pNode);
      }
      var maxX = freeLeaves.Max(leaf => leaf.Rectangle.Position.x + leaf.Rectangle.Size.x);
      var maxY = freeLeaves.Max(leaf => leaf.Rectangle.Position.y + leaf.Rectangle.Size.y);
      //Debug.Log("maxX: " + maxX + " maxY: " + maxY);
      foreach (PNode pNode in Traverse1(root))
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
    //Garbage

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
    public PNode FindNodeById1(string id)
    {
      foreach (var node in Traverse1(Root))
      {
        if (node.Id == id)
        {
          return node;
        }
      }
      return null;
    }

    //************************************************************************************************************************************
    public PNode FindNodeById2(string id)
    {
      foreach (var node in Root.Rests)
      {
        if (node.Id == id)
        {
          return node;
        }
      }
      return null;
    }
    //************************************************************************************************************************************


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

    //************************************************************************************************************************************

    public void GrowLeaf1(PNode leaf, Vector3 newScale)
    {
      var oldSize = leaf.Rectangle.Size;
      leaf.Rectangle.Size = new Vector2(
          newScale.x,
          newScale.z
      );
      var deltaSize = leaf.Rectangle.Size - oldSize;

      PropagateGrowUp1(leaf, deltaSize);
    }
    public void PropagateGrowUp1(PNode node, Vector2 delta)
    {
      // delta should be reseted every time we go up by the parents diff of new size and old size 
      if (node.Parent == null)
        return;

      PNode parent = node.Parent;
      
      if (delta.x < 0)
      {
        detectAndSetRest(parent);
      }
      if (delta.x > 0)
      {
        List<PNode> siblingsToMove = parent.Rests.Except(new List<PNode>() { node }).Where(r => r.Rectangle.Position.x >= (node.Rectangle.Position.x + node.Rectangle.Size.x - delta.x)).ToList();

        Debug.Log(siblingsToMove.Count + "---------------------- are there siblings to move x? : delta" + delta);
        ShiftSubtree1(delta.x, 0f, siblingsToMove);
        

      }
      if (delta.y < 0)
      {
        detectAndSetRest(parent);

      }
      if (delta.y > 0)
      {
        List<PNode> siblingsToMove = parent.Rests.Except(new List<PNode>() { node }).Where(r => r.Rectangle.Position.y >= (node.Rectangle.Position.y + node.Rectangle.Size.y - delta.y)).ToList();

        Debug.Log(siblingsToMove.Count + "---------------------- are there siblings to move y? : delta" + delta);

        ShiftSubtree1(0, delta.y, siblingsToMove);
        

      }

      //set parent size, position
      parent.Rectangle.Position = parent.Rests[0].Rectangle.Position;
      Vector2 OldParentSize = parent.Rectangle.Size;
      if (delta.x > 0)
      {
        parent.Rectangle.Size.x += delta.x;
        detectAndSetRest(parent);
      }
      if (delta.y > 0)
      {
        parent.Rectangle.Size.y += delta.y;
        detectAndSetRest(parent);
      }
      
      //MergeEmptyNodes(parent);
      PropagateGrowUp1(parent, delta);

    }
    //************************************************************************************************************************************

    public void GrowLeaf2(PNode leaf, Vector3 newScale)
    {

      var oldSize = leaf.Rectangle.Size;
      leaf.Rectangle.Size = new Vector2(
          newScale.x,
          newScale.z
      );
      var deltaSize = leaf.Rectangle.Size - oldSize;

      //Debug.Log("----------------------------------Growing leaf: " + leaf + " from old size: " + oldSize + " to new size: " + leaf.Rectangle.Size + " with delta: " + deltaSize);
      PropagateGrowUp2(leaf, deltaSize);
    }
    public void PropagateGrowUp2(PNode node, Vector2 delta)
    {

      //Debug.Log("----------------------------------Propagating grow up from node: " + node + " with delta: " + delta);
      // delta should be reseted every time we go up by the parents diff of new size and old size 
      //if (node.Parent == null)
      //  return;
      //Debug.Log("----------------------------------Node parent: " + node.Parent.ToString1());

      PNode parent = node.Parent;

      //if (delta.x < 0)
      //{

      //}
      if (delta.x > 0)
      {
        //Debug.Log("---------------------delta x is greater than 0, checking siblings to move...");
        List<PNode> siblingsToMove = parent.Rests.Except(new List<PNode>() { node }).Where(r => r.Rectangle.Position.x >= (node.Rectangle.Position.x + node.Rectangle.Size.x - delta.x)).ToList();

        Debug.Log(siblingsToMove.Count + "---------------------- are there siblings to move x? : delta" + delta);
        ShiftSubtree1(delta.x, 0f, siblingsToMove);

      }
      //if (delta.y < 0)
      //{
      //}
      if (delta.y > 0)
      {
        //Debug.Log("---------------------delta y is greater than 0, checking siblings to move...");
        List<PNode> siblingsToMove = parent.Rests.Except(new List<PNode>() { node }).Where(r => r.Rectangle.Position.y >= (node.Rectangle.Position.y + node.Rectangle.Size.y - delta.y)).ToList();

        Debug.Log(siblingsToMove.Count + "---------------------- are there siblings to move y? : delta" + delta);

        ShiftSubtree1(0, delta.y, siblingsToMove);
        
      }

      //set parent size, position
      parent.Rectangle.Position = parent.Rests[0].Rectangle.Position;
      Vector2 OldParentSize = parent.Rectangle.Size;
      if (delta.x > 0)
      {
        parent.Rectangle.Size.x += delta.x;
      }
      if (delta.y > 0)
      {
        parent.Rectangle.Size.y += delta.y;
      }
      //Tighten(parent);

      //MergeEmptyNodes(parent);
      //PropagateGrowUp2(parent, delta);

    }
    //************************************************************************************************************************************

    public void Tighten(PNode node)
    {
      Debug.Log("Tightening layout for node: " + node);
      // Get all rectangles in the subtree of the given node
      var rects = node.Rests;
      // Push left and up to tighten the layout
      //PushLeftStick(rects, 4.0f);
      //PushUpStick(rects, 4.0f);

      //PushLeftStick(rects);
      //PushUpStick(rects);

      //Compact(rects, new PNode(0f,0f,4f,4f));
      //Compact(rects, node);

      CompactFully(rects, node);

    }

    const float EPS = 0.0001f;

    bool OverlapY(PNode a, PNode b)
    {
      return !(a.YY >= b.PNodeBottom || a.PNodeBottom <= b.YY);
    }

    bool OverlapX(PNode a, PNode b)
    {
      return !(a.XX >= b.PNodeRight || a.PNodeRight <= b.XX);
    }

    float ComputeLeftLimit(PNode rect, List<PNode> rects, float leftBoundary)
    {
      float limit = leftBoundary;

      foreach (var other in rects)
      {
        if (other == rect) continue;

        if (OverlapY(rect, other))
        {
          if (other.PNodeRight <= rect.XX)
          {
            limit = Math.Max(limit, other.PNodeRight);
          }
        }
      }

      return limit;
    }

    float ComputeTopLimit(PNode rect, List<PNode> rects, float topBoundary)
    {
      float limit = topBoundary;

      foreach (var other in rects)
      {
        if (other == rect) continue;

        if (OverlapX(rect, other))
        {
          if (other.PNodeBottom <= rect.YY)
          {
            limit = Math.Max(limit, other.PNodeBottom);
          }
        }
      }

      return limit;
    }

    public void CompactFully(List<PNode> rects, PNode bounds)
    {
      bool moved;

      do
      {
        moved = false;

        // Sort for stable behavior
        rects = rects.OrderBy(r => r.XX).ThenBy(r => r.YY).ToList();

        foreach (var r in rects)
        {
          float newX = ComputeLeftLimit(r, rects, bounds.XX);

          if (Math.Abs(newX - r.XX) > EPS)
          {
            r.Rectangle.Position.x = newX;
            moved = true;
          }
        }

        rects = rects.OrderBy(r => r.YY).ThenBy(r => r.XX).ToList();

        foreach (var r in rects)
        {
          float newY = ComputeTopLimit(r, rects, bounds.YY);

          if (Math.Abs(newY - r.YY) > EPS)
          {
            r.Rectangle.Position.y = newY;
            moved = true;
          }
        }

      } while (moved);
    }


    //************************************************************************************************************************************


    /*
    static void MoveLeft(Rect rect, List<Rect> others, float leftBoundary)
    {
      float targetX = leftBoundary;

      foreach (var other in others)
      {
        if (other == rect) continue;

        // Check vertical overlap
        bool overlapY = !(rect.Y >= other.Bottom || rect.Bottom <= other.Y);

        if (overlapY)
        {
          if (other.Right <= rect.X)
          {
            targetX = Math.Max(targetX, other.Right);
          }
        }
      }

      rect.X = targetX;
    }

    static void MoveUp(Rect rect, List<Rect> others, float topBoundary)
    {
      float targetY = topBoundary;

      foreach (var other in others)
      {
        if (other == rect) continue;

        // Check horizontal overlap
        bool overlapX = !(rect.X >= other.Right || rect.Right <= other.X);

        if (overlapX)
        {
          if (other.Bottom <= rect.Y)
          {
            targetY = Math.Max(targetY, other.Bottom);
          }
        }
      }

      rect.Y = targetY;
    }

    public static void Compact(List<Rect> rects, Rect bounds)
    {
      // Sort helps stability
      rects = rects.OrderBy(r => r.X).ThenBy(r => r.Y).ToList();

      // Move left
      foreach (var r in rects)
      {
        MoveLeft(r, rects, bounds.X);
      }

      // Move up
      rects = rects.OrderBy(r => r.Y).ThenBy(r => r.X).ToList();

      foreach (var r in rects)
      {
        MoveUp(r, rects, bounds.Y);
      }
    }
     */

    //************************************************************************************************************************************

    /*
    const float EPS = 0.0001f;

    public float Normalize(float v)
    {
      return (float)Math.Round(v, 4);
    }

    public bool Intersects(PNode a, PNode b)
    {
      return !(a.XX >= b.PNodeRight ||
                a.PNodeRight <= b.XX ||
                a.YY >= b.PNodeBottom ||
                a.PNodeBottom <= b.YY);
    }

    // MAIN FUNCTION: call this
    public List<PNode> FindMaxEmptyRectangles(PNode big, List<PNode> filled)
  {
    var xs = new SortedSet<float>();
    var ys = new SortedSet<float>();

    // Add big rectangle borders
    xs.Add(Normalize(big.XX));
    xs.Add(Normalize(big.PNodeRight));
    ys.Add(Normalize(big.YY));
    ys.Add(Normalize(big.PNodeBottom));

    // Add inner rectangle borders
    foreach (var r in filled)
    {
      xs.Add(Normalize(r.XX));
      xs.Add(Normalize(r.PNodeRight));
      ys.Add(Normalize(r.YY));
      ys.Add(Normalize(r.PNodeBottom));
    }

    var xList = xs.ToList();
    var yList = ys.ToList();

    int w = xList.Count - 1;
    int h = yList.Count - 1;

    bool[,] occupied = new bool[w, h];

    // Step 1: mark occupied cells
    for (int i = 0; i < w; i++)
    {
      for (int j = 0; j < h; j++)
      {
        var cell = new PNode(
            xList[i],
            yList[j],
            xList[i + 1] - xList[i],
            yList[j + 1] - yList[j]
        );

        foreach (var r in filled)
        {
          if (Intersects(cell, r))
          {
            occupied[i, j] = true;
            break;
          }
        }
      }
    }

    // Step 2: find maximal empty rectangles
    var result = new List<PNode>();
    bool[,] used = new bool[w, h];

    for (int i = 0; i < w; i++)
    {
      for (int j = 0; j < h; j++)
      {
        if (occupied[i, j] || used[i, j])
          continue;

        int maxW = 0;

        // expand width
        while (i + maxW < w && !occupied[i + maxW, j])
          maxW++;

        int maxH = 1;
        bool done = false;

        // expand height
        while (j + maxH < h && !done)
        {
          for (int k = 0; k < maxW; k++)
          {
            if (occupied[i + k, j + maxH])
            {
              done = true;
              break;
            }
          }
          if (!done) maxH++;
        }

        // mark used
        for (int dx = 0; dx < maxW; dx++)
        {
          for (int dy = 0; dy < maxH; dy++)
          {
            used[i + dx, j + dy] = true;
          }
        }

        // create rectangle
        result.Add(new PNode(
            xList[i],
            yList[j],
            xList[i + maxW] - xList[i],
            yList[j + maxH] - yList[j]
        ));
      }
    }

    return result;
  
  }
     */



    //************************************************************************************************************************************

    /*
     
    public void Compact(List<PNode> rects, PNode bounds)
    {
      //CompactHorizontally(rects, bounds);
      CompactVertically(rects, bounds);
    }
    public void CompactHorizontally(List<PNode> rects, PNode bounds)
    {
      // Sort by X so we process left-most first (they stay, others push against them)
      var sorted = rects.OrderBy(r => r.XX).ToList();

      for (int i = 0; i < sorted.Count; i++)
      {
        var rect = sorted[i];
        float newX = bounds.XX; // Start at left edge of container

        // Find the rightmost edge of any rect to the left that overlaps in Y
        for (int j = 0; j < i; j++)
        {
          var other = sorted[j];
          if (OverlapsHorizontally(rect, other))
          {
            newX = Math.Max(newX, other.PNodeRight);
          }
        }

        // Update the rect
        sorted[i] = new PNode(newX, rect.YY, rect.Width, rect.Height);
      }

      // Copy back to original list
      for (int i = 0; i < rects.Count; i++)
      {
        int idx = sorted.FindIndex(r =>
            r.Width == rects[i].Width &&
            r.Height == rects[i].Height &&
            r.YY == rects[i].YY);
        // Match by original position logic - simpler to just rebuild
      }

      // Simpler: clear and re-add
      rects.Clear();
      rects.AddRange(sorted);
    }
    public void CompactVertically(List<PNode> rects, PNode bounds)
    {
      // Sort by Y so we process top-most first (they stay, others push against them)
      var sorted = rects.OrderByDescending(r => r.YY).ToList();

      for (int i = 0; i < sorted.Count; i++)
      {
        var rect = sorted[i];
        float newY = bounds.YY; // Start at top edge of container

        // Find the bottom-most edge of any rect above that overlaps in X
        for (int j = 0; j < i; j++)
        {
          var other = sorted[j];
          if (OverlapsVertically(rect, other))
          {
            newY = Math.Max(newY, other.PNodeBottom);
          }
        }

        // Update the rect
        sorted[i] = new PNode(rect.XX, newY, rect.Width, rect.Height);
      }

      rects.Clear();
      rects.AddRange(sorted);
    }
    
    private bool OverlapsVertically(PNode a, PNode b)
    {
      return a.YY < b.PNodeBottom && a.PNodeBottom > b.YY;
    }
    
    private bool OverlapsHorizontally(PNode a, PNode b)
    {
      return a.XX < b.PNodeRight && a.PNodeRight > b.XX;
    }
     */


    //************************************************************************************************************************************


    /*
    List<List<PNode>> GroupByY(List<PNode> rects)
    {
      var groups = new List<List<PNode>>();

      foreach (var r in rects)
      {
        bool added = false;

        foreach (var g in groups)
        {
          foreach (var other in g)
          {
            bool overlapY = Overlap(r, other);

            if (overlapY)
            {
              g.Add(r);
              added = true;
              break;
            }
          }
          if (added) break;
        }

        if (!added)
          groups.Add(new List<PNode> { r });
      }

      return groups;
    }
    void PushLeftStick(List<PNode> rects)
    {
      var groups = GroupByY(rects);

      foreach (var group in groups)
      {
        // Sort left → right
        group.Sort((a, b) => a.XX.CompareTo(b.XX));

        // Pack tightly inside group
        float currentX = 0;

        foreach (var r in group)
        {
          r.Rectangle.Position.x = currentX;
          currentX += r.Width;
        }
      }
    }
    List<List<PNode>> GroupByX(List<PNode> rects)
    {
      var groups = new List<List<PNode>>();

      foreach (var r in rects)
      {
        bool added = false;

        foreach (var g in groups)
        {
          foreach (var other in g)
          {
            bool overlapX = Overlap(r, other);

            if (overlapX)
            {
              g.Add(r);
              added = true;
              break;
            }
          }
          if (added) break;
        }

        if (!added)
          groups.Add(new List<PNode> { r });
      }

      return groups;
    }

    void PushUpStick(List<PNode> rects)
    {
      var groups = GroupByX(rects);

      foreach (var group in groups)
      {
        group.Sort((a, b) => a.YY.CompareTo(b.YY));

        float currentY = 0;

        foreach (var r in group)
        {
          r.Rectangle.Position.y = currentY;
          currentY += r.Height;
        }
      }
    }
     */

    //************************************************************************************************************************************
    /*
    // the same as below without while 
    void PushLeftStick(List<PNode> rects)
    {
      float minGap = float.MaxValue;

      foreach (var r in rects)
      {
        foreach (var other in rects)
        {
          if (r == other) continue;

          // must overlap in Y
          bool overlapY = Overlap(r, other);

          if (!overlapY) continue;

          // r is to the right of other
          if (r.XX > other.PNodeRight)
          {
            float gap = r.XX - other.PNodeRight;
            if (gap > 0)
              minGap = Math.Min(minGap, gap);
          }
        }
      }

      // also consider wall
      foreach (var r in rects)
      {
        minGap = Math.Min(minGap, r.XX);
      }

      if (minGap == float.MaxValue)
        return;

      // move ALL rectangles together
      foreach (var r in rects)
      {
        r.Rectangle.Position.x -= minGap;
      }
    }

    void PushUpStick(List<PNode> rects)
    {
      float minGap = float.MaxValue;

      foreach (var r in rects)
      {
        foreach (var other in rects)
        {
          if (r == other) continue;

          bool overlapX = Overlap(r, other);

          if (!overlapX) continue;

          if (r.YY > other.PNodeBottom)
          {
            float gap = r.YY - other.PNodeBottom;
            if (gap > 0)
              minGap = Math.Min(minGap, gap);
          }
        }
      }

      foreach (var r in rects)
      {
        minGap = Math.Min(minGap, r.YY);
      }

      if (minGap == float.MaxValue)
        return;

      foreach (var r in rects)
      {
        r.Rectangle.Position.y -= minGap;
      }
    }
     */
    //************************************************************************************************************************************
    /*
     the same as below and works with hasfullcolumn and hasfullrow 
    void PushUpStick(List<PNode> rects, float bigHeight)
    {
      while (true)
      {
        float minShift = float.MaxValue;

        foreach (var r in rects)
        {
          float shift = r.YY; // distance to bottom wall

          foreach (var other in rects)
          {
            if (r == other) continue;

            // Only consider rectangles below with X overlap
            bool overlapX = !(r.PNodeRight <= other.XX || r.XX >= other.PNodeRight);

            if (overlapX && other.YY < r.YY)
            {
              float dist = r.YY - (other.YY + other.Height);
              shift = Math.Min(shift, dist);
            }
          }

          minShift = Math.Min(minShift, shift);
        }

        if (minShift <= 0)
          break;

        // Move ALL rectangles at once (exact jump)
        foreach (var r in rects)
        {
          r.Rectangle.Position.y -= minShift;
        }

        // Stop EARLY if your condition is satisfied
        if (HasFullColumn(rects, bigHeight))
          break;
      }
    }

    void PushLeftStick(List<PNode> rects, float bigWidth)
    {
      while (true)
      {
        float minShift = float.MaxValue;

        foreach (var r in rects)
        {
          float shift = r.XX; // distance to left wall

          foreach (var other in rects)
          {
            if (r == other) continue;

            bool overlapY = Overlap(r, other);

            if (overlapY && other.XX < r.XX)
            {
              float dist = r.XX - (other.XX + other.Width);
              shift = Math.Min(shift, dist);
            }
          }

          minShift = Math.Min(minShift, shift);
        }

        if (minShift <= 0)
          break;

        foreach (var r in rects)
        {
          r.Rectangle.Position.x -= minShift;
        }

        if (HasFullRow(rects, bigWidth))
          break;
      }
    }
     */
    //************************************************************************************************************************************
    /*
    bool HasFullColumn(List<PNode> rects, float bigHeight)
    {
      var xs = new HashSet<float>();

      foreach (var r in rects)
      {
        xs.Add(r.XX);
        xs.Add(r.PNodeRight);
      }

      foreach (var x in xs)
      {
        var intervals = new List<(float start, float end)>();

        foreach (var r in rects)
        {
          // Rectangle crosses this vertical line
          if (r.XX <= x && r.PNodeRight > x)
          {
            intervals.Add((r.YY, r.PNodeBottom));
          }
        }

        if (intervals.Count == 0) continue;

        // Sort bottom → top
        intervals.Sort((a, b) => a.start.CompareTo(b.start));

        float covered = 0;

        foreach (var (start, end) in intervals)
        {
          if (start > covered)
            break;

          covered = Math.Max(covered, end);

          if (covered >= bigHeight)
            return true; // full column found
        }
      }

      return false;
    }
    bool HasFullRow(List<PNode> rects, float bigWidth)
    {
      // Collect all Y-levels to test
      var ys = new HashSet<float>();

      foreach (var r in rects)
      {
        ys.Add(r.YY);
        ys.Add(r.PNodeBottom);
      }

      foreach (var y in ys)
      {
        var intervals = new List<(float start, float end)>();

        foreach (var r in rects)
        {
          if (r.YY <= y && r.PNodeBottom > y)
          {
            intervals.Add((r.XX, r.PNodeRight));
          }
        }

        if (intervals.Count == 0) continue;

        // Sort intervals
        intervals.Sort((a, b) => a.start.CompareTo(b.start));

        float covered = 0;

        foreach (var (start, end) in intervals)
        {
          if (start > covered)
            break;

          covered = Math.Max(covered, end);

          if (covered >= bigWidth)
            return true;
        }
      }

      return false;
    }
    void PushLeftStick(List<PNode> rects, float bigWidth)
    {
      while (true)
      {
        // Move by small step (important!)
        float step = 1.0f;

        foreach (var r in rects)
          r.Rectangle.Position.x -= step;

        // Prevent overlap (simple correction)
        foreach (var r in rects)
        {
          if (r.XX < 0)
            r.Rectangle.Position.x = 0;
        }

        // Stop condition
        if (HasFullRow(rects, bigWidth))
          break;
      }
    }


    void PushUpStick(List<PNode> rects, float bigHeight)
    {
      while (true)
      {
        float minShift = float.MaxValue;

        foreach (var r in rects)
        {
          // Distance to bottom wall (y = 0)
          float shift = r.YY;

          foreach (var other in rects)
          {
            if (r == other) continue;

            // Check horizontal overlap
            bool overlapX = Overlap(r, other);

            if (overlapX && other.YY < r.YY)
            {
              float dist = r.YY - (other.YY + other.Height);
              shift = Math.Min(shift, dist);
            }
          }

          minShift = Math.Min(minShift, shift);
        }

        if (minShift <= 0)
          break;

        // Move ALL rectangles together
        foreach (var r in rects)
        {
          r.Rectangle.Position.y -= minShift;
        }

        //  STOP EARLY when a full column exists
        if (HasFullColumn(rects, bigHeight))
          break;
      }
    }

     */

    //************************************************************************************************************************************
    /*
     
    void PushLeftStick(List<PNode> rects)
    {
      while (true)
      {
        float minShift = float.MaxValue;

        foreach (var r in rects)
        {
          // Distance to left wall
          float shift = r.XX;

          foreach (var other in rects)
          {
            if (r == other) continue;

            // Check vertical overlap
            bool overlapY = Overlap(r,other);

            if (overlapY && other.XX < r.XX)
            {
              float dist = r.XX - (other.PNodeRight);
              shift = Math.Min(shift, dist);
            }
          }

          minShift = Math.Min(minShift, shift);
        }

        if (minShift <= 0)
          break;

        // Move ALL rectangles together
        foreach (var r in rects)
        {
          r.Rectangle.Position.x -= minShift;
        }
      }
    }

    void PushUpStick(List<PNode> rects)
    {
      while (true)
      {
        float minShift = float.MaxValue;

        foreach (var r in rects)
        {
          float shift = r.YY;

          foreach (var other in rects)
          {
            if (r == other) continue;

            bool overlapX = Overlap(r, other);

            if (overlapX && other.YY < r.YY)
            {
              float dist = r.YY - (other.YY + other.Height);
              shift = Math.Min(shift, dist);
            }
          }

          minShift = Math.Min(minShift, shift);
        }

        if (minShift <= 0)
          break;

        foreach (var r in rects)
        {
          r.Rectangle.Position.y -= minShift;
        }
      }
    }
     */


    //************************************************************************************************************************************
    //in use
    public List<PNode> Subtract(PNode a, PNode b)
    {
      var result = new List<PNode>();

      // No overlap
      if (b.XX >= a.PNodeRight || b.PNodeRight <= a.XX ||
          b.YY >= a.PNodeBottom || b.PNodeBottom <= a.YY)
      {
        result.Add(a);
        return result;
      }

      // Top
      if (b.YY > a.YY)
        result.Add(new PNode(new Vector2( a.XX, a.YY), new Vector2( a.Rectangle.Size.x, b.YY - a.YY)));

      // Bottom
      if (b.PNodeBottom < a.PNodeBottom)
        result.Add(new PNode(new Vector2(a.XX, b.PNodeBottom), new Vector2(a.Width, a.PNodeBottom - b.PNodeBottom)));

      // Left
      if (b.XX > a.XX)
        result.Add(new PNode(new Vector2(a.XX, Math.Max(a.YY, b.YY)),
            new Vector2(b.XX - a.XX,
            Math.Min(a.PNodeBottom, b.PNodeBottom) - Math.Max(a.YY, b.YY))));

      // Right
      if (b.PNodeRight < a.PNodeRight)
        result.Add(new PNode(new Vector2(b.PNodeRight, Math.Max(a.YY, b.YY)),
            new Vector2(a.PNodeRight - b.PNodeRight,
            Math.Min(a.PNodeBottom, b.PNodeBottom) - Math.Max(a.YY, b.YY))));

      return result;
    }

    public List<PNode> FindEmpty(PNode big, List<PNode> filled)
    {
      var empty = new List<PNode> { big };

      foreach (var r in filled)
      {
        var newEmpty = new List<PNode>();

        foreach (var e in empty)
        {
          newEmpty.AddRange(Subtract(e, r));
        }

        empty = newEmpty;
      }

      return empty;
    }

    //************************************************************************************************************************************
    //Garbage
    public void detectAndSetRest(PNode parent)
    {
      var empties = FindEmpty(parent, parent.Rests).Where(empty => empty.Rectangle.Size.x > 0 && empty.Rectangle.Size.y > 0).ToList();

      empties.ForEach(empty =>
      {
        if (empty.Rectangle.Size.x > 0 && empty.Rectangle.Size.y > 0)
        {
          PNode rest = new PNode(empty.Rectangle.Position, empty.Rectangle.Size, parent);
          parent.Rests.Add(rest);
          FreeLeaves.Add(rest);
        }
      });
    }


    //************************************************************************************************************************************
    // garbage
    public void MergeEmptyNodes(PNode parent)
    {

      var mergedempties = ExpandAll(FreeLeaves.ToList(), parent);

      Debug.Log("merged empties count: " + mergedempties.Count);

    }

    bool Intersects(PNode a, PNode b)
    {
      return !(a.PNodeRight <= b.XX || b.PNodeRight <= a.XX ||
               a.PNodeBottom <= b.YY || b.PNodeBottom <= a.YY);
    }

    PNode Expand(PNode r, List<PNode> others, PNode bounds)
    {
      float left = r.XX;
      float right = r.PNodeRight;
      float top = r.YY;
      float bottom = r.PNodeBottom;

      bool expanded;

      do
      {
        expanded = false;

        // Try expand UP
        float newTop = top - 1;
        if (newTop >= bounds.YY)
        {
          var test = new PNode(left, newTop, right - left, bottom - newTop);
          if (!others.Any(o => o != r && Intersects(test, o)))
          {
            top = newTop;
            expanded = true;
          }
        }

        // Try expand DOWN
        float newBottom = bottom + 1;
        if (newBottom <= bounds.PNodeBottom)
        {
          var test = new PNode(left, top, right - left, newBottom - top);
          if (!others.Any(o => o != r && Intersects(test, o)))
          {
            bottom = newBottom;
            expanded = true;
          }
        }

        // Try expand LEFT
        float newLeft = left - 1;
        if (newLeft >= bounds.XX)
        {
          var test = new PNode(newLeft, top, right - newLeft, bottom - top);
          if (!others.Any(o => o != r && Intersects(test, o)))
          {
            left = newLeft;
            expanded = true;
          }
        }

        // Try expand RIGHT
        float newRight = right + 1;
        if (newRight <= bounds.PNodeRight)
        {
          var test = new PNode(left, top, newRight - left, bottom - top);
          if (!others.Any(o => o != r && Intersects(test, o)))
          {
            right = newRight;
            expanded = true;
          }
        }

      } while (expanded);

      return new PNode(left, top, right - left, bottom - top);
    }

    List<PNode> ExpandAll(List<PNode> rects, PNode bounds)
    {
      var result = new List<PNode>();

      foreach (var r in rects)
      {
        var expanded = Expand(r, rects, bounds);
        result.Add(expanded);
      }

      return result;
    }


    //************************************************************************************************************************************
    //garbage
    bool IsPerfectRectangle(List<PNode> rects)
    {
      float minX = rects.Min(r => r.XX);
      float minY = rects.Min(r => r.YY);
      float maxX = rects.Max(r => r.PNodeRight);
      float maxY = rects.Max(r => r.PNodeBottom);

      float boundingArea = (maxX - minX) * (maxY - minY);
      float sumArea = rects.Sum(r => r.Width * r.Height);

      if (Math.Abs(boundingArea - sumArea) > 0.0001f)
        return false;

      // Check for overlaps (optional but safer)
      for (int i = 0; i < rects.Count; i++)
      {
        for (int j = i + 1; j < rects.Count; j++)
        {
          if (Overlap(rects[i], rects[j]))
            return false;
        }
      }

      return true;
    }

    public bool Overlap(PNode a, PNode b)
    {
      return !(a.PNodeRight <= b.XX || b.PNodeRight <= a.XX ||
               a.PNodeBottom <= b.YY || b.PNodeBottom <= a.YY);
    }

    (List<PNode> group, PNode merged)? FindBestMerge(List<PNode> rects)
    {
      int n = rects.Count;
      float bestArea = 0;
      List<PNode> bestGroup = null;
      PNode bestRect = null;

      // Try all subsets (2^n) → OK for small n
      for (int mask = 1; mask < (1 << n); mask++)
      {
        var group = new List<PNode>();

        for (int i = 0; i < n; i++)
        {
          if ((mask & (1 << i)) != 0)
            group.Add(rects[i]);
        }

        if (group.Count < 2) continue;

        if (!IsPerfectRectangle(group)) continue;

        float minX = group.Min(r => r.XX);
        float minY = group.Min(r => r.YY);
        float maxX = group.Max(r => r.PNodeRight);
        float maxY = group.Max(r => r.PNodeBottom);

        float area = (maxX - minX) * (maxY - minY);

        if (area > bestArea)
        {
          bestArea = area;
          bestGroup = group;
          bestRect = new PNode(minX, minY, maxX - minX, maxY - minY);
        }
      }

      if (bestGroup == null) return null;

      return (bestGroup, bestRect);
    }

    List<PNode> MergeAll(List<PNode> rects)
    {
      while (true)
      {
        var result = FindBestMerge(rects);

        if (result == null)
          break;

        var (group, merged) = result.Value;

        // Remove old rectangles
        foreach (var r in group)
          rects.Remove(r);

        // Add merged rectangle
        rects.Add(merged);
      }

      return rects;
    }


    //************************************************************************************************************************************

    //garbage
    List<PNode> MergeRectangles(List<PNode> rects)
    {
      bool mergedSomething;

      do
      {
        mergedSomething = false;

        PNode bestA = null, bestB = null, bestMerged = null;
        float bestScore = float.MaxValue;

        for (int i = 0; i < rects.Count; i++)
        {
          for (int j = i + 1; j < rects.Count; j++)
          {
            if (CanMerge(rects[i], rects[j], out PNode merged))
            {
              float score =
                  Squareness(merged) * 10   // prefer square
                  - (merged.Width * merged.Height); // prefer big

              if (score < bestScore)
              {
                bestScore = score;
                bestA = rects[i];
                bestB = rects[j];
                bestMerged = merged;
              }
            }
          }
        }

        if (bestMerged != null)
        {
          rects.Remove(bestA);
          rects.Remove(bestB);
          rects.Add(bestMerged);
          mergedSomething = true;
        }

      } while (mergedSomething);

      return rects;
    }

    float Squareness(PNode r)
    {
      float ratio = r.Width / r.Height;
      if (ratio < 1) ratio = 1 / ratio;

      return ratio; // 1 = perfect square, higher = worse
    }

    bool CanMerge(PNode a, PNode b, out PNode merged)
    {
      merged = null;

      // Horizontal merge
      if (a.YY == b.YY && a.Height == b.Height)
      {
        if (a.PNodeRight == b.XX)
        {
          merged = new PNode(new Vector2(a.XX, a.YY), new Vector2(a.Width + b.Width, a.Height));
          return true;
        }
        if (b.PNodeRight == a.XX)
        {
          merged = new PNode(new Vector2(b.XX, b.YY), new Vector2(a.Width + b.Width, a.Height));
          return true;
        }
      }

      // Vertical merge
      if (a.XX == b.XX && a.Width == b.Width)
      {
        if (a.PNodeBottom == b.YY)
        {
          merged = new PNode(new Vector2(a.XX, a.YY), new Vector2(a.Width, a.Height + b.Height));
          return true;
        }
        if (b.PNodeBottom == a.YY)
        {
          merged = new PNode(new Vector2(a.XX, b.YY), new Vector2(a.Width, a.Height + b.Height));
          return true;
        }
      }

      return false;
    }

    //************************************************************************************************************************************

    private void ShiftSubtree(PNode node, float dx, float dy, PNode restNode = null)
    {
      foreach (var n in PTree.Traverse1(node))
      {
        n.Rectangle.Position.x += dx;
        n.Rectangle.Position.y += dy;
      }
    }

    private void ShiftSubtree1(float dx, float dy, List<PNode> restNodes = null)
    {
      foreach (var n in restNodes)
      {
        ShiftSubtree(n, dx, dy);
      }

    }

    //************************************************************************************************************************************

    public void GrowLeaf(PNode leaf, Vector3 newScale)
    {
      var oldSize = leaf.Rectangle.Size;
      leaf.Rectangle.Size = new Vector2(
          newScale.x,
          newScale.z
      );
      var deltaSize = leaf.Rectangle.Size - oldSize;

      PropagateGrowUp(leaf, deltaSize);
    }
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
      bool isLeftRight = right.Rectangle.Position.x != left.Rectangle.Position.x + left.Rectangle.Size.x;
      bool restNeeded = false;
      bool RestIsNull = parent.Rest == null;
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
        if (isLeftRight && isLeft && delta.x < 0 || delta.x > 0)
        {
          sibling.Rectangle.Position.x += delta.x;
          sibling.Rectangle.Size.x -= delta.x;
        }
        else if (isLeftRight && isLeft && delta.y < 0 || delta.y > 0)
        {
          sibling.Rectangle.Size.y += delta.y;
        }
        else if (isLeftRight && !isLeft && delta.x < 0 || delta.x > 0)
        {
        }
        else if (isLeftRight && !isLeft && delta.y < 0 || delta.y > 0)
        {
          left.Rectangle.Size.y += delta.y;
        }
        else if (!isLeftRight && isLeft && delta.x < 0 || delta.x > 0)
        {
          sibling.Rectangle.Size.x += delta.x;
        }
        else if (!isLeftRight && isLeft && delta.y < 0 || delta.y > 0)
        {
          sibling.Rectangle.Position.y += delta.y;
          sibling.Rectangle.Size.y -= delta.y;
        }
        else if (!isLeftRight && !isLeft && delta.x < 0 || delta.x > 0)
        {
          left.Rectangle.Size.x += delta.x;
        }
        else if (!isLeftRight && !isLeft && delta.y < 0 || delta.y > 0)
        {
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
      float newDeltaX = 0f;
      float newDeltaY = 0f;
      GetCoverectArea(sibling, out Vector2 coverecOfSibling);
      float diffX = sibling.Rectangle.Size.x - coverecOfSibling.x;
      float diffY = sibling.Rectangle.Size.y - coverecOfSibling.y;
      Vector2 oldWorstCaseSize = sibling.Rectangle.Size;

      if (isLeftRight && isLeft && sibling != null && !FreeLeaves.Contains(sibling) && sibling.Occupied == false)
      {
        /*
        //resize sibling subtree in y
        // test it with FreeLeavesAdjust 
        Debug.Log("**************************sibling.y before " + sibling.Rectangle.Size.y);
        //Debug.Log("**************************sibling.y after " + sibling.Rectangle.Size.y + " delta.y " + delta.y);
        //delta.y -= diffY;
        //Debug.Log("***************************delta.y after resize sibling: " + diffY + " " + delta.y);
         */
        if (delta.x < 0)
        {
          newDeltaX = delta.x;
          ShiftSubtree(sibling, delta.x, 0f);
        }
        if (delta.x > 0)
        {
          newDeltaX = diffX - delta.x;
          if (newDeltaX == 0f) delta.x = 0f;
          ShiftSubtree(sibling, Mathf.Min(delta.x, diffX), 0f);
          ResizeSubtree(sibling, new Vector2(-Mathf.Min(delta.x, diffX), 0f));
        }
        if (delta.y < 0)
        {
          newDeltaY = diffY - Mathf.Abs(delta.y);
          if (newDeltaY == 0f) { }
          else if (newDeltaY < 0) { restNeeded = true; newDeltaY = 0f; }
          else if (newDeltaY > 0) newDeltaY = 0f;
          ResizeSubtree(sibling, new Vector2(0f, -Mathf.Min(Mathf.Abs(delta.y), diffY)));
        }
        if (delta.y > 0)
        {
          newDeltaY += delta.y;
          if (RestIsNull) restNeeded = true;
        }

        /*
        //Garbage
        if (delta.y > 0)
        {
          sibling.Rectangle.Size.x -= Mathf.Min(Mathf.Abs(delta.x), diffX);
          FreeLeavesAdjust(oldWorstCaseSize, sibling);
        } else if (delta.y > 0)
        {
          sibling.Rectangle.Size.y = node.Rectangle.Size.y;
          FreeLeavesAdjust(oldWorstCaseSize, sibling);
        }
         */
      }
      else if (isLeftRight && !isLeft && sibling != null && !FreeLeaves.Contains(sibling) && sibling.Occupied == false)
      {
        if (delta.x < 0)
        {
          newDeltaX = delta.x;
        }
        if (delta.x > 0)
        {
          newDeltaX = delta.x;
        }
        if (delta.y < 0)
        {
          newDeltaY = diffY - Mathf.Abs(delta.y);
          ResizeSubtree(sibling, new Vector2(0f, -Mathf.Min(Mathf.Abs(delta.y), diffY)));
          if (newDeltaY < 0f) { restNeeded = true; newDeltaY = 0; }
          if (newDeltaY > 0f) { }


        }
        if (delta.y > 0)
        {
          restNeeded = true;
          newDeltaY = delta.y;
        }

      }

      /*
                //resize sibling subtree in y
                // test it with FreeLeavesAdjust 
                Debug.Log("**************************sibling.y before " + sibling.Rectangle.Size.y);
                //Debug.Log("**************************sibling.y after " + sibling.Rectangle.Size.y + " delta.y " + delta.y);
                //delta.y -= diffY;
                //Debug.Log("***************************delta.y after resize sibling: " + diffY + " " + delta.y);
                 */
      /*
        GetCoverectArea(sibling, out Vector2 sizeOfSibling);
        //var diffY = sibling.Rectangle.Size.y - sizeOfSibling.y;
        //var oldWorstCaseSize = sibling.Rectangle.Size;
        if (delta.y < 0)
        {
          sibling.Rectangle.Size.y -= Mathf.Min(Mathf.Abs(delta.y), diffY);
          FreeLeavesAdjust(oldWorstCaseSize, sibling);
        }
        else if (delta.y > 0)
        {
          sibling.Rectangle.Size.y = node.Rectangle.Size.y;
          FreeLeavesAdjust(oldWorstCaseSize, sibling);
        }
         */
      
      else if (!isLeftRight && isLeft && sibling != null && !FreeLeaves.Contains(sibling) && sibling.Occupied == false) 
      {
        if (delta.x < 0)
        {
          newDeltaX = diffX - Mathf.Abs(delta.x);
          if (newDeltaX < 0f) { restNeeded = true; newDeltaX = -newDeltaX; }
          if (newDeltaX > 0f) { newDeltaX = delta.x; }
          if (newDeltaX == 0f) { newDeltaX = delta.x; }
          ResizeSubtree(sibling, new Vector2(-Mathf.Min(delta.x, diffX), 0f));
        }
        if (delta.x > 0)
        {
          restNeeded = true;

        }
        if (delta.y < 0)
        {
          newDeltaY = delta.y;
          ShiftSubtree(sibling, 0f, delta.y);
        }
        if (delta.y > 0)
        {
          newDeltaY = delta.y;
          ShiftSubtree(sibling, 0f, delta.y);
        }
      }
      
      
      else if (!isLeftRight && !isLeft && sibling != null && !FreeLeaves.Contains(sibling) && sibling.Occupied == false) 
      {
        if (delta.x < 0)
        {
          newDeltaX = diffX - Mathf.Abs(delta.x);
          if (newDeltaX < 0f) { restNeeded = true; newDeltaX = -newDeltaX; }
          if (newDeltaX > 0f) { newDeltaX = delta.x; }
          if (newDeltaX == 0f) { newDeltaX = delta.x; }
          ResizeSubtree(sibling, new Vector2(-Mathf.Min(delta.x, diffX), 0f));
        }
        if (delta.x > 0)
        {
          restNeeded = true;

        }
        if (delta.y < 0)
        {
          newDeltaY = delta.y;
        }
        if (delta.y > 0)
        {
          newDeltaY = delta.y;
        }

      }

      /*
      else if (!isLeftRight && sibling != null && !FreeLeaves.Contains(sibling) && sibling.Occupied == false)
      {
        GetCoverectArea(sibling, out Vector2 sizeOfSibling);
        //var diffX = sibling.Rectangle.Size.x - sizeOfSibling.x;
        //var oldWorstCaseSize = sibling.Rectangle.Size;
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
       
       */
        /*
        //resize sibling subtree in x
        // test it with FreeLeavesAdjust 
        //delta.x -= diffX;
        Debug.Log("***************************delta.x after resize sibling: " + diffX);
         */

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
          ShiftSubtree(sibling, delta.x, delta.y, parent.Rest);
        }
        else if (isLeft)
        {
          ShiftSubtree(sibling, delta.x, delta.y);
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

    private void GetCoverectArea(PNode node, out Vector2 coverecOfSibling)
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
      coverecOfSibling = new Vector2(maxX, maxY);
    }
    
    private void ResizeSubtree(PNode node, Vector2 delta)
    {
      foreach (var n in PTree.Traverse(node))
      {
        if (n.Occupied == false)
        {
          n.Rectangle.Size.x += delta.x;
          n.Rectangle.Size.y += delta.y;
        }
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

    public static IEnumerable<PNode> Traverse1(PNode node)
    {
      if (node == null)
        yield break;

      yield return node;

      foreach (var n in node.Rests)
      {
        foreach (var child in Traverse1(n))
        {
          yield return child;
        }
      }
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
        Root.Rectangle.Size = Root.Rectangle.Size + 1.1f * size;
        FreeLeaves = FindEmpty(Root, Root.Rests);
        //FreeLeavesAdjust(oldWorstCaseSize);
        //result = (List<PNode>)GetSufficientlyLargeLeaves(size, oldWorstCaseSize);
        foreach (PNode leaf in FreeLeaves)
        {
          if (FitsInto(size, leaf.Rectangle.Size))
          {
            result.Add(leaf);
          }
        }
        
      }
      if (result.Count == 0)
      {
        Debug.Log("After proper enlargment still no free leave " + coverec + " : " + Root.Rectangle.Size + " : ");
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
        if (leaf.Occupied) Debug.Log("a leaf in FreeLeaves is marked occupied");
        if (FitsInto(size, leaf.Rectangle.Size) && !leaf.Occupied)
        {
          result.Add(leaf);
        }
      }
      return result;
    }

    public IList<PNode> GetSufficientlyLargeLeaves(Vector2 size, List<PNode> freeLeaves)
    {
      List<PNode> result = new();
      foreach (PNode leaf in freeLeaves)
      {
        if (leaf.Occupied) Debug.Log("a leaf in FreeLeaves is marked occupied");
        if (FitsInto(size, leaf.Rectangle.Size) && !leaf.Occupied)
        {
          result.Add(leaf);
        }
      }
      return result;
    }

    public void Print()
    {
      Print(Root, "", true);
    }

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

    public void Print1()
    {
      Print1(Root, "|-");
    }

    public void Print1(PNode node, string indent)
    {
      if (node == null) return;

      if (node.Rests.Count == 0)
      {
        Debug.Log(indent + node.ToString1() + " :" + node.Rectangle.Size + ": " + "\n");
        return;
      }

      Debug.Log(indent + node.ToString1() + " :" + node.Rectangle.Size + ": " + "\n");


      foreach (var n in node.Rests)
      {
        //Debug.Log(indent + "       " + n + " :" + node.Rectangle.Size + ": " + "\n");

        Print1(n, indent+ "       |-");
      }

    }
  }
}
