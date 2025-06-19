using System;
using UnityEngine;

namespace SEE.Utils
{
    /// <summary>
    /// Utility for calculating line bounds given a <see cref="LineRenderer"/>.
    /// </summary>
    public class GeometryUtils
    {
        /// <summary>
        /// Calculates the axis-aligned bounding box (AABB) of a <see cref="LineRenderer"/>,
        /// either in local space or world space, based on the <paramref name="worldSpace"/> parameter.
        /// </summary>
        /// <param name="lr">The <see cref="LineRenderer"/> component whose bounds are to be calculated.</param>
        /// <param name="worldSpace">
        /// If true, calculates bounds in world space.
        /// If false, calculates bounds in the <see cref="LineRenderer"/>'s local space.
        /// </param>
        /// <returns>
        /// The calculated <see cref="Bounds"/> that tightly encapsulates all points of the <see cref="LineRenderer"/>
        /// in the requested coordinate space.
        /// </returns>
        /// <remarks>
        /// This method retrieves the positions from the <see cref="LineRenderer"/> and, if necessary,
        /// transforms them between local and world space based on the <see cref="LineRenderer"/>'s
        /// <see cref="LineRenderer.useWorldSpace"/> property and the <paramref name="worldSpace"/> argument.
        /// <list type="bullet">
        /// <item>
        /// If <see cref="LineRenderer.useWorldSpace"/> matches <paramref name="worldSpace"/>,
        /// positions are converted to local space using <see cref="Transform.InverseTransformPoint"/>.
        /// </item><item>
        /// Otherwise, positions are used as-is.
        /// </item>
        /// </list>
        /// The resulting bounds are a tight fit around the actual line geometry, unlike the built-in
        /// <see cref="Renderer.bounds"/> which may be overly conservative for LineRenderers.
        /// </remarks>
        public static Bounds CalculateLineBounds(LineRenderer lr, bool worldSpace)
        {
            if (lr.positionCount == 0)
            {
                return new Bounds();
            }

            Vector3[] positions = new Vector3[lr.positionCount];
            lr.GetPositions(positions);
            bool convertSpace = lr.useWorldSpace != worldSpace;

            Func<Vector3, Vector3> convert = worldSpace ? lr.transform.TransformPoint : lr.transform.InverseTransformPoint;

            Vector3 firstPoint = convertSpace ? convert(positions[0]) : positions[0];
            Bounds bounds = new(firstPoint, Vector3.zero);

            for (int i = 1; i < positions.Length; i++)
            {
                bounds.Encapsulate(convertSpace ? convert(positions[i]) : positions[i]);
            }
            return bounds;
        }
    }
}
