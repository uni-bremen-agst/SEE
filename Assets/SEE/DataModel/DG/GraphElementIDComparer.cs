using SEE.DataModel.DG;
using System.Collections.Generic;

namespace SEE.DataModel
{
    /// <summary>
    /// A comparer for <see cref="GraphElement"/> that considers only the
    /// <see cref="GraphElement.ID"/>. It can be used to compare graph
    /// elements from different graphs.
    /// </summary>
    internal class GraphElementIDComparer : IEqualityComparer<GraphElement>
    {
        /// <summary>
        /// Returns true if <paramref name="it"/> and <paramref name="other"/>
        /// are identical, both null, or have the same <see cref="GraphElement.ID"/>
        /// if they have the same type.
        /// </summary>
        /// <param name="it">To be compared to <paramref name="other"/>.</param>
        /// <param name="other">To be compared to <paramref name="it"/>.</param>
        /// <returns>True if they have the same id.</returns>
        public bool Equals(GraphElement it, GraphElement other)
        {
            if (it == other)
            {
                // This captures also the case that both it and other are null.
                return true;
            }

            if (it == null || other == null)
            {
                // Only one of them can be null if we arrive here. See above.
                return false;
            }

            if (it.GetType() != other.GetType())
            {
                return false;
            }

            return it.ID == other.ID;
        }

        /// <summary>
        /// The hash code the <see cref="GraphElement.ID"/> of <paramref name="graphElement"/>.
        /// </summary>
        /// <param name="graphElement">Graph element whose hash code is expected.</param>
        /// <returns>Hash code.</returns>
        public int GetHashCode(GraphElement graphElement)
        {
            return graphElement == null ? 0 : graphElement.ID.GetHashCode();
        }
    }
}
