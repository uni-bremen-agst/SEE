using SEE.GO;
using SEE.Utils;
using System;
using UnityEngine;

namespace SEE.Game.SceneManipulation
{
    /// <summary>
    /// Stores metrics that are used for working with node placement.
    /// </summary>
    public static class SpatialMetrics
    {
        /// <summary>
        /// A small offset used for placement just outside specific bounds to prevent Z-fighting.
        /// </summary>
        public const float PlacementOffset = 0.0001f;

        /// <summary>
        /// The default local X/Z scale of a (new) node placed on another node.
        /// </summary>
        public const float DefaultNodeLocalScale = 0.2f;

        /// <summary>
        /// Half the default local X/Z scale of a (new) node placed on another node,
        /// typically used in bounds.
        /// </summary>
        public const float HalfDefaultNodeLocalScale = DefaultNodeLocalScale / 2f;

        /// <summary>
        /// The default world-space Y-axis size of a (new) node.
        /// </summary>
        public const float DefaultNodeHeight = 0.002f;

        /// <summary>
        /// The minimal size of a node in world space.
        /// </summary>
        public static readonly Vector3 MinNodeSize = new(0.06f, 0.001f, 0.06f);

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
        public Bounds2D(float left, float right, float back, float front)
        {
            Left = left;
            Right = right;
            Back = back;
            Front = front;
        }

        /// <summary>
        /// Creates a new <see cref="Bounds2D"/> from 2D position and size.
        /// <para>
        /// The origin is assumed to be dead center.
        /// </para>
        /// </summary>
        /// <param name="position">The position of the object.</param>
        /// <param name="size">The size of the object.</param>
        public Bounds2D(Vector2 position, Vector2 size)
        {
            Left = position.x - size.x / 2f;
            Right = position.x + size.x / 2f;
            Back = position.y - size.y / 2f;
            Front = position.y + size.y / 2f;
        }

        /// <summary>
        /// Creates a new <see cref="Bounds2D"/> from 3D position and size.
        /// <para>
        /// The origin is assumed to be dead center and the directions will be taken from the x/z axes.
        /// </para>
        /// </summary>
        /// <param name="position">The position of the 3D object.</param>
        /// <param name="size">The size of the 3D object.</param>
        public Bounds2D(Vector3 position, Vector3 size) : this(position.XZ(), size.XZ()) { }

        /// <summary>
        /// Creates a new <see cref="Bounds2D"/> from a <see cref="GameObject"/>.
        /// <para>
        /// Calls <see cref="GameObjectExtensions.WorldSpaceSize(GameObject, out Vector3, out Vector3)"/> to retrieve
        /// position and size.
        /// </para>
        /// </summary>
        /// <param name="go">The <see cref="GameObject"/> to create the <see cref="Bounds2D"/> from.</param>
        public Bounds2D(GameObject go)
        {
            go.WorldSpaceSize(out Vector3 size, out Vector3 position);

            Left = position.x - size.x / 2f;
            Right = position.x + size.x / 2f;
            Back = position.z - size.z / 2f;
            Front = position.z + size.z / 2f;
        }

        /// <summary>
        /// Creates a new <see cref="Bounds2D"/> from a <see cref="Portal"/>.
        /// </summary>
        /// <param name="leftFront">The portal's lower bounds (x_min, z_min).</param>
        /// <param name="rightBack">The portal's upper bounds (x_max, z_max).</param>
        /// <returns>The new <see cref="Bounds2D"/>.</returns>
        public static Bounds2D FromPortal(Vector2 leftFront, Vector2 rightBack)
        {
            Vector2 size = rightBack - leftFront;
            return new(
                leftFront.x,
                leftFront.x + size.x,
                leftFront.y,
                leftFront.y + size.y
            );
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
            readonly get
            {
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

            set
            {
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
            readonly get
            {
                return dir switch
                {
                    Direction2D.Left => Left,
                    Direction2D.Right => Right,
                    Direction2D.Back => Back,
                    Direction2D.Front => Front,
                    _ => throw new IndexOutOfRangeException($"Given direction is not possible in {nameof(Bounds2D)}: {dir}")
                };
            }

            set
            {
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
        /// <returns>this <see cref="Bounds2D"/> in readable form</returns>
        public readonly override string ToString()
        {
            return $"{nameof(Bounds2D)}(Left: {Left}, Right: {Right}, Back: {Back}, Front: {Front})";
        }

        /// <summary>
        /// Checks if the <paramref name="other"/> bounds is contained in this <see cref="Bounds2D"/>.
        /// </summary>
        /// <param name="other">The bounds of another object.</param>
        /// <returns>True if <paramref name="other"/> is contained in this <see cref="Bounds2D"/>.</returns>
        public readonly bool Contains(Bounds2D other)
        {
            return other.Left >= Left && other.Right <= Right && other.Back >= Back && other.Front <= Front;
        }

        /// <summary>
        /// Checks if the <paramref name="point"/> is contained in this <see cref="Bounds2D"/>.
        /// </summary>
        /// <param name="point">The point to check against.</param>
        /// <returns>True if <paramref name="point"/> is contained in this <see cref="Bounds2D"/>.</returns>
        public readonly bool Contains(Vector2 point)
        {
            return point.x >= Left && point.x <= Right && point.y >= Back && point.y <= Front;
        }

        /// <summary>
        /// Checks if the 2D equivalent of <paramref name="point"/> is contained in this <see cref="Bounds2D"/>.
        /// <para>
        /// Takes the x and z coordinates of the point and converts them to 2D coordinates.
        /// </para>
        /// </summary>
        /// <param name="point">The point to check against.</param>
        /// <returns>True if 2D equivalent of <paramref name="point"/> is contained in this <see cref="Bounds2D"/>.</returns>
        public readonly bool Contains(Vector3 point) { return Contains(point.XZ()); }

        /// <summary>
        /// Checks if there is an overlap between this <see cref="Bounds2D"/> and <paramref name="other"/>.
        /// </summary>
        /// <param name="other">The bounds of another object.</param>
        /// <returns><c>true</c> iff the bounds overlap.</returns>
        public readonly bool HasOverlap(Bounds2D other)
        {
            return Front >= other.Back && Back <= other.Front && Left <= other.Right && Right >= other.Left;
        }

        /// <summary>
        /// Simulates a simple raycast and returns <c>true</c> if the ray from <see cref="point"/>
        /// in given <see cref="direction"/> intersects with the bounds.
        /// </summary>
        /// <param name="point">The position from which to cast.</param>
        /// <param name="direction">The direction in which to cast.</param>
        /// <returns><c>true</c> if the ray intersects with the bounds, else <c>false</c>.>.</returns>
        public readonly bool LineIntersect(Vector2 point, Direction2D direction)
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
