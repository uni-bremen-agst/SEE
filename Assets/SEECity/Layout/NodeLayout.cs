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
        protected float LevelLift(LayoutNode node)
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
        public abstract Dictionary<LayoutNode, NodeTransform> Layout(ICollection<LayoutNode> layoutNodes);

        /// <summary>
        /// Adds the given <paramref name="offset"/> to every node position in the given <paramref name="layout"/>.
        /// </summary>
        /// <param name="layout">node layout to be adjusted</param>
        /// <param name="offset">offset to be added</param>
        /// <returns><paramref name="layout"/> where <paramref name="offset"/> has been added to each position</returns>
        public static Dictionary<LayoutNode, NodeTransform> Move(Dictionary<LayoutNode, NodeTransform> layout, Vector3 offset)
        {
            Dictionary<LayoutNode, NodeTransform> result = new Dictionary<LayoutNode, NodeTransform>();
            foreach(var entry in layout)
            {
                NodeTransform transform = entry.Value;
                transform.position += offset;
                result[entry.Key] = transform;
            }
            return result;
        }

        /// <summary>
        /// If true, the layout can handle both inner nodes and leaves; otherwise
        /// only leaves.
        /// </summary>
        /// <returns>whether the layout can handle hierarchical graphs</returns>
        public abstract bool IsHierarchical();
    }
}
