using SEE.Game.Drawable.Configurations;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SEE.Game.Drawable.ActionHelpers
{
    /// <summary>
    /// Provides point calculations for line caps (decorations at the start or end of a line).
    /// Line caps are geometric shapes such as arrowheads or diamonds that are attached
    /// to a line based on its direction.
    /// </summary>
    public static class LineCapPointsCalculator
    {
        /// <summary>
        /// Defines the available types of line caps.
        /// </summary>
        public enum LineCap
        {
            None,
            Arrowhead,
            Aggregation,
            Composition,
            Circle
        }

        /// <summary>
        /// Specifies whether a line cap is calculated for the start or end of a line.
        /// </summary>
        public enum LineCapPosition
        {
            Start,
            End
        }

        /// <summary>
        /// Represents the calculated geometry of a line cap.
        /// </summary>
        public readonly struct LineCapShape
        {
            /// <summary>
            /// The drawable points of the line cap.
            /// </summary>
            public Vector3[] Points { get; }

            /// <summary>
            /// The point where the main line should connect to the line cap.
            /// </summary>
            public Vector3 ConnectionPoint { get; }

            /// <summary>
            /// Creates a new line cap shape result.
            /// </summary>
            /// <param name="points">The drawable points of the line cap.</param>
            /// <param name="connectionPoint">The point where the main line should connect to the line cap.</param>
            public LineCapShape(Vector3[] points, Vector3 connectionPoint)
            {
                Points = points;
                ConnectionPoint = connectionPoint;
            }
        }

        /// <summary>
        /// Gets a list with all line caps.
        /// </summary>
        /// <returns>A list that holds all line caps.</returns>
        public static List<LineCap> GetLineCaps()
        {
            return Enum.GetValues(typeof(LineCap)).Cast<LineCap>().ToList();
        }

        /// <summary>
        /// Gets the calculated shape for the given line cap kind.
        /// </summary>
        /// <param name="capKind">The line cap kind.</param>
        /// <param name="lineConf">The line configuration on which the cap is based.</param>
        /// <param name="position">Whether the cap belongs to the start or end of the line.</param>
        /// <returns>The calculated line cap shape.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown if the given <paramref name="capKind"/> is not supported.
        /// </exception>
        public static LineCapShape GetShape(LineCap capKind, LineConf lineConf, LineCapPosition position)
        {
            return capKind switch
            {
                LineCap.Arrowhead => Arrowhead(lineConf, position),
                _ => throw new ArgumentOutOfRangeException(nameof(capKind), capKind, "Unsupported line cap kind.")
            };
        }

        /// <summary>
        /// Calculates the local drawable points and transform data for the given line cap
        /// on the specified <paramref name="line"/>.
        /// </summary>
        /// <param name="capKind">The kind of line cap to calculate.</param>
        /// <param name="line">The line GameObject to which the cap belongs.</param>
        /// <param name="position">Whether the cap is calculated for the start or end of the line.</param>
        /// <param name="anchor">
        /// The anchor position of the cap in the local space of the parent line.
        /// </param>
        /// <param name="angleInDegrees">
        /// The rotation angle of the cap in degrees.
        /// </param>
        /// <returns>
        /// The local drawable points of the requested line cap.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="line"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown if the line configuration cannot be determined, if the line has too few positions,
        /// or if the selected line segment has zero length.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown if the given <paramref name="capKind"/> is not supported.
        /// </exception>
        public static Vector3[] CalculatePoints(LineCap capKind, GameObject line, LineCapPosition position,
            out Vector3 anchor, out float angleInDegrees)
        {
            if (line == null)
            {
                throw new ArgumentNullException(nameof(line));
            }

            LineConf lineConf = LineConf.GetLine(line);
            if (lineConf == null || lineConf.RendererPositions == null || lineConf.RendererPositions.Length < 2)
            {
                throw new ArgumentException("The line must contain at least two positions.", nameof(line));
            }

            Vector3 segmentStart;
            Vector3 segmentEnd;

            if (position == LineCapPosition.Start)
            {
                segmentStart = lineConf.RendererPositions[0];
                segmentEnd = lineConf.RendererPositions[1];
                anchor = segmentStart;
            }
            else
            {
                segmentStart = lineConf.RendererPositions[lineConf.RendererPositions.Length - 2];
                segmentEnd = lineConf.RendererPositions[lineConf.RendererPositions.Length - 1];
                anchor = segmentEnd;
            }

            Vector3 direction = position == LineCapPosition.Start
                ? segmentStart - segmentEnd
                : segmentEnd - segmentStart;

            if (direction.sqrMagnitude <= 0.000001f)
            {
                throw new ArgumentException("The selected line segment must not have zero length.", nameof(line));
            }

            angleInDegrees = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            LineCapShape shape = GetShape(capKind, lineConf, position);

            return shape.Points;
        }

        /// <summary>
        /// Calculates the canonical local geometry of a closed arrowhead.
        /// The tip of the arrowhead is located at the origin and the arrowhead
        /// points along the positive x-axis.
        /// </summary>
        /// <param name="line">The line configuration containing the visual settings.</param>
        /// <param name="position">
        /// Specifies whether the arrowhead is calculated for the start or end of the line.
        /// The selected segment is only used to determine the maximum allowed arrow size.
        /// </param>
        /// <returns>
        /// A <see cref="LineCapShape"/> containing the local arrowhead points and the local connection point.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="line"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown if the line does not contain enough positions to determine a segment
        /// or if the selected line segment has zero length.
        /// </exception>
        public static LineCapShape Arrowhead(LineConf line, LineCapPosition position)
        {
            if (line == null)
            {
                throw new ArgumentNullException(nameof(line));
            }

            if (line.RendererPositions == null || line.RendererPositions.Length < 2)
            {
                throw new ArgumentException("The line must contain at least two positions.", nameof(line));
            }

            Vector3 segmentStart;
            Vector3 segmentEnd;

            switch (position)
            {
                case LineCapPosition.Start:
                    {
                        segmentStart = line.RendererPositions[0];
                        segmentEnd = line.RendererPositions[1];
                        break;
                    }
                case LineCapPosition.End:
                    {
                        segmentStart = line.RendererPositions[line.RendererPositions.Length - 2];
                        segmentEnd = line.RendererPositions[line.RendererPositions.Length - 1];
                        break;
                    }
                default:
                    {
                        throw new ArgumentOutOfRangeException(nameof(position), position, null);
                    }
            }

            float segmentLength = Vector3.Distance(segmentStart, segmentEnd);
            if (segmentLength <= 0.0001f)
            {
                throw new ArgumentException("The selected line segment must not have zero length.", nameof(line));
            }

            float defaultLength = Mathf.Max(line.Thickness * 6.0f, 0.01f);
            float maxLength = segmentLength * 0.75f;
            float length = Mathf.Min(defaultLength, maxLength);
            float width = length;

            Vector3 tip = Vector3.zero;
            Vector3 left = new Vector3(-length, width / 2.0f, 0.0f);
            Vector3 right = new Vector3(-length, -width / 2.0f, 0.0f);
            Vector3 connectionPoint = new Vector3(-length, 0.0f, 0.0f);

            return new LineCapShape(
                new Vector3[] { left, tip, right, connectionPoint, left },
                connectionPoint);
        }
    }
}