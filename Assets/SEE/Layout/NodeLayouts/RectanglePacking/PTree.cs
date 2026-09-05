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
        /// <param name="leaf">The leaf node to grow.</param>
        /// <param name="newScale">The new scale to apply to the leaf node.</param>
        /// <returns>The grown leaf node.</returns>
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
        /// Propagates a size change from the specified node up through its ancestors, adjusting parent extents and
        /// shifting sibling subtrees as needed to preserve layout constraints.
        /// </summary>
        /// <param name="node">The node whose growth (or shrinkage) is being propagated. If null, the method performs no action.</param>
        /// <param name="delta">The amount of size change to propagate. Positive values indicate growth; negative values indicate shrinkage.</param>
        /// <returns>The same node after propagation; returns null if <paramref name="node"/> was null.</returns>
        /// <remarks>
        /// The method mutates parent nodes' size/bounds to account for <paramref name="delta"/> and may translate sibling nodes to prevent overlaps. Callers should revalidate any layout invariants after propagation.
        /// </remarks>
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
        /// Compacts and tightens the layout of all rectangles contained in the specified node by removing internal gaps
        /// and shifting rectangles so they occupy space more efficiently without changing their sizes.
        /// </summary>
        /// <param name="node">The node whose contained rectangles will be compacted. If null, the method performs no action and returns null.</param>
        /// <returns>
        /// The same node instance after its rectangles have been tightened (position data mutated in-place).
        /// Returns null if <paramref name="node"/> is null.
        /// </returns>
        /// <remarks>
        /// This operation adjusts positions of rectangles within the node (and typically its subtree) to reduce empty space and avoid overlaps while preserving rectangle widths and heights and the tree topology. The exact final arrangement can depend on the input ordering of rectangles; callers that require deterministic results should provide a stable ordering. The method mutates the node and its contained rectangles rather than creating new instances.
        /// </remarks>
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
        /// <param name="a">The first rectangle.</param>
        /// <param name="b">The second rectangle.</param>
        /// <returns>returns true if the rectangles overlap in the Y direction, false otherwise.</returns>
        private bool OverlapY(PNode a, PNode b)
        {
            return !(a.YY >= b.PNodeBottom || a.PNodeBottom <= b.YY);
        }

        /// <summary>
        /// Checks if two rectangles overlap in the X direction.
        /// </summary>
        /// <param name="a">The first rectangle.</param>
        /// <param name="b">The second rectangle.</param>
        /// <returns>returns true if the rectangles overlap in the X direction, false otherwise.</returns>
        private bool OverlapX(PNode a, PNode b)
        {
            return !(a.XX >= b.PNodeRight || a.PNodeRight <= b.XX);
        }

        /// <summary>
        /// Computes the left limit for a rectangle by checking for overlaps with other rectangles and adjusting the limit accordingly.
        /// </summary>
        /// <param name="rect">The rectangle for which to compute the left limit.</param>
        /// <param name="rects">A collection of other rectangles to consider when computing the limit.</param>
        /// <param name="leftBoundary">An initial left boundary for the limit.</param>
        /// <returns>The computed left limit.</returns>
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
        /// Computes a safe top (Y) limit for <paramref name="rect"/> by examining other rectangles and reducing the limit to avoid vertical overlap.
        /// </summary>
        /// <param name="rect">The rectangle whose top limit is being computed (axis-aligned). The method does not modify this rectangle.</param>
        /// <param name="rects">A collection of other rectangles to consider when computing collisions. Rectangles whose horizontal span intersects <paramref name="rect"/> are used to lower the top limit.</param>
        /// <param name="topBoundary">An initial upper bound for the top coordinate (for example the container's top). The returned limit will not exceed this value.</param>
        /// <returns>
        /// A float representing the computed top coordinate (Y) that keeps <paramref name="rect"/> from overlapping any intersecting rectangles in <paramref name="rects"/> and does not exceed <paramref name="topBoundary"/>.
        /// </returns>
        /// <remarks>
        /// The method typically inspects rectangles whose X ranges intersect <paramref name="rect"/>, finds the nearest blocking top edge, and returns the largest Y value that avoids overlap. It treats rectangles as closed axis-aligned boxes and does not merge or modify the input rectangles.
        /// </remarks>
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
        /// Compacts the provided rectangles inside the specified bounding rectangle by shifting them leftward and upward
        /// as far as possible without causing overlaps or leaving the bounds.
        /// </summary>
        /// <param name="rects">A collection of rectangles to compact. Each rectangle's position is modified in-place.</param>
        /// <param name="bounds">The bounding rectangle that all rects must remain inside after compaction.</param>
        /// <remarks>
        /// The method iteratively translates rectangles toward the top-left (decreasing X and Y) while preserving non-overlap and containment within <paramref name="bounds"/>. The final arrangement may depend on the input ordering of <paramref name="rects"/>; callers who require deterministic results should provide a stable ordering. This operation mutates the rectangles and does not allocate replacement instances.
        /// </remarks>
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
        /// Subtracts rectangle <paramref name="b"/> from rectangle <paramref name="a"/> and returns the axis-aligned subrectangles that remain inside <paramref name="a"/> but outside <paramref name="b"/>.
        /// </summary>
        /// <param name="a">The source rectangle to subtract from (the universe). Must be axis-aligned.</param>
        /// <param name="b">The rectangle to remove from <paramref name="a"/>. Must be axis-aligned.</param>
        /// <returns>
        /// A list of rectangles representing the non-overlapping portions of <paramref name="a"/> after <paramref name="b"/> is removed. The returned rectangles are contained within <paramref name="a"/>, do not intersect <paramref name="b"/>, and collectively cover the area of <paramref name="a"/> \ <paramref name="b"/> (subject to the method's splitting strategy).
        /// </returns>
        /// <remarks>
        /// The method does not modify its input rectangles. If <paramref name="b"/> does not intersect <paramref name="a"/>, the result contains a single rectangle equal to <paramref name="a"/>. If <paramref name="b"/> completely covers <paramref name="a"/>, the result is an empty list. For partial overlap the implementation typically produces up to four rectangular pieces (left, right, top, bottom) depending on the overlap shape; adjacent pieces are not merged. The exact ordering of returned rectangles is unspecified.
        /// </remarks>
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
        /// Computes the set of axis-aligned empty sub-rectangles that remain inside <paramref name="big"/>
        /// after removing the areas covered by <paramref name="filled"/>.
        /// </summary>
        /// <param name="big">The containing rectangle from which filled areas are subtracted. Treated as the universe for the computation.</param>
        /// <param name="filled">A collection of rectangles that occupy space inside <paramref name="big"/> and should be removed. Rectangles may overlap; the method treats their union as occupied.</param>
        /// <returns>
        /// A list of rectangles representing the empty (unoccupied) regions inside <paramref name="big"/> after subtraction. /// The returned rectangles are axis-aligned, contained within <paramref name="big"/>, and do not overlap the union of <paramref name="filled"/> (implementation may or may not merge adjacent empty regions).
        /// </returns>
        /// <remarks>
        /// The method does not modify the input rectangles. If <paramref name="filled"/> is empty or all filled rectangles lie outside <paramref name="big"/>, the result will contain <paramref name="big"/> (or an equivalent covering). If the union of <paramref name="filled"/> completely covers <paramref name="big"/>, an empty list is returned. Callers should not rely on a specific ordering of the returned rectangles.
        /// </remarks>
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
        /// Translates the subtree rooted at <paramref name="node"/> by the specified offsets.
        /// </summary>
        /// <param name="node">
        /// The root node of the subtree to shift. If null, the method performs no action.
        /// </param>
        /// <param name="dx">Amount to shift in the X direction (added to each node's X coordinate).</param>
        /// <param name="dy">Amount to shift in the Y direction (added to each node's Y coordinate).</param>
        /// <param name="restNode">An auxiliary node or context that should also be updated as part of the shift (semantics depend on the implementation). May be null.</param>
        /// <remarks>
        /// The method mutates position data in-place for <paramref name="node"/> and all of its descendants (recursive translation). It does not change subtree topology or sizes. Callers should ensure the nodes belong to the expected tree and handle concurrency if necessary.
        /// </remarks>
        private void ShiftSubtreeHelper(PNode node, float dx, float dy, PNode restNode = null)
        {
            foreach (PNode n in PTree.Traverse(node))
            {
                n.Rectangle.Position.x += dx;
                n.Rectangle.Position.y += dy;
            }
        }

        /// <summary>
        /// Translates each subtree rooted at the nodes in <paramref name="restNodes"/> by the specified offsets.
        /// </summary>
        /// <param name="dx">The amount to shift in the X direction.
        /// </param>
        /// <param name="dy">The amount to shift in the Y direction.
        /// </param>
        /// <param name="restNodes">
        /// A collection of root nodes whose entire subtrees will be translated. Nodes that are null are ignored.
        /// </param>
        /// <returns>
        /// A list of the nodes that were translated.
        /// </returns>
        /// <remarks>
        /// For each node in <paramref name="restNodes"/>, this method adds <paramref name="dx"/> to the X coordinate and
        /// <paramref name="dy"/> to the Y coordinate of the node and recursively applies the same translation to all its descendants.
        /// The operation mutates the nodes' position data in-place and does not modify node sizes, topology, or return a new tree.
        /// Callers should ensure the nodes belong to the expected tree and manage concurrency if the tree may be accessed from
        /// multiple threads.
        /// </remarks>
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
        /// Performs a depth-first traversal starting at the specified node and yields every visited node.
        /// </summary>
        /// <param name="node">
        /// The node at which to start the traversal. If null, the method yields no nodes.
        /// </param>
        /// <returns>
        /// An enumeration that yields nodes in depth-first order (pre-order: the current node is yielded before its children).
        /// </returns>
        /// <remarks>
        /// Child nodes are visited in the order provided by the node's child collection.
        /// The traversal is read-only and does not modify the tree. The enumeration is produced lazily as nodes are visited.
        /// </remarks>
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
        public IList<PNode> GetSufficientlyLargeLeavesIncremental(Vector2 size)
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
                Debug.LogError($"After proper enlargment still no free leave {Coverec} : {Root.Rectangle.Size} : ");
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
        /// Prints the tree to the console. Can be used for debugging.
        /// </summary>
        public void Print()
        {
            Print(Root, "", true);
        }

        /// <summary>
        /// Prints the tree rooted by <paramref name="node"/> to the console. Can be used for debugging.
        /// </summary>
        /// <param name="node">The root of the tree to be printed.</param>
        /// <param name="indent">Indentation before the node is printed.</param>
        /// <param name="last">Whether this is the last node to be printed.</param>
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
