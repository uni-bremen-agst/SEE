namespace SEE.Layout.NodeLayouts.EvoStreets
{
    /// <summary>
    /// A absolute location in a two-dimensional world space.
    /// </summary>
    internal struct Location
    {
        /// <summary>
        /// Absolute value along the X axis (width) in world space.
        /// </summary>
        public float X;

        /// <summary>
        /// Absolute value along the Y axis (depth) in world space.
        /// </summary>
        public float Y;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="x">Absolute value along the X axis (width) in world space.</param>
        /// <param name="y">Absolute value along the Y axis (depth) in world space.</param>
        public Location(float x, float y) : this()
        {
            X = x;
            Y = y;
        }

        /// <summary>
        /// The co-ordinates as a human-readable string.
        /// </summary>
        /// <returns>Co-ordinates as a human-readable string.</returns>
        public override string ToString()
        {
            return $"[x={X:F4}, y={Y:F4}]";
        }
    }
}
