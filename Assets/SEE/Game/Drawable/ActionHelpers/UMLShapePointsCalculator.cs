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
            return new Vector3[] { rect.A, rect.B, rect.C, rect.D, lTitleHeight, rTitleHeight, rTitleWidth, rect.D, rect.A };
        }

        /// <summary>
        /// Generates the point sequence for a UML provided interface (lollipop).
        /// </summary>
        /// <param name="point">Center of the interface circle.</param>
        /// <param name="radius">Radius of the circle.</param>
        /// <returns>Ordered points used to draw the symbol.</returns>
        public static Vector3[] ProvideInterface(Vector3 point, float radius)
        {
            List<Vector3> circle = new(Circle(point, radius));
            return BuildInterface(circle, radius);
        }

        /// <summary>
        /// Generates the point sequence for a UML required interface (socket).
        /// </summary>
        /// <param name="point">Center of the half circle.</param>
        /// <param name="radius">Radius of the half circle.</param>
        /// <returns>Ordered points used to draw the symbol.</returns>
        public static Vector3[] ReceiveInterface(Vector3 point, float radius)
        {
            List<Vector3> halfCircle =
                new(HalfCircle(
                    point,
                    radius,
                    HalfCircleOrientation.Up));

            return BuildInterface(halfCircle, radius);
        }

        /// <summary>
        /// Builds the connector structure for interface symbols.
        /// </summary>
        /// <param name="arcPoints">Points describing the circle or half circle.</param>
        /// <param name="radius">Connector length.</param>
        /// <returns>Drawable point sequence.</returns>
        private static Vector3[] BuildInterface(List<Vector3> arcPoints, float radius)
        {
            if (arcPoints.Count < 2)
                throw new ArgumentException("Arc must contain at least two points.");

            int mid = arcPoints.Count / 2;
            int upperCount = mid + 1;

            List<Vector3> positions = new List<Vector3>(arcPoints.Count + 2);

            positions.AddRange(arcPoints.GetRange(0, upperCount));

            Vector3 bottom = arcPoints[mid];
            Vector3 connector = new Vector3(bottom.x, bottom.y - radius);

            positions.Add(connector);
            positions.Add(connector);

            positions.AddRange(arcPoints.GetRange(mid, upperCount));

            return positions.ToArray();
        }

        /// <summary>
        /// Generates the point sequence for a UML send activity action.
        /// The symbol is represented by a rectangle with an outgoing connector on the right side.
        /// </summary>
        /// <param name="point">Center position of the activity.</param>
        /// <param name="width">Width of the rectangle.</param>
        /// <param name="height">Height of the rectangle.</param>
        /// <param name="connectorRight">
        /// If true, the connector is placed on the right side.
        /// If false, it is placed on the left side.
        /// </param>
        /// <returns>An ordered array of drawable points.</returns>
        /// <returns>An ordered array of <see cref="Vector3"/> used to draw the shape.</returns>
        public static Vector3[] SendActivity(Vector3 point, float width, float height, bool connectorRight)
        {
            return BuildActivity(point, width, height, connectorRight, true);
        }

        /// <summary>
        /// Generates the point sequence for a UML receive activity action.
        /// The symbol is represented by a rectangle with an incoming connector on the left side.
        /// </summary>
        /// <param name="point">Center position of the activity.</param>
        /// <param name="width">Width of the rectangle.</param>
        /// <param name="height">Height of the rectangle.</param>
        /// <param name="connectorRight">
        /// If true, the connector is placed on the right side.
        /// If false, it is placed on the left side.
        /// </param>
        /// <returns>An ordered array of <see cref="Vector3"/> used to draw the shape.</returns>
        public static Vector3[] ReceiveActivity(Vector3 point, float width, float height, bool connectorRight)
        {
            return BuildActivity(point, width, height, connectorRight, false);
        }

        /// <summary>
        /// Builds the point sequence for send/receive activity shapes.
        /// </summary>
        /// <param name="point">Center position of the activity.</param>
        /// <param name="width">Width of the rectangle.</param>
        /// <param name="height">Height of the rectangle.</param>
        /// <param name="mirror">
        /// If true, the connector is placed on the right side.
        /// If false, it is placed on the left side.
        /// </param>
        /// <param name="sendActivity">If true, the positions of a send activity object will be calculated.
        /// If false, the positions of a receive object will be calculated.</param>
        /// <returns>An ordered array of drawable points.</returns>
        private static Vector3[] BuildActivity(Vector3 point, float width, float height, bool mirror, bool sendActivity)
        {
            RectangleShape rect = BuildRectangle(point, width, height);
            Vector3 middlePoint = GetMiddlePoint();
            float offset = (mirror ? 1 : -1) * width * 0.2f;
            Vector3 connector =  new(middlePoint.x - offset, middlePoint.y, middlePoint.z);

            return (sendActivity ^ mirror) ?
                new Vector3[] { rect.A, rect.B, connector, rect.C, rect.D, rect.A }
                : new Vector3[] { rect.A, rect.B, rect.C, rect.D, connector, rect.A };

            Vector3 GetMiddlePoint()
            {
                float x = (sendActivity ^ mirror) ? rect.B.x : rect.A.x;
                float y = rect.A.y + height / 2;
                float z = rect.A.z;
                return new Vector3(x, y, z);
            }
        }
    }
}
