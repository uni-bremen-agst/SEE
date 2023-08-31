using System.Collections.Generic;

namespace SEE.Layout.NodeLayouts.IncrementalTreeMap
{
    /// <summary>
    /// A local move a is small transformation of a layout that only affects two nodes.
    /// There are two types of local moves: <see cref="FlipMove"/> and <see cref="StretchMove"/>.
    /// </summary>
    internal abstract class LocalMove
    {
        /// <summary>
        /// one affected node
        /// </summary>
        public Node Node1;

        /// <summary>
        /// the other affected node
        /// </summary>
        public Node Node2;

        /// <summary>
        /// Execute the local move transformation on the layout of <see cref="Node1"/> and <see cref="Node2"/>.
        /// </summary>
        public abstract void Apply();

        /// <summary>
        /// Creates a new local move, that can be applied on the layout of the node clones.
        /// </summary>
        /// <param name="cloneMap"> dictionary that maps id to node, assuming that the cloneMap represents a clone
        /// of the node layout of of <see cref="Node1"/> and <see cref="Node2"/>.</param>
        /// <returns>a new local move</returns>
        public abstract LocalMove Clone(IDictionary<string, Node> cloneMap);
    }
}
