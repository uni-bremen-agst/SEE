﻿using System.Collections.Generic;
using UnityEngine;

namespace SEE.Layout
{
    public interface IGameNode
    {
        /// <summary>
        /// A unique ID for a node.
        /// </summary>
        string ID { get; }

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
        /// <summary>
        /// the relative position from a sublayoutNode to its sublayoutRoot node
        /// </summary>
        Vector3 RelativePosition { get; set; }

        /// <summary>
        /// true if node is a sublayouNode
        /// </summary>
        bool IsSublayoutNode { get; set; }

        /// <summary>
        /// true if node is a root node of a sublayout
        /// </summary>
        bool IsSublayoutRoot { get; set; }

        /// <summary>
        /// if the node is a sublayout root, this is the sublayout 
        /// </summary>
        Sublayout Sublayout { get; set; }

        /// <summary>
        /// the sublayout root node
        /// </summary>
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