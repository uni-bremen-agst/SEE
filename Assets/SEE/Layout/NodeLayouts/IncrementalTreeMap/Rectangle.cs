namespace SEE.Layout.NodeLayouts.IncrementalTreeMap
{
    /// <summary>
    /// A class that represents a rectangle as part of a layout
    /// </summary>
    internal class Rectangle
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="x"><see cref="X"/></param>
        /// <param name="z"><see cref="Z"/></param>
        /// <param name="width"><see cref="Width"/></param>
        /// <param name="depth"><see cref="Depth"/></param>
        public Rectangle(double x, double z, double width, double depth)
        {
            X = x;
            Z = z;
            Width = width;
            Depth = depth;
        }

        /// <summary>
        /// The position of the left side
        /// </summary>
        public double X;

        /// <summary>
        /// the position of the lower side 
        /// </summary>
        public double Z;

        /// <summary>
        /// The lenght of x-axis
        /// </summary>
        public double Width;

        /// <summary>
        /// The lenght of z-axis
        /// </summary>
        public double Depth; // depth

        /// <summary>
        /// The ratio of the longer side to the smaller one.
        /// </summary>
        /// <returns>val >= 1</returns>
        public double AspectRatio()
        {
            return Width >= Depth ? Width / Depth : Depth / Width;
        }

        /// <summary>
        /// the area of the rectangle 
        /// </summary>
        /// <returns><see cref="Width"/>*<see cref="Depth"/></returns>
        public double Area()
        {
            return Width * Depth;
        }

        /// <summary>
        /// Creates a new identical Rectangle
        /// </summary>
        /// <returns>the clone</returns>
        public Rectangle Clone()
        {
            return new Rectangle(x: X, z: Z, width: Width, depth: Depth);
        }
    }
}