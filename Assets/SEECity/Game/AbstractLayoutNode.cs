using SEE.DataModel;
using SEE.Layout;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.Game
{
    /// <summary>
    /// Abstract super class for layout nodes representing a graph node.
    /// Implements the methods shared by all subclasses.
    /// </summary>
    public abstract class AbstractLayoutNode : ILayoutNode
    {
        /// <summary>
        /// The graph node to be laid out.
        /// </summary>
        protected readonly Node node;

        /// <summary>
        /// Constructor setting the graph <paramref name="node"/> corresponding to this layout node
        /// and the <paramref name="to_layout_node"/> mapping. The mapping maps all graph nodes to be
        /// laid out onto their corresponding layout node and is shared among all layout nodes.
        /// The given <paramref name="node"/> will be added to <paramref name="to_layout_node"/>.
        /// </summary>
        /// <param name="node">graph node corresponding to this layout node</param>
        /// <param name="to_layout_node">the mapping of graph nodes onto LayoutNodes this node should be added to</param>
        protected AbstractLayoutNode(Node node, Dictionary<Node, ILayoutNode> to_layout_node)
        {
            this.node = node;
            this.to_layout_node = to_layout_node;
            this.to_layout_node[node] = this;
        }

        /// <summary>
        /// The tree level of the node. Roots have level 0, for all other nodes the level is the 
        /// distance to its root.
        /// </summary>
        private int level;

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
        /// The mapping from graph nodes onto layout nodes. Every layout node created by any of the
        /// constructors of this class or one of its subclasses will be added to it. All layout nodes 
        /// given to the layout will refer to the same mapping, i.e., to_layout_node is the same for all. 
        /// The mapping will be given by the constructor.
        /// </summary>
        protected readonly Dictionary<Node, ILayoutNode> to_layout_node;

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
        /// The parent of this node. May be null if it has none.
        /// 
        /// Note: Parent may be null even if the underlying graph node actually has a 
        /// parent in the graph, yet that parent was never passed to any of the 
        /// constructors of this class. For instance, non-hierarchical layouts will 
        /// receive only leaf nodes, i.e., their parents will not be passed to the 
        /// layout, in which case Parent will be null.
        /// </summary>
        public ILayoutNode Parent
        {
            get
            {
                if (node.Parent == null)
                {
                    // The node does not have a parent in the original graph.
                    return null;
                }
                else if (to_layout_node.TryGetValue(node.Parent, out ILayoutNode result))
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
        /// The set of children of this node. Note: For nodes for which IsLeaf
        /// returns true, the empty list will be returned. 
        /// </summary>
        /// <returns>children of this node</returns>
        public ICollection<ILayoutNode> Children()
        {
            IList<ILayoutNode> children = new List<ILayoutNode>();
            if (!IsLeaf)
            {
                foreach (Node node in node.Children())
                {
                    children.Add(to_layout_node[node]);
                }
            }
            return children;
        }

        public ICollection<ILayoutNode> Successors
        {
            get
            {
                ICollection<ILayoutNode> successors = new List<ILayoutNode>();
                foreach (Edge edge in node.Outgoings)
                {
                    successors.Add(to_layout_node[edge.Target]);
                }
                return successors;
            }
        }

        // Features defined by LayoutNode that must be implemented by subclasses of 
        public abstract Vector3 Scale { get; set; }
        public abstract Vector3 CenterPosition { get; set; }
        public abstract float Rotation { get; set; }
        public abstract Vector3 Roof { get; }
        public abstract Vector3 Ground { get; }
    }
}