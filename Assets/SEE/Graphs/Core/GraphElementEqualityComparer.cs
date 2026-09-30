using System.Collections.Generic;

namespace SEE.Graphs.DG
{
    /// <summary>
    /// Compares two instances of <see cref="GraphElement"/> by their ID for equality.
    /// </summary>
    public abstract class GraphElementEqualityComparer<T> : IEqualityComparer<T> where T : GraphElement
    {
        /// <summary>
        /// True if <paramref name="x"/> and <paramref name="y"/> have the same ID.
        /// </summary>
        /// <param name="x">Node to be compared to <paramref name="y"/>.</param>
        /// <param name="y">Node to be compared to <paramref name="x"/>.</param>
        /// <returns>True if <paramref name="x"/> and <paramref name="y"/> have the same ID.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if <paramref name="x"/> or
        /// <paramref name="y"/> is null.</exception>
        public bool Equals(T x, T y)
        {
            if (x == null || y == null)
            {
                throw new System.ArgumentNullException("Parameters must not be null.");
            }
            return x.ID.Equals(y?.ID);
        }

        /// <summary>
        /// Hash code for <paramref name="node"/> based on its ID.
        /// </summary>
        /// <param name="node">Node whose hash code is requested.</param>
        /// <returns>Hash code for <paramref name="node"/>.</returns>
        public int GetHashCode(T node)
        {
            return node.ID.GetHashCode();
        }
    }

    /// <summary>
    /// Compares two instances of <see cref="Node"/> by their ID for equality.
    /// </summary>
    public class NodeEqualityComparer : GraphElementEqualityComparer<Node> { }

    /// <summary>
    /// Compares two instances of <see cref="Edge"/> by their ID for equality.
    /// </summary>
    public class EdgeEqualityComparer : GraphElementEqualityComparer<Edge> { }
}
