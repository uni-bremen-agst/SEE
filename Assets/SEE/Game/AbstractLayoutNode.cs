using System.Collections.Generic;
using SEE.DataModel.DG;
using SEE.Layout;
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
        /// The game object this layout node encapsulates.
        /// </summary>
        public GameObject gameObject { get; protected set; }

        /// <summary>
        /// The underlying graph node represented by this laid out node.
        /// </summary>
        public Node ItsNode => node;

        /// <summary>
        /// Constructor setting the graph <paramref name="node"/> corresponding to this layout node
        /// and the <paramref name="toLayoutNode"/> mapping. The mapping maps all graph nodes to be
        /// laid out onto their corresponding layout node and is shared among all layout nodes.
        /// The given <paramref name="node"/> will be added to <paramref name="toLayoutNode"/>.
        /// </summary>
        /// <param name="node">graph node corresponding to this layout node</param>
        /// <param name="toLayoutNode">the mapping of graph nodes onto LayoutNodes this node should be added to</param>
        protected AbstractLayoutNode(Node node, Dictionary<Node, ILayoutNode> toLayoutNode)
        {
            this.node = node;
            this.to_layout_node = toLayoutNode;
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
        /// A unique ID for a node: the ID of the graph node underlying this layout node.
        /// </summary>
        /// <returns>unique ID for this node</returns>
        public string ID => node.ID;

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
        /// is true, the empty list will be returned.
        /// 
        /// Note: If a child of the node in the underlying graph, has no
        /// corresponding layout node (<see cref="to_layout_node"/>), it will be ignored silently. 
        /// This is useful in situation where only a subset of nodes is to be considered for a layout.
        /// </summary>
        /// <returns>children of this node</returns>
        public ICollection<ILayoutNode> Children()
        {
            IList<ILayoutNode> result;
            if (!IsLeaf)
            {
                List<Node> children = node.Children();
                result = new List<ILayoutNode>(children.Count);
                foreach (Node node in children)
                {
                    if (to_layout_node.TryGetValue(node, out ILayoutNode layoutNode))
                    {
                        result.Add(layoutNode);
                    }
                    //else
                    //{
                    //    Debug.LogError($"Child {node.ID} of {ID} has no corresponding layout node.\n");
                    //}
                }
            }
            else
            {
                result = new List<ILayoutNode>();
            }
            return result;
        }

        public void SetOrigin()
        {
            CenterPosition = relativePosition + sublayoutRoot.CenterPosition;
        }

        public void SetRelative(ILayoutNode node)
        {
            relativePosition.x -= node.CenterPosition.x;
            relativePosition.z -= node.CenterPosition.z;
            sublayoutRoot = node;
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

        // Features defined by LayoutNode that must be implemented by subclasses.
        public abstract Vector3 LocalScale { get; set; }
        public abstract Vector3 AbsoluteScale { get; }

        public abstract void ScaleBy(float factor);
        public abstract Vector3 CenterPosition { get; set; }
        public abstract float Rotation { get; set; }
        public abstract Vector3 Roof { get; }
        public abstract Vector3 Ground { get; }

        private Vector3 relativePosition;
        private bool isSublayoutNode = false;
        private bool isSublayoutRoot = false;
        private Sublayout sublayout;
        private ILayoutNode sublayoutRoot;

        public Vector3 RelativePosition { get => relativePosition; set => relativePosition = value; }
        public bool IsSublayoutNode { get => isSublayoutNode; set => isSublayoutNode = value; }
        public bool IsSublayoutRoot { get => isSublayoutRoot; set => isSublayoutRoot = value; }
        public Sublayout Sublayout { get => sublayout; set => sublayout = value; }
        public ILayoutNode SublayoutRoot { get => sublayoutRoot; set => sublayoutRoot = value; }

        public override string ToString()
        {
            string result = base.ToString();
            result += " ID=" + ID + " Level=" + Level + " IsLeaf=" + IsLeaf
                + " Parent=" + (Parent != null ? Parent.ID : "<NO PARENT>");
            return result;
        }

    }
}