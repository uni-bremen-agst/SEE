using System.Collections.Generic;
using SEE.DataModel.DG;
using SEE.Layout;
using UnityEngine;

namespace SEE.Game.CityRendering
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
        protected readonly Node Node;

        /// <summary>
        /// The game object this layout node encapsulates.
        /// </summary>
        public GameObject GameObject { get; protected set; }

        /// <summary>
        /// The underlying graph node represented by this laid out node.
        /// </summary>
        public Node ItsNode => Node;

        /// <summary>
        /// Constructor setting the graph <paramref name="node"/> corresponding to this layout node
        /// and the <paramref name="toLayoutNode"/> mapping. The mapping maps all graph nodes to be
        /// laid out onto their corresponding layout node and is shared among all layout nodes.
        /// The given <paramref name="node"/> will be added to <paramref name="toLayoutNode"/>.
        /// </summary>
        /// <param name="node">graph node corresponding to this layout node</param>
        /// <param name="toLayoutNode">the mapping of graph nodes onto LayoutNodes this node should be added to</param>
        protected AbstractLayoutNode(Node node, IDictionary<Node, ILayoutNode> toLayoutNode)
        {
            Node = node;
            ToLayoutNode = toLayoutNode;
            ToLayoutNode.Add(node, this);
        }

        /// <summary>
        /// The mapping from graph nodes onto layout nodes. Every layout node created by any of the
        /// constructors of this class or one of its subclasses will be added to it. All layout nodes
        /// given to the layout will refer to the same mapping, i.e., <see cref="ToLayoutNode"/> is the
        /// same for all. The mapping will be gathered by the constructor.
        ///
        /// This mapping is used to decide which nodes are to be laid out (nodes
        /// not part of this mapping will be ignored by the layout).
        /// </summary>
        protected readonly IDictionary<Node, ILayoutNode> ToLayoutNode;

        #region IHierarchyNode
        /// <summary>
        /// The tree level of the node. Roots have level 0, for all other nodes the level is the
        /// distance to its root.
        /// </summary>
        private int level;

        /// <summary>
        /// The tree level of the node. Roots have level 0, for all other nodes the level is the
        /// distance to its root.
        ///
        /// <see cref="IHierarchyNode.Level"/>
        /// </summary>
        public int Level
        {
            get => level;
            set => level = value;
        }

        /// <summary>
        /// Whether this node represents a leaf.
        ///
        /// <see cref="IHierarchyNode.IsLeaf"/>
        /// </summary>
        /// <returns>true if this node represents a leaf</returns>
        public bool IsLeaf => Node.IsLeaf();

        /// <summary>
        /// The parent of this node. May be null if it has none.
        ///
        /// <see cref="IHierarchyNode.Parent"/>
        ///
        /// Note: Parent may be null even if the underlying graph node actually has a
        /// parent in the graph, yet that parent was never passed to any of the
        /// constructors of this class.
        /// </summary>
        public ILayoutNode Parent
        {
            get
            {
                if (Node.Parent == null)
                {
                    // The node does not have a parent in the original graph.
                    return null;
                }
                else if (ToLayoutNode.TryGetValue(Node.Parent, out ILayoutNode result))
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
        /// corresponding layout node (<see cref="ToLayoutNode"/>), it will be ignored silently.
        /// This is useful in situation where only a subset of nodes is to be considered for a layout.
        ///
        /// <see cref="IHierarchyNode.Children"/>
        /// </summary>
        /// <returns>children of this node</returns>
        public ICollection<ILayoutNode> Children()
        {
            if (!ToLayoutNode.ContainsKey(Node))
            {
                throw new System.InvalidOperationException($"Cannot retrieve children for layout node {Node.ID} to be ignored.");
            }
            IList<ILayoutNode> result;
            if (!IsLeaf)
            {
                IList<Node> children = Node.Children();
                result = new List<ILayoutNode>(children.Count);
                foreach (Node node in children)
                {
                    if (ToLayoutNode.TryGetValue(node, out ILayoutNode layoutNode))
                    {
                        result.Add(layoutNode);
                    }
                }
            }
            else
            {
                result = new List<ILayoutNode>();
            }
            return result;
        }
        #endregion IHierarchyNode

        #region IGameNode

        /// <summary>
        /// A unique ID for a node: the ID of the graph node underlying this layout node.
        ///
        /// <see cref="IGameNode.ID"/>.
        /// </summary>
        /// <returns>unique ID for this node</returns>
        public string ID => Node.ID;

        /// <summary>
        /// Implementation of <see cref="ILayoutNode{T}.HasType(string)"/>.
        /// </summary>
        /// <param name="typeName">Name of a node type</param>
        /// <returns>True if this node has a type with the given <paramref name="typeName"/>.</returns>
        public bool HasType(string typeName)
        {
            return Node.Type == typeName;
        }

        /// </summary>
        /// <summary>
        /// <see cref="IGameNode.LocalScale"/>.
        /// </summary>
        public abstract Vector3 LocalScale { get; set; }
        /// <summary>
        /// <see cref="IGameNode.AbsoluteScale"/>.
        /// </summary>
        public abstract Vector3 AbsoluteScale { get; }

        /// <summary>
        /// <see cref="IGameNode.ScaleXZBy(float)"/>.
        /// </summary>
        public abstract void ScaleXZBy(float factor);
        /// <summary>
        /// <see cref="IGameNode.CenterPosition"/>.
        /// </summary>
        public abstract Vector3 CenterPosition { get; set; }
        /// <summary>
        /// <see cref="IGameNode.Rotation"/>.
        /// </summary>
        public abstract float Rotation { get; set; }
        /// <summary>
        /// <see cref="IGameNode.Roof"/>.
        /// </summary>
        public abstract Vector3 Roof { get; }
        /// <summary>
        /// <see cref="IGameNode.Ground"/>.
        /// </summary>
        public abstract Vector3 Ground { get; }
        #endregion

        public override string ToString()
        {
            string result = base.ToString();
            result += " ID=" + ID + " Level=" + Level + " IsLeaf=" + IsLeaf
                + " Parent=" + (Parent != null ? Parent.ID : "<NO PARENT>");
            return result;
        }
    }
}
