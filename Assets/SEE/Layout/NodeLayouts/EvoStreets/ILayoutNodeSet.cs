using System.Collections.Generic;

namespace SEE.Layout.NodeLayouts.EvoStreets
{
    /// <summary>
    /// A set of <see cref="ILayoutNode"/>s where the comparison of two nodes is
    /// based on their IDs.
    /// </summary>
    internal class ILayoutNodeSet : HashSet<ILayoutNode>
    {
        /// <summary>
        /// Default constructor injecting the custom comparer <see cref="ILayoutNodeComparer"/>.
        /// </summary>
        public ILayoutNodeSet() : base(new ILayoutNodeComparer())
        {
        }

        /// <summary>
        /// Constructor injecting the custom comparer <see cref="ILayoutNodeComparer"/>
        /// allowing to set the initial <paramref name="capacity"/>.
        /// </summary>
        /// <param name="capacity">Preallocated capacity.</param>
        public ILayoutNodeSet(int capacity)
        : base(capacity, new ILayoutNodeComparer())
        {
        }

        /// <summary>
        /// Constructor adding all elements in <paramref name="collection"/> and injecting
        /// the custom comparer <see cref="ILayoutNodeComparer"/>.
        /// </summary>
        /// <param name="collection">The elements to be added.</param>
        public ILayoutNodeSet(IEnumerable<ILayoutNode> collection) : base(collection, new ILayoutNodeComparer())
        {
        }
    }
}
