namespace SEE.Layout
{
    /// <summary>
    /// The abstract super class of all hierarchical node layouts.
    /// </summary>
    public abstract class HierarchicalNodeLayout : NodeLayout
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="groundLevel">the y co-ordinate setting the ground level; all nodes will be
        /// placed on this level</param>
        /// <param name="leafNodeFactory">the factory used to created leaf nodes</param>
        public HierarchicalNodeLayout
            (float groundLevel,
             NodeFactory leafNodeFactory)
            : base(groundLevel, leafNodeFactory)
        {
        }

        /// <summary>
        /// Always true because hierarchical layouts can handle both inner nodes and leaves.
        /// </summary>
        /// <returns>always true</returns>
        public override bool IsHierarchical()
        {
            return true;
        }
    }
}
