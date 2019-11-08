using SEE.DataModel;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.Layout
{
    public abstract class NodeLayout
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="groundLevel">the y co-ordinate setting the ground level; all nodes will be
        /// placed on this level</param>
        /// <param name="leafNodeFactory">the factory used to created leaf nodes</param>
        public NodeLayout(float groundLevel,
                          NodeFactory leafNodeFactory)
        {
            this.groundLevel = groundLevel;
            this.leafNodeFactory = leafNodeFactory;
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
        /// The factory that createed visual representations of graph nodes for leaves 
        /// (e.g., cubes or CScape buildings).
        /// </summary>
        protected readonly NodeFactory leafNodeFactory;

        /// <summary>
        /// The height of circles (y co-ordinate) drawn for inner nodes.
        /// </summary>
        protected const float circleHeight = 0.1f;

        /// <summary>
        /// If inner nodes are represented as visible objects covering their total area
        /// and the visualizations of those inner nodes are stacked in a hierarchical layout,
        /// their visualizations should not be on the same level; otherwise they will hide
        /// each other. For these reasons, the inner nodes will be slightly lifted along the 
        /// y axis according to their tree depth so that they can be stacked visually 
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
        protected float LevelLift(Node node)
        {
            return node.Level * levelIncreaseForInnerNodes;
        }

        /// <summary>
        /// Yields layout information for all nodes given.
        /// For every game object g in gameNodes: result[g] is the node transforms,
        /// i.e., the game object's position and scale.
        /// 
        /// Precondition: each game node must contain a NodeRef component.
        /// </summary>
        /// <param name="gameNodes">set of game nodes for which to compute the layout</param>
        /// <returns>node layout</returns>
        public abstract Dictionary<GameObject, NodeTransform> Layout(ICollection<GameObject> gameNodes);

        /// <summary>
        /// A mapping of graph nodes onto their game nodes.
        /// </summary>
        protected Dictionary<Node, GameObject> to_game_node;

        /// <summary>
        /// Returns a mapping of graph nodes onto their game nodes.
        /// 
        /// Precondition: Every game object must contain a NodeRef component referencing 
        /// a graph node.
        /// </summary>
        /// <param name="gameNodes">the game nodes to be mapped (target objects of the mapping)</param>
        /// <returns>mapping of graph nodes onto their game nodes</returns>
        protected static Dictionary<Node, GameObject> NodeMapping(ICollection<GameObject> gameNodes)
        {
            Dictionary<Node, GameObject> map = new Dictionary<Node, GameObject>();
            foreach (GameObject gameNode in gameNodes)
            {
                Node node = gameNode.GetComponent<NodeRef>().node;
                map[node] = gameNode;
            }
            return map;
        }

        /// <summary>
        /// Returns all root graph nodes within gameNodes.
        /// </summary>
        /// <param name="gameNodes">game nodes for which to determine root nodes</param>
        /// <returns>all root graph nodes within gameNodes</returns>
        protected static List<Node> GetRoots(ICollection<GameObject> gameNodes)
        {
            List<Node> roots = new List<Node>();

            foreach (GameObject gameObject in gameNodes)
            {
                Node node = gameObject.GetComponent<NodeRef>().node;
                if (node.IsRoot())
                {
                    roots.Add(node);
                }
            }
            return roots;
        }
    }
}
