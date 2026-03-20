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