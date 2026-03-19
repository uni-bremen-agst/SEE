using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static SEE.Game.Drawable.ActionHelpers.ShapePointsCalculator;

namespace SEE.Game.Drawable.ActionHelpers
{
    /// <summary>
    /// This class provides the position calculation for UML shapes.
    /// </summary>
    public static class UMLShapePointsCalculator
    {
        /// <summary>
        /// The different kinds of an UML shape.
        /// </summary>
        public enum UMLShape
        {
            Actor,
            Note,
            Package,
            ProvideInterf,
            ReceiveInterf,
            SendActivity,
            ReceiveActivity
        }

        /// <summary>
        /// Gets a list with all UML shape kinds.
        /// </summary>
        /// <returns>A list that holds all UML shape kinds.</returns>
        public static List<UMLShape> GetUMLShapes()
        {
            return Enum.GetValues(typeof(UMLShape)).Cast<UMLShape>().ToList();
        }

        /// <summary>
        /// Generates the points for a stick-figure Actor.
        /// </summary>
        /// <param name="point">The central point of the Actor (roughly at the feet or center of the figure).</param>
        /// <param name="length">The base length/scale of the Actor.</param>
        /// <returns>An array of <see cref="Vector3"/> positions that define the Actor's shape.</returns>
        public static Vector3[] Actor(Vector3 point, float length)
        {
            if (length < 0.0001f)
            {
                return new Vector3[] { point };
            }
            float scale = length * 10f;

            const float baseDist = 0.1f;
            const float baseHeadRadius = 0.03f;

            float dist = baseDist * scale;
            float headRadius = baseHeadRadius * scale;
            float limbOffset = dist / 3f;

            int circleVertices = Mathf.CeilToInt(PointsCalculator.DefaultVertices * scale);

            Vector3 circleMid = new(point.x, point.y + limbOffset + headRadius, point.z);
            Vector3[] circle = Polygon(circleMid, headRadius, headRadius, circleVertices);
            int halfCircleLength = circle.Length / 2;

            Vector3 neck = new(point.x, point.y + limbOffset, point.z);
            Vector3 body = new(point.x, point.y - limbOffset, point.z);
            Vector3 leftArm = new(point.x - limbOffset, point.y, point.z);
            Vector3 rightArm = new(point.x + limbOffset, point.y, point.z);
            Vector3 leftLeg = new(point.x - dist / 4, point.y - dist / 2, point.z);
            Vector3 rightLeg = new(point.x + dist / 4, point.y - dist / 2, point.z);

            List<Vector3> positions = circle
                .Take(halfCircleLength)
                .Concat(new[]
                {
                    neck, point, leftArm, rightArm, point, body,
                    leftLeg, body, rightLeg, body, neck
                })
                .Concat(circle.Skip(halfCircleLength))
                .ToList();

            return positions.ToArray();
        }

        /// <summary>
        /// Generates the points for a UML Note shape, including the folded corner.
        /// </summary>
        /// <param name="point">The central point of the Note (roughly the bottom-left corner of the rectangle).</param>
        /// <param name="aLength">The width of the Note.</param>
        /// <param name="bLength">The height of the Note.</param>
        /// <returns>
        /// An array of <see cref="Vector3"/> representing the Note's vertices in drawable coordinates.
        /// The array includes the folded corner lines for visualization.
        /// </returns>
        public static Vector3[] Note(Vector3 point, float aLength, float bLength)
        {
            float foldA = aLength / 3;
            float foldB = bLength / 3;

            Vector3 a = PointsCalculator.ToDrawable(point.x - aLength / 2, point.y - bLength / 2);
            Vector3 b = PointsCalculator.ToDrawable(a.x + aLength, a.y);
            Vector3 bc = PointsCalculator.ToDrawable(b.x, b.y + bLength - foldB);
            Vector3 cd = PointsCalculator.ToDrawable(b.x - foldA, b.y + bLength);
            Vector3 bcd = PointsCalculator.ToDrawable(cd.x, cd.y - foldB);
            Vector3 d = PointsCalculator.ToDrawable(a.x, a.y + bLength);
            return new Vector3[] { a, b, bc, cd, bcd, bc, cd, d, a };
        }

