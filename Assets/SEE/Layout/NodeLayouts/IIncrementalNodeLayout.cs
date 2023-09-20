namespace SEE.Layout.NodeLayouts
{
    /// <summary>
    /// Defines the interface for incremental node layouts.
    ///
    /// Incremental node layouts are designed for the animation of evolution
    /// and each layout depends on the layout of the last revision.
    ///
    /// This interface extends a layout by <see cref="OldLayout"/>
    /// to hand over the layout of the last revision.
    /// </summary>
    public interface IIncrementalNodeLayout
    {
        /// <summary>
        /// Setter for the layout of the last revision.
        /// </summary>
        IIncrementalNodeLayout OldLayout { set; }
    }
}
