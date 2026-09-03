using System;
using MoreLinq.Extensions;
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
        /// <param name="position">Position of the rectangle represented by the root.</param>
        /// <param name="size">Size of the rectangle represented by the root.</param>
        public PTree(Vector2 position, Vector2 size)
        {
            Root = new PNode(position, size);
            FreeLeaves = new List<PNode>
            {
                Root
            };
            Coverec = Vector2.zero;
        }


        /// <summary>
        /// The root of the PTree corresponds to the entire available space, while
        /// each of the other nodes corresponds to a particular partition of the space.
        /// </summary>
        public PNode Root;

        /// <summary>
        /// The rectangle that covers all the rectangles in the tree.
        /// This is used to determine the overall bounds of the tree and to help with layout calculations.
        /// </summary>
        public Vector2 Coverec;

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
        /// The number of attempts made to find sufficiently large leaves.
        /// This is used to prevent infinite loops in the GetSufficientlyLargeLeaves method.
        /// </summary>
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
        public PNode Split(PNode node, Vector2 size)
        {
            PNode result;

            // Node is no longer a free leaf. As a matter of fact, technically, it may
            // still be a leaf if the requested size perfectly matches the size of node,
            // so that it is actually not split, but it is not free.
            if (!FreeLeaves.Remove(node))
            {
                throw new Exception("Node to be split is not a free leaf.");
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
                    node.Left.Direction = PNode.SplitDirection.Left;
                    node.Left.Rectangle = new PRectangle(node.Rectangle.Position, size);
                    node.Left.Occupied = true;

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

        /// <summary>
        /// Finds a node in the tree by its ID.
        /// </summary>
        /// <param name="id">The ID of the node to find.</param>
        /// <returns>The node with the specified ID, or null if not found.</returns>
        public PNode FindNodeById(string id)
        {
            foreach (PNode node in Root.Rests)
            {
                if (node.Id == id)
                {
                    return node;
                }
            }
            return null;
        }

        /// <summary>
        /// Grows the given leaf node to the new scale and propagates the growth upwards in the tree,
        /// adjusting parent nodes and shifting sibling nodes as necessary.
        /// </summary>
        /// <param name="leaf"></param>
        /// <param name="newScale"></param>
        public void GrowLeaf(PNode leaf, Vector3 newScale)
        {

            Vector2 oldSize = leaf.Rectangle.Size;
            leaf.Rectangle.Size = new Vector2(
                newScale.x,
                newScale.z
            );
            Vector2 deltaSize = leaf.Rectangle.Size - oldSize;

            PropagateGrowUp(leaf, deltaSize);
        }

        /// <summary>
        /// Propagates the growth of a node upwards in the tree, adjusting the sizes of parent nodes and shifting sibling nodes as necessary.
        /// </summary>
        /// <param name="node"></param>
        /// <param name="delta"></param>
        public void PropagateGrowUp(PNode node, Vector2 delta)
        {
            PNode parent = node.Parent;
            if (delta.x > 0)
            {
                List<PNode> siblingsToMove = parent.Rests.Except(new List<PNode>() { node }).Where(r => r.Rectangle.Position.x >= (node.Rectangle.Position.x + node.Rectangle.Size.x - delta.x)).ToList();
                ShiftSubtree(delta.x, 0f, siblingsToMove);
            }
            if (delta.y > 0)
            {
                List<PNode> siblingsToMove = parent.Rests.Except(new List<PNode>() { node }).Where(r => r.Rectangle.Position.y >= (node.Rectangle.Position.y + node.Rectangle.Size.y - delta.y)).ToList();
                ShiftSubtree(0, delta.y, siblingsToMove);
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

        /// <summary>
        /// Tightens the rectangles within the given node by compacting them fully.
        /// </summary>
        /// <param name="node"></param>
        public void Tighten(PNode node)
        {
            List<PNode> rects = node.Rests;
            CompactFully(rects, node);
        }

        /// <summary>
        /// A small epsilon value used for floating-point comparisons to avoid precision issues.
        /// </summary>
        const float EPS = 0.0001f;

        /// <summary>
        /// Checks if two rectangles overlap in the Y direction.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        private bool OverlapY(PNode a, PNode b)
        {
            return !(a.YY >= b.PNodeBottom || a.PNodeBottom <= b.YY);
        }

        /// <summary>
        /// Checks if two rectangles overlap in the X direction.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        private bool OverlapX(PNode a, PNode b)
        {
            return !(a.XX >= b.PNodeRight || a.PNodeRight <= b.XX);
        }

        /// <summary>
        /// Computes the left limit for a rectangle by checking for overlaps with other rectangles and adjusting the limit accordingly.
        /// </summary>
        /// <param name="rect"></param>
        /// <param name="rects"></param>
        /// <param name="leftBoundary"></param>
        /// <returns></returns>
        private float ComputeLeftLimit(PNode rect, List<PNode> rects, float leftBoundary)
        {
            float limit = leftBoundary;

            foreach (PNode other in rects)
            {
                if (other == rect) { continue; }

                if (OverlapY(rect, other) && other.PNodeRight <= rect.XX)
                {
                    limit = Math.Max(limit, other.PNodeRight);
                }
            }

            return limit;
        }

        /// <summary>
        /// Computes the top limit for a rectangle by checking for overlaps with other rectangles and adjusting the limit accordingly.
        /// </summary>
        /// <param name="rect"></param>
        /// <param name="rects"></param>
        /// <param name="topBoundary"></param>
        /// <returns></returns>
        private float ComputeTopLimit(PNode rect, List<PNode> rects, float topBoundary)
        {
            float limit = topBoundary;

            foreach (PNode other in rects)
            {
                if (other == rect) { continue; }

                if (OverlapX(rect, other) && other.PNodeBottom <= rect.YY)
                {
                    limit = Math.Max(limit, other.PNodeBottom);
                }
            }

            return limit;
        }

        /// <summary>
        /// Compacts the rectangles within the given bounds by moving them left and up as much as possible without overlapping.
        /// </summary>
        /// <param name="rects"></param>
        /// <param name="bounds"></param>
        public void CompactFully(List<PNode> rects, PNode bounds)
        {
            bool moved;

            do
            {
                moved = false;

                rects = rects.OrderBy(r => r.XX).ThenBy(r => r.YY).ToList();

                foreach (PNode r in rects)
                {
                    float newX = ComputeLeftLimit(r, rects, bounds.XX);

                    if (Math.Abs(newX - r.XX) > EPS)
                    {
                        r.Rectangle.Position.x = newX;
                        moved = true;
                    }
                }

                rects = rects.OrderBy(r => r.YY).ThenBy(r => r.XX).ToList();

                foreach (PNode r in rects)
                {
                    float newY = ComputeTopLimit(r, rects, bounds.YY);

                    if (Math.Abs(newY - r.YY) > EPS)
                    {
                        r.Rectangle.Position.y = newY;
                        moved = true;
                    }
                }
            }
            while (moved);
        }

        /// <summary>
        /// Subtracts rectangle b from rectangle a and returns the resulting non-overlapping rectangles.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public List<PNode> Subtract(PNode a, PNode b)
        {
            List<PNode> result = new List<PNode>();

            if (b.XX >= a.PNodeRight || b.PNodeRight <= a.XX ||
                b.YY >= a.PNodeBottom || b.PNodeBottom <= a.YY)
            {
                result.Add(a);
                return result;
            }

            if (b.YY > a.YY)
            {
                result.Add(new PNode(new Vector2(a.XX, a.YY), new Vector2(a.Rectangle.Size.x, b.YY - a.YY)));
            }

            if (b.PNodeBottom < a.PNodeBottom)
            {
                result.Add(new PNode(new Vector2(a.XX, b.PNodeBottom), new Vector2(a.Width, a.PNodeBottom - b.PNodeBottom)));
            }

            if (b.XX > a.XX)
            {
                result.Add(new PNode(new Vector2(a.XX, Math.Max(a.YY, b.YY)),
                    new Vector2(b.XX - a.XX,
                    Math.Min(a.PNodeBottom, b.PNodeBottom) - Math.Max(a.YY, b.YY))));
            }

            if (b.PNodeRight < a.PNodeRight)
            {
                result.Add(new PNode(new Vector2(b.PNodeRight, Math.Max(a.YY, b.YY)),
                    new Vector2(a.PNodeRight - b.PNodeRight,
                    Math.Min(a.PNodeBottom, b.PNodeBottom) - Math.Max(a.YY, b.YY))));
            }

            return result;
        }

        /// <summary>
        /// Finds the empty rectangles within a larger rectangle after subtracting the filled rectangles.
        /// </summary>
        /// <param name="big"></param>
        /// <param name="filled"></param>
        /// <returns></returns>
        public List<PNode> FindEmpty(PNode big, List<PNode> filled)
        {
            List<PNode> empty = new List<PNode> { big };

            foreach (PNode r in filled)
            {
                List<PNode> newEmpty = new List<PNode>();

                foreach (PNode e in empty)
                {
                    newEmpty.AddRange(Subtract(e, r));
                }

                empty = newEmpty;
            }

            return empty;
        }

        /// <summary>
        /// Shifts the subtree rooted at <paramref name="node"/> by the given amounts in the x and y directions.
        /// </summary>
        /// <param name="node"></param>
        /// <param name="dx"></param>
        /// <param name="dy"></param>
        /// <param name="restNode"></param>
        private void ShiftSubtreeHelper(PNode node, float dx, float dy, PNode restNode = null)
        {
            foreach (PNode n in PTree.Traverse(node))
            {
                n.Rectangle.Position.x += dx;
                n.Rectangle.Position.y += dy;
            }
        }

        /// <summary>
        /// Shifts the subtree rooted at each node in <paramref name="restNodes"/> by the given amounts in the x and y directions.
        /// </summary>
        /// <param name="dx"></param>
        /// <param name="dy"></param>
        /// <param name="restNodes"></param>
        public void ShiftSubtree(float dx, float dy, List<PNode> restNodes = null)
        {
            if (restNodes != null)
            {
                foreach (PNode n in restNodes)
                {
                    ShiftSubtreeHelper(n, dx, dy);
                }
            }
        }

        /// <summary>
        /// Traverses the tree in a depth-first manner, yielding each node in the tree.
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public static IEnumerable<PNode> Traverse(PNode node)
        {
            if (node == null)
            {
                yield break;
            }

            yield return node;

            foreach (PNode n in node.Rests)
            {
                foreach (PNode child in Traverse(n))
                {
                    yield return child;
                }
            }
        }

        /// <summary>
        /// Returns all free leaves having at least the requested size.
        /// </summary>
        /// <param name="size">Requested size of the rectangle.</param>
        /// <returns>All free leaves having at least the requested size.</returns>
        public IList<PNode> GetSufficientlyLargeLeaves(Vector2 size, Vector2 oldWorstCaseSize)
        {
            if (++attempts > 1000)
            {
                throw new InvalidOperationException("No sufficiently large leaves possible.");
            }
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
                Debug.LogError("After proper enlargment still no free leave " + Coverec + " : " + Root.Rectangle.Size + " : ");
            }
            return result;
        }


        /// <summary>
        /// Prints the tree to the console. Can be used for debugging.
        /// Returns all free leaves having at least the requested size.
        /// (relevant for the rectangle packing)
        /// </summary>
        /// <param name="size"></param>
        /// <returns></returns>
        public IList<PNode> GetSufficientlyLargeLeaves(Vector2 size)
        {
            List<PNode> result = new();
            foreach (PNode leaf in FreeLeaves)
            {
                if (leaf.Occupied)
                {
                    Debug.Log("a leaf in FreeLeaves is marked occupied");
                }
                if (FitsInto(size, leaf.Rectangle.Size) && !leaf.Occupied)
                {
                    result.Add(leaf);
                }
            }
            return result;
        }

        /// <summary>
        /// Prints the structure of the PTree to the debug log, showing the hierarchy of nodes and their corresponding rectangles.
        /// </summary>
        public void Print()
        {
            Print(Root, "", true);
        }

        /// <summary>
        /// Prints the structure of the PTree starting from the given node, showing the hierarchy of nodes and their corresponding rectangles.
        /// Helper method for the Print() function.
        /// </summary>
        /// <param name="node"></param>
        /// <param name="indent"></param>
        /// <param name="last"></param>
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

        /// <summary>
        /// Prints the structure of the PTree to the debug log, showing the hierarchy of nodes and their corresponding rectangles.
        /// relevant for incremental rectangle packing
        /// </summary>
        public void PrintA()
        {
            PrintA(Root, "|-");
        }

        /// <summary>
        /// Prints the structure of the PTree starting from the given node, showing the hierarchy of nodes and their corresponding rectangles.
        /// relevant for incremental rectangle packing
        /// helper method for the PrintA() function.
        /// </summary>
        /// <param name="node"></param>
        /// <param name="indent"></param>
        public void PrintA(PNode node, string indent)
        {
            if (node == null) { return; }

            if (node.Rests.Count == 0)
            {
                Debug.Log(indent + node.ToStringNotOverride() + " :" + node.Rectangle.Size + ": " + "\n");
                return;
            }

            Debug.Log(indent + node.ToStringNotOverride() + " :" + node.Rectangle.Size + ": " + "\n");
            foreach (PNode n in node.Rests)
            {
                PrintA(n, indent + "       |-");
            }

        }
    }
}
