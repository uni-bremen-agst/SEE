using System;
using UnityEngine;

namespace SEE.Game.SceneManipulation
{
    /// <summary>
    /// Stores metrics that are used for working with node placement.
    /// </summary>
    public class SpatialMetrics
    {
        /// <summary>
        /// A small offset used for placement just outside specific bounds to prevent Z-fighting.
        /// </summary>
        public const float PlacementOffset = 0.0001f;

        /// <summary>
        /// The default local X/Z scale of a (new) node placed on another node.
        /// </summary>
        public static float DefaultNodeLocalScale = 0.2f;

        /// <summary>
        /// Half the default local X/Z scale of a (new) node placed on another node,
        /// typically used in bounds.
        /// </summary>
        public static float HalfDefaultNodeLocalScale = DefaultNodeLocalScale / 2f;

        /// <summary>
        /// The default world-space Y-axis size of a (new) node.
        /// </summary>
        public static float DefaultNodeHeight = 0.002f;

        /// <summary>
        /// The minimal size of a node in world space.
        /// </summary>
        public static readonly Vector3 MinNodeSize = new (0.06f, 0.001f, 0.06f);

        /// <summary>
        /// The minimal world-space distance between nodes for placing or resizing (x/z, y).
        /// </summary>
        public static readonly Vector3 Padding = new(0.004f, 0.0001f, 0.004f);

        /// <summary>
        /// The minimal size of a node in local scale.
        /// </summary>
        /// <param name="localTransform">Transform of the reference node.</param>
        /// <returns>The <see cref="MinNodeSize"/> in <paramref name="localTransform"/>'s local scale.</returns>
        public static Vector3 MinNodeSizeLocalScale(Transform localTransform)
        {
            return localTransform.InverseTransformVector(MinNodeSize);
        }
    }

    /// <summary>
    /// Direction in 2D plane.
    /// </summary>
    public enum Direction2D
    {
        Left, Right, Back, Front, None
    }

    /// <summary>
    /// Data structure for 2-dimensional bounds.
    /// </summary>
    public struct Bounds2D
    {
        /// <summary>
        /// The left side of the area.
        /// </summary>
        public float Left;
        /// <summary>
        /// The right side of the area.
        /// </summary>
        public float Right;
        /// <summary>
        /// The back side of the area.
        /// </summary>
        public float Back;
        /// <summary>
        /// The front side of the area.
        /// </summary>
        public float Front;

        /// <summary>
        /// Initializes the struct.
        /// </summary>
        public Bounds2D (float left, float right, float back, float front)
        {
            Left = left;
            Right = right;
            Back = back;
            Front = front;
        }

        /// <summary>
        /// Gets or sets the element at the specified direction.
        /// </summary>
        /// <param name="dir">The direction of the element to get or set.</param>
        /// <returns>The element at the specified direction.</returns>
        /// <exception cref="IndexOutOfRangeException">
        /// Thrown when the direction is ambiguous or zero.
        /// </exception>
        public float this[Vector3 dir]
        {
            get {
                if (dir == Vector3.left)
                {
                    return Left;
                }
                if (dir == Vector3.right)
                {
                    return Right;
                }
                if (dir == Vector3.forward)
                {
                    return Front;
                }
                if (dir == Vector3.back)
                {
                    return Back;
                }
                throw new IndexOutOfRangeException($"Given direction is not possible in {nameof(Bounds2D)}: {dir}");
            }

            set {
                if (dir == Vector3.left)
                {
                    Left = value;
                }
                else if (dir == Vector3.right)
                {
                    Right = value;
                }
                else if (dir == Vector3.back)
                {
                    Back = value;
                }
                else if (dir == Vector3.forward)
                {
                    Front = value;
                }
                throw new IndexOutOfRangeException($"Given direction is not possible in {nameof(Bounds2D)}: {dir}");
            }
        }

        /// <summary>
        /// Gets or sets the element at the specified direction.
        /// </summary>
        /// <param name="dir">The direction of the element to get or set.</param>
        /// <returns>The element at the specified direction.</returns>
        /// <exception cref="IndexOutOfRangeException">
        /// Thrown when the direction is <see cref="Direction2D.None"/>.
        /// </exception>
        public float this[Direction2D dir]
        {
            get {
                switch (dir)
                {
                    case Direction2D.Left:
                        return Left;
                    case Direction2D.Right:
                        return Right;
                    case Direction2D.Back:
                        return Back;
                    case Direction2D.Front:
                        return Front;
                    default:
                        throw new IndexOutOfRangeException($"Given direction is not possible in {nameof(Bounds2D)}: {dir}");
                }
            }

            set {
                switch (dir)
                {
                    case Direction2D.Left:
                        Left = value;
                        break;
                    case Direction2D.Right:
                        Right = value;
                        break;
                    case Direction2D.Back:
                        Back = value;
                        break;
                    case Direction2D.Front:
                        Front = value;
                        break;
                    default:
                        throw new IndexOutOfRangeException($"Given direction is not possible in {nameof(Bounds2D)}: {dir}");
                }
            }
        }

        /// <summary>
        /// Implicit conversion to string.
        /// <summary>
        public static implicit operator string(Bounds2D bounds)
        {
            return bounds.ToString();
        }

        /// <summary>
        /// Returns a printable string with the struct's values.
        /// <summary>
        public override readonly string ToString()
        {
            return $"{nameof(Bounds2D)}(Left: {Left}, Right: {Right}, Back: {Back}, Front: {Front})";
        }

        /// <summary>
        /// Checks if there is an overlap between two bounds.
        /// </summary>
        /// <param name="other">The bounds of another object.</param>
        /// <returns><c>true</c> iff the bounds overlap.</returns>
        public bool HasOverlap(Bounds2D other)
        {
            if (Front < other.Back || Back > other.Front || Left > other.Right || Right < other.Left)
            {
                // No overlap detected
                return false;
            }
            return true;
        }

        /// <summary>
        /// Simulates a simple raycast and returns <c>true</c> if the ray from <see cref="point"/>
        /// in given <see cref="direction"/> intersects with the bounds.
        /// </summary>
        /// <param name="point">The position from which to cast.</param>
        /// <param name="direction">The direction in which to cast.</param>
        /// <returns><c>true</c> if the ray intersects with the bounds, else <c>false</c>.></returns>
        public bool LineIntersect(Vector2 point, Direction2D direction)
        {
            if ((direction == Direction2D.Left || direction == Direction2D.Right) &&
                    point.y >= Back && point.y <= Front)
            {
                if (direction == Direction2D.Left && point.x >= Right ||
                        direction == Direction2D.Right && point.x <= Left)
                {
                    return true;
                }
            }
            else if ((direction == Direction2D.Back || direction == Direction2D.Front) &&
                    point.x >= Left && point.x <= Right)
            {
                if (direction == Direction2D.Back && point.y >= Front ||
                        direction == Direction2D.Front && point.y <= Back)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
