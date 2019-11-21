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
