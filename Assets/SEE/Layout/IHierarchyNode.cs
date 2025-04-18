﻿using System.Collections.Generic;

namespace SEE.Layout
{
    /// <summary>
    /// Defines the interface of nodes in a node hierarchy.
    /// </summary>
    /// <typeparam name="T">The type of the node in the hierarchy (parent and children).</typeparam>
    public interface IHierarchyNode<T>
    {
        /// <summary>
        /// The parent of the node. Is null if the node is a root.
        /// </summary>
        T Parent { get; }

        /// <summary>
        /// The level of the node in the node hierarchy. A root node has
        /// level 0. For all other nodes, the level is the distance from
        /// the node to its root.
        /// </summary>
        int Level { get; set; }

        /// <summary>
        /// True if the given node is to be interpreted as a leaf by the layouter.
        ///
        /// Note: Even leaves may have children. What to do with those is the decision of the
        /// layouter. It may or may not lay them out.
        /// </summary>
        bool IsLeaf { get; }

        /// <summary>
        /// Adds given <paramref name="child"/> to the list of children of this node.
        /// </summary>
        /// <param name="child">child to be added</param>
        void AddChild(T child);

        /// <summary>
        /// Removes given <paramref name="child"/> from the list of children of this node.
        /// </summary>
        /// <param name="child">child to be removed</param>
        /// <exception cref="System.Exception">in case <paramref name="child"/> is not a child</exception>
        void RemoveChild(T child);

        /// <summary>
        /// The set of children of this node. Note: Even nodes for which IsLeaf
        /// returns true, may still have children. Layouts may refuse to layout
        /// the children of a node for which IsLeaf returns true.
        /// </summary>
        /// <returns>children of this node</returns>
        ICollection<T> Children();
    }
}