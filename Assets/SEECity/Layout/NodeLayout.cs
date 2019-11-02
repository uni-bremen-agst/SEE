
using SEE.DataModel;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.Layout
{
    public abstract class NodeLayout
    {
        public NodeLayout(float groundLevel,
                          NodeFactory blockFactory)
        {
            this.groundLevel = groundLevel;
            this.blockFactory = blockFactory;
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
        /// A factory to create visual representations of graph nodes (e.g., cubes or CScape buildings).
        /// </summary>
        protected readonly NodeFactory blockFactory;

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

        protected Dictionary<Node, GameObject> to_game_node;

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
