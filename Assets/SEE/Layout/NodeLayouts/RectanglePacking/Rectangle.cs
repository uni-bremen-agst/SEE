using UnityEngine;

namespace SEE.Layout.NodeLayouts.RectanglePacking
{
    /// <summary>
    /// Representation of a rectangle area for PTree, either occupied or free.
    /// </summary>
    public class PRectangle
    {
        /// <summary>
        /// Size - width (x) and depth (y) -- of the rectangle.
        /// </summary>
        public Vector2 Size;

        /// <summary>
        /// Position of the rectangle relative to the origin, i.e., the left upper corner
        /// of the original available space.
        /// </summary>
        public Vector2 Position;

        /// <summary>
        /// Constructor. Equivalent to <see cref="PRectangle"/>(Vector2.zero, Vector2.zero).
        /// </summary>
        public PRectangle()
        {
            Size = Vector2.zero;
            Position = Vector2.zero;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="position">Position of the rectangle relative to the origin.</param>
        /// <param name="size">Size - width (x) and depth (y) -- of the rectangle.</param>
        public PRectangle(Vector2 position, Vector2 size)
        {
            this.Size = size;
            this.Position = position;
        }

        /// <summary>
        /// The rectangle as a string. Used for debugging.
        /// </summary>
        /// <returns>Rectangle in human-readable form.</returns>
        public override string ToString()
        {
            return "[position=" + Position + ", size=" + Size + "]";
        }
    }
}
