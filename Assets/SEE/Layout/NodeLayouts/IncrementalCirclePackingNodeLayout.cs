using SEE.Layout.NodeLayouts.CirclePacking;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SEE.Layout.NodeLayouts
{
    public class IncrementalCirclePackingNodeLayout : NodeLayout, IIncrementalNodeLayout
    {
        /// <summary>
        /// Initializes the <see cref="IncrementalCirclePackingNodeLayout"/> class.
        /// </summary>
        static IncrementalCirclePackingNodeLayout()
        {
            Name = "Incremental Circle Packing";
        }

        /// <summary>
        /// Stores the old layout.
        /// </summary>
        public IncrementalCirclePackingNodeLayout oldLayout;

        /// <summary>
        /// Sets the old layout.
        /// </summary>
        public IIncrementalNodeLayout OldLayout
        {
            set
            {
                if (value is IncrementalCirclePackingNodeLayout layout)
                {
                    oldLayout = layout;
                }
                else
                {
                    throw new ArgumentException(
                        $"Predecessor of {nameof(IncrementalCirclePackingNodeLayout)} was not an {nameof(IncrementalCirclePackingNodeLayout)}.");
                }
            }
        }

        /// <summary>
        /// The layout result as a dictionary mapping each <see cref="ILayoutNode"/> to its corresponding <see cref="NodeTransform"/>.
        /// </summary>
        public Dictionary<ILayoutNode, NodeTransform> layoutResult;

        /// <summary>
        /// Stores the last positions of the nodes as a dictionary mapping each node's
        /// ID to a list of tuples containing the child node's ID, radius, and position.
        /// </summary>
        public Dictionary<string, List<(string, float, Vector2)>> lastPositions;


        /// <summary>
        /// Performs the incremental circle packing layout.
        /// </summary>
        /// <param name="layoutNodes">The nodes to layout.</param>
        /// <param name="centerPosition">The center position of the layout.</param>
        /// <param name="rectangle">The rectangle enclosing the layout.</param>
        /// <returns>The layout result.</returns>
        protected override Dictionary<ILayoutNode, NodeTransform> Layout(IEnumerable<ILayoutNode> layoutNodes, Vector3 centerPosition, Vector2 rectangle)
        {

            FirstScenario(layoutNodes, centerPosition, rectangle);

            return layoutResult;

        }

        /// <summary>
        /// Executes the first scenario for the incremental circle packing layout.
        /// </summary>
        /// <param name="layoutNodes">The nodes to layout.</param>
        /// <param name="centerPosition">The center position of the layout.</param>
        /// <param name="rectangle">The rectangle enclosing the layout.</param>
        /// <returns>The layout result.</returns>
        public Dictionary<ILayoutNode, NodeTransform> FirstScenario(IEnumerable<ILayoutNode> layoutNodes, Vector3 centerPosition, Vector2 rectangle)
        {
            layoutResult = new Dictionary<ILayoutNode, NodeTransform>();
            lastPositions = new Dictionary<string, List<(string, float, Vector2)>>();
            if (oldLayout != null)
            {
                lastPositions = oldLayout.lastPositions;
            }
            else
            {
                lastPositions = new();
            }


            ICollection<ILayoutNode> roots = LayoutNodes.GetRoots(layoutNodes);
            if (roots.Count == 0)
            {
                throw new System.Exception("Graph has no root node.");
            }
            else if (roots.Count > 1)
            {
                throw new System.Exception("Graph has more than one root node.");
            }
            else
            {
                ILayoutNode root = roots.FirstOrDefault();

                float outRadius = PlaceNodes(root, layoutResult);
                Vector2 position = Vector2.zero;
                layoutResult[root] = new NodeTransform(position.x, position.y, GetScale(root, outRadius));
                MakeGlobal(layoutResult, position, root.Children());
                return layoutResult;
            }
        }

        /// <summary>
        /// Converts the local positions of the nodes to global positions.
        /// </summary>
        /// <param name="layoutResult">The layout result.</param>
        /// <param name="position">The global position.</param>
        /// <param name="children">The child nodes.</param>
        private static void MakeGlobal(Dictionary<ILayoutNode, NodeTransform> layoutResult, Vector2 position, ICollection<ILayoutNode> children)
        {
            foreach (ILayoutNode child in children)
            {
                NodeTransform childTransform = layoutResult[child];
                Vector2 childPosition = new Vector2(childTransform.X, childTransform.Z) + position;
                childTransform.MoveTo(childPosition.x, childPosition.y);
                layoutResult[child] = childTransform;
                MakeGlobal(layoutResult, childPosition, child.Children());
            }
        }

        /// <summary>
        /// Places the nodes in the layout.
        /// </summary>
        /// <param name="parent">The parent node.</param>
        /// <param name="layout">The layout result.</param>
        /// <returns>The radius of the placed nodes.</returns>
        public float PlaceNodes(ILayoutNode parent, Dictionary<ILayoutNode, NodeTransform> layout)
        {
            ICollection<ILayoutNode> children = parent.Children();

            if (children.Count == 0)
            {

                return LeafRadius(parent);
            }
            else
            {
                List<TheCircle> circles = new(children.Count);

                int i = 0;
                foreach (ILayoutNode child in children)
                {
                    float radius = child.IsLeaf ? LeafRadius(child) : PlaceNodes(child, layout);

                    float radians = (i / (float)children.Count) * (2.0f * Mathf.PI);
                    circles.Add(new TheCircle(child, new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * radius, radius));
                    i++;
                }

                IncrementalCirclePacker.PackCircles(circles, Vector2.zero, out float outOuterRadius, lastPositions, parent.ID);

                if (children.Count == 1 && !children.ElementAt(0).IsLeaf)
                {
                    outOuterRadius *= 1.2f;
                }

                foreach (TheCircle circle in circles)
                {

                    layout[circle.GameObject]
                         = new NodeTransform(circle.Center.x, circle.Center.y,
                                             GetScale(circle.GameObject, circle.Radius));
                }
                return outOuterRadius;
            }
        }

        /// <summary>
        /// Gets the scale for a node based on its radius.
        /// </summary>
        /// <param name="node">The node.</param>
        /// <param name="radius">The radius.</param>
        /// <returns>The scale.</returns>
        private static Vector3 GetScale(ILayoutNode node, float radius)
        {
            return node.IsLeaf ? node.AbsoluteScale
                               : new Vector3(2 * radius, node.AbsoluteScale.y, 2 * radius);
        }

        /// <summary>
        /// Gets the radius of a leaf node.
        /// </summary>
        /// <param name="block">The leaf node.</param>
        /// <returns>The radius.</returns>
        private static float LeafRadius(ILayoutNode block)
        {
            Vector3 extent = block.AbsoluteScale / 2.0f;
            return Mathf.Sqrt(extent.x * extent.x + extent.z * extent.z);
        }
    }
}
