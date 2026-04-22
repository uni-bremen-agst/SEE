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
        #region Types
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
            /// A combined interface symbol consisting of required followed by provided.
            /// Visually: ----( o-
            /// </summary>
            RequiredProvided,

            /// <summary>
            /// A combined interface symbol consisting of provided followed by required.
            /// Visually: ---o )-
            /// </summary>
            ProvidedRequired,

            /// <summary>
            /// Represents a UML reference preset.
            /// This option is intended for shape creation and is internally resolved
            /// to a dashed main line with an arrow-shaped line cap.
            /// It should not be treated as an independent geometric line cap.
            /// </summary>
            Reference
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
        #endregion

        #region Public API
        /// <summary>
        /// Gets a list with all line caps.
        /// </summary>
        /// <returns>A list that holds all line caps.</returns>
        public static List<LineCap> GetAllLineCaps()
        {
            return Enum.GetValues(typeof(LineCap)).Cast<LineCap>().ToList();
        }

        /// <summary>
        /// Gets a list with all editiable line caps.
        /// </summary>
        /// <returns>A list that holds all editable lien caps.</returns>
        public static List<LineCap> GetEditableLineCaps()
        {
            return GetAllLineCaps().Where(cap => cap != LineCap.Reference).ToList();
        }

        /// <summary>
        /// Gets the shape or shapes for the given line cap.
        /// </summary>
        /// <param name="capKind">The line cap kind.</param>
        /// <param name="lineConf">The line configuration.</param>
        /// <param name="position">Whether the cap belongs to the start or end of the line.</param>
        /// <returns>The calculated shape or shapes.</returns>
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
        /// Checks whether the given line cap can be calculated.
        /// </summary>
        /// <param name="line">The line configuration.</param>
        /// <param name="position">Whether the cap belongs to the start or end of the line.</param>
        /// <returns>True if the cap can be calculated, false otherwise.</returns>
        public static bool CanCalculate(LineConf line, LineCapPosition position)
        {
            if (!TryGetSegment(line, position, out Vector3 segmentStart, out Vector3 segmentEnd))
            {
                return false;
            }

            return Vector3.Distance(segmentStart, segmentEnd) > 0.0001f;
        }
        #endregion

        #region Segment Helpers
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
        /// Gets the validated segment of the given line for the requested cap position.
        /// </summary>
        /// <param name="line">The line configuration.</param>
        /// <param name="position">Whether the cap belongs to the start or end of the line.</param>
        /// <param name="segmentStart">The validated segment start point.</param>
        /// <param name="segmentEnd">The validated segment end point.</param>
        /// <returns>The length of the validated segment.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="line"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown if the line does not contain enough positions or if the selected segment has zero length.
        /// </exception>
        private static float GetValidatedSegment(LineConf line, LineCapPosition position,
            out Vector3 segmentStart, out Vector3 segmentEnd)
        {
            if (line == null)
            {
                throw new ArgumentNullException(nameof(line));
            }

            if (!TryGetSegment(line, position, out segmentStart, out segmentEnd))
            {
                throw new ArgumentException("The line must contain at least two positions.", nameof(line));
            }

            float segmentLength = Vector3.Distance(segmentStart, segmentEnd);
            if (segmentLength <= 0.0001f)
            {
                throw new ArgumentException("The selected line segment must not have zero length.", nameof(line));
            }

            return segmentLength;
        }
        #endregion

        #region Size Helpers
        /// <summary>
        /// Calculates the size of a closed arrowhead.
        /// </summary>
        /// <param name="line">The line configuration.</param>
        /// <param name="segmentLength">The length of the selected segment.</param>
        /// <returns>The calculated cap size.</returns>
        private static float GetArrowheadSize(LineConf line, float segmentLength)
        {
            float defaultLength = Mathf.Max(line.Thickness * 6.0f, 0.01f);
            float maxLength = segmentLength * 0.75f;
            return Mathf.Min(defaultLength, maxLength);
        }

        /// <summary>
        /// Calculates the size of an open arrow.
        /// The default size is the same as for a closed arrowhead, but the arrow is reduced
        /// dynamically for short lines.
        /// </summary>
        /// <param name="line">The line configuration.</param>
        /// <param name="segmentLength">The length of the selected segment.</param>
        /// <returns>The calculated cap size.</returns>
        private static float GetArrowSize(LineConf line, float segmentLength)
        {
            float defaultLength = Mathf.Max(line.Thickness * 6.0f, 0.01f);
            float maxLength = segmentLength * 0.45f;
            return Mathf.Min(defaultLength, maxLength);
        }

        /// <summary>
        /// Calculates the size of a diamond-shaped line cap.
        /// </summary>
        /// <param name="line">The line configuration.</param>
        /// <param name="segmentLength">The length of the selected segment.</param>
        /// <returns>The calculated half-size of the diamond.</returns>
        private static float GetDiamondSize(LineConf line, float segmentLength)
        {
            float defaultLength = Mathf.Max(line.Thickness * 8.0f, 0.02f);
            float maxLength = segmentLength * 0.35f;

            return Mathf.Min(defaultLength, maxLength);
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
        /// Calculates the shared layout values for combined interface line caps.
        /// </summary>
        /// <param name="line">The line configuration.</param>
        /// <param name="position">Whether the cap belongs to the start or end of the line.</param>
        /// <param name="radius">The calculated interface radius.</param>
        /// <param name="gap">The gap between the interface elements.</param>
        /// <param name="connectorLength">The connector length.</param>
        private static void GetCombinedInterfaceLayout(LineConf line, LineCapPosition position,
            out float radius, out float gap, out float connectorLength)
        {
            float segmentLength = GetValidatedSegment(line, position, out _, out _);

            radius = GetInterfRadius(line, segmentLength);
            gap = radius * 0.35f;
            connectorLength = radius * 0.9f;
        }
        #endregion

        #region Basic Cap Shapes
        /// <summary>
        /// Calculates the local shape of a closed arrowhead.
        /// </summary>
        /// <param name="line">The line configuration.</param>
        /// <param name="position">Whether the cap belongs to the start or end of the line.</param>
        /// <returns>The calculated arrowhead shape.</returns>
        private static LineCapShape Arrowhead(LineConf line, LineCapPosition position)
        {
            float segmentLength = GetValidatedSegment(line, position, out _, out _);

            float size = GetArrowheadSize(line, segmentLength);

            Vector3 tip = Vector3.zero;
            Vector3 left = new(-size, size / 2.0f, 0.0f);
            Vector3 right = new(-size, -size / 2.0f, 0.0f);
            Vector3 connectionPoint = new(-size, 0.0f, 0.0f);

            return new LineCapShape(
                new[] { left, tip, right, connectionPoint, left },
                connectionPoint);
        }

        /// <summary>
        /// Calculates the local shape of an open arrow.
        /// </summary>
        /// <param name="line">The line configuration.</param>
        /// <param name="position">Whether the cap belongs to the start or end of the line.</param>
        /// <returns>The calculated arrow shape.</returns>
        private static LineCapShape Arrow(LineConf line, LineCapPosition position)
        {
            float segmentLength = GetValidatedSegment(line, position, out _, out _);

            float size = GetArrowSize(line, segmentLength);

            Vector3 tip = Vector3.zero;
            Vector3 left = new(-size, size / 2.0f, 0.0f);
            Vector3 right = new(-size, -size / 2.0f, 0.0f);

            return new LineCapShape(
                new[] { left, tip, right },
                Vector3.zero);
        }

        /// <summary>
        /// Calculates the local shape of a diamond line cap.
        /// </summary>
        /// <param name="line">The line configuration.</param>
        /// <param name="position">Whether the cap belongs to the start or end of the line.</param>
        /// <returns>The calculated diamond shape.</returns>
        private static LineCapShape Diamond(LineConf line, LineCapPosition position)
        {
            float segmentLength = GetValidatedSegment(line, position, out _, out _);

            float size = GetDiamondSize(line, segmentLength);

            Vector3 tip = Vector3.zero;
            Vector3 top = new(-size, size / 2.0f, 0.0f);
            Vector3 connectionPoint = new(-2.0f * size, 0.0f, 0.0f);
            Vector3 bottom = new(-size, -size / 2.0f, 0.0f);

            return new LineCapShape(
                new[] { tip, top, connectionPoint, bottom, tip },
                connectionPoint);
        }

        /// <summary>
        /// Calculates the local shape of a provided interface symbol.
        /// </summary>
        /// <param name="line">The line configuration.</param>
        /// <param name="position">Whether the cap belongs to the start or end of the line.</param>
        /// <returns>The calculated provided-interface shape.</returns>
        private static LineCapShape Ball(LineConf line, LineCapPosition position)
        {
            float segmentLength = GetValidatedSegment(line, position, out _, out _);

            float radius = GetInterfRadius(line, segmentLength);

            Vector3 center = new(-radius, 0.0f, 0.0f);
            Vector3 connectionPoint = new(-2.0f * radius, 0.0f, 0.0f);

            Vector3[] circle = ShapePointsCalculator.Circle(center, radius);

            return new LineCapShape(circle, connectionPoint);
        }

        /// <summary>
        /// Calculates the local shape of a required interface symbol using the default orientation.
        /// </summary>
        /// <param name="line">The line configuration.</param>
        /// <param name="position">Whether the cap belongs to the start or end of the line.</param>
        /// <returns>The calculated required-interface shape.</returns>
        private static LineCapShape Socket(LineConf line, LineCapPosition position)
        {
            return Socket(line, position, ShapePointsCalculator.Orientation.Left);
        }

        /// <summary>
        /// Calculates the local shape of a required interface symbol with the given orientation.
        /// </summary>
        /// <param name="line">The line configuration.</param>
        /// <param name="position">Whether the cap belongs to the start or end of the line.</param>
        /// <param name="orientation">The socket orientation.</param>
        /// <returns>The calculated required-interface shape.</returns>
        private static LineCapShape Socket(LineConf line, LineCapPosition position, ShapePointsCalculator.Orientation orientation)
        {
            float segmentLength = GetValidatedSegment(line, position, out _, out _);

            float radius = GetInterfRadius(line, segmentLength);

            Vector3 center = Vector3.zero;
            Vector3 connectionPoint = new(-radius, 0.0f, 0.0f);

            if (orientation == ShapePointsCalculator.Orientation.Right)
            {
                connectionPoint = new Vector3(radius, 0.0f, 0.0f);
            }

            Vector3[] halfCircle = ShapePointsCalculator.HalfCircle(center, radius, orientation);

            return new LineCapShape(halfCircle, connectionPoint);
        }
        #endregion

        #region Comined Interface Caps
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
        /// Calculates the combined required-provided interface cap.
        /// Visually: ----( o-
        /// </summary>
        /// <param name="line">The line configuration.</param>
        /// <param name="position">Whether the cap belongs to the start or end of the line.</param>
        /// <returns>The calculated combined interface shapes.</returns>
        private static List<LineCapShape> RequiredProvided(LineConf line, LineCapPosition position)
        {
            GetCombinedInterfaceLayout(line, position, out _, out float gap, out float connectorLength);

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
        /// Calculates the combined provided-required interface cap.
        /// Visually: ---o )-
        /// </summary>
        /// <param name="line">The line configuration.</param>
        /// <param name="position">Whether the cap belongs to the start or end of the line.</param>
        /// <returns>The calculated combined interface shapes.</returns>
        private static List<LineCapShape> ProvidedRequired(LineConf line, LineCapPosition position)
        {
            GetCombinedInterfaceLayout(line, position, out _, out float gap, out float connectorLength);

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
        #endregion

        #region Geometry Helpers
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
        #endregion
    }
}
