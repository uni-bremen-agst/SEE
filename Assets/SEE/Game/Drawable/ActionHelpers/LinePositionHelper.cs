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
            Vector3[] originalPositions = GameDrawer.GetOriginalLinePositions(line);

            if (originalPositions == null)
            {
                LineRenderer renderer = line.GetComponent<LineRenderer>();

                originalPositions = new Vector3[renderer.positionCount];
                renderer.GetPositions(originalPositions);
            }

            return originalPositions;
        }
    }
}