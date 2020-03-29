using System.Collections.Generic;
using UnityEngine;

namespace SEE.Layout
{
    public interface IGameNode
    {
        /// <summary>
        /// A unique ID for a node.
        /// </summary>
        string LinkName { get; }

        /// <summary>
        /// Scale of a node.
        /// </summary>
        Vector3 Scale { get; set; }

        /// <summary>
        /// Center position of a node in world space.
        /// </summary>
        Vector3 CenterPosition { get; set; }

        /// <summary>
        /// Rotation around the y axis in degrees.
        /// </summary>
        float Rotation { get; set; }

        /// <summary>
        /// X-Z center position of the roof of the node in world space.
        /// </summary>
        Vector3 Roof { get; }

        /// <summary>
        /// X-Z center position of the ground of the node in world space.
        /// </summary>
        Vector3 Ground { get; }
    }

    public interface IGraphNode<T>
    {
        /// <summary>
        /// The set of immediate successors of this node.
        /// </summary>
        ICollection<T> Successors { get; }
    }

    public interface ISublayoutNode<T>
    {
        Vector3 RelativePosition { get; set; }

        bool IsSublayoutNode { get; set; }

        bool IsSublayoutRoot { get; set; }

        Sublayout Sublayout { get; set; }

        T SublayoutRoot { get; set; }

        void SetOrigin();

        void SetRelative(T node);
    }

    /// <summary>
    ///  Defines the methods for all nodes to be laid out.
    /// </summary>
    public interface ILayoutNode : IGameNode, IGraphNode<ILayoutNode>, IHierarchyNode<ILayoutNode>, ISublayoutNode<ILayoutNode>
    {
    }
}