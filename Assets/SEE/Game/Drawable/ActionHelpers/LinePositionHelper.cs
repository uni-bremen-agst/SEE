using SEE.Game.Drawable.Line;
using SEE.Game.Drawable.ValueHolders;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.Game.Drawable.ActionHelpers
{
    /// <summary>
    /// Provides helper methods for safely accessing line position data.
    /// </summary>
    /// <remarks>
    /// These helpers ensure that drawable line operations can still work
    /// even if no original line anchor data is available.
    /// In such cases, the current <see cref="LineRenderer"/> positions
    /// are used as fallback.
    /// </remarks>
    public static class LinePositionHelper
    {
        /// <summary>
        /// Returns the original line positions if available.
        /// Falls back to the current <see cref="LineRenderer"/> positions
        /// when no original position data exists.
        /// </summary>
        /// <param name="line">
        /// The line whose original positions should be retrieved.
        /// </param>
        /// <returns>
        /// The original line positions or, if unavailable,
        /// the current renderer positions.
        /// </returns>
        public static Vector3[] GetSafeOriginalPositions(GameObject line)
        {
            Vector3[] originalPositions = GameLineGeometry.GetOriginalLinePositions(line);

            if (originalPositions == null)
            {
                LineRenderer renderer = line.GetComponent<LineRenderer>();

                originalPositions = new Vector3[renderer.positionCount];
                renderer.GetPositions(originalPositions);
            }

            return originalPositions;
        }

        /// <summary>
        /// Translates indices of rendered line positions into indices of the
        /// original line geometry. For capped loops, the additional closing
        /// renderer position represents the first logical line point.
        /// </summary>
        /// <param name="line">The line containing the rendered positions.</param>
        /// <param name="rendererIndices">
        /// The indices referring to the rendered line positions.
        /// </param>
        /// <returns>
        /// The corresponding logical indices, without duplicates and
        /// sorted in ascending order.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="line"/> or
        /// <paramref name="rendererIndices"/> is null.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown if a rendered index is invalid or cannot be mapped
        /// to an original line position.
        /// </exception>
        public static List<int> ToLogicalIndices(
            GameObject line,
            IEnumerable<int> rendererIndices)
        {
            if (line == null)
            {
                throw new ArgumentNullException(nameof(line));
            }

            if (rendererIndices == null)
            {
                throw new ArgumentNullException(nameof(rendererIndices));
            }

            LineRenderer renderer = line.GetComponent<LineRenderer>();
            Vector3[] originalPositions = GetSafeOriginalPositions(line);
            LineAnchorValueHolder anchorHolder = line.GetComponent<LineAnchorValueHolder>();

            bool hasSyntheticClosingPoint =
                anchorHolder != null
                && anchorHolder.UsesExplicitLoopCapGeometry
                && originalPositions.Length > 0
                && renderer.positionCount == originalPositions.Length + 1;

            List<int> logicalIndices = new();

            foreach (int rendererIndex in rendererIndices)
            {
                if (rendererIndex < 0 || rendererIndex >= renderer.positionCount)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(rendererIndices),
                        rendererIndex,
                        "The index is outside the renderer positions.");
                }

                int logicalIndex =
                    hasSyntheticClosingPoint
                    && rendererIndex == originalPositions.Length
                        ? 0
                        : rendererIndex;

                if (logicalIndex >= originalPositions.Length)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(rendererIndices),
                        rendererIndex,
                        "The renderer point has no corresponding original line point.");
                }

                if (!logicalIndices.Contains(logicalIndex))
                {
                    logicalIndices.Add(logicalIndex);
                }
            }

            logicalIndices.Sort();
            return logicalIndices;
        }
    }
}
