using System.Collections.Generic;
using SEE.DataModel;
using SEE.GO;
using UnityEngine;

namespace SEE.Layout
{
    /// <summary>
    /// Implementation of LayoutNode. It is simply a wrapper to game objects
    /// created for inner nodes or leaf nodes.
    /// </summary>
    public class GameNode : LayoutNode
    {
        /// <summary>
        /// The game object his layout node encapsulates.
        /// </summary>
        private readonly GameObject gameObject;
        /// <summary>
        /// The node factory that created the game object. Required to obtain scaling information.
        /// </summary>
        private readonly NodeFactory leafNodeFactory;
        /// <summary>
        /// The graph node attached to gameObject.
        /// </summary>
        private readonly Node node;
        /// <summary>
        /// The mapping from graph nodes onto game nodes. Every game node created by any of the
        /// constructors of this class will be added to it. 
        /// </summary>
        private readonly Dictionary<Node, GameNode> to_layout_node = new Dictionary<Node, GameNode>();

        /// <summary>
        /// The tree level of the node. Roots have level 0, for all other nodes the level is the 
        /// distance to its root.
        /// </summary>
        private int level;

        /// <summary>
        /// Constructor in cases where <paramref name="gameObject"/> is not a leaf and was created by 
        /// a <paramref name="nodeFactory"/>..
        /// </summary>
        /// <param name="to_layout_node">the mapping of graph nodes onto GameNodes this node should be added to</param>
        /// <param name="gameObject">the game object this layout node represents</param>
        /// <param name="nodeFactory">the node factory that created <paramref name="gameObject"/>; 
        /// may be null for inner nodes</param>
        public GameNode(Dictionary<Node, GameNode> to_layout_node, GameObject gameObject, NodeFactory nodeFactory)
        {
            this.gameObject = gameObject;
            this.leafNodeFactory = nodeFactory;
            this.node = this.gameObject.GetComponent<NodeRef>().node;
            this.to_layout_node = to_layout_node;
            to_layout_node[node] = this;
        }

        /// <summary>
        /// Constructor in cases where <paramref name="gameObject"/> is an inner not created by a 
        /// <paramref name="nodeFactory"/> for leaf nodes.
        /// </summary>
        /// <param name="to_layout_node">the mapping of graph nodes onto GameNodes this node should be added to</param>
        /// <param name="gameObject">the game object this layout node represents</param>
        public GameNode(Dictionary<Node, GameNode> to_layout_node, GameObject gameObject)
            : this(to_layout_node, gameObject, null)
        {
        }

        /// <summary>
        /// The parent of this node. May be null if it has none.
        /// 
        /// Note: Parent may be null even if the underlying graph node actually has a 
        /// parent in the graph, yet that parent was never passed to any of the 
        /// constructors of this class. For instance, non-hierarchical layouts will 
        /// receive only leaf nodes, i.e., their parents will not be passed to the 
        /// layout, in which case Parent will be null.
        /// </summary>
        public LayoutNode Parent
        {
            get
            {
                if (node.Parent == null)
                {
                    // The node does not have a parent in the original graph.
                    return null;
                }
                else if (to_layout_node.TryGetValue(node.Parent, out GameNode result))
                {
                    // The node has a parent in the original graph and that parent was passed to the layout.
                    return result;
                }
                else
                {
                    // The node has a parent in the original graph, but it was not passed to the layout.
                    return null;
                }
            }
        }

        /// <summary>
        /// The tree level of the node. Roots have level 0, for all other nodes the level is the 
        /// distance to its root.
        /// </summary>
        public int Level
        {
            get => level;
            set => level = value;
        }

        /// <summary>
        /// Yields the game object corresponding to this layout node.
        /// </summary>
        /// <returns>game object corresponding to this layout node</returns>
        public GameObject GetGameObject()
        {
            return gameObject;
        }

        /// <summary>
        /// The scale of this node.
        /// </summary>
        /// <returns>scale of this node</returns>
        public Vector3 GetSize()
        {
            if (node.IsLeaf())
            {
                return leafNodeFactory.GetSize(gameObject);
            }
            else
            {
                return gameObject.transform.localScale;
            }
        }

        /// <summary>
        /// Whether this node represents a leaf.
        /// </summary>
        /// <returns>true if this node represents a leaf</returns>
        public bool IsLeaf => node.IsLeaf();

        /// <summary>
        /// A unique ID for a node: the LinkName of the graph node underlying this layout node.
        /// </summary>
        /// <returns>unique ID for this node</returns>
        public string LinkName { get => node.LinkName; }

        /// <summary>
        /// The set of children of this node. Note: For nodes for which IsLeaf
        /// returns true, the empty list will be returned. 
        /// </summary>
        /// <returns>children of this node</returns>
        public IList<LayoutNode> Children()
        {
            IList<LayoutNode> children = new List<LayoutNode>();
            if (!IsLeaf)
            {
                foreach (Node node in node.Children())
                {
                    children.Add(to_layout_node[node]);
                }
            }
            return children;
        }
    }
}