        /// <summary>
        /// Creates a package shape consisting of a main rectangle with a title rectangle attached to the top-left.
        /// The main rectangle is defined by its center point, width (aLength), and height (bLength).
        /// The title rectangle is always present and positioned above the top-left corner of the main rectangle.
        /// Its width and height can be specified but are clamped to reasonable proportions of the main rectangle.
        /// </summary>
        /// <param name="point">Center point of the main rectangle.</param>
        /// <param name="aLength">Width of the main rectangle.</param>
        /// <param name="bLength">Height of the main rectangle.</param>
        /// <param name="titleWidth">
        /// Desired width of the title rectangle. The value is clamped between one third and 80% of <paramref name="aLength"/>.
        /// </param>
        /// <param name="titleHeight">
        /// Desired height of the title rectangle. The value is clamped between one third and 50% of <paramref name="bLength"/>.
        /// </param>
        /// <returns>
        /// An array of <see cref="Vector3"/> points representing the package outline.
        /// The points describe the main rectangle and the attached title rectangle in drawing order.
        /// The first point is repeated at the end to close the shape.
        /// </returns>
        /// <remarks>
        /// If the provided title dimensions fall outside the allowed range,
        /// they are automatically clamped to keep the title rectangle visually proportional to the main rectangle.
        /// </remarks>
        public static Vector3[] Package(Vector3 point, float aLength, float bLength, float titleWidth, float titleHeight)
        {
            float width = Math.Clamp(titleWidth, aLength / 3f, aLength * 0.8f);
            float height = Math.Clamp(titleHeight, bLength / 3f, bLength * 0.5f);

            RectangleShape rect = BuildRectangle(point, aLength, bLength);

            Vector3 lTitleHeight = new(rect.D.x, rect.D.y + height, 0);
            Vector3 rTitleHeight = new(rect.D.x + width, lTitleHeight.y, 0);
            Vector3 rTitleWidth = new(rect.D.x + width, rect.D.y, 0);
            return new[] { rect.A, rect.B, rect.C, rect.D, lTitleHeight, rTitleHeight, rTitleWidth, rect.D, rect.A };
        }

        /// <summary>
        /// Generates the point sequence for a UML provided interface (lollipop).
        /// The base geometry is created facing right and then transformed
        /// to the requested <paramref name="orientation"/>.
        /// </summary>
        /// <param name="point">Center of the interface circle.</param>
        /// <param name="radius">Radius of the circle.</param>
        /// <param name="orientation">Target orientation.</param>
        /// <returns>An ordered array of drawable points.</returns>
        public static Vector3[] ProvideInterface(Vector3 point, float radius, Orientation orientation)
        {
            List<Vector3> circle = new(Circle(point, radius));
            Vector3[] baseShape = BuildInterfaceBase(circle, point, radius);

            return TransformPoints(baseShape, point, orientation);
        }

        /// <summary>
        /// Generates the point sequence for a UML required interface (socket).
        /// The base geometry is created facing right and then transformed
        /// to the requested <paramref name="orientation"/>.
        /// </summary>
        /// <param name="point">Center of the half circle.</param>
        /// <param name="radius">Radius of the half circle.</param>
        /// <param name="orientation">Target orientation.</param>
        /// <returns>An ordered array of drawable points.</returns>
        public static Vector3[] ReceiveInterface(Vector3 point, float radius, Orientation orientation)
        {
            List<Vector3> halfCircle = new(HalfCircle(point, radius, Orientation.Right));
            Vector3[] baseShape = BuildInterfaceBase(halfCircle, point, radius);

            return TransformPoints(baseShape, point, orientation);
        }

        /// <summary>
        /// Builds the base interface geometry facing right.
        /// A connector is inserted at the rightmost point of the arc.
        /// </summary>
        /// <param name="arcPoints">Circle or half-circle points.</param>
        /// <param name="center">Center of the shape.</param>
        /// <param name="radius">Radius of the shape.</param>
        /// <returns>The base geometry with connector.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown if <paramref name="arcPoints"/> is invalid.
        /// </exception>
        private static Vector3[] BuildInterfaceBase(List<Vector3> arcPoints, Vector3 center, float radius)
        {
            if (arcPoints.Count < 2)
            {
                throw new ArgumentException("Arc must contain at least two points.");
            }

            Vector3 target = center + Vector3.right * radius;

            int nearestIndex = 0;
            float nearestDistance = Vector3.SqrMagnitude(arcPoints[0] - target);

            for (int i = 1; i < arcPoints.Count; i++)
            {
                float distance = Vector3.SqrMagnitude(arcPoints[i] - target);

                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestIndex = i;
                }
            }

            Vector3 connector = arcPoints[nearestIndex] + Vector3.right * radius;

            List<Vector3> result = new List<Vector3>(arcPoints.Count + 2);

            for (int i = 0; i <= nearestIndex; i++)
            {
                result.Add(arcPoints[i]);
            }

            result.Add(connector);
            result.Add(arcPoints[nearestIndex]);

            for (int i = nearestIndex + 1; i < arcPoints.Count; i++)
            {
                result.Add(arcPoints[i]);
            }

            return result.ToArray();
        }

