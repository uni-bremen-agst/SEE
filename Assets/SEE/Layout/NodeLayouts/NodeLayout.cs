using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SEE.Layout.NodeLayouts
{
    /// <summary>
    /// The abstract super class of all node layouts.
    /// </summary>
    public abstract class NodeLayout
    {
        /// <summary>
        /// The unique name of a layout. Must be set by all concrete subclasses.
        /// </summary>
        public static string Name
        {
            get; protected set;
        } = string.Empty;

        /// <summary>
        /// The y co-ordinate of the ground level where all nodes will be placed initially
        /// by calling <see cref="Layout(IEnumerable{ILayoutNode}, Vector2)"/>.
        /// </summary>
        protected const float groundLevel = 0.0f;

        /// <summary>
        /// If inner nodes are represented as visible objects covering their total area
        /// and the visualizations of those inner nodes are stacked in a hierarchical layout,
        /// their visualizations should not be on the same level; otherwise they will hide
        /// each other. For this reason, every inner node will be slightly lifted along the
        /// y axis according to its tree depth so that inner nodes are stacked visually
        /// (level 0 is at the bottom). The value levelIncreaseForInnerNodes is the
        /// height factor for each level. It will be multiplied by the level to obtain
        /// an inner node's y co-ordinate.
        /// </summary>
        protected const float LevelIncreaseForInnerNodes = 0.015f;

        /// <summary>
        /// Returns the lift for an innner node as a product of its tree level
        /// and levelIncreaseForInnerNodes. This value is intended to be added
        /// to the ground level to define the y co-ordindate of an inner node
        /// where visualizations of inner nodes can be stacked and would possibly
        /// hide each other if they were all at the same height.
        /// </summary>
        /// <param name="node">an inner node to be lifted</param>
        /// <returns>lift for an innner node</returns>
        protected static float LevelLift(ILayoutNode node)
        {
            return node.Level * LevelIncreaseForInnerNodes;
        }

        /// <summary>
        /// Yields the layout for all given <paramref name="layoutNodes"/>.
        /// For every node n in <paramref name="layoutNodes"/>: result[n] is the node transform,
        /// i.e., the game object's position, scale, and rotation.
        ///
        /// The nodes will be placed into a rectangle whose width is the
        /// x co-ordindate of <paramref name="rectangle"/> and whose depth is the y co-ordinate
        /// of <paramref name="rectangle"/>.
        /// </summary>
        /// <param name="layoutNodes">set of layout nodes for which to compute the layout</param>
        /// <param name="rectangle">The size of the rectangle within all nodes will be placed.</param>
        /// <returns>node layout</returns>
        public abstract Dictionary<ILayoutNode, NodeTransform> Layout
            (IEnumerable<ILayoutNode> layoutNodes,
             Vector2 rectangle);

        /// <summary>
        /// Adds the given <paramref name="offset"/> to every node position in the given <paramref name="layout"/>.
        /// </summary>
        /// <param name="layout">node layout to be adjusted</param>
        /// <param name="offset">offset to be added</param>
        /// <returns><paramref name="layout"/> where <paramref name="offset"/> has been added to each position</returns>
        private static Dictionary<ILayoutNode, NodeTransform> Move(Dictionary<ILayoutNode, NodeTransform> layout, Vector3 offset)
        {
            Dictionary<ILayoutNode, NodeTransform> result = new();
            foreach (KeyValuePair<ILayoutNode, NodeTransform> entry in layout)
            {
                NodeTransform transform = entry.Value;
                transform.Position += offset;
                result[entry.Key] = transform;
            }
            return result;
        }

        /// <summary>
        /// Moves all <paramref name="layoutNodes"/> together to the cube defined by <paramref name="target"/>.
        /// More precisely, the bounding box enclosing all nodes in <paramref name="layoutNodes"/> will be
        /// translated (moved such that there is no change in size or shape) to a new location defined by
        /// <paramref name="target"/> such that the center of the bounding box at the new location
        /// is at <paramref name="target"/>.
        /// </summary>
        /// <param name="layoutNodes">layout nodes to be moved</param>
        /// <param name="target">worldspace center point where to move the <paramref name="layoutNodes"/></param>
        private static void MoveTo(IEnumerable<ILayoutNode> layoutNodes, Vector3 target)
        {
            IList<ILayoutNode> layoutList = layoutNodes.ToList();
            Bounding3DBox(layoutList.ToList(), out Vector3 left, out Vector3 right);
            // centerPosition is the worldspace center of the bounding 3D box (cube) enclosing all layoutNodes.
            // The center of the cube is the midpoint of the line segment connecting its
            // left front corner and its right back corner. The coordinates of the center
            // can be calculated using the midpoint formula:
            Vector3 centerPosition = (right + left) / 2.0f;
            // The offest is the vector from the current center of the bounding box to the
            // target center point and needs to be added to all nodes.
            Vector3 offset = target - centerPosition;
            // It is assumed that target.y is the lowest point of the city.
            // offset.y = target.y; FIXME can be removed.
            foreach (ILayoutNode layoutNode in layoutList)
            {
                layoutNode.CenterPosition += offset;
            }
        }

        /// <summary>
        /// Scales the width and depth of all nodes in <paramref name="layoutNodes"/> so that they fit into
        /// a rectangled defined by <paramref name="width"/> and <paramref name="depth"/>.
        /// The aspect ratio of every node is maintained.
        /// </summary>
        /// <param name="layoutNodes">layout nodes to be scaled</param>
        /// <param name="width">the absolute width (x axis) the required space for the laid out nodes must have</param>
        /// <param name="depth">the absolute depth (z axis) the required space for the laid out nodes must have</param>
        /// <returns>the factor by which the scale of edge node was multiplied</returns>
        private static float ScaleXZ(IEnumerable<ILayoutNode> layoutNodes, float width, float depth)
        {
            IList<ILayoutNode> layoutNodeList = layoutNodes.ToList();
            Bounding3DBox(layoutNodeList, out Vector3 leftFrontCorner, out Vector3 rightBackCorner);
            // The actual occupied space is a rectangle defined by leftFrontCorner and rightBackCorner.

            float actualWidth = rightBackCorner.x - leftFrontCorner.x;
            float actualDepth = rightBackCorner.z - leftFrontCorner.z;

            float scaleFactor = Mathf.Min(width / actualWidth, depth / actualDepth);
            foreach (ILayoutNode layoutNode in layoutNodeList)
            {
                layoutNode.ScaleXZBy(scaleFactor);
                // The x/z co-ordinates of the position must be adjusted after scaling.
                Vector3 newPosition = layoutNode.CenterPosition;
                newPosition.x *= scaleFactor;
                newPosition.z *= scaleFactor;
                layoutNode.CenterPosition = newPosition;
            }
            return scaleFactor;
        }

        /// <summary>
        /// Stacks all <paramref name="layoutNodes"/> onto each other with <paramref name="levelDelta"/>
        /// in between parent and children (children are on top of their parent) where the initial
        /// y co-ordinate ground position of the roof nodes is specified by <paramref name="groundLevel"/>.
        /// The x and z co-ordinates of the <paramref name="layoutNodes"/> are not changed.
        /// </summary>
        /// <param name="layoutNodes">the nodes to be stacked onto each other</param>
        /// <param name="groundLevel">target y co-ordinate ground position of the layout nodes</param>
        /// <param name="levelDelta">the y distance between parents and their children</param>
        private static void Stack(IEnumerable<ILayoutNode> layoutNodes, float groundLevel, float levelDelta = 0.001f)
        {
            // position all root nodes at groundLevel
            foreach (ILayoutNode layoutNode in layoutNodes)
            {
                if (layoutNode.Parent == null)
                {
                    float newRoofY = PutOn(layoutNode, groundLevel);
                    // Continue with the children
                    foreach (ILayoutNode child in layoutNode.Children())
                    {
                        Stack(child, newRoofY + levelDelta, levelDelta);
                    }
                }
            }
        }

        /// <summary>
        /// Positions the ground of <paramref name="layoutNode"/> on <paramref name="groundLevel"/>
        /// and then stacks its children onto <paramref name="layoutNode"/> with <paramref name="levelDelta"/>
        /// space along the y axis in between. Recurses to its children. Children are put on top of
        /// <paramref name="layoutNode"/>.
        /// <param name="layoutNode">the node to be positioned</param>
        /// <param name="groundLevel">the target y co-ordinate ground position of <paramref name="layoutNode"/></param>
        /// <param name="levelDelta">the y distance between <paramref name="layoutNode"/> and its children</param>
        private static void Stack(ILayoutNode layoutNode, float groundLevel, float levelDelta)
        {
            float newRoofY = PutOn(layoutNode, groundLevel);
            foreach (ILayoutNode child in layoutNode.Children())
            {
                Stack(child, newRoofY + levelDelta, levelDelta);
            }
        }

        /// <summary>
        /// Puts the ground of <paramref name="layoutNode"/> on <paramref name="groundLevel"/> (y axis).
        /// </summary>
        /// <param name="layoutNode">the layout node to be positioned</param>
        /// <param name="groundLevel">the y-coordinate of the ground of <paramref name="layoutNode"/> to be positioned</param>
        /// <returns>the y co-ordinate of the roof of the <paramref name="groundLevel"/> after it was moved along the y-axis</returns>
        private static float PutOn(ILayoutNode layoutNode, float groundLevel)
        {
            Vector3 centerPosition = layoutNode.CenterPosition;
            float yExtent = layoutNode.AbsoluteScale.y / 2.0f;
            centerPosition.y = groundLevel + yExtent;
            layoutNode.CenterPosition = centerPosition;
            return centerPosition.y + yExtent;
        }

        /// <summary>
        /// Returns the bounding 3D box enclosing all given <paramref name="layoutNodes"/>.
        /// </summary>
        /// <param name="layoutNodes">the list of layout nodes that are enclosed in the resulting bounding 3D box</param>
        /// <param name="left">the left lower front corner of the bounding box in worldspace</param>
        /// <param name="right">the right upper back corner of the bounding box in worldspace</param>
        private static void Bounding3DBox(ICollection<ILayoutNode> layoutNodes, out Vector3 left, out Vector3 right)
        {
            if (layoutNodes.Count == 0)
            {
                left = Vector3.zero;
                right = Vector3.zero;
            }
            else
            {
                left = new Vector3(Mathf.Infinity, Mathf.Infinity, Mathf.Infinity);
                right = new Vector3(Mathf.NegativeInfinity, Mathf.NegativeInfinity, Mathf.NegativeInfinity);

                foreach (ILayoutNode node in layoutNodes)
                {
                    Vector3 extent = node.AbsoluteScale / 2.0f;
                    // Note: position denotes the center of the object
                    Vector3 position = node.CenterPosition;
                    {
                        // left x co-ordinate of node
                        float x = position.x - extent.x;
                        if (x < left.x)
                        {
                            left.x = x;
                        }
                    }
                    {   // right x co-ordinate of node
                        float x = position.x + extent.x;
                        if (x > right.x)
                        {
                            right.x = x;
                        }
                    }
                    {
                        // lower y co-ordinate of node
                        float y = position.y - extent.y;
                        if (y < left.y)
                        {
                            left.y = y;
                        }
                    }
                    {
                        // upper y co-ordinate of node
                        float y = position.y + extent.y;
                        if (y > right.y)
                        {
                            right.y = y;
                        }
                    }
                    {
                        // front z co-ordinate of node
                        float z = position.z - extent.z;
                        if (z < left.z)
                        {
                            left.z = z;
                        }
                    }
                    {
                        // back z co-ordinate of node
                        float z = position.z + extent.z;
                        if (z > right.z)
                        {
                            right.z = z;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// If true, the layout can handle both inner nodes and leaves; otherwise
        /// only leaves.
        /// </summary>
        /// <returns>whether the layout can handle hierarchical graphs</returns>
        public abstract bool IsHierarchical();

        /// <summary>
        /// Calculates and applies the layout to the given <paramref name="layoutNodes"/>.
        /// </summary>
        /// <param name="layoutNodes">nodes for which to apply the layout</param>
        /// <param name="rectangle">The ground where all nodes will be placed.</param>
        /// <param name="centerPosition">The center of the rectangle in worldspace.</param>
        ///
        public void Apply(IEnumerable<ILayoutNode> layoutNodes, Vector2 rectangle, Vector3 centerPosition)
        {
            Dictionary<ILayoutNode, NodeTransform> layout = Layout(layoutNodes, rectangle);
            ApplyLayoutNodeTransform(layout);
            // FIXME: Not needed for some layouts because they already scale the nodes so that they fit into rectangle.
            ScaleXZ(layoutNodes, rectangle.x, rectangle.y);
            MoveTo(layoutNodes, centerPosition);
            Stack(layoutNodes, centerPosition.y);
        }

        /// <summary>
        /// Applies the <see cref="NodeTransform"/> values to its corresponding <see cref="ILayoutNode"/>.
        /// </summary>
        /// <param name="layout">the calculated layout to be applied</param>
        private static void ApplyLayoutNodeTransform<T>(Dictionary<T, NodeTransform> layout) where T : ILayoutNode
        {
            foreach (KeyValuePair<T, NodeTransform> entry in layout)
            {
                T node = entry.Key;
                NodeTransform transform = entry.Value;
                // y co-ordinate of transform.position refers to the ground
                Vector3 position = transform.Position;
                position.y += transform.Scale.y / 2.0f;
                node.CenterPosition = position;
                node.LocalScale = transform.Scale;
                node.Rotation = transform.Rotation;
            }
        }

        #region Padding
        /// <summary>
        /// Some padding will be added between nodes. That padding depends upon the minimum
        /// of the width and depth of a node, multiplied by this factor.
        /// </summary>
        private const float paddingFactor = 0.05f;

        /// <summary>
        /// The minimal padding between nodes in absolute (world space) terms.
        /// </summary>
        private const float minimimalAbsolutePadding = 0.01f;

        /// <summary>
        /// The maximal padding between nodes in absolute (world space) terms.
        /// </summary>
        private const float maximalAbsolutePadding = 0.1f;

        /// <summary>
        /// Returns the padding to be added around a node to separate it visually
        /// from its neigboaring nodes.
        ///
        /// The resulting padding will clamped into <see cref="minimimalAbsolutePadding"/>
        /// and <see cref="maximalAbsolutePadding"/>.
        /// </summary>
        /// <remarks>The actual padding added between two neighboaring nodes
        /// may be double this value if padding was added for both nodes.</remarks>
        /// <param name="width">the width of the node</param>
        /// <param name="depth">the depth of the node</param>
        /// <returns>padding to be added</returns>
        protected static float Padding(float width, float depth)
        {
            return Mathf.Clamp(Mathf.Min(width, depth) * paddingFactor, minimimalAbsolutePadding, maximalAbsolutePadding);
        }

        /// <summary>
        /// The inverse function of <see cref="Padding(float, float)"/>. It returns
        /// the padding that was added to a node to obtain an area with <paramref name="widthWithPadding"/>
        /// and <paramref name="depthWithPadding"/>. This padding needs to removed
        /// from this area to get the original node area before padding was added.
        ///
        /// Let o = (w, d) be the original area and p = Padding(w, d).
        /// Let n = (w+p, d+p) be the area where padding p was added.
        /// Let p' = ReversePadding(w+p, d+p). Then (w+p-p', d+p-p') = (w, p).
        ///
        /// The resulting padding will clamped into <see cref="minimimalAbsolutePadding"/>
        /// and <see cref="maximalAbsolutePadding"/>.
        /// </summary>
        /// <param name="widthWithPadding">the width of the node after padding was added</param>
        /// <param name="depthWithPadding">the depth of the node after padding was added</param>
        /// <returns>padding to be added</returns>
        protected static float ReversePadding(float widthWithPadding, float depthWithPadding)
        {
            float min = Mathf.Min(widthWithPadding, depthWithPadding);
            return Mathf.Clamp((min * paddingFactor)/(1 + paddingFactor), minimimalAbsolutePadding, maximalAbsolutePadding);
        }

        #endregion Padding
    }
}
