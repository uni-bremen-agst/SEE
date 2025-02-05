using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

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
        /// Yields the layout for all given <paramref name="layoutNodes"/>.
        /// For every node n in <paramref name="layoutNodes"/>: result[n] is the node transform,
        /// i.e., the game object's position, scale, and rotation. It is this node transform
        /// that is calculated by this method.
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
        /// Calculates and applies the layout to the given <paramref name="layoutNodes"/>.
        /// </summary>
        /// <param name="layoutNodes">nodes for which to apply the layout</param>
        /// <param name="centerPosition">The center of the rectangle in worldspace.</param>
        /// <param name="rectangle">The rectangle in which all nodes will be placed.</param>
        ///
        public void Apply(IEnumerable<ILayoutNode> layoutNodes, Vector3 centerPosition, Vector2 rectangle)
        {
            if (!layoutNodes.Any())
            {
                return;
            }
            Dictionary<ILayoutNode, NodeTransform> layout = Layout(layoutNodes, rectangle);
            // FIXME: We can remove the following assertion later.
            Assert.IsTrue(new HashSet<ILayoutNode>(layout.Keys).SetEquals(new HashSet<ILayoutNode>(layoutNodes)));

            // FIXME: Not needed for some layouts because they already scale the nodes so that they fit into rectangle.
            Box box = Bounding3DBox(layout.Values);
            //Test(layout.Values);
            //MoveTo(layout.Values, centerPosition, box);
            float factor = ScaleXZ(layout.Values, rectangle.x, rectangle.y, box);
            //Stack(layout, centerPosition.y);

            ApplyLayoutNodeTransform(layout);
        }

        private void Test(Dictionary<ILayoutNode, NodeTransform>.ValueCollection values)
        {
            foreach (NodeTransform nodeTransform in values)
            {
                nodeTransform.ScaleXZBy(0);
            }
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
                // y co-ordinate of transform.GroundCenter refers to the ground
                Vector3 position = transform.GroundCenter;
                position.y += transform.Scale.y / 2.0f;
                node.CenterPosition = position;
                node.LocalScale = transform.Scale;
                node.Rotation = transform.Rotation;
            }
        }

        /// <summary>
        /// The y co-ordinate of the ground level where all nodes will be placed initially
        /// by calling <see cref="Layout(IEnumerable{ILayoutNode}, Vector2)"/>.
        /// </summary>
        protected const float groundLevel = 0.0f;

        #region Modifiers

        /// <summary>
        /// A 3D box enclosing all nodes.
        /// </summary>
        /// <param name="LeftFrontCorner">the left front corner</param>
        /// <param name="RightBackCorner">the right back corner</param>
        record Box(Vector3 LeftFrontCorner, Vector3 RightBackCorner);

        /// <summary>
        /// Scales the width and depth of all nodes in <paramref name="layout"/> so that they fit into
        /// a rectangled defined by <paramref name="width"/> and <paramref name="depth"/>.
        /// The aspect ratio of every node is maintained. Only the NodeTransforms (values of the
        /// dictionary) are affected.
        /// </summary>
        /// <param name="layout">layout nodes to be scaled</param>
        /// <param name="width">the absolute width (x axis) the required space for the laid out nodes must have</param>
        /// <param name="depth">the absolute depth (z axis) the required space for the laid out nodes must have</param>
        /// <returns>the factor by which the scale of edge node was multiplied</returns>
        private static float ScaleXZ(ICollection<NodeTransform> layout, float width, float depth, Box box)
        {
            float actualWidth = box.RightBackCorner.x - box.LeftFrontCorner.x;
            float actualDepth = box.RightBackCorner.z - box.LeftFrontCorner.z;

            float scaleFactor = Mathf.Min(width / actualWidth, depth / actualDepth);

            foreach (NodeTransform nodeTransform in layout)
            {
                nodeTransform.ScaleXZBy(scaleFactor);
            }
            return scaleFactor;
        }

        /// <summary>
        /// Moves all <paramref name="layout"/> together to the cube defined by <paramref name="target"/>.
        /// More precisely, the bounding box enclosing all nodes in <paramref name="layout"/> will be
        /// translated (moved such that there is no change in size or shape) to a new location defined by
        /// <paramref name="target"/> such that the center of the bounding box at the new location
        /// is at <paramref name="target"/>.
        /// </summary>layout">layout to be moved</param>
        /// <param name="target">worldspace center point where to move the <paramref name="layout"/></param>
        private static void MoveTo(ICollection<NodeTransform> layout, Vector3 target, Box box)
        {
            // centerPosition is the worldspace center of the bounding 3D box (cube) enclosing all layoutNodes.
            // The center of the cube is the midpoint of the line segment connecting its
            // left front corner and its right back corner. The coordinates of the center
            // can be calculated using the midpoint formula:
            Vector3 centerPosition = (box.RightBackCorner + box.LeftFrontCorner) / 2.0f;
            // The offest is the vector from the current center of the bounding box to the
            // target center point and needs to be added to all nodes.
            Vector3 offset = target - centerPosition;
            // It is assumed that target.y is the lowest point of the city. FIXME????
            foreach (NodeTransform nodeTransform in layout)
            {
                nodeTransform.TranslateBy(offset);
            }
        }

        /// <summary>
        /// Stacks all nodes in the <paramref name="layout"/> onto each other with <paramref name="levelDelta"/>
        /// in between parent and children (children are on top of their parent) where the initial
        /// y co-ordinate ground position of the roof nodes is specified by <paramref name="groundLevel"/>.
        /// The x and z co-ordinates of the nodes are not changed.
        /// </summary>
        /// <param name="layout">layout whose nodes are to be stacked onto each other</param>
        /// <param name="groundLevel">target y co-ordinate ground position of the layout nodes</param>
        /// <param name="levelDelta">the y distance between parents and their children</param>
        private static void Stack(Dictionary<ILayoutNode, NodeTransform> layout, float groundLevel, float levelDelta = 0.001f)
        {
            // position all root nodes at groundLevel
            foreach (ILayoutNode root in layout.Keys)
            {
                if (root.Parent == null)
                {
                    float newRoofY = PutOn(layout[root], groundLevel) + levelDelta;
                    // Continue with the children
                    foreach (ILayoutNode child in root.Children())
                    {
                        Stack(child, newRoofY);
                    }
                }
            }
            return;

            void Stack(ILayoutNode layoutNode, float level)
            {
                float newRoofY = PutOn(layout[layoutNode], level) + levelDelta;
                foreach (ILayoutNode child in layoutNode.Children())
                {
                    Stack(child, newRoofY);
                }
            }

            static float PutOn(NodeTransform nodeTransform, float level)
            {
                nodeTransform.LiftTo(level);
                return nodeTransform.Roof;
            }
        }

        /// <summary>
        /// Returns the bounding 3D box enclosing all given <paramref name="nodeTransforms"/>.
        /// </summary>
        /// <param name="nodeTransforms">the list of layout node transforms that are enclosed in the resulting bounding 3D box</param>
        /// <param name="left">the left lower front corner of the bounding box in worldspace</param>
        /// <param name="right">the right upper back corner of the bounding box in worldspace</param>
        private static Box Bounding3DBox(IEnumerable<NodeTransform> nodeTransforms)
        {
            Vector3 left = new(Mathf.Infinity, Mathf.Infinity, Mathf.Infinity);
            Vector3 right = new(Mathf.NegativeInfinity, Mathf.NegativeInfinity, Mathf.NegativeInfinity);

            foreach (NodeTransform node in nodeTransforms)
            {
                Vector3 extent = node.Scale / 2.0f;
                // Note: position denotes the center of the *ground* of the object;
                // this has consequences on the interpretation of the y co-ordinate.
                Vector3 position = node.GroundCenter;
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
                    // lower y co-ordinate of node; note: position.y is the ground
                    float y = position.y;
                    if (y < left.y)
                    {
                        left.y = y;
                    }
                }
                {
                    // upper y co-ordinate of node; note: position.y is the ground
                    float y = position.y + 2 * extent.y;
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
            return new Box(left, right);
        }
        #endregion Modifiers

        #region Hierarchy
        /// <summary>
        /// The roots of the subtrees of the original graph that are to be laid out.
        /// A node is considered a root if it has either no parent in the original
        /// graph or its parent is not contained in the set of nodes to be laid out.
        /// </summary>
        protected IList<ILayoutNode> Roots;

        /// <summary>
        /// Returns the maximal depth of the forest with the given root nodes.
        /// If roots.Count == 0, 0 is the maximal depth. If there is at least
        /// one root, the minimum value of the maximal depth is 1.
        /// </summary>
        /// <param name="roots">set of root tree nodes of the forest</param>
        /// <returns>maximal depth of the forest</returns>
        protected static int MaxDepth(List<ILayoutNode> roots)
        {
            int result = 0;
            foreach (ILayoutNode root in roots)
            {
                int depth = MaxDepth(root);
                if (depth > result)
                {
                    result = depth;
                }
            }
            return result;
        }

        /// <summary>
        /// Returns the maximal depth of the tree rooted by given node. The depth of
        /// a tree with only one node is 1.
        /// </summary>
        /// <param name="node">root node of the tree</param>
        /// <returns>maximal depth of the tree</returns>
        protected static int MaxDepth(ILayoutNode node)
        {
            int result = 0;
            foreach (ILayoutNode child in node.Children())
            {
                int depth = MaxDepth(child);
                if (depth > result)
                {
                    result = depth;
                }
            }
            return result + 1;
        }

        #endregion Hierarchy

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
