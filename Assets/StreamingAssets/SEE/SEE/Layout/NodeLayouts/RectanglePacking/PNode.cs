using UnityEngine;

namespace SEE.Layout.NodeLayouts.RectanglePacking
{
    /// <summary>
    /// A node of PTree that is aware of its assigned space 
    /// </summary>
    public class PNode
    {
        /// <summary>
        /// A node is aware of its assigned space rectangle.
        /// </summary>
        public PRectangle rectangle = new PRectangle();

        /// <summary>
        /// Whether the rectangle is occupied.
        /// </summary>
        public bool occupied;

        /// <summary>
        /// Left child.
        /// </summary>
        public PNode left;

        /// <summary>
        /// Right child.
        /// </summary>
        public PNode right;

        /// <summary>
        /// Creates a new PNode representing a non-occupied rectangle with position Vector2.zero and size
        /// Vector2.zero and without leaves (nested rectangles). Equivalent to PNode(Vector2.zero, Vector2.zero).
        /// </summary>
        public PNode() : this(Vector2.zero, Vector2.zero)
        {
        }

        /// <summary>
        /// Creates a new PNode representing a non-occupied rectangle with given position and size
        /// and without leaves (nested rectangles).
        /// </summary>
        /// <param name="position">position of the rectangle</param>
        /// <param name="size">size of the rectangle</param>
        public PNode(Vector2 position, Vector2 size)
        {
            rectangle = new PRectangle(position, size);
            occupied = false;
        }

        public override string ToString()
        {
            return "(occupied=" + occupied + ", rectangle=" + rectangle.ToString()
                + ", left=" + (left == null ? "" : left.ToString())
                + ", right=" + (right == null ? "" : right.ToString())
                + ")";
        }
    }
}
