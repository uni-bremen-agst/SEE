namespace SEE.Layout.NodeLayouts
{
    /// <summary>
    /// The abstract super class of all non-hierarchical (flat) node layouts.
    /// </summary>
    public abstract class FlatNodeLayout : NodeLayout
    {
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
