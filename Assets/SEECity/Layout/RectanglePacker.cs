using System.Collections.Generic;
using UnityEngine;

using SEE.Layout.RectanglePacking;

namespace SEE.Layout
{
    /// <summary>
    /// Allows one to pack smaller rectangles into a single larger rectangle
    /// so that the contained rectangles do not overlap, are as close together
    /// as possible (with some padding) and the containing rectangle is as
    /// small as possible (no optimal solution is provided). The containing
    /// rectangle is organized in stripes whose aspect ratio is as close to
    /// one as possible. The layout maintains the size and orientation of
    /// all smaller rectangles.
    /// 
    /// The algorithm proposed by Richard Wettel in his dissertation 
    /// "Software Systems as Cities" described on page 36 is used.
    /// </summary>
    public class RectanglePacker : FlatNodeLayout
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="groundLevel">the y co-ordinate setting the ground level; all nodes will be
        /// placed on this level</param>
        /// <param name="leafNodeFactory">the factory used to created leaf nodes</param>
        /// <param name="padding">the padding to be added between neighboring nodes;
        /// the actual value used is padding * leafNodeFactory.Unit()</param>
        public RectanglePacker(float groundLevel, float Unit, float padding = 0.1f)
            : base(groundLevel)
        {
            name = "Rectangle Packing";
            this.padding = padding * Unit;
        }

        private readonly float padding;

        /// <summary>
        /// Returns the area size of given layout node, i.e., its width (x co-ordinate)
        /// multiplied by its depth (z co-ordinate).
        /// </summary>
        /// <param name="layoutNode">node whose size is to be returned</param>
        /// <returns>area size of given layout node</returns>
        private static float AreaSize(LayoutNode layoutNode)
        {
            Vector3 size = layoutNode.GetSize();
            return size.x * size.z;
        }

