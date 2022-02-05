using SEE.DataModel.DG;
using SEE.Layout.NodeLayouts.Cose;
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
        /// Constructor.
        /// </summary>
        /// <param name="groundLevel">the y co-ordinate setting the ground level; all nodes will be
        /// placed on this level</param>
        public NodeLayout(float groundLevel)
        {
            this.groundLevel = groundLevel;
        }

        /// <summary>
        /// Name of the layout. Must be set by all concrete subclasses.
        /// </summary>
        protected string name = "";

        /// <summary>
        /// The unique name of a layout.
        /// </summary>
        public string Name
        {
            get => name;
        }

        /// <summary>
        /// The y co-ordinate of the ground where blocks are placed.
        /// </summary>
        protected readonly float groundLevel;

        public float Groundlevel { get => groundLevel; }

        public float InnerNodeHeight { get => innerNodeHeight; }

        /// <summary>
        /// The height of inner nodes (y co-ordinate).
        /// </summary>
        protected const float innerNodeHeight = 0.01f;

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
        protected const float levelIncreaseForInnerNodes = 0.015f;

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
            return node.Level * levelIncreaseForInnerNodes;
        }

        /// <summary>
        /// Yields the layout for all given <paramref name="layoutNodes"/>.
        /// For every node n in <paramref name="layoutNodes"/>: result[n] is the node transform,
        /// i.e., the game object's position, scale, and rotation.
        ///
        /// IMPORTANT NOTE: The y co-ordinate of the position in NodeTransform will
        /// be interpreted as the ground position of the game object (unlike in Unity
        /// where it is the center height).
        /// </summary>
        /// <param name="layoutNodes">set of layout nodes for which to compute the layout</param>
        /// <returns>node layout</returns>
        ///
        public abstract Dictionary<ILayoutNode, NodeTransform> Layout(IEnumerable<ILayoutNode> layoutNodes);

        /// <summary>
        /// Yields the layout for all given <paramref name="layoutNodes"/>.
        /// For every node n in <paramref name="layoutNodes"/>: result[n] is the node transform,
        /// i.e., the game object's position, scale, and rotation.
        ///
        /// <paramref name="edges"/> contains all edges of the overlying graph and <paramref name="sublayouts"/> contains all sublayouts
        /// </summary>
        /// <param name="layoutNodes"></param>
        /// <param name="edges"></param>
        /// <param name="sublayouts"></param>
        /// <returns></returns>
        public abstract Dictionary<ILayoutNode, NodeTransform> Layout(ICollection<ILayoutNode> layoutNodes, ICollection<Edge> edges, ICollection<SublayoutLayoutNode> sublayouts);

        /// <summary>
        /// Adds the given <paramref name="offset"/> to every node position in the given <paramref name="layout"/>.
        /// </summary>
        /// <param name="layout">node layout to be adjusted</param>
        /// <param name="offset">offset to be added</param>
        /// <returns><paramref name="layout"/> where <paramref name="offset"/> has been added to each position</returns>
        public static Dictionary<ILayoutNode, NodeTransform> Move(Dictionary<ILayoutNode, NodeTransform> layout, Vector3 offset)
        {
            Dictionary<ILayoutNode, NodeTransform> result = new Dictionary<ILayoutNode, NodeTransform>();
            foreach (KeyValuePair<ILayoutNode, NodeTransform> entry in layout)
            {
                NodeTransform transform = entry.Value;
                transform.position += offset;
                result[entry.Key] = transform;
            }
            return result;
        }

        /// <summary>
        /// Adds the given <paramref name="target"/> to every node in <paramref name="layoutNodes"/>.
        /// </summary>
        /// <param name="layoutNodes">layout nodes to be adjusted</param>
        /// <param name="target">offset to be added</param>
        public static void MoveTo(IEnumerable<ILayoutNode> layoutNodes, Vector3 target)
        {
            IList<ILayoutNode> layoutList = layoutNodes.ToList();
            Bounding3DBox(layoutList.ToList(), out Vector3 left, out Vector3 right);
            // The center of the bounding 3D box enclosing all layoutNodes
            Vector3 centerPosition = (right + left) / 2.0f;
            Vector3 offset = target - centerPosition;
            // It is assumed that target.y is the lowest point of the city.
            offset.y = target.y;
            foreach (ILayoutNode layoutNode in layoutList)
            {
                layoutNode.CenterPosition += offset;
            }
        }

        /// <summary>
        /// Scales all nodes in <paramref name="layoutNodes"/> so that the total width
        /// of the layout (along the x axis) equals <paramref name="width"/>.
        /// The aspect ratio of every node is maintained.
        /// </summary>
        /// <param name="layoutNodes">layout nodes to be scaled</param>
        /// <param name="width">the absolute width (x axis) the required space for the laid out nodes must have</param>
        /// <returns>the factor by which the scale of edge node was multiplied</returns>
        public static float Scale(IEnumerable<ILayoutNode> layoutNodes, float width)
        {
            IList<ILayoutNode> layoutNodeList = layoutNodes.ToList();
            Bounding3DBox(layoutNodeList.ToList(), out Vector3 left, out Vector3 right);
            float currentWidth = right.x - left.x;
            float scaleFactor = width / currentWidth;
            foreach (ILayoutNode layoutNode in layoutNodeList)
            {
                layoutNode.ScaleBy(scaleFactor);
                // The x/z co-ordinates must be adjusted after scaling
                Vector3 newPosition = layoutNode.CenterPosition * scaleFactor;
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
        public static void Stack(IEnumerable<ILayoutNode> layoutNodes, float groundLevel, float levelDelta = 0.001f)
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
        /// <param name="left">the left lower front corner of the bounding box</param>
        /// <param name="right">the right upper back corner of the bounding box</param>
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

                foreach (ILayoutNode go in layoutNodes)
                {
                    Vector3 extent = go.AbsoluteScale / 2.0f;
                    // Note: position denotes the center of the object
                    Vector3 position = go.CenterPosition;
                    {
                        // left x co-ordinate of go
                        float x = position.x - extent.x;
                        if (x < left.x)
                        {
                            left.x = x;
                        }
                    }
                    {   // right x co-ordinate of go
                        float x = position.x + extent.x;
                        if (x > right.x)
                        {
                            right.x = x;
                        }
                    }
                    {
                        // lower y co-ordinate of go
                        float y = position.y - extent.y;
                        if (y < left.y)
                        {
                            left.y = y;
                        }
                    }
                    {
                        // upper y co-ordinate of go
                        float y = position.y + extent.y;
                        if (y > right.y)
                        {
                            right.y = y;
                        }
                    }
                    {
                        // front z co-ordinate of go
                        float z = position.z - extent.z;
                        if (z < left.z)
                        {
                            left.z = z;
                        }
                    }
                    {
                        // back z co-ordinate of go
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
        /// If true, the layout needs the edges to calculate the Layout
        /// </summary>
        /// <returns></returns>
        public abstract bool UsesEdgesAndSublayoutNodes();

        /// <summary>
        /// Calculates and applies the layout to the given <paramref name="layoutNodes"/>.
        /// </summary>
        /// <param name="layoutNodes">nodes for which to apply the layout</param>
        public void Apply(IEnumerable<ILayoutNode> layoutNodes)
        {
            ApplyLayoutNodeTransform(Layout(layoutNodes));
        }

        /// <summary>
        /// Calculates and applies the layout to the given <paramref name="layoutNodes"/>.
        /// </summary>
        /// <param name="layoutNodes">nodes for which to apply the layout</param>
        /// <param name="edges">edges of the underlying graph</param>
        /// <param name="sublayouts">the sublayouts for the layout</param>
        public void Apply(ICollection<ILayoutNode> layoutNodes, ICollection<Edge> edges, ICollection<SublayoutLayoutNode> sublayouts)
        {
            ApplyLayoutNodeTransform(Layout(layoutNodes, edges, sublayouts));
        }

        /// <summary>
        /// Applies the <see cref="NodeTransform"/> values to its corresponding <see cref="ILayoutNode"/>.
        /// </summary>
        /// <param name="layout">the calculated layout to be applied</param>
        private void ApplyLayoutNodeTransform<T>(Dictionary<T, NodeTransform> layout) where T : ILayoutNode
        {
            foreach (KeyValuePair<T, NodeTransform> entry in layout)
            {
                T node = entry.Key;
                NodeTransform transform = entry.Value;
                // y co-ordinate of transform.position refers to the ground
                Vector3 position = transform.position;
                position.y += transform.scale.y / 2.0f;
                node.CenterPosition = position;
                node.LocalScale = transform.scale;
                node.Rotation = transform.rotation;
            }
        }
    }
}
