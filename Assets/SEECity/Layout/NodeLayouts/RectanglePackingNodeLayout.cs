using SEE.Layout.RectanglePacking;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SEE.Layout
{
    /// <summary>
    /// This layout packs rectangles closely together as a set of nested rectangles to decrease 
    /// the total area of city. It is an extension of the flat RectanglePacker for hierarchies.
    /// </summary>
    public class RectanglePackingNodeLayout : HierarchicalNodeLayout
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="groundLevel">the y co-ordinate setting the ground level; all nodes will be
        /// placed on this level</param>
        /// <param name="Unit">the unit for blocks; will be used to adjust the padding</param>
        /// <param name="padding">the padding to be added between neighboring nodes;
        /// the actual value used is padding * leafNodeFactory.Unit()</param>
        public RectanglePackingNodeLayout(float groundLevel, float Unit, float padding = 1.0f)
            : base(groundLevel)
        {
            name = "Rectangle Packing";
            this.padding = padding * Unit;
        }

        public override bool IsHierarchical()
        {
            return false; // FIXME
        }

        /// <summary>
        /// The padding between neighboring rectangles.
        /// </summary>
        private readonly float padding;

        public override Dictionary<ILayoutNode, NodeTransform> Layout(ICollection<ILayoutNode> layoutNodes)
        {
            Dictionary<ILayoutNode, NodeTransform> layout_result = new Dictionary<ILayoutNode, NodeTransform>();

            if (layoutNodes.Count == 1)
            {
                ILayoutNode layoutNode = layoutNodes.FirstOrDefault();
                layout_result[layoutNode] = new NodeTransform(Vector3.zero, layoutNode.Scale);
                return layout_result;
            }

            // Do we have only leaves?
            {
                int numberOfLeaves = 0;
                foreach (ILayoutNode node in layoutNodes)
                {
                    if (node.IsLeaf)
                    {
                        // All leaves maintain their original size. Pack assumes that
                        // their sizes are already set in layout_result.
                        // We add the padding upfront. Padding is added on both sides.
                        // The padding will later be removed again.
                        Vector3 scale = node.Scale;
                        scale.x += 2.0f * padding; 
                        scale.y += 2.0f * padding;
                        layout_result[node] = new NodeTransform(Vector3.zero, scale);
                        numberOfLeaves++;
                    }
                }
                if (numberOfLeaves == layoutNodes.Count)
                {
                    // There are only leaves.
                    Pack(layout_result, layoutNodes.Cast<ILayoutNode>().ToList());
                    RemovePadding(layout_result, padding);
                    return layout_result;
                }
            }
            // Not all nodes are leaves.
            ICollection<ILayoutNode> roots = LayoutNodes.GetRoots(layoutNodes);
            if (roots.Count == 0)
            {
                return layout_result;
            }
            else if (roots.Count > 1)
            {
                throw new System.Exception("Graph has more than one root node.");
            }
            else
            {
                ILayoutNode root = roots.FirstOrDefault();
                Vector2 area = PlaceNodes(layout_result, root);
                Vector3 position = new Vector3(0.0f, groundLevel, 0.0f);
                // Maintain the original height of all inner nodes (and root is an inner node).
                layout_result[root] = new NodeTransform(position, new Vector3(area.x, root.Scale.y, area.y));
                MakeGlobal(layout_result, position, roots);
                return layout_result;
            }
        }

        private void RemovePadding(Dictionary<ILayoutNode, NodeTransform> layout, float padding)
        {
            foreach (var entry in layout)
            {
                Vector3 scale = layout[entry.Key].scale;
                scale.x -= 2.0f * padding;
                scale.y -= 2.0f * padding;
                // Since we removed the padding, we need to adjust the position, too,
                // to center the node within the assigned rectangle.
                Vector3 position = layout[entry.Key].position;
                position.x += padding;
                position.z += padding;
            }
        }

        private Vector2 PlaceNodes(Dictionary<ILayoutNode, NodeTransform> layout_result, ILayoutNode node)
        {
            Debug.LogFormat("PlaceNodes called with {0}\n", node);
            if (node.IsLeaf)
            {
                Debug.LogFormat("Node {0} is a leaf.\n", node);
                // Leaves maintain their scale. All positions are relative at the moment,
                // hence, we use Vector3.zero. The position will be adjusted later.
                // layout_result[node] = new NodeTransform(Vector3.zero, node.Scale); // Already set initially
                return new Vector2(node.Scale.x, node.Scale.z); // FIXME: Add padding
            }
            else
            {
                // Inner node.
                Debug.LogFormat("Node {0} is an inner node.\n", node);
                ICollection<ILayoutNode> children = node.Children();

                // First recurse towards the leaves and determine the sizes of all descendants.
                foreach (ILayoutNode child in children)
                {
                    if (child.IsLeaf)
                    {
                        Debug.LogFormat("Child {0} of node {1} is a leaf.\n", child, node);
                        // layout_result[child] = new NodeTransform(Vector3.zero, child.Scale); // Already set initially
                    }
                    else
                    {
                        Debug.LogFormat("Child {0} of node {1} is an inner node.\n", child, node);
                        Vector2 childArea = PlaceNodes(layout_result, child);
                        layout_result[child] = new NodeTransform(Vector3.zero, new Vector3(childArea.x, groundLevel, childArea.y));
                        Debug.LogFormat("Child {0} of node {1} is an inner node has layout {2}.\n", child, node, layout_result[child]);
                    }
                }
                // The scales of all descendants of the node have now been set. Now
                // let's pack the children of node.
                if (children.Count > 0)
                {
                    Vector2 area = Pack(layout_result, children.Cast<ILayoutNode>().ToList());
                    // FIXME: Is the following assignment needed at all? The layout of a node will be set one level
                    // up at the point where its parent iterates the children and the node is one of these children.
                    //layout_result[node] = new NodeTransform(Vector3.zero, new Vector3(area.x, groundLevel, area.y));
                    return area;
                }
                else
                {
                    return Vector2.zero;
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
            Vector3 size = node.scale;
            return size.x * size.z;
        }

        /// <summary>
        /// Returns the ground area size of the given <paramref name="node"/>:
        /// (x -> width, z -> depth) plus <paramref name="padding"/> on both axes.
        /// </summary>
        /// <param name="node">node whose ground area size is requested</param>
        /// <returns>ground area size of the given <paramref name="node"/></returns>
        private static Vector2 GetRectangleSize(NodeTransform node, float padding)
        {
            Vector3 size = node.scale;
            return new Vector2(size.x + padding, size.z + padding);
        }

        /// <summary>
        /// Returns the sum of the required ground area over all given <paramref name="nodes"/> including
        /// the padding for each. A node's width is mapped onto the x co-ordinate 
        /// and its depth is mapped onto the y co-ordinate of the resulting Vector2.
        /// </summary>
        /// <param name="nodes">nodes whose ground area size is requested</param>
        /// <param name="layout_result">the currently existing layout information for each node 
        /// (its scale is required only)</param>
        /// <param name="padding">the padding to be added to a node's ground area size</param>
        /// <returns>sum of the required ground area over all given <paramref name="nodes"/></returns>
        private Vector2 Sum(List<ILayoutNode> nodes, Dictionary<ILayoutNode, NodeTransform> layout_result, float padding)
        {
            Vector2 result = Vector2.zero;
            foreach (ILayoutNode element in nodes)
            {
                Vector3 size = layout_result[element].scale;
                result.x += size.x + padding;
                result.y += size.z + padding;
            }
            return result;
        }

        private Vector2 Pack(Dictionary<ILayoutNode, NodeTransform> layout_result, List<ILayoutNode> elements)
        {
            // To increase the efficiency of the space usage, we order the elements by one of the sizes.
            // Elements must be sorted by size, descending
            elements.Sort(delegate (ILayoutNode left, ILayoutNode right)
            { return AreaSize(layout_result[right]).CompareTo(AreaSize(layout_result[left])); });

            Dump(elements, layout_result, "Pack");

            // Since we initially do not know how much space we need, we assign a space of the 
            // worst case to the root. Note that we want to add padding in between the nodes,
            // so we need to increase the required size accordingly.
            Vector2 worstCaseSize = Sum(elements, layout_result, padding);
            Debug.LogFormat("Pack: worstCaseSize={0}\n", worstCaseSize);
            PTree tree = new PTree(Vector2.zero, worstCaseSize);

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

            foreach (ILayoutNode el in elements)
            {
                // We assume that the scale of all nodes in elements have already been set.

                // The size we need to place el plus the padding between nodes.                
                Vector2 requiredSize = GetRectangleSize(layout_result[el], padding);

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
                Vector3 scale = layout_result[el].scale;
                layout_result[el] = new NodeTransform(new Vector3(fitNode.rectangle.position.x + (scale.x + padding) / 2.0f,
                                                                  groundLevel,
                                                                  fitNode.rectangle.position.y + (scale.z + padding) / 2.0f),
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
            return covrec;
        }

        private void Dump(List<ILayoutNode> nodes, Dictionary<ILayoutNode, NodeTransform> layout, string message)
        {
            foreach (ILayoutNode node in nodes)
            {
                Debug.LogFormat("{0}: {1} with node transform={2}.\n", message, node, layout[node]);
            }
        }
    }
}