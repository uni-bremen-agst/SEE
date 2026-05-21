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
        /// See <see cref="IGameNode.ID"/>.
        /// </summary>
        public override string ID => Node.ID;

        /// <summary>
        /// Constructor setting the graph <paramref name="node"/> corresponding to this layout node.
        /// </summary>
        /// <param name="node">Graph node corresponding to this layout node.</param>
        protected AbstractLayoutNode(Node node)
        {
            Node = node;
        }

        /// <summary>
        /// Implementation of <see cref="ILayoutNode{T}.HasType(string)"/>.
        /// </summary>
        /// <param name="typeName">Name of a node type.</param>
        /// <returns>True if this node has a type with the given <paramref name="typeName"/>.</returns>
        public override bool HasType(string typeName)
        {
            return Node.Type == typeName;
        }

        /// <summary>
        /// Human-readable representation of this layout node for debugging purposes.
        /// </summary>
        /// <returns>Combination of <see cref="ID"/>, <see cref="ILayoutNode.Level"/>,
        /// <see cref="ILayoutNode.IsLeaf"/> and <see cref="ILayoutNode.Parent"/>.</returns>
        public override string ToString()
        {
            string result = base.ToString();
            result += " ID=" + ID + " Level=" + Level + " IsLeaf=" + IsLeaf
                + " Parent=" + (Parent != null ? Parent.ID : "<NO PARENT>");
            return result;
        }
    }
}
