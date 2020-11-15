using System.Collections.Generic;
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
            FreeLeaves = new List<PNode>();
            FreeLeaves.Add(Root);
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
                throw new System.Exception("Node to be split is not a free leaf.");
            }
            else if (size.x > node.rectangle.size.x || size.y > node.rectangle.size.y)
            {
                throw new System.Exception("Requested size does not fit into this rectangle.");
            }
            else if (size.x == node.rectangle.size.x)
            {
                if (size.y == node.rectangle.size.y)
                {
                    // size.x = rectangle.size.x && size.y = rectangle.size.y. Perfect match.
                    node.occupied = true;
                    result = node;
                }
                else
                {
                    // size.x = rectangle.size.x && size.y < rectangle.size.y
                    node.left = new PNode();
                    node.left.rectangle = new PRectangle(node.rectangle.position, size);
                    node.left.occupied = true;

                    node.right = new PNode();
                    node.right.rectangle = new PRectangle(new Vector2(node.rectangle.position.x, node.rectangle.position.y + size.y),
                                                          new Vector2(node.rectangle.size.x, node.rectangle.size.y - size.y));
                    FreeLeaves.Add(node.right);
                    result = node.left;
                }
            }
            else
            {
                // size.x < rectangle.size.x
                if (size.y == node.rectangle.size.y)
                {
                    // size.x < rectangle.size.x && size.y = rectangle.size.y
                    node.left = new PNode();
                    node.left.rectangle = new PRectangle(node.rectangle.position, size);
                    node.left.occupied = true;

                    node.right = new PNode();
                    node.right.rectangle = new PRectangle(new Vector2(node.rectangle.position.x + size.x, node.rectangle.position.y),
                                                          new Vector2(node.rectangle.size.x - size.x, size.y));
                    FreeLeaves.Add(node.right);
                    result = node.left;
                }
                else
                {
                    // size.x < rectangle.size.x && size.y < rectangle.size.y
                    // The node will be split vertically into two sub-rectangles. The upper rectangle is
                    // left and the lower rectangle is right.
                    // The origin of left is the origin of the enclosing rectangle. Its width is the width
                    // of the enclosing rectangle. Its depth is the size of the requested rectangle.

                    node.left = new PNode();
                    node.left.rectangle = new PRectangle(node.rectangle.position, new Vector2(node.rectangle.size.x, size.y));

                    node.right = new PNode();
                    node.right.rectangle = new PRectangle(new Vector2(node.rectangle.position.x, node.rectangle.position.y + size.y),
                                                        new Vector2(node.rectangle.size.x, node.rectangle.size.y - size.y));
                    FreeLeaves.Add(node.right);

                    // The upper enclosed rectangle is split again. Its left rectangle will be the rectangle
                    // requested. Its right rectangle is available.
                    node.left.left = new PNode();
                    // This space is not available anymore.
                    node.left.left.occupied = true;
                    // The allocated rectangle is added at the left upper corner of left node.
                    node.left.left.rectangle = new PRectangle(node.left.rectangle.position, size);

                    // The remaining rectangle sits right of the allocated one and occupies
                    // the remaining space of left.
                    node.left.right = new PNode();
                    node.left.right.rectangle = new PRectangle(new Vector2(node.left.rectangle.position.x + size.x, node.left.rectangle.position.y),
                                                              new Vector2(node.left.rectangle.size.x - size.x, node.left.rectangle.size.y));
                    FreeLeaves.Add(node.left.right);
                    result = node.left.left;
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
        /// Returns all free leaves having at least the requested size.
        /// </summary>
        /// <param name="size">requested size of the rectangle</param>
        /// <returns>all free leaves having at least the requested size</returns>
        public IList<PNode> GetSufficientlyLargeLeaves(Vector2 size)
        {
            List<PNode> result = new List<PNode>();
            foreach (PNode leaf in FreeLeaves)
            {
                if (FitsInto(size, leaf.rectangle.size))
                {
                    result.Add(leaf);
                }
            }
            return result;
        }
    }
}
