using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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
            Actor
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
            Vector3[] circle = ShapePointsCalculator.Polygon(circleMid, headRadius, headRadius, circleVertices);
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
    }
}
