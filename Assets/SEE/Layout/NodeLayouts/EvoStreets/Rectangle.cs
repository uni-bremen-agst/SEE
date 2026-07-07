namespace SEE.Layout.NodeLayouts.EvoStreets
{
    /// <summary>
    /// A rectangle.
    /// </summary>
    internal struct Rectangle
    {
        /// <summary>
        /// The width in world space, i.e., along the X axis.
        /// </summary>
        public float Width;

        /// <summary>
        /// The depth in world space, i.e., along the Z axis.
        /// </summary>
        public float Depth;

        /// <summary>
        /// The center point of the rectangle in world space.
        /// </summary>
        public Location Center;

        /// <summary>
        /// The rectangle as a human-readable string.
        /// </summary>
        /// <returns>Rectangle as a human-readable string.</returns>
        public override readonly string ToString()
        {
            return $"[center={Center}, width={Width:F4}, depth={Depth:F4}]";
        }
    }
}
