using System;

namespace SEE.Layout.NodeLayouts.EmptySpace
{
    /// <summary>
    /// A recangle defined by its top-left corner (Left, Top), width, and height.
    ///
    /// Co-ordinates increase to the right (X) and down (Y).
    /// </summary>
    internal class Rectangle
    {
        /// <summary>
        /// Left coordinate (X) of the rectangle's top-left corner.
        /// </summary>
        public float Left { get; set; }
        /// <summary>
        /// Top coordinate (Y) of the rectangle's top-left corner.
        /// </summary>
        public float Top { get; set; }
        /// <summary>
        /// Width of the rectangle.
        /// </summary>
        public float Width { get; set; }
        /// <summary>
        /// Height of the rectangle.
        /// </summary>
        public float Height { get; set; }

        /// <summary>
        /// Right coordinate (X) of the rectangle's bottom-right corner.
        /// </summary>
        public float Right => Left + Width;
        /// <summary>
        /// Bottom coordinate (Y) of the rectangle's bottom-right corner.
        /// </summary>
        public float Bottom => Top + Height;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="left">left corner</param>
        /// <param name="top">top corner</param>
        /// <param name="width">width (must not be negative)</param>
        /// <param name="height">height (must not be negative)</param>
        /// <exception cref="ArgumentOutOfRangeException">thrown if <paramref name="width"/> or
        /// <paramref name="height"/> are negative</exception>
        public Rectangle(float left, float top, float width, float height)
        {
            if (width < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(width), "Width must be non-negative");
            }
            if (height < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(height), "Height must be non-negative");
            }
            Left = left;
            Top = top;
            Width = width;
            Height = height;
        }

        /// <summary>
        /// String representation of the rectangle.
        /// </summary>
        /// <returns>string representation of the rectangle</returns>
        public override string ToString() => $"({Left},{Top}) W={Width} H={Height}";

        /// <summary>
        /// True if this rectangle overlaps with the <paramref name="other"/> rectangle.
        /// </summary>
        /// <param name="other">other rectangle (must not be null)</param>
        /// <returns>true if this rectangle overlaps with the <paramref name="other"/> rectangle</returns>
        public bool Overlaps(Rectangle other)
        {
            return Left <= other.Right && Right >= other.Left
                && Top <= other.Bottom && Bottom >= other.Top;
        }

        /// <summary>
        /// True if the <paramref name="other"/> rectangle is fully contained within this rectangle.
        /// </summary>
        /// <param name="other">other rectangle (must not be null)</param>
        /// <returns>true if the <paramref name="other"/> rectangle is fully contained within this rectangle</returns>
        public bool Contains(Rectangle other)
        {
            return Left <= other.Left && other.Right <= Right
                && Top <= other.Top && other.Bottom <= Bottom;
        }

        /// <summary>
        /// True if a point (<paramref name="px"/>, <paramref name="py"/>) is inside or on the border
        /// of this rectangle.
        /// </summary>
        /// <param name="px">x-coordinate of point</param>
        /// <param name="py">y-coordindate of point</param>
        /// <returns>true if point is inside or on the border</returns>
        public bool ContainsPoint(int px, int py)
        {
            return px >= Left && px <= Right && py >= Top && py <= Bottom;
        }

        /// <summary>
        /// True if obj is a Rectangle with the same X, Y, Width, and Height.
        /// </summary>
        /// <param name="obj">other object to be compared to this rectangle</param>
        /// <returns>true if obj is a Rectangle with the same X, Y, Width, and Height</returns>
        public override bool Equals(object? obj)
            => obj is Rectangle other && Left == other.Left && Top == other.Top && Width == other.Width && Height == other.Height;

        /// <summary>
        /// Returns a hash code based on X, Y, Width, and Height.
        /// </summary>
        /// <returns>hash code</returns>
        public override int GetHashCode() => HashCode.Combine(Left, Top, Width, Height);
    }
}
