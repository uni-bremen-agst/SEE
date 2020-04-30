using System.Collections.Generic;
using UnityEngine;

namespace SEE.Layout
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
        public abstract Dictionary<ILayoutNode, NodeTransform> Layout(ICollection<ILayoutNode> layoutNodes);

        /// <summary>
        /// Adds the given <paramref name="offset"/> to every node position in the given <paramref name="layout"/>.
        /// </summary>
        /// <param name="layout">node layout to be adjusted</param>
        /// <param name="offset">offset to be added</param>
        /// <returns><paramref name="layout"/> where <paramref name="offset"/> has been added to each position</returns>
        public static Dictionary<ILayoutNode, NodeTransform> Move(Dictionary<ILayoutNode, NodeTransform> layout, Vector3 offset)
        {
            Dictionary<ILayoutNode, NodeTransform> result = new Dictionary<ILayoutNode, NodeTransform>();
            foreach(var entry in layout)
            {
                NodeTransform transform = entry.Value;
                transform.position += offset;
                result[entry.Key] = transform;
            }
            return result;
        }

        /// <summary>
        /// Adds the given <paramref name="offset"/> to every node in <paramref name="layoutNodes"/>.
        /// </summary>
        /// <param name="layoutNodes">layout nodes to be adjusted</param>
        /// <param name="offset">offset to be added</param>
        public static void Move(ICollection<ILayoutNode> layoutNodes, Vector3 offset)
        {
            foreach (ILayoutNode layoutNode in layoutNodes)
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
        public static void Scale(ICollection<ILayoutNode> layoutNodes, float width)
        {
            BoundingBox(layoutNodes, out Vector2 leftLowerCorner, out Vector2 rightUpperCorner);
            float currentWidth = rightUpperCorner.x - leftLowerCorner.x;
            float scaleFactor = width / currentWidth;
            foreach (ILayoutNode layoutNode in layoutNodes)
            {
                layoutNode.Scale *= scaleFactor;
                // The x/z co-ordinates must be adjusted after scaling, but we do maintain the height
                Vector3 newPosition = layoutNode.CenterPosition * scaleFactor;
                //newPosition.y = layoutNode.CenterPosition.y;
                layoutNode.CenterPosition = newPosition;
            }
        }

        /// <summary>
        /// Returns the bounding box (2D rectangle) enclosing all given <paramref name="layoutNodes"/>.
        /// </summary>
        /// <param name="layoutNodes">the list of layout nodes that are enclosed in the resulting bounding box</param>
        /// <param name="leftLowerCorner">the left lower front corner (x axis in 3D space) of the bounding box</param>
        /// <param name="rightUpperCorner">the right lower back corner (z axis in 3D space) of the bounding box</param>
        public static void BoundingBox(ICollection<ILayoutNode> layoutNodes, out Vector2 leftLowerCorner, out Vector2 rightUpperCorner)
        {
            if (layoutNodes.Count == 0)
            {
                leftLowerCorner = Vector2.zero;
                rightUpperCorner = Vector2.zero;
            }
            else
            {
                leftLowerCorner = new Vector2(Mathf.Infinity, Mathf.Infinity);
                rightUpperCorner = new Vector2(Mathf.NegativeInfinity, Mathf.NegativeInfinity);

                foreach (ILayoutNode go in layoutNodes)
                {
                    Vector3 extent = go.Scale;
                    // Note: position denotes the center of the object
                    Vector3 position = go.CenterPosition;
                    {
                        // x co-ordinate of lower left corner
                        float x = position.x - extent.x;
                        if (x < leftLowerCorner.x)
                        {
                            leftLowerCorner.x = x;
                        }
                    }
                    {
                        // z co-ordinate of lower left corner
                        float z = position.z - extent.z;
                        if (z < leftLowerCorner.y)
                        {
                            leftLowerCorner.y = z;
                        }
                    }
                    {   // x co-ordinate of upper right corner
                        float x = position.x + extent.x;
                        if (x > rightUpperCorner.x)
                        {
                            rightUpperCorner.x = x;
                        }
                    }
                    {
                        // z co-ordinate of upper right corner
                        float z = position.z + extent.z;
                        if (z > rightUpperCorner.y)
                        {
                            rightUpperCorner.y = z;
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
        public void Apply(ICollection<ILayoutNode> layoutNodes)
        {
            Dictionary<ILayoutNode, NodeTransform> layout = Layout(layoutNodes);
            foreach (var entry in layout)
            {
                ILayoutNode node = entry.Key;
                NodeTransform transform = entry.Value;
                // y co-ordinate of transform.position refers to the ground
                Vector3 position = transform.position;
                position.y += transform.scale.y / 2.0f;
                node.CenterPosition = position;
                node.Scale = transform.scale;
                node.Rotation = transform.rotation;
            }
        }
    }
}