        /// <summary>
        /// Places the given elements in a minimally sized rectangle without overlapping.
        /// 
        /// Allows one to pack smaller rectangles into a single larger rectangle
        /// so that the contained rectangles do not overlap, are as close together
        /// as possible (with some padding) and the containing rectangle is as
        /// small as possible (no optimal solution is provided). The containing
        /// rectangle is organized in stripes whose aspect ratio is as close to
        /// one as possible. The layout maintains the size and orientation of
        /// all smaller rectangles. The largest contained rectangle appears at the 
        /// left lower corner of the containing rectangle at position (0, groundlevel, 0).
        /// 
        /// Precondition: every node in <paramref name="gameNodes"/> must have been
        /// created by the node factory passed to the constructor.
        /// </summary>
        /// <param name="gameNodes">the game objects to be laid out</param>
        public override Dictionary<LayoutNode, NodeTransform> Layout(ICollection<LayoutNode> gameNodes)
        {
            /// The node layout we compute as a result.
            Dictionary<LayoutNode, NodeTransform> layout_result = new Dictionary<LayoutNode, NodeTransform>();

            List<LayoutNode> elements = new List<LayoutNode>();
            elements.AddRange(gameNodes);

            // To increase the efficiency of the space usage, we order the elements by one of the sizes.
            // Elements must be sorted by size, descending
            elements.Sort(delegate (LayoutNode left, LayoutNode right) 
                          { return AreaSize(right).CompareTo(AreaSize(left)); });

            // Since we initially do not know how much space we need, we assign a space of the 
            // worst case to the root. Note that we want to add padding in between the nodes,
            // so we need to increase the required size accordingly.
            PTree tree = new PTree(Vector2.zero, Sum(elements, padding));

            // Keeps track of the area currently covered by elements. It is the bounding
            // box containing all rectangles placed so far.
            // Initially, there are no placed elements yet, and therefore the covered 
            // area is initialized to (0, 0).
            Vector2 covrec = Vector2.zero;

            // All nodes in pnodes that preserve the size of coverec. The
            // value is the amount of remaining space if the node were split to 
            // place el.
            Dictionary<PNode, float> preservers = new Dictionary<PNode, float>();
            // All nodes in pnodes that do not preserve the size of coverec.
            // The value is the aspect ratio of coverec if the node were used to
            // place el.
            Dictionary<PNode, float> expanders = new Dictionary<PNode, float>();

            foreach (LayoutNode el in elements)
            {
                // The size we need to place el plus the padding between nodes.
                Vector2 requiredSize = GetRectangleSize(el, padding);

                preservers.Clear();
                expanders.Clear();

                foreach (PNode pnode in tree.GetSufficientlyLargeLeaves(requiredSize))
                {
                    // Right lower corner of new rectangle 
                    Vector2 corner = pnode.rectangle.position + requiredSize;
                    // Expanded covrec.
                    Vector2 expandedCoveRec = new Vector2(Mathf.Max(covrec.x, corner.x), Mathf.Max(covrec.y, corner.y));

                    // If placing el in pnode would preserve the size of coverec
                    if (PTree.FitsInto(expandedCoveRec, covrec))
                    {
                        // The remaining area of pnode if el were placed into it.
                        float waste = pnode.rectangle.size.x * pnode.rectangle.size.y - requiredSize.x * requiredSize.y;
                        preservers[pnode] = waste;
                    }
                    else
                    {
                        // The aspect ratio of coverec if pnode were used to place el.
                        float ratio = expandedCoveRec.x / expandedCoveRec.y;
                        expanders[pnode] = ratio;
                    }
                }

                PNode targetNode = null;
                if (preservers.Count > 0)
                {
                    // targetNode is the node with the lowest waste in preservers
                    float lowestWaste = Mathf.Infinity;
                    foreach (var entry in preservers)
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
                    // If there are more potential candidates, all large enough to host the
                    // element and all of them boundary expanders, we need to chose the one 
                    // that expands the boundaries such that the resulting covered area has 
                    // an aspect ratio closer to a square.

                    // targetNode is the node with the aspect ratio closest to 1
                    float bestRatio = Mathf.Infinity;
                    foreach (var entry in expanders)
                    {
                        if (entry.Value < bestRatio)
                        {
                            targetNode = entry.Key;
                            bestRatio = entry.Value;
                        }
                    }
                }

                // Place el into targetNode.
                // The free leaf node that has the requested size allocated within targetNode. 
                PNode fitNode = tree.Split(targetNode, requiredSize);

                // The size of the node remains unchanged. We set only the position.
                // The x and y co-ordinates of the rectangle denote the corner. The layout
                // position returned must be the center plus the padding.
                Vector3 scale = el.GetSize();
                layout_result[el] = new NodeTransform(new Vector3(fitNode.rectangle.position.x + (scale.x + padding) / 2.0f, 
                                                                  groundLevel, 
                                                                  fitNode.rectangle.position.y + (scale.z  + padding) / 2.0f),
                                                      scale);

                // If fitNode is a boundary expander, then we need to expand coverc to the
                // newly covered area.
                {
                    // Right lower corner of fitNode 
                    Vector2 corner = fitNode.rectangle.position + requiredSize;
                    // Expanded covrec.
                    Vector2 expandedCoveRec = new Vector2(Mathf.Max(covrec.x, corner.x), Mathf.Max(covrec.y, corner.y));

                    // If placing fitNode does not preserve the size of coverec
                    if (!PTree.FitsInto(expandedCoveRec, covrec))
                    {
                        covrec = expandedCoveRec;
                    }
                }
            }
            return layout_result;
        }

        /// <summary>
        /// Returns the ground area size of the given layout node (x -> width, z -> depth).
        /// </summary>
        /// <param name="layoutNode">layout node whose ground area size is requested</param>
        /// <returns>ground area size of the given game object</returns>
        private static Vector2 GetRectangleSize(LayoutNode layoutNode, float padding)
        {
            Vector3 size = layoutNode.GetSize();
            return new Vector2(size.x + padding, size.z + padding);
        }

        /// <summary>
        /// Returns the sum of the required ground area over all given elements including
        /// the padding for each. A game object's width is mapped on the x co-ordinate 
        /// and its depth is mapped on the y co-ordinate of the resulting Vector2s.
        /// </summary>
        /// <param name="elements">game objects whose ground area size is requested</param>
        /// <param name="nodeFactory">the factory that created each node</param>
        /// <param name="padding">the padding to be added to an object's ground area size</param>
        /// <returns>sum of the required ground area over all given elements</returns>
        private Vector2 Sum(List<LayoutNode> elements, float padding)
        {
            Vector2 result = Vector2.zero;
            foreach (LayoutNode element in elements)
            {
                Vector3 size = element.GetSize();
                result.x += size.x + padding;
                result.y += size.z + padding;
            }
            return result;
        }
    }
}
