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
        /// Yields the layout for all given <paramref name="layoutNodes"/>.
        /// For every node n in <paramref name="layoutNodes"/>: result[n] is the node transform,
        /// i.e., the game object's position, scale, and rotation. It is this node transform
        /// that is calculated by this method.
        ///
        /// The nodes will be placed into a rectangle whose width is the
        /// x co-ordindate of <paramref name="rectangle"/> and whose depth is the y co-ordinate
        /// of <paramref name="rectangle"/>.
        ///
        /// The center of that rectangle is given by <paramref name="centerPosition"/>.
        /// </summary>
        /// <param name="layoutNodes">set of layout nodes for which to compute the layout</param>
        /// <param name="centerPosition">The center of the rectangle in worldspace.</param>
        /// <param name="rectangle">The size of the rectangle within all nodes will be placed.</param>
        /// <returns>node layout</returns>
        public virtual Dictionary<ILayoutNode, NodeTransform> Create
            (IEnumerable<ILayoutNode> layoutNodes,
             Vector3 centerPosition,
             Vector2 rectangle)
        {
            if (!layoutNodes.Any())
            {
                return new();
            }
            Dictionary<ILayoutNode, NodeTransform> layout = Layout(layoutNodes, centerPosition, rectangle);
            // nodes will have random positions at this point; leaves will have their three dimensions set.

            // We now move and scale the layout such that it fits into the rectangle at centerPosition.
            // We need to compute the bounding box; we cannot simply use the bounding of the root note.
            // That would be possible only for layouts which express containment as spatial enclosing.
            // Yet, for EvoStreets this does not hold.
            Box box = Bounding3DBox(layout.Values);
            MoveTo(layout.Values, centerPosition, box);
            float scaleFactor = Mathf.Min(rectangle.x / box.Width, rectangle.y / box.Depth);
            /// This case is required when a <see cref="ReflexionLayout"/>
            /// is used and only a single Subroot should be displayed.
            scaleFactor = float.IsNaN(scaleFactor) ? 0f : scaleFactor;
            // Note: Scaling may not be needed for some layouts because they already scale the nodes
            // so that they fit into rectangle (e.g., tree map).
            // The box is now at centerPosition.
            ScaleXZ(layout.Values, scaleFactor, new Vector2(centerPosition.x, centerPosition.z));

            // MoveTo and ScaleXZ affect only the scales and X/Z positions of the nodes.
            // The y co-ordinates are not changed and are still the ones set by the caller.
            // We need to stack the nodes on top of each other.
            Stack(layout, centerPosition.y);
            return layout;
        }

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
        /// <param name="centerPosition">The center of the rectangle in worldspace.</param>
        /// <param name="rectangle">The size of the rectangle within all nodes will be placed.</param>
        /// <returns>node layout</returns>
        protected abstract Dictionary<ILayoutNode, NodeTransform> Layout
                                                                    (IEnumerable<ILayoutNode> layoutNodes,
                                                                     Vector3 centerPosition,
                                                                     Vector2 rectangle);

        /// <summary>
        /// Applies the <see cref="NodeTransform"/> values to its corresponding <see cref="ILayoutNode"/>.
        /// </summary>
        /// <param name="layout">the calculated layout to be applied</param>
        public static void Apply<T>(Dictionary<T, NodeTransform> layout) where T : ILayoutNode
        {
            foreach (KeyValuePair<T, NodeTransform> entry in layout)
            {
                T node = entry.Key;
                NodeTransform transform = entry.Value;
                node.CenterPosition = transform.CenterPosition;
                node.AbsoluteScale = transform.Scale;
                node.Rotation = transform.Rotation;
            }
        }

        /// <summary>
        /// Creates cubes for each node in the layout and places them at the calculated positions.
        /// This method can be used for debugging to show intermediate results of the layouting
        /// process.
        /// </summary>
        /// <param name="layout">layout to be shown</param>
        /// <param name="prefix">a prefix to be prepanded to the gameObject name</param>
        static void Draw(Dictionary<ILayoutNode, NodeTransform> layout, string prefix)
        {
            foreach (KeyValuePair<ILayoutNode, NodeTransform> entry in layout)
            {
                ILayoutNode node = entry.Key;
                NodeTransform transform = entry.Value;
                GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = prefix + "+" + node.ID;
                go.transform.position = transform.CenterPosition;
                go.transform.localScale = transform.Scale;
            }
        }

        /// <summary>
        /// The y co-ordinate of the ground level where all nodes will be placed initially
        /// by calling <see cref="Layout(IEnumerable{ILayoutNode}, Vector2)"/>.
        /// </summary>
        public const float GroundLevel = 0.0f;

        #region Modifiers

        /// <summary>
        /// A 3D box enclosing all nodes.
        /// </summary>
        /// <param name="LeftFrontCorner">the left front lower corner</param>
        /// <param name="RightBackCorner">the right back upper corner</param>
        record Box(Vector3 LeftFrontCorner, Vector3 RightBackCorner)
        {
            /// <summary>
            /// The center of the box.
            /// </summary>
            /// <remarks>The center of the box is the midpoint of the line segment connecting its
            /// left front corner and its right back corner. The coordinates of the center
            /// can be calculated using the midpoint formula.</remarks>
            public Vector3 Center => (LeftFrontCorner + RightBackCorner) / 2.0f;

            /// <summary>
            /// The height of the box.
            /// </summary>
            public float Height => RightBackCorner.y - LeftFrontCorner.y;
            /// <summary>
            /// The width of the box.
            /// </summary>
            public float Width => RightBackCorner.x - LeftFrontCorner.x;
            /// <summary>
            /// The depth of the box.
            /// </summary>
            public float Depth => RightBackCorner.z - LeftFrontCorner.z;
        }

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
        private static float ScaleXZ(ICollection<NodeTransform> layout, float scaleFactor, Vector2 centerPosition)
        {
            foreach (NodeTransform nodeTransform in layout)
            {
                nodeTransform.ScaleXZBy(scaleFactor, centerPosition);
            }
            return scaleFactor;
        }

        /// <summary>
        /// Moves all nodes in <paramref name="layout"/> together to the X/Z plane defined
        /// by <paramref name="targetGroundCenter"/>.
        /// More precisely, the bounding <paramref name="box"/> enclosing all nodes in <paramref name="layout"/> will be
        /// translated (moved such that there is no change in size or shape) to a new location
        /// such that the ground center of the bounding box at the new location is <paramref name="targetGroundCenter"/>.
        /// </summary>
        /// <param name="layout">layout to be moved</param>
        /// <param name="targetGroundCenter">worldspace center point where to move the <paramref name="layout"/></param>
        /// <param name="box">bounding box enclosing all nodes in <paramref name="layout"/></param>
        private static void MoveTo(ICollection<NodeTransform> layout, Vector3 targetGroundCenter, Box box)
        {
            // centerPosition is the worldspace center of the bounding 3D box (cube) enclosing all layoutNodes.
            Vector3 centerPosition = box.Center;
            // Note that centerPosition relates to the center of the bounding box, while
            // targetGroundCenter is the ground center of the target bounding box.
            // Hence, we need to adjust the y co-ordinate of centerPosition.
            centerPosition.y -= box.Height / 2.0f;
            // The offest is the vector from the current center of the bounding box to the
            // target center point and needs to be added to all nodes.
            Vector3 offset = targetGroundCenter - centerPosition;
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
                nodeTransform.LiftGroundTo(level);
                return nodeTransform.Roof;
            }
        }

        /// <summary>
        /// Returns the bounding 3D box enclosing all given <paramref name="nodeTransforms"/>.
        /// </summary>
        /// <param name="nodeTransforms">the list of layout node transforms that are enclosed in the resulting bounding 3D box</param>
        private static Box Bounding3DBox(IEnumerable<NodeTransform> nodeTransforms)
        {
            Vector3 left = new(Mathf.Infinity, Mathf.Infinity, Mathf.Infinity);
            Vector3 right = new(Mathf.NegativeInfinity, Mathf.NegativeInfinity, Mathf.NegativeInfinity);

            foreach (NodeTransform node in nodeTransforms)
            {
                Vector3 extent = node.Scale / 2.0f;
                // Note: position denotes the center of the *ground* of the object;
                // this has consequences on the interpretation of the y co-ordinate.
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
