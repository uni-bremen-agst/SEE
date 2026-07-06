using System.Collections.Generic;

namespace SEE.Layout.NodeLayouts.EvoStreets
{
    /// <summary>
    /// Comparator for <see cref="ILayoutNode"/>. Two nodes are equivalent if they have
    /// the same ID.
    /// </summary>
    public class ILayoutNodeComparer : IEqualityComparer<ILayoutNode>
    {
        /// <summary>
        /// True if <paramref name="left"/> and <paramref name="right"/> have the same id.
        /// </summary>
        /// <param name="left">Left argument.</param>
        /// <param name="right">Right argument.</param>
        /// <returns>True if <paramref name="left"/> and <paramref name="right"/> have the same id.</returns>
        bool IEqualityComparer<ILayoutNode>.Equals(ILayoutNode? left, ILayoutNode? right)
        {
            // If they are the exact same reference (or both null), they are equal.
            if (ReferenceEquals(left, right))
            {
                return true;
            }

            // If one is null but not the other, they are not equal.
            if (left == null || right == null)
            {
                return false;
            }
            return left.ID == right.ID;
        }

        /// <summary>
        /// Returns the hash value for the id of <paramref name="node"/>.
        /// </summary>
        /// <param name="node">Nodes whose hash code is required.</param>
        /// <returns>Hash code for the id of <paramref name="node"/>.</returns>
        int IEqualityComparer<ILayoutNode>.GetHashCode(ILayoutNode node)
        {
            if (node == null || node.ID == null)
            {
                return 0;
            }
            return node.ID.GetHashCode();
        }
    }
}
