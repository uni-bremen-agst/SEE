using UnityEngine;
using System.Collections.Generic;

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

        /// <summary>
        /// The ID of the rectangle.
        /// </summary>
        public string Id;

        /// <summary>
        /// Left child. (in the Rectangle Packer but not in incremental rectangle packing)
        /// </summary>
        public PNode Left;

        /// <summary>
        /// Right child. (in the Rectangle Packer but not in incremental rectangle packing)
        /// </summary>
        public PNode Right;

        /// <summary>
        /// all the rectangles that are occupied in this level of the tree. (in incremental rectangle packing)
        /// </summary>
        public List<PNode> Rests;

        /// <summary>
        /// The parent node of this PNode. (in incremental rectangle packing)
        /// </summary>
        public PNode Parent;

        /// <summary>
        /// Gets the right-down corner of the rectangle represented by this PNode.
        /// </summary>
        public Vector2 RightDownCorner
        {
            get
            {
                return new Vector2(Rectangle.Position.x + Rectangle.Size.x, Rectangle.Position.y + Rectangle.Size.y);
            }
        }

        /// <summary>
        /// Gets the right edge of the rectangle represented by this PNode.
        /// </summary>
        public float PNodeRight => Rectangle.Position.x + Rectangle.Size.x;

        /// <summary>
        /// Gets the bottom edge of the rectangle represented by this PNode.
        /// </summary>
        public float PNodeBottom => Rectangle.Position.y + Rectangle.Size.y;

        /// <summary>
        /// Gets the X coordinate of the rectangle's position.
        /// </summary>
        public float XX => Rectangle.Position.x;

        /// <summary>
        /// Gets the Y coordinate of the rectangle's position.
        /// </summary>
        public float YY => Rectangle.Position.y;

        /// <summary>
        /// Gets or sets the width of the rectangle represented by this PNode.
        /// </summary>
        public float Width
        {
            get => Rectangle.Size.x;
            set => Rectangle.Size.x = value;
        }

        /// <summary>
        /// Gets or sets the height of the rectangle represented by this PNode.
        /// </summary>
        public float Height
        {
            get => Rectangle.Size.y;
            set => Rectangle.Size.y = value;
        }

        /// <summary>
        /// Gets the position of the rectangle represented by this PNode.
        /// </summary>
        public Vector2 Position => Rectangle.Position;

        /// <summary>
        /// Gets the size of the rectangle represented by this PNode.
        /// </summary>
        public Vector2 Size => Rectangle.Size;

        /// <summary>
        /// Defines the direction in which the rectangle represented by this PNode is split.
        /// (relevant for debugging purposes in the rectangle packing)
        /// </summary>
        public enum SplitDirection
        {
            Left,
            Right,
            None
        }

        /// <summary>
        /// Defines the direction in which the rectangle represented by this PNode is split.
        /// (relevant for debugging purposes in the rectangle packing)
        /// </summary>
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

        /// <summary>
        /// Creates a new PNode representing a non-occupied rectangle with given position, size, and ID
        /// </summary>
        /// <param name="position"></param>
        /// <param name="size"></param>
        /// <param name="newID"></param>
        public PNode(Vector2 position, Vector2 size, string newID)
        {
            Rectangle = new PRectangle(position, size);
            Occupied = false;
            Id = newID;
            Rests = new List<PNode>();
            Parent = null;
        }

        /// <summary>
        /// Returns a string representation of the PNode, including its direction, ID,
        /// occupancy status, rectangle details, and information about its left and right children.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return "("
              + Direction
              + ", ID=" + Id
              + ", occupied=" + Occupied
              + ", rectangle=" + Rectangle.ToString()
              + ", =" + (Left == null ? "" : Left.ToString())
              + ", =" + (Right == null ? "" : Right.ToString())
              + ")";
        }


        /// <summary>
        /// Returns a string representation of the PNode, including its ID, occupancy status, and rectangle details.
        /// </summary>
        /// <returns></returns>
        public string ToStringNotOverride()
        {
            return "("
              + "ID=" + Id
              + ", occupied=" + Occupied
              + ", rectangle=" + Rectangle.ToString()
              + ")";
        }
    }
}