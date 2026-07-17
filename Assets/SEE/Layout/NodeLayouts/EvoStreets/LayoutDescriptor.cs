namespace SEE.Layout.NodeLayouts.EvoStreets
{
    /// <summary>
    /// Provides parameters regarding the layout of the EvoStreets.
    /// </summary>
    internal struct LayoutDescriptor
    {
        /// <summary>
        /// The maximal depth of the tree.
        /// </summary>
        public float MaximalDepth;

        /// <summary>
        /// The width of the street for the root node. The width of streets at the lower level will be depicted
        /// smaller relative to this value depending upon their level in the tree and <see cref="MaximalDepth"/>.
        /// </summary>
        public float StreetWidth;

        /// <summary>
        /// The distance between two neighboring node representations.
        /// </summary>
        public float OffsetBetweenBuildings;
    }
}
