using System.Collections.Generic;
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
        public PRectangle Rectangle = new();

        /// <summary>
        /// Whether the rectangle is occupied.
        /// </summary>
        public bool Occupied;

        public string Id;

        /// <summary>
        /// Left child.
        /// </summary>
        public PNode Left;

        /// <summary>
        /// Right child.
        /// </summary>
        public PNode Right;

        public PNode Rest;

        public List<PNode> Rests;

        public PNode Parent;

        public Vector2 RightDownCorner
        {
            get
            {
                return new Vector2(Rectangle.Position.x + Rectangle.Size.x, Rectangle.Position.y + Rectangle.Size.y);
            }
        }

        public float PNodeRight => Rectangle.Position.x + Rectangle.Size.x;
        public float PNodeBottom => Rectangle.Position.y + Rectangle.Size.y;

        public float XX => Rectangle.Position.x;
        public float YY => Rectangle.Position.y;

        public float Width
        {
            get => Rectangle.Size.x;
            set => Rectangle.Size.x = value;
        }
        public float Height
        {
            get => Rectangle.Size.y;
            set => Rectangle.Size.y = value;
        }


        public Vector2 Position => Rectangle.Position;

        public Vector2 Size => Rectangle.Size;

        public enum SplitDirection
        {
            Left,
            Right,
            None
        }

        public SplitDirection Direction;

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
            Rectangle = new PRectangle(position, size);
            Occupied = false;
            Id = null;
            Rests = new List<PNode>();
            Parent = null;
        }

        public PNode(Vector2 position, Vector2 size, string newID)
        {
            Rectangle = new PRectangle(position, size);
            Occupied = false;
            Id = newID;
            Rests = new List<PNode>();
            Parent = null;
        }


        public PNode(Vector2 position, Vector2 size, PNode parent)
        {
            Rectangle = new PRectangle(position, size);
            Occupied = false;
            Id = null;
            Parent = parent;
            Rests = new List<PNode>();
        }


        public override string ToString()
        {
            return "("
              + Direction
              + ", ID=" + Id
              + ", occupied=" + Occupied
              + ", rectangle=" + Rectangle.ToString()
              + ", =" + (Left == null ? "" : Left.ToString())
              + ", =" + (Right == null ? "" : Right.ToString())
              + ", =" + (Rest == null ? "" : Rest.ToString())
              + ")";
        }
        public string ToString1()
        {
            return "("
              + "ID=" + Id
              + ", occupied=" + Occupied
              + ", rectangle=" + Rectangle.ToString()
              + ")";
        }
    }
}