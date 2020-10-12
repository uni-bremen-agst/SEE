using UnityEngine;

namespace SEE.Layout.NodeLayouts.RectanglePacking
{
    /// <summary>
    /// Representation of a rectangle area for PTree, either occupied or free.
    /// </summary>
    public class PRectangle
    {
        // width (x) and depth (y) of the rectangle
        public Vector2 size;
        // position of the rectangle relative to the origin that is the left upper corner of the original available space
        public Vector2 position;

        public PRectangle()
        {
            size = Vector2.zero;
            position = Vector2.zero;
        }

        public PRectangle(Vector2 position, Vector2 size)
        {
            this.size = size;
            this.position = position;
        }

        public override string ToString()
        {
            return "[position=" + position + ", size=" + size + "]";
        }
    }
}
