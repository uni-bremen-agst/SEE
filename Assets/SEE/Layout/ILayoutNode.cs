using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace SEE.Layout
{
    /// <summary>
    /// Defines the methods for all nodes to be laid out.
    /// </summary>
    public abstract class ILayoutNode : IGameNode, IHierarchyNode<ILayoutNode>
    {
        /// <summary>
        /// See <see cref="IGameNode.ID"/>.
        /// </summary>
        public abstract string ID { get; }

        /// <summary>
        /// See <see cref="IGameNode.AbsoluteScale"/>.
        /// </summary>
        public abstract Vector3 AbsoluteScale { get; set; }
        /// <summary>
        /// See <see cref="IGameNode.CenterPosition"/>.
        /// </summary>
        public abstract Vector3 CenterPosition { get; set; }
        /// <summary>
        /// See <see cref="IGameNode.Rotation"/>.
        /// </summary>
        public abstract float Rotation { get; set; }
        /// <summary>
        /// See <see cref="IGameNode.Roof"/>.
        /// </summary>
        public abstract Vector3 Roof { get; }
        /// <summary>
        /// See <see cref="IGameNode.Ground"/>.
        /// </summary>
        public abstract Vector3 Ground { get; }
        /// <summary>
        /// See <see cref="IGameNode.HasType"/>.
        /// </summary>
        public abstract bool HasType(string typeName);
        /// <summary>
        /// See <see cref="IGameNode.ScaleXZBy"/>.
        /// </summary>
        public abstract void ScaleXZBy(float factor);

        /// <summary>
        /// See <see cref="IHierarchyNode.Parent"/>.
        /// </summary>
        public ILayoutNode Parent { get; protected set; }

        /// <summary>
        /// See <see cref="IHierarchyNode.Parent"/>.
        /// </summary>
        public int Level { get; set; } = 0;

        /// <summary>
        /// List of children of this node.
        /// </summary>
        private readonly List<ILayoutNode> children = new();

        /// <summary>
        /// See <see cref="IHierarchyNode.IsLeaf"/>.
        /// </summary>
        public bool IsLeaf => children.Count == 0;

        /// <summary>
        /// See <see cref="IHierarchyNode.Children"/>.
        /// </summary>
        public ICollection<ILayoutNode> Children()
        {
            return children;
        }

        /// <summary>
        /// See <see cref="IHierarchyNode.AddChild(T)"/>.
        /// </summary>
        public void AddChild(ILayoutNode child)
        {
            children.Add(child);
            child.Parent = this;
        }

        /// <summary>
        /// See <see cref="IHierarchyNode.RemoveChild(T)"/>.
        /// </summary>
        public void RemoveChild(ILayoutNode child)
        {
            if (!children.Remove(child))
            {
                throw new System.Exception("Child not found.");
            }
            child.Parent = null;
        }

        /// <summary>
        /// Returns a string representation of this object, including its ID, absolute scale, center position, rotation,
        /// parent ID, level, leaf status, and IDs of any children if applicable.
        /// </summary>
        /// <returns>A formatted string representing the key properties of this object.</returns>
        public override string ToString()
        {
            StringBuilder sb = new();
            sb.Append($"Id={ID}");
            sb.Append($" AbsoluteScale={AbsoluteScale}");
            sb.Append($" CenterPosition={CenterPosition}");
            sb.Append($" Rotation={Rotation}");
            sb.Append($" Parent={(Parent == null ? "NONE" : Parent.ID)}");
            sb.Append($" Level={Level}");
            sb.Append($" IsLeaf={IsLeaf}");
            if (!IsLeaf)
            {
                sb.Append(" Children=(");
                foreach (ILayoutNode child in Children())
                {
                    sb.Append(child.ID + " ");
                }
                sb.Append(")");
            }
            return sb.ToString();
        }
    }
}
