namespace SEE.Layout.NodeLayouts.EmptySpace
{
    /// <summary>
    /// Represents a strip of potential maximal empty space.
    /// Invariant: 0 <= <see cref="Begin"/> &lt; <see cref="End"/>.
    /// </summary>
    internal class Gap
    {
        /// <summary>
        /// Begin of the gap.
        /// </summary>
        /// <remarks>Never negative.</remarks>
        public float Begin { get; set; }

        /// <summary>
        /// End of the gap.
        /// </summary>
        /// <remarks>Always greater than <see cref="Begin"/>.</remarks>
        public float End { get; set; }
    }
}
