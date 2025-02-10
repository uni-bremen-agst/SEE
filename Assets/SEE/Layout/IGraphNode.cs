using System.Collections.Generic;

namespace SEE.Layout
{
    /// <summary>
    /// Defines the interface of nodes in a node hierarchy.
    /// </summary>
    /// <typeparam name="T">the type of nodes</typeparam>
    public interface IGraphNode<T>
    {
        /// <summary>
        /// True if this node has a type with the given <paramref name="typeName"/>.
        /// </summary>
        /// <param name="typeName">Name of a node type</param>
        /// <returns>True if this node has a type with the given <paramref name="typeName"/>.</returns>
        bool HasType(string typeName);
    }
}
