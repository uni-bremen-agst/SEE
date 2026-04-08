using SEE.Game.Drawable.Configurations;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SEE.Game.Drawable.ActionHelpers
{
    /// <summary>
    /// Provides point calculations for line caps (decorations at the start or end of a line).
    /// Line caps are geometric shapes that are attached to a line based on its direction.
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
            Arrow,
            Aggregation,
            Composition,
            // Circle
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
            /// The drawable points of the line cap in local cap space.
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
                LineCap.Arrow => Arrow(lineConf, position),
                LineCap.Aggregation => Diamond(lineConf, position),
                LineCap.Composition => Diamond(lineConf, position),
                _ => throw new ArgumentOutOfRangeException(nameof(capKind), capKind, "Unsupported line cap kind.")
            };
        }

        /// <summary>
        /// Checks whether a line cap can be calculated for the given line and position.
        /// A line cap can only be calculated if the line has at least two positions and
        /// the relevant segment has a non-zero length.
        /// </summary>
        /// <param name="line">The line configuration.</param>
        /// <param name="position">Whether the cap belongs to the start or end of the line.</param>
        /// <returns>True if the line cap can be calculated, false otherwise.</returns>
        public static bool CanCalculate(LineConf line, LineCapPosition position)
        {
            if (!TryGetSegment(line, position, out Vector3 segmentStart, out Vector3 segmentEnd))
            {
                return false;
            }

            return Vector3.Distance(segmentStart, segmentEnd) > 0.0001f;
        }

        /// <summary>
        /// Calculates the local drawable points and transform data for the given line cap
        /// on the specified <paramref name="line"/>.
        /// If the cap cannot be calculated, an empty array is returned.
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
        /// The local drawable points of the requested line cap, or an empty array if the cap
        /// cannot be calculated.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="line"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown if the line configuration cannot be determined or if the line has too few positions.
        /// </exception>
        public static Vector3[] CalculatePoints(LineCap capKind, GameObject line, LineCapPosition position,
            out Vector3 anchor, out float angleInDegrees)
        {
            anchor = Vector3.zero;
            angleInDegrees = 0.0f;

            if (line == null)
            {
                throw new ArgumentNullException(nameof(line));
            }

            LineConf lineConf = LineConf.GetLine(line);
            if (lineConf == null || lineConf.RendererPositions == null || lineConf.RendererPositions.Length < 2)
            {
                throw new ArgumentException("The line must contain at least two positions.", nameof(line));
            }

            if (!TryGetSegment(lineConf, position, out Vector3 segmentStart, out Vector3 segmentEnd))
            {
                return Array.Empty<Vector3>();
            }

            anchor = position == LineCapPosition.Start ? segmentStart : segmentEnd;

            Vector3 direction = position == LineCapPosition.Start
                ? segmentStart - segmentEnd
                : segmentEnd - segmentStart;

            if (direction.sqrMagnitude <= 0.000001f)
            {
                return Array.Empty<Vector3>();
            }

            angleInDegrees = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            return GetShape(capKind, lineConf, position).Points;
        }

        /// <summary>
        /// Tries to get the relevant line segment for the given cap position.
        /// </summary>
        /// <param name="line">The line configuration.</param>
        /// <param name="position">Whether the cap belongs to the start or end of the line.</param>
        /// <param name="segmentStart">The start point of the selected segment.</param>
        /// <param name="segmentEnd">The end point of the selected segment.</param>
        /// <returns>True if a valid segment could be determined, false otherwise.</returns>
        private static bool TryGetSegment(LineConf line, LineCapPosition position,
            out Vector3 segmentStart, out Vector3 segmentEnd)
        {
            segmentStart = Vector3.zero;
            segmentEnd = Vector3.zero;

            if (line == null || line.RendererPositions == null || line.RendererPositions.Length < 2)
            {
                return false;
            }

            switch (position)
            {
                case LineCapPosition.Start:
                    {
                        segmentStart = line.RendererPositions[0];
                        segmentEnd = line.RendererPositions[1];
                        return true;
                    }
                case LineCapPosition.End:
                    {
                        segmentStart = line.RendererPositions[line.RendererPositions.Length - 2];
                        segmentEnd = line.RendererPositions[line.RendererPositions.Length - 1];
                        return true;
                    }
                default:
                    {
                        return false;
                    }
            }
        }

        /// <summary>
        /// Calculates the size of a closed arrowhead.
        /// </summary>
        /// <param name="line">The line configuration.</param>
        /// <param name="segmentLength">The length of the selected segment.</param>
        /// <param name="length">The calculated cap length.</param>
        /// <param name="width">The calculated cap width.</param>
        private static void GetArrowheadSize(LineConf line, float segmentLength, out float length, out float width)
        {
            float defaultLength = Mathf.Max(line.Thickness * 6.0f, 0.01f);
            float maxLength = segmentLength * 0.75f;
            length = Mathf.Min(defaultLength, maxLength);
            width = length;
        }

        /// <summary>
        /// Calculates the size of an open arrow.
        /// The default size is the same as for a closed arrowhead, but the arrow is reduced
        /// dynamically for short lines.
        /// </summary>
        /// <param name="line">The line configuration.</param>
        /// <param name="segmentLength">The length of the selected segment.</param>
        /// <param name="length">The calculated cap length.</param>
        /// <param name="width">The calculated cap width.</param>
        private static void GetArrowSize(LineConf line, float segmentLength, out float length, out float width)
        {
            float defaultLength = Mathf.Max(line.Thickness * 6.0f, 0.01f);
            float maxLength = segmentLength * 0.45f;
            length = Mathf.Min(defaultLength, maxLength);
            width = length;
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

            if (!TryGetSegment(line, position, out Vector3 segmentStart, out Vector3 segmentEnd))
            {
                throw new ArgumentException("The line must contain at least two positions.", nameof(line));
            }

            float segmentLength = Vector3.Distance(segmentStart, segmentEnd);
            if (segmentLength <= 0.0001f)
            {
                throw new ArgumentException("The selected line segment must not have zero length.", nameof(line));
            }

            GetArrowheadSize(line, segmentLength, out float length, out float width);

            Vector3 tip = Vector3.zero;
            Vector3 left = new Vector3(-length, width / 2.0f, 0.0f);
            Vector3 right = new Vector3(-length, -width / 2.0f, 0.0f);
            Vector3 connectionPoint = new Vector3(-length, 0.0f, 0.0f);

            return new LineCapShape(
                new Vector3[] { left, tip, right, connectionPoint, left },
                connectionPoint);
        }

        /// <summary>
        /// Calculates the canonical local geometry of an open arrow.
        /// The tip of the arrow is located at the origin and the arrow
        /// points along the positive x-axis.
        /// </summary>
        /// <param name="line">The line configuration containing the visual settings.</param>
        /// <param name="position">
        /// Specifies whether the arrow is calculated for the start or end of the line.
        /// The selected segment is only used to determine the maximum allowed arrow size.
        /// </param>
        /// <returns>
        /// A <see cref="LineCapShape"/> containing the local arrow points and the local connection point.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="line"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown if the line does not contain enough positions to determine a segment
        /// or if the selected line segment has zero length.
        /// </exception>
        public static LineCapShape Arrow(LineConf line, LineCapPosition position)
        {
            if (line == null)
            {
                throw new ArgumentNullException(nameof(line));
            }

            if (!TryGetSegment(line, position, out Vector3 segmentStart, out Vector3 segmentEnd))
            {
                throw new ArgumentException("The line must contain at least two positions.", nameof(line));
            }

            float segmentLength = Vector3.Distance(segmentStart, segmentEnd);
            if (segmentLength <= 0.0001f)
            {
                throw new ArgumentException("The selected line segment must not have zero length.", nameof(line));
            }

            GetArrowSize(line, segmentLength, out float length, out float width);

            Vector3 tip = Vector3.zero;
            Vector3 left = new Vector3(-length, width / 2.0f, 0.0f);
            Vector3 right = new Vector3(-length, -width / 2.0f, 0.0f);

            // For an open arrow, the main line should connect directly to the tip.
            Vector3 connectionPoint = Vector3.zero;

            return new LineCapShape(
                new Vector3[] { left, tip, right },
                connectionPoint);
        }

        /// <summary>
        /// Calculates the size of a diamond-shaped line cap.
        /// </summary>
        /// <param name="line">The line configuration.</param>
        /// <param name="segmentLength">The length of the selected segment.</param>
        /// <param name="length">The calculated half-length of the diamond.</param>
        /// <param name="width">The calculated half-width of the diamond.</param>
        private static void GetDiamondSize(LineConf line, float segmentLength, out float length, out float width)
        {
            float defaultLength = Mathf.Max(line.Thickness * 8.0f, 0.02f);
            float maxLength = segmentLength * 0.35f;

            length = Mathf.Min(defaultLength, maxLength);
            width = length;
        }

        /// <summary>
        /// Calculates the canonical local geometry of a diamond-shaped line cap.
        /// The outer tip of the diamond is located at the origin and the diamond
        /// points along the positive x-axis.
        /// </summary>
        /// <param name="line">The line configuration containing the visual settings.</param>
        /// <param name="position">
        /// Specifies whether the diamond is calculated for the start or end of the line.
        /// The selected segment is only used to determine the maximum allowed diamond size.
        /// </param>
        /// <returns>
        /// A <see cref="LineCapShape"/> containing the local diamond points and the local connection point.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="line"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown if the line does not contain enough positions to determine a segment
        /// or if the selected line segment has zero length.
        /// </exception>
        public static LineCapShape Diamond(LineConf line, LineCapPosition position)
        {
            if (line == null)
            {
                throw new ArgumentNullException(nameof(line));
            }

            if (!TryGetSegment(line, position, out Vector3 segmentStart, out Vector3 segmentEnd))
            {
                throw new ArgumentException("The line must contain at least two positions.", nameof(line));
            }

            float segmentLength = Vector3.Distance(segmentStart, segmentEnd);
            if (segmentLength <= 0.0001f)
            {
                throw new ArgumentException("The selected line segment must not have zero length.", nameof(line));
            }

            GetDiamondSize(line, segmentLength, out float length, out float width);

            Vector3 tip = Vector3.zero;
            Vector3 top = new Vector3(-length, width / 2.0f, 0.0f);
            Vector3 connectionPoint = new Vector3(-2.0f * length, 0.0f, 0.0f);
            Vector3 bottom = new Vector3(-length, -width / 2.0f, 0.0f);

            return new LineCapShape(
                new Vector3[] { tip, top, connectionPoint, bottom, tip },
                connectionPoint);
        }
    }
}
