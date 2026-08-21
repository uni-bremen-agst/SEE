using MoreLinq.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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
                throw new Exception("Node to be split is not a free leaf." + node);
                result = null;
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


        //************************************************************************************************************************************






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
            PNode parent = node.Parent;

            if (delta.x < 0 && delta.y < 0)
            {
                IncrementalRectanglePackingLayout.changedOrDeleted = true;

            }

            if (delta.x < 0)
            {
            }
            if (delta.x > 0)
            {

                List<PNode> siblingsToMove = parent.Rests.Except(new List<PNode>() { node }).Where(r => r.Rectangle.Position.x >= (node.Rectangle.Position.x + node.Rectangle.Size.x - delta.x)).ToList();
                ShiftSubtree1(delta.x, 0f, siblingsToMove);

            }
            if (delta.y < 0)
            {
            }
            if (delta.y > 0)
            {
                List<PNode> siblingsToMove = parent.Rests.Except(new List<PNode>() { node }).Where(r => r.Rectangle.Position.y >= (node.Rectangle.Position.y + node.Rectangle.Size.y - delta.y)).ToList();

                ShiftSubtree1(0, delta.y, siblingsToMove);

            }
            if (delta.x > 0)
            {
                parent.Rectangle.Size.x += delta.x;
            }
            if (delta.y > 0)
            {
                parent.Rectangle.Size.y += delta.y;
            }

        }
        //************************************************************************************************************************************

        public void Tighten(PNode node)
        {
            //Debug.Log("Tightening layout for node: " + node);
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
                result.Add(new PNode(new Vector2(a.XX, a.YY), new Vector2(a.Rectangle.Size.x, b.YY - a.YY)));

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


        private void ShiftSubtree(PNode node, float dx, float dy, PNode restNode = null)
        {
            foreach (var n in PTree.Traverse1(node))
            {
                n.Rectangle.Position.x += dx;
                n.Rectangle.Position.y += dy;
            }
        }

        public void ShiftSubtree1(float dx, float dy, List<PNode> restNodes = null)
        {
            foreach (var n in restNodes)
            {
                ShiftSubtree(n, dx, dy);
            }

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
                Debug.Log("//////////////////////////////////////////////////////////////////////////////enlarged");
                Root.Rectangle.Size = Root.Rectangle.Size + 1.1f * size;
                FreeLeaves = FindEmpty(Root, Root.Rests);
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

                Print1(n, indent + "       |-");
            }

        }
    }
}
