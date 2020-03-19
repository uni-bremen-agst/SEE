using SEE.GO;

namespace SEE.Layout
{
    /// <summary>
    /// The abstract super class of all non-hierarchical (flat) node layouts.
    /// </summary>
    public abstract class FlatNodeLayout : NodeLayout
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="groundLevel">the y co-ordinate setting the ground level; all nodes will be
        /// placed on this level</param>
        /// <param name="leafNodeFactory">the factory used to created leaf nodes</param>
        public FlatNodeLayout
            (float groundLevel,
             NodeFactory leafNodeFactory)
            : base(groundLevel, leafNodeFactory)
        {
        }

        /// <summary>
        /// Always false because non-hierarchical layouts can handle only leaves.
        /// </summary>
        /// <returns>always false</returns>
        public override bool IsHierarchical()
        {
            return false;
        }
    }
}
