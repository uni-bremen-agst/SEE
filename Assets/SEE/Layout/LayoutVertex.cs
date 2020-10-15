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
            scale = initialSize;
            id = index.ToString();
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="id">unique ID of the node</param>
        public LayoutVertex(string id)
        {
            scale = Vector3.zero;
            this.id = id;
        }

        /// <summary>
        /// Unique ID of the node.
        /// </summary>
        private readonly string id;

        /// <summary>
        /// Immediate ancestor of the node. May be null, if the node is a root.
        /// </summary>
        private ILayoutNode parent;

        /// <summary>
        /// Immediate ancestor of the node. May be null, if the node is a root.
        /// </summary>
        public ILayoutNode Parent
        {
            get => parent;
            set => parent = value;
        }

        /// <summary>
        /// The level of the node in the node hierarchy, that is, the number
        /// of ancestors. A root has level 0.
        /// </summary>
        private int level = 0;

        /// <summary>
        /// The level of the node in the node hierarchy, that is, the number
        /// of ancestors. A root has level 0.
        /// </summary>
        public int Level
        {
            get => level;
            set => level = value;
        }

        /// <summary>
        /// Immediate children of the node.
        /// </summary>
        private readonly List<ILayoutNode> children = new List<ILayoutNode>();

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

        /// <summary>
        /// The local scale of the node, that is, the size of the node
        /// relative to its parent. If size is Vector3.one, the node is
        /// as large as its parent.
        /// </summary>
        private Vector3 scale;

        /// <summary>
        /// The local scale of the node, that is, the size of the node
        /// relative to its parent. If size is Vector3.one, the node is
        /// as large as its parent.
        /// </summary>
        public Vector3 LocalScale
        {
            get => scale;
            set => scale = value;
        }

        /// <summary>
        /// Increases the size of this node by the given factor.
        /// </summary>
        /// <param name="factor">factor by which to multiply this node's scale</param>
        public void ScaleBy(float factor)
        {
            scale *= factor;
        }

        public void SetOrigin()
        {
            throw new System.NotImplementedException();
        }

        public void SetRelative(ILayoutNode node)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// The center position of the node.
        /// </summary>
        private Vector3 centerPosition;

        /// <summary>
        /// The center position of the node.
        /// </summary>
        public Vector3 CenterPosition
        {
            get => centerPosition;
            set => centerPosition = value;
        }

        /// <summary>
        /// The X-Z center position of the node's roof.
        /// </summary>
        public Vector3 Roof
        {
            get => centerPosition + Vector3.up * 0.5f * scale.y;
        }

        /// <summary>
        /// The X-Z center position of the node's ground.
        /// </summary>
        public Vector3 Ground
        {
            get => centerPosition - Vector3.up * 0.5f * scale.y;
        }

        /// <summary>
        /// True if this node is a leaf, that is, has no children.
        /// </summary>
        public bool IsLeaf => children.Count == 0;

        /// <summary>
        /// The unique ID of this node.
        /// </summary>
        public string ID { get => id; }

        /// <summary>
        /// The rotation of the node along the y axis in degrees.
        /// </summary>
        private float rotation;

        /// <summary>
        /// The rotation of the node along the y axis in degrees.
        /// </summary>
        public float Rotation
        {
            get => rotation;
            set => rotation = value;
        }

        /// <summary>
        /// The set of successor nodes.
        /// </summary>
        public ICollection<ILayoutNode> Successors => new List<ILayoutNode>();

        /// <summary>
        /// The scale of the node in world space, i.e., in absolute Unity units
        /// independent of its parent.
        /// </summary>
        public Vector3 AbsoluteScale => scale;

        public Vector3 RelativePosition { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
        public bool IsSublayoutNode { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
        public bool IsSublayoutRoot { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
        public Sublayout Sublayout { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
        public ILayoutNode SublayoutRoot { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
    }
}