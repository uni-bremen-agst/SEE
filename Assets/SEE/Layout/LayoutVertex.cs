using System.Collections.Generic;
using UnityEngine;

namespace SEE.Layout
{
    /// <summary>
    /// A simple implementation of ILayoutNode.
    /// </summary>
    public class LayoutVertex : ILayoutNode
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="initialSize">the size of the node</param>
        /// <param name="index">the unique ID of the node (a number to be converted into a string)</param>
        public LayoutVertex(Vector3 initialSize, int index)
        {
            LocalScale = initialSize;
            ID = index.ToString();
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="id">unique ID of the node</param>
        public LayoutVertex(string id)
        {
            LocalScale = Vector3.zero;
            ID = id;
        }

        #region IHierarchyNode

        /// <summary>
        /// Immediate ancestor of the node. May be null, if the node is a root.
        /// </summary>
        public ILayoutNode Parent { get; set; }

        /// <summary>
        /// The level of the node in the node hierarchy, that is, the number
        /// of ancestors. A root has level 0.
        /// </summary>
        public int Level { get; set; } = 0;

        /// <summary>
        /// Immediate children of the node.
        /// </summary>
        private readonly List<ILayoutNode> children = new();

        /// <summary>
        /// Immediate children of the node.
        /// </summary>
        public ICollection<ILayoutNode> Children()
        {
            return children;
        }

        /// <summary>
        /// Adds given <paramref name="node"/> to the children of this node.
        /// </summary>
        /// <param name="node">child node</param>
        public void AddChild(LayoutVertex node)
        {
            children.Add(node);
            node.Parent = this;
        }

        #endregion IHierarchyNode

        #region IGameNode

        /// <summary>
        /// The local scale of the node, that is, the size of the node
        /// relative to its parent. If size is Vector3.one, the node is
        /// as large as its parent.
        /// </summary>
        public Vector3 LocalScale { set; get; }

        /// <summary>
        /// <see cref="IGameNode.ScaleXZBy(float)"/>.
        /// </summary>
        public void ScaleXZBy(float factor)
        {
            Vector3 result = LocalScale;
            result.x *= factor;
            result.z *= factor;
            LocalScale = result;
        }

        /// <summary>
        /// The center position of the node.
        /// </summary>
        public Vector3 CenterPosition { set; get; }

        /// <summary>
        /// The X-Z center position of the node's roof.
        /// </summary>
        public Vector3 Roof
        {
            get => CenterPosition + 0.5f * LocalScale.y * Vector3.up;
        }

        /// <summary>
        /// The X-Z center position of the node's ground.
        /// </summary>
        public Vector3 Ground
        {
            get => CenterPosition - 0.5f * LocalScale.y * Vector3.up;
        }

        /// <summary>
        /// True if this node is a leaf, that is, has no children.
        /// </summary>
        public bool IsLeaf => children.Count == 0;

        /// <summary>
        /// The unique ID of this node.
        /// </summary>
        public string ID { get; private set; }

        /// <summary>
        /// The rotation of the node along the y axis in degrees.
        /// </summary>
        public float Rotation { set; get; }

        /// <summary>
        /// The scale of the node in world space, i.e., in absolute Unity units
        /// independent of its parent.
        /// </summary>
        public Vector3 AbsoluteScale => LocalScale;

        #endregion IGameNode

        #region IGraphNode

        /// <summary>
        /// The set of successor nodes.
        /// </summary>
        public ICollection<ILayoutNode> Successors => new List<ILayoutNode>();

        /// <summary>
        /// Implementation of <see cref="IGraphNode{T}.HasType(string)"/>.
        /// </summary>
        /// <param name="typeName">ignored</param>
        /// <returns>always false</returns>
        public bool HasType(string typeName)
        {
            return false;
        }

        #endregion IGraphNode
    }
}
