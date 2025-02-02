using SEE.Layout.NodeLayouts.RectanglePacking;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SEE.Layout.NodeLayouts
{
    /// <summary>
    /// This layout packs rectangles closely together as a set of nested packed rectangles to decrease
    /// the total area of city. The algorithm is based on the dissertation of Richard Wettel
    /// "Software Systems as Cities" (2010); see page 35.
    /// https://www.inf.usi.ch/lanza/Downloads/PhD/Wett2010b.pdf
    /// </summary>
    public class RectanglePackingNodeLayout : HierarchicalNodeLayout
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="padding">the padding to be added between neighboring nodes</param>
        public RectanglePackingNodeLayout(float padding = 0.01f)
        {
            this.padding = padding;
        }

        static RectanglePackingNodeLayout()
        {
            Name = "Rectangle Packing";
        }

        /// <summary>
        /// The padding between neighboring rectangles.
        /// </summary>
        private readonly float padding;

        public override Dictionary<ILayoutNode, NodeTransform> Layout(IEnumerable<ILayoutNode> layoutNodes, Vector2 rectangle)
        {
            Dictionary<ILayoutNode, NodeTransform> layoutResult = new();

            IList<ILayoutNode> layoutNodeList = layoutNodes.ToList();
            if (layoutNodeList.Count == 1)
            {
                ILayoutNode layoutNode = layoutNodeList.First();
                layoutResult[layoutNode] = new NodeTransform(Vector3.zero, layoutNode.AbsoluteScale);
                return layoutResult;
            }

            // Do we have only leaves?
            {
                int numberOfLeaves = 0;
                foreach (ILayoutNode node in layoutNodeList)
                {
                    if (node.IsLeaf)
                    {
                        // All leaves maintain their original size. Pack assumes that
                        // their sizes are already set in layoutResult.
                        // We add the padding upfront. Padding is added on both sides.
                        // The padding will later be removed again.
                        Vector3 scale = node.AbsoluteScale;
                        scale.x += 2.0f * padding;
                        scale.z += 2.0f * padding;
                        layoutResult[node] = new NodeTransform(Vector3.zero, scale);
                        numberOfLeaves++;
                    }
                }
                if (numberOfLeaves == layoutNodeList.Count)
                {
                    // There are only leaves.
                    Pack(layoutResult, layoutNodeList.Cast<ILayoutNode>().ToList(), groundLevel);
                    RemovePadding(layoutResult, padding);
                    return layoutResult;
                }
            }
            // Not all nodes are leaves.
            ICollection<ILayoutNode> roots = LayoutNodes.GetRoots(layoutNodeList);
            if (roots.Count == 0)
            {
                return layoutResult;
            }
            else if (roots.Count > 1)
            {
                throw new System.Exception("Graph has more than one root node.");
            }
            else
            {
                ILayoutNode root = roots.FirstOrDefault();
                Vector2 area = PlaceNodes(layoutResult, root, groundLevel);
                Vector3 position = new(0.0f, groundLevel, 0.0f);
                // Maintain the original height of all inner nodes (and root is an inner node).
                layoutResult[root] = new NodeTransform(position, new Vector3(area.x, root.AbsoluteScale.y, area.y));
                RemovePadding(layoutResult, padding);
                // Pack() distributes the rectangles starting at the origin (0, 0) in the x/z plane
                // for each node hierarchy level anew. That is why we need to adjust the layout so
                // that all rectangles are truly nested.
                MakeContained(layoutResult, root);
                return layoutResult;
            }
        }

        /// <summary>
        /// Adjusts the layout so that all rectangles are truly nested. Also lifts the inner
        /// nodes a bit along the y axis so that they are stacked. This may be necessary for
        /// space filling inner nodes such as cubes. It is not needed for lines, however.
        /// </summary>
        /// <param name="layout">the layout to be adjusted</param>
        /// <param name="parent">the parent node whose children are to be adjusted</param>
        private static void MakeContained
            (Dictionary<ILayoutNode, NodeTransform> layout,
             ILayoutNode parent)
        {
            NodeTransform parentTransform = layout[parent];
            Vector3 parentCenterPosition = parentTransform.Position;
            Vector3 parentExtent = parentTransform.Scale / 2.0f;
            // The x co-ordinate of the left lower corner of the parent.
            float xCorner = parentCenterPosition.x - parentExtent.x;
            // The z co-ordinate of the left lower corner of the parent.
            float zCorner = parentCenterPosition.z - parentExtent.z;

            foreach (ILayoutNode child in parent.Children())
            {
                NodeTransform childTransform = layout[child];
                Vector3 newChildPosition = childTransform.Position;
                newChildPosition.x += xCorner;
                newChildPosition.z += zCorner;
                if (!child.IsLeaf)
                {
                    // The inner nodes will be slightly lifted along the y axis according to their
                    // tree depth so that they can be stacked visually (level 0 is at the bottom).
                    newChildPosition.y += LevelLift(child);
                }
                layout[child] = new NodeTransform(newChildPosition, childTransform.Scale, childTransform.Rotation);
                MakeContained(layout, child);
            }
        }

        /// <summary>
        /// Removes the <paramref name="padding"/> for all NodeTransforms in <paramref name="layout"/>.
        /// </summary>
        /// <param name="layout">layout containing the NodeTransform.scale to be adjusted</param>
        /// <param name="padding">padding to be removed</param>
        private static void RemovePadding(Dictionary<ILayoutNode, NodeTransform> layout, float padding)
        {
            ICollection<ILayoutNode> keys = new List<ILayoutNode>(layout.Keys);

            foreach (ILayoutNode key in keys)
            {
                NodeTransform value = layout[key];
                Vector3 scale = value.Scale;
                scale.x -= 2.0f * padding;
                scale.z -= 2.0f * padding;
                // Since we removed the padding, we need to adjust the position, too,
                // to center the node within the assigned rectangle.
                Vector3 position = value.Position;
                position.x += padding;
                position.z += padding;
                layout[key] = new NodeTransform(position, scale);
            }
        }

        /// <summary>
        /// Recursively places the given node and its descendants in nested packed rectangles.
        ///
        /// Precondition: layout has the final scale of all leaves already set.
        /// </summary>
        /// <param name="layout">the current layout; will be updated</param>
        /// <param name="node">node to be laid out (includings all its descendants)</param>
        /// <param name="groundLevel">The y-coordindate of the ground where all nodes will be placed.</param>
        /// <returns>the width and depth of the area covered by the rectangle for <paramref name="node"/></returns>
        private Vector2 PlaceNodes(Dictionary<ILayoutNode, NodeTransform> layout, ILayoutNode node, float groundLevel)
        {
            if (node.IsLeaf)
            {
                // Leaves maintain their scale, which was already set initially. The position will
                // be adjusted later at a higher level of the node hierarchy when Pack() is
                // applied to this leaf and all its siblings.
                return new Vector2(node.AbsoluteScale.x, node.AbsoluteScale.z);
            }
            else
            {
                // Inner node.
                ICollection<ILayoutNode> children = node.Children();

                // First recurse towards the leaves and determine the sizes of all descendants.
                foreach (ILayoutNode child in children)
                {
                    if (!child.IsLeaf)
                    {
                        Vector2 childArea = PlaceNodes(layout, child, groundLevel);
                        // childArea is the ground area size required for this inner node.
                        // The position of this inner node in layout will be below in the call to Pack().
                        // The position is relative to the parent of this inner node.
                        // We only need to set the scale here.
                        // Note: We have already added padding to leaf nodes, but this one here is an
                        // inner node. Nevertheless, we do not add padding here, because padding is already
                        // included in the returned childArea.
                        layout[child] = new NodeTransform(Vector3.zero,
                                                          new Vector3(childArea.x, groundLevel, childArea.y));
                    }
                }
                // The scales of all children of the node have now been set. Now
                // let's pack those children.
                if (children.Count > 0)
                {
                    Vector2 area = Pack(layout, children.Cast<ILayoutNode>().ToList(), groundLevel);
                    return new Vector2(area.x + 2.0f * padding, area.y + 2.0f * padding);
                }
                else
                {
                    // Can we ever arrive here? That would mean that node is not a leaf
                    // and does not have children.
                    return new Vector2(node.LocalScale.x, node.LocalScale.z);
                }
            }
        }

        /// <summary>
        /// Returns the area size of given <paramref name="node"/>, i.e., its width (x co-ordinate)
        /// multiplied by its depth (z co-ordinate).
        /// </summary>
        /// <param name="node">node whose size is to be returned</param>
        /// <returns>area size of given layout node</returns>
        private static float AreaSize(NodeTransform node)
        {
            Vector3 size = node.Scale;
            return size.x * size.z;
        }

        /// <summary>
        /// Returns the ground area size of the given <paramref name="node"/>:
        /// (x -> width, z -> depth).
        /// </summary>
        /// <param name="node">node whose ground area size is requested</param>
        /// <returns>ground area size of the given <paramref name="node"/></returns>
        private static Vector2 GetRectangleSize(NodeTransform node)
        {
            Vector3 size = node.Scale;
            return new Vector2(size.x, size.z);
        }

        /// <summary>
        /// Returns the sum of the required ground area over all given <paramref name="nodes"/> including
        /// the padding for each. A node's width is mapped onto the x co-ordinate
        /// and its depth is mapped onto the y co-ordinate of the resulting Vector2.
        /// </summary>
        /// <param name="nodes">nodes whose ground area size is requested</param>
        /// <param name="layoutResult">the currently existing layout information for each node
        /// (its scale is required only)</param>
        /// <param name="padding">the padding to be added to a node's ground area size</param>
        /// <returns>sum of the required ground area over all given <paramref name="nodes"/></returns>
        private static Vector2 Sum(List<ILayoutNode> nodes, Dictionary<ILayoutNode, NodeTransform> layoutResult)
        {
            Vector2 result = Vector2.zero;
            foreach (ILayoutNode element in nodes)
            {
                Vector3 size = layoutResult[element].Scale;
                result.x += size.x;
                result.y += size.z;
            }
            return result;
        }

        /// <summary>
        /// Places the given <paramref name="nodes"/> in a minimally sized rectangle without
        /// overlapping.
        ///
        /// Allows one to pack smaller rectangles into a single larger rectangle
        /// so that the contained rectangles do not overlap, are as close together
        /// as possible (without padding) and the containing rectangle is as
        /// small as possible (no optimal solution is provided). The containing
        /// rectangle is organized in stripes whose aspect ratio is as close to
        /// one as possible. The layout maintains the size and orientation of
        /// all smaller rectangles. The largest contained rectangle appears at the
        /// left lower corner of the containing rectangle at position (0, groundlevel, 0).
        ///
        /// Precondition: The scales of all <paramref name="nodes"/> are set in
        /// the corresponding NodeTransforms in <paramref name="layout"/>.
        /// </summary>
        /// <param name="layout">the current layout (positions of <paramref name="nodes"/>
        /// will be updated</param>
        /// <param name="nodes">the nodes to be laid out</param>
        /// <param name="groundLevel">The y-coordindate of the ground where all nodes will be placed.</param>
        /// <returns>the width (x) and depth (y) of the outer rectangle in which all
        /// <paramref name="nodes"/> were placed</returns>
        private Vector2 Pack(Dictionary<ILayoutNode, NodeTransform> layout, List<ILayoutNode> nodes, float groundLevel)
        {
            // To increase the efficiency of the space usage, we order the elements by one of the sizes.
            // Elements must be sorted by size, descending
            nodes.Sort(delegate (ILayoutNode left, ILayoutNode right)
            { return AreaSize(layout[right]).CompareTo(AreaSize(layout[left])); });

            // Since we initially do not know how much space we need, we assign a space of the
            // worst case to the root. Note that we want to add padding in between the nodes,
            // so we need to increase the required size accordingly.
            Vector2 worstCaseSize = Sum(nodes, layout);
            // The worst-case size is increased slightly to circumvent potential
            // imprecisions of floating-point arithmetics.
            PTree tree = new(Vector2.zero, 1.1f * worstCaseSize);

            // Keeps track of the area currently covered by elements. It is the bounding
            // box containing all rectangles placed so far.
            // Initially, there are no placed elements yet, and therefore the covered
            // area is initialized to (0, 0).
            Vector2 covrec = Vector2.zero;

            // All nodes in pnodes that preserve the size of coverec. The
            // value is the amount of remaining space if the node were split to
            // place el.
            Dictionary<PNode, float> preservers = new();
            // All nodes in pnodes that do not preserve the size of coverec.
            // The value is the absolute difference of the aspect ratio of coverec from 1
            // (1 being the perfect ratio) if the node were used to place el.
            Dictionary<PNode, float> expanders = new();

            foreach (ILayoutNode el in nodes)
            {
                // We assume that the scale of all nodes in elements have already been set.

                // The size we need to place el plus the padding between nodes.
                Vector2 requiredSize = GetRectangleSize(layout[el]);

                preservers.Clear();
                expanders.Clear();

                foreach (PNode pnode in tree.GetSufficientlyLargeLeaves(requiredSize))
                {
                    // Right lower corner of new rectangle
                    Vector2 corner = pnode.Rectangle.Position + requiredSize;
                    // Expanded covrec.
                    Vector2 expandedCoveRec = new(Mathf.Max(covrec.x, corner.x), Mathf.Max(covrec.y, corner.y));

                    // If placing el in pnode would preserve the size of coverec
                    if (PTree.FitsInto(expandedCoveRec, covrec))
                    {
                        // The remaining area of pnode if el were placed into it.
                        float waste = pnode.Rectangle.Size.x * pnode.Rectangle.Size.y - requiredSize.x * requiredSize.y;
                        preservers[pnode] = waste;
                    }
                    else
                    {
                        // The aspect ratio of coverec if pnode were used to place el.
                        float ratio = expandedCoveRec.x / expandedCoveRec.y;
                        expanders[pnode] = Mathf.Abs(ratio - 1);
                    }
                }

                PNode targetNode = null;
                if (preservers.Count > 0)
                {
                    // targetNode is the node with the lowest waste in preservers
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
                    // If there are more potential candidates, all large enough to host the
                    // element and all of them boundary expanders, we need to chose the one
                    // that expands the boundaries such that the resulting covered area has
                    // an aspect ratio closer to a square.

                    // targetNode is the node with the aspect ratio closest to 1
                    float bestRatio = Mathf.Infinity;
                    foreach (KeyValuePair<PNode, float> entry in expanders)
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
                // The x and y co-ordinates of the rectangle denote the left front corner. The layout
                // position returned must be the center. The y co-ordinate is the ground level.
                Vector3 scale = layout[el].Scale;
                layout[el] = new NodeTransform(new Vector3(fitNode.Rectangle.Position.x + scale.x / 2.0f,
                                                           groundLevel,
                                                           fitNode.Rectangle.Position.y + scale.z / 2.0f),
                                               scale);

                // If fitNode is a boundary expander, then we need to expand covrec to the
                // newly covered area.
                {
                    // Right lower corner of fitNode
                    Vector2 corner = fitNode.Rectangle.Position + requiredSize;
                    // Expanded covrec.
                    Vector2 expandedCoveRec = new(Mathf.Max(covrec.x, corner.x), Mathf.Max(covrec.y, corner.y));

                    // If placing fitNode does not preserve the size of coverec
                    if (!PTree.FitsInto(expandedCoveRec, covrec))
                    {
                        covrec = expandedCoveRec;
                    }
                }
            }
            return covrec;
        }
    }
}
