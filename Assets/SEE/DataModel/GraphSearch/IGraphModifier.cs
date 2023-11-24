using System.Collections.Generic;
using SEE.DataModel.DG;

namespace SEE.DataModel.GraphSearch
{
    /// <summary>
    /// Modifies a collection of graph elements by filtering, sorting, or otherwise transforming it.
    /// Intended for use with <see cref="GraphSearch"/>.
    /// </summary>
    public interface IGraphModifier
    {
        /// <summary>
        /// Applies the modifier to the given collection of graph elements.
        /// </summary>
        /// <param name="elements">The graph elements to modify.</param>
        /// <typeparam name="T">The type of the graph elements.</typeparam>
        /// <returns>The modified collection.</returns>
        IEnumerable<T> Apply<T>(IEnumerable<T> elements) where T : GraphElement;

        /// <summary>
        /// Returns whether the modifier is active, that is,
        /// whether it would modify the collection in its current configuration.
        /// </summary>
        bool IsActive();

        /// <summary>
        /// Resets the modifier to its default configuration.
        /// </summary>
        void Reset();
    }
}
