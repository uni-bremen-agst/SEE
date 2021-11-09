namespace SEE.Layout.NodeLayouts
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
        public FlatNodeLayout(float groundLevel)
            : base(groundLevel)
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

        public override bool UsesEdgesAndSublayoutNodes()
        {
            return false;
        }
    }
}