        /// <summary>
        /// Generates the point sequence for a UML send activity action.
        /// The symbol is represented by a rectangle with an outgoing tip
        /// on the side defined by <paramref name="orientation"/>.
        /// </summary>
        /// <param name="point">Center position of the activity.</param>
        /// <param name="width">Width of the rectangle.</param>
        /// <param name="height">Height of the rectangle.</param>
        /// <param name="orientation">Orientation of the outgoing side.</param>
        /// <returns>
        /// An ordered array of <see cref="Vector3"/> used to draw the shape.
        /// </returns>
        public static Vector3[] SendActivity(Vector3 point, float width, float height, Orientation orientation)
        {
            return BuildActivity(point, width, height, orientation, true);
        }

        /// <summary>
        /// Generates the point sequence for a UML receive activity action.
        /// The symbol is represented by a rectangle with an inward tip
        /// on the side defined by <paramref name="orientation"/>.
        /// </summary>
        /// <param name="point">Center position of the activity.</param>
        /// <param name="width">Width of the rectangle.</param>
        /// <param name="height">Height of the rectangle.</param>
        /// <param name="orientation">Orientation of the incoming side.</param>
        /// <returns>
        /// An ordered array of <see cref="Vector3"/> used to draw the shape.
        /// </returns>
        public static Vector3[] ReceiveActivity(Vector3 point, float width, float height, Orientation orientation)
        {
            return BuildActivity(point, width, height, orientation, false);
        }

        /// <summary>
        /// Builds the point sequence for UML send and receive activity shapes.
        /// The base geometry is always created in the left-facing orientation.
        /// Other orientations are derived by transforming that base geometry,
        /// ensuring consistent proportions across all directions.
        /// </summary>
        /// <param name="point">Center position of the activity shape.</param>
        /// <param name="width">Width of the activity shape.</param>
        /// <param name="height">Height of the activity shape.</param>
        /// <param name="orientation">Target orientation of the activity shape.</param>
        /// <param name="sendActivity">
        /// <see langword="true"/> for a send activity (outward tip);
        /// otherwise a receive activity (inward notch).
        /// </param>
        /// <returns>An ordered array of drawable points.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown if <paramref name="orientation"/> is invalid.
        /// </exception>
        private static Vector3[] BuildActivity(Vector3 point, float width, float height, Orientation orientation, bool sendActivity)
        {
            RectangleShape rect = BuildRectangle(point, width, height);
            float offset = width * 0.2f;

            Vector3 connector = sendActivity
                ? new Vector3(rect.A.x - offset, rect.A.y + (height / 2.0f), rect.A.z)
                : new Vector3(rect.A.x + offset, rect.A.y + (height / 2.0f), rect.A.z);

            Vector3[] basePoints = new[]
            {
                rect.A, rect.B, rect.C, rect.D, connector, rect.A
            };

            return TransformPoints(basePoints, point, orientation);
        }

        /// <summary>
        /// Transforms a set of points from the base left-oriented geometry
        /// into the target <paramref name="orientation"/>.
        /// </summary>
        /// <param name="points">The base points to transform.</param>
        /// <param name="center">The center of transformation.</param>
        /// <param name="orientation">The desired orientation.</param>
        /// <returns>The transformed point array.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown if <paramref name="orientation"/> is invalid.
        /// </exception>
        private static Vector3[] TransformPoints(Vector3[] points, Vector3 center, Orientation orientation)
        {
            if (orientation == Orientation.Left)
            {
                return points;
            }

            Vector3[] result = new Vector3[points.Length];

            for (int i = 0; i < points.Length; i++)
            {
                Vector3 local = points[i] - center;

                switch (orientation)
                {
                    case Orientation.Right:
                        {
                            result[i] = new Vector3(
                                center.x - local.x,
                                center.y + local.y,
                                points[i].z);
                            break;
                        }

                    case Orientation.Up:
                        {
                            result[i] = new Vector3(
                                center.x + local.y,
                                center.y - local.x,
                                points[i].z);
                            break;
                        }

                    case Orientation.Down:
                        {
                            result[i] = new Vector3(
                                center.x - local.y,
                                center.y + local.x,
                                points[i].z);
                            break;
                        }

                    default:
                        {
                            throw new ArgumentOutOfRangeException(nameof(orientation), orientation, null);
                        }
                }
            }

            return result;
        }

        /// <summary>
        /// Returns the axis-aligned direction vector for the given orientation.
        /// </summary>
        /// <param name="orientation">The orientation to convert.</param>
        /// <returns>A direction vector matching the orientation.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown if <paramref name="orientation"/> is not a valid value.
        /// </exception>
        private static Vector3 GetOrientationDirection(Orientation orientation)
        {
            switch (orientation)
            {
                case Orientation.Up:
                    return Vector3.up;
                case Orientation.Down:
                    return Vector3.down;
                case Orientation.Left:
                    return Vector3.left;
                case Orientation.Right:
                    return Vector3.right;
                default:
                    throw new ArgumentOutOfRangeException(nameof(orientation), orientation, null);
            }
        }
    }
}
