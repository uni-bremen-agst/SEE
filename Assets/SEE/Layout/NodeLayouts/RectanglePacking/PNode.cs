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

    /// <summary>
    /// Left child.
    /// </summary>
    public PNode Left;

    /// <summary>
    /// Right child.
    /// </summary>
    public PNode Right;

    public PNode Parent;

    /// <summary>
    /// Creates a new PNode representing a non-occupied rectangle with position Vector2.zero and size
    /// Vector2.zero and without leaves (nested rectangles). Equivalent to PNode(Vector2.zero, Vector2.zero).
    /// </summary>
    public PNode() : this(Vector2.zero, Vector2.zero, null)
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
    }

    public PNode(Vector2 position, Vector2 size, PNode parent)
    {
      Rectangle = new PRectangle(position, size);
      Occupied = false;
      Parent = parent;
    }

    public void RecomputeBounds()
    {
      this.Rectangle.Position.x = Left.Rectangle.Position.x;
      this.Rectangle.Position.y = Left.Rectangle.Position.y;
      this.Rectangle.Size.x = Right.Rectangle.Size.x + Left.Rectangle.Size.x;
      this.Rectangle.Size.y = Mathf.Max(Right.Rectangle.Size.y, Left.Rectangle.Size.y);         
    }
    /*
    public void RecomputeBounds()
    {
      if (Left == null || Right == null)
        return;

      // Parent covers exactly the min bounds of children
      this.Rectangle.Position = Left.Rectangle.Position;

      // Width is sum of horizontal children
      this.Rectangle.Size.x = Left.Rectangle.Size.x + Right.Rectangle.Size.x;

      // Height is max of vertical children
      this.Rectangle.Size.y = Mathf.Max(Left.Rectangle.Size.y, Right.Rectangle.Size.y);
    }
     */


    public override string ToString()
    {
      return "(occupied=" + Occupied + ", rectangle=" + Rectangle.ToString()
          + ", left=" + (Left == null ? "" : Left.ToString())
          + ", right=" + (Right == null ? "" : Right.ToString())
          + ")";
    }
  }
}
