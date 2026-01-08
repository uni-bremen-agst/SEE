namespace SEE.Layout.NodeLayouts.IncrementalTreeMap
{
    /// <summary>
    /// A class that represents a rectangle shape as part of an incremental tree-map layout.
    /// </summary>
    internal class Rectangle
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="x"><see cref="X"/>.</param>
        /// <param name="z"><see cref="Z"/>.</param>
        /// <param name="width"><see cref="Width"/>.</param>
        /// <param name="depth"><see cref="Depth"/>.</param>
        public Rectangle(double x, double z, double width, double depth)
        {
            X = x;
            Z = z;
            Width = width;
            Depth = depth;
        }

        /// <summary>
        /// The position of the <see cref="Direction.Left"/> edge.
        /// </summary>
        public double X;

        /// <summary>
        /// The position of the <see cref="Direction.Lower"/> edge.
        /// </summary>
        public double Z;

        /// <summary>
        /// The length of x axis.
        /// </summary>
        public double Width;

        /// <summary>
        /// The length of z axis.
        /// </summary>
        public double Depth;

        /// <summary>
        /// The ratio of the longer edge to the smaller one.
        /// Notice that the aspect ratio is greater than or equal to 1 by definition.
        /// </summary>
        /// <returns>The aspect ratio.</returns>
        public double AspectRatio()
        {
            return Width >= Depth ? Width / Depth : Depth / Width;
        }

        /// <summary>
        /// The area of the rectangle.
        /// </summary>
        /// <returns><see cref="Width"/>*<see cref="Depth"/>.</returns>
        public double Area()
        {
            return Width * Depth;
        }

        /// <summary>
        /// Returns a new identical Rectangle.
        /// </summary>
        /// <returns>The clone.</returns>
        public Rectangle Clone()
        {
            return new Rectangle(x: X, z: Z, width: Width, depth: Depth);
        }
    }
}
