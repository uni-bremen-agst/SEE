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
            /// <summary>
            /// No line cap.
            /// </summary>
            None,

            /// <summary>
            /// A closed arrowhead.
            /// </summary>
            Arrowhead,

            /// <summary>
            /// An open arrow.
            /// </summary>
            Arrow,

            /// <summary>
            /// A hollow diamond.
            /// </summary>
            Aggregation,

            /// <summary>
            /// A filled diamond.
            /// </summary>
            Composition,

            /// <summary>
            /// A provided interface symbol (ball / lollipop).
            /// </summary>
            Provided,

            /// <summary>
            /// A required interface symbol (socket / half circle).
            /// </summary>
            Required,

            /// <summary>
            /// A combined interface symbol consisting of ball followed by socket.
            /// </summary>
            RequiredProvided,

            /// <summary>
            /// A combined interface symbol consisting of socket followed by ball.
            /// </summary>
            ProvidedRequired
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
        /// Gets the calculated shape or shapes for the given line cap kind.
        /// Most line caps consist of a single shape, while combined interface caps
        /// may consist of multiple shapes.
        /// </summary>
        /// <param name="capKind">The line cap kind.</param>
        /// <param name="lineConf">The line configuration on which the cap is based.</param>
        /// <param name="position">Whether the cap belongs to the start or end of the line.</param>
        /// <returns>A list containing the calculated shape or shapes of the requested line cap.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown if the given <paramref name="capKind"/> is not supported.
        /// </exception>
        public static List<LineCapShape> GetShapes(LineCap capKind, LineConf lineConf, LineCapPosition position)
        {
            return capKind switch
            {
                LineCap.Arrowhead => new List<LineCapShape> { Arrowhead(lineConf, position) },
                LineCap.Arrow => new List<LineCapShape> { Arrow(lineConf, position) },
                LineCap.Aggregation => new List<LineCapShape> { Diamond(lineConf, position) },
                LineCap.Composition => new List<LineCapShape> { Diamond(lineConf, position) },
                LineCap.Provided => new List<LineCapShape> { Ball(lineConf, position) },
                LineCap.Required => new List<LineCapShape> { Socket(lineConf, position) },
                LineCap.RequiredProvided => RequiredProvided(lineConf, position),
                LineCap.ProvidedRequired => ProvidedRequired(lineConf, position),
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
            Vector3 left = new(-length, width / 2.0f, 0.0f);
            Vector3 right = new(-length, -width / 2.0f, 0.0f);
            Vector3 connectionPoint = new(-length, 0.0f, 0.0f);

            return new LineCapShape(
                new[] { left, tip, right, connectionPoint, left },
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
            Vector3 left = new(-length, width / 2.0f, 0.0f);
            Vector3 right = new(-length, -width / 2.0f, 0.0f);

            // For an open arrow, the main line should connect directly to the tip.
            Vector3 connectionPoint = Vector3.zero;

            return new LineCapShape(
                new[] { left, tip, right },
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
            Vector3 top = new(-length, width / 2.0f, 0.0f);
            Vector3 connectionPoint = new(-2.0f * length, 0.0f, 0.0f);
            Vector3 bottom = new(-length, -width / 2.0f, 0.0f);

            return new LineCapShape(
                new[] { tip, top, connectionPoint, bottom, tip },
                connectionPoint);
        }

        /// <summary>
        /// Calculates the radius of an interface cap.
        /// </summary>
        /// <param name="line">The line configuration.</param>
        /// <param name="segmentLength">The length of the selected segment.</param>
        /// <returns>The radius of the interface cap.</returns>
        private static float GetInterfRadius(LineConf line, float segmentLength)
        {
            float defaultRadius = Mathf.Max(line.Thickness * 3.0f, 0.01f);
            float maxRadius = segmentLength * 0.15f;
            return Mathf.Min(defaultRadius, maxRadius);
        }

        /// <summary>
        /// Calculates the canonical local geometry of a provided interface symbol.
        /// The symbol is represented as a circle ("ball").
        /// </summary>
        /// <param name="line">The line configuration containing the visual settings.</param>
        /// <param name="position">
        /// Specifies whether the symbol is calculated for the start or end of the line.
        /// The selected segment is only used to determine the maximum allowed size.
        /// </param>
        /// <returns>
        /// A <see cref="LineCapShape"/> containing the local circle points and the local connection point.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="line"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown if the line does not contain enough positions to determine a segment
        /// or if the selected line segment has zero length.
        /// </exception>
        public static LineCapShape Ball(LineConf line, LineCapPosition position)
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

            float radius = GetInterfRadius(line, segmentLength);

            Vector3 center = new(-radius, 0.0f, 0.0f);
            Vector3 connectionPoint = new(-2.0f * radius, 0.0f, 0.0f);

            Vector3[] circle = ShapePointsCalculator.Circle(center, radius);

            return new LineCapShape(circle, connectionPoint);
        }

        /// <summary>
        /// Calculates the canonical local geometry of a required interface symbol.
        /// The symbol is represented as an open half circle ("socket").
        /// </summary>
        /// <param name="line">The line configuration containing the visual settings.</param>
        /// <param name="position">
        /// Specifies whether the symbol is calculated for the start or end of the line.
        /// The selected segment is only used to determine the maximum allowed size.
        /// </param>
        /// <returns>
        /// A <see cref="LineCapShape"/> containing the local half-circle points and the local connection point.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="line"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown if the line does not contain enough positions to determine a segment
        /// or if the selected line segment has zero length.
        /// </exception>
        public static LineCapShape Socket(LineConf line, LineCapPosition position)
        {
            return Socket(line, position, ShapePointsCalculator.Orientation.Left);
        }

        /// <summary>
        /// Calculates the canonical local geometry of a required interface symbol
        /// with the given orientation.
        /// </summary>
        /// <param name="line">The line configuration containing the visual settings.</param>
        /// <param name="position">
        /// Specifies whether the symbol is calculated for the start or end of the line.
        /// The selected segment is only used to determine the maximum allowed size.
        /// </param>
        /// <param name="orientation">The orientation of the socket.</param>
        /// <returns>
        /// A <see cref="LineCapShape"/> containing the local half-circle points and the local connection point.
        /// </returns>
        private static LineCapShape Socket(LineConf line, LineCapPosition position, ShapePointsCalculator.Orientation orientation)
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

            float radius = GetInterfRadius(line, segmentLength);

            Vector3 center = Vector3.zero;
            Vector3 connectionPoint = new Vector3(-radius, 0.0f, 0.0f);

            if (orientation == ShapePointsCalculator.Orientation.Right)
            {
                connectionPoint = new Vector3(radius, 0.0f, 0.0f);
            }

            Vector3[] halfCircle = ShapePointsCalculator.HalfCircle(center, radius, orientation);

            return new LineCapShape(halfCircle, connectionPoint);
        }

        /// <summary>
        /// Creates a short horizontal connector line in local cap space.
        /// </summary>
        /// <param name="fromX">The x-coordinate of the start point.</param>
        /// <param name="toX">The x-coordinate of the end point.</param>
        /// <returns>A <see cref="LineCapShape"/> representing the connector line.</returns>
        private static LineCapShape InterfConnector(float fromX, float toX)
        {
            Vector3[] points =
            {
                new Vector3(fromX, 0.0f, 0.0f),
                new Vector3(toX, 0.0f, 0.0f)
            };

            return new LineCapShape(points, new Vector3(fromX, 0.0f, 0.0f));
        }

        /// <summary>
        /// Calculates a combined interface symbol consisting of a socket, a ball,
        /// and a short connector line on the right side of the ball.
        /// The symbol has the form: ----( o-
        ///
        /// The connector ends at x = 0. This ensures that the complete line cap
        /// does not extend beyond the original line end.
        /// </summary>
        /// <param name="line">The line configuration containing the visual settings.</param>
        /// <param name="position">Whether the symbol belongs to the start or end of the line.</param>
        /// <returns>A list of <see cref="LineCapShape"/> objects representing the combined symbol.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="line"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown if the line does not contain enough positions or if the selected segment has zero length.
        /// </exception>
        private static List<LineCapShape> RequiredProvided(LineConf line, LineCapPosition position)
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

            float radius = GetInterfRadius(line, segmentLength);
            float gap = radius * 0.35f;
            float connectorLength = radius * 0.9f;

            LineCapShape connector = InterfConnector(-connectorLength, 0.0f);

            LineCapShape ball = Ball(line, position);
            ball = OffsetShape(ball, new Vector3(-connectorLength, 0.0f, 0.0f));

            LineCapShape socket = Socket(line, position, ShapePointsCalculator.Orientation.Left);
            float socketOffsetX = GetMinX(ball) - gap - GetMaxX(socket);
            socket = OffsetShape(socket, new Vector3(socketOffsetX, 0.0f, 0.0f));

            return new List<LineCapShape>
            {
                socket,
                ball,
                connector
            };
        }

        /// <summary>
        /// Calculates a reversed combined interface symbol consisting of a ball,
        /// a socket, and a short connector line on the right side of the socket.
        /// The symbol has the form: ---o )-
        ///
        /// The connector ends at x = 0. This ensures that the complete line cap
        /// does not extend beyond the original line end.
        /// </summary>
        /// <param name="line">The line configuration containing the visual settings.</param>
        /// <param name="position">Whether the symbol belongs to the start or end of the line.</param>
        /// <returns>A list of <see cref="LineCapShape"/> objects representing the reversed combined symbol.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="line"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown if the line does not contain enough positions or if the selected segment has zero length.
        /// </exception>
        private static List<LineCapShape> ProvidedRequired(LineConf line, LineCapPosition position)
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

            float radius = GetInterfRadius(line, segmentLength);
            float gap = radius * 0.35f;
            float connectorLength = radius * 0.9f;

            LineCapShape connector = InterfConnector(-connectorLength, 0.0f);

            LineCapShape socket = Socket(line, position, ShapePointsCalculator.Orientation.Right);
            float socketOffsetX = -connectorLength - GetMaxX(socket);
            socket = OffsetShape(socket, new Vector3(socketOffsetX, 0.0f, 0.0f));

            LineCapShape ball = Ball(line, position);
            float ballOffsetX = GetMinX(socket) - gap - GetMaxX(ball);
            ball = OffsetShape(ball, new Vector3(ballOffsetX, 0.0f, 0.0f));

            return new List<LineCapShape>
            {
                ball,
                socket,
                connector
            };
        }

        /// <summary>
        /// Creates a translated copy of the given line-cap shape.
        /// </summary>
        /// <param name="shape">The original line-cap shape.</param>
        /// <param name="offset">The offset to apply in local cap space.</param>
        /// <returns>A translated copy of <paramref name="shape"/>.</returns>
        private static LineCapShape OffsetShape(LineCapShape shape, Vector3 offset)
        {
            Vector3[] points = shape.Points.Select(point => point + offset).ToArray();
            Vector3 connectionPoint = shape.ConnectionPoint + offset;

            return new LineCapShape(points, connectionPoint);
        }

        /// <summary>
        /// Returns the minimum x-coordinate of the given line-cap shape.
        /// </summary>
        /// <param name="shape">The line-cap shape.</param>
        /// <returns>The minimum x-coordinate.</returns>
        private static float GetMinX(LineCapShape shape)
        {
            return shape.Points.Min(point => point.x);
        }

        /// <summary>
        /// Returns the maximum x-coordinate of the given line-cap shape.
        /// </summary>
        /// <param name="shape">The line-cap shape.</param>
        /// <returns>The maximum x-coordinate.</returns>
        private static float GetMaxX(LineCapShape shape)
        {
            return shape.Points.Max(point => point.x);
        }
    }
}
