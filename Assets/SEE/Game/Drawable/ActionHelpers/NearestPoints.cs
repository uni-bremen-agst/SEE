using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SEE.Game.Drawable.ActionHelpers
{
    /// <summary>
    /// This class provides methods to calculate the nearest points.
    /// </summary>
    public static class NearestPoints
    {
        /// <summary>
        /// This method calculates the nearest points of a Vector3 array from a given point.
        /// </summary>
        /// <param name="positions">The Vector3 array that holds the positions.</param>
        /// <param name="hitPoint">The point from which the nearest points should be determined.</param>
        /// <returns>The indices of all nearest (same) points from the array.</returns>
        public static List<int> GetNearestIndices(Vector3[] positions, Vector3 hitPoint)
        {
            List<int> matchedIndices = new();
            float nearestDistance = float.MaxValue;
            for (int i = 0; i < positions.Length; i++)
            {
                if (Vector3.Distance(positions[i], hitPoint) < nearestDistance)
                {
                    nearestDistance = Vector3.Distance(positions[i], hitPoint);
                    matchedIndices = new List<int>
                    {
                        i
                    };
                }
                else if (Vector3.Distance(positions[i], hitPoint) == nearestDistance)
                {
                    matchedIndices.Add(i);
                }
            }
            return matchedIndices;
        }

        /// <summary>
        /// Calculates the nearest point of a given point.
        /// </summary>
        /// <param name="node">Contains the line renderer for the line on which
        /// the nearest point to <paramref name="point"/> should be found.</param>
        /// <param name="point">The point from which the nearest point on the line
        /// should be found.</param>
        /// <returns>The first nearest point that was found.</returns>
        public static Vector3 GetNearestPoint(GameObject node, Vector3 point)
        {
            LineRenderer renderer = node.GetComponentInChildren<LineRenderer>();
            Vector3[] positions = new Vector3[renderer.positionCount];
            renderer.GetPositions(positions);
            Vector3[] transformedPositions = new Vector3[positions.Length];
            Array.Copy(sourceArray: positions, destinationArray: transformedPositions,
                length: positions.Length);
            node.transform.TransformPoints(transformedPositions);

            List<int> Indices = GetNearestIndices(transformedPositions, point);
            return transformedPositions[Indices[0]];
        }

        /// <summary>
        /// Calculates the nearest points of a line to a given point.
        /// </summary>
        /// <param name="line">The line for which points are being sought.</param>
        /// <param name="point">The point for which the nearest points on the line
        /// should be found.</param>
        /// <param name="positionsList">The line positions as a list.</param>
        /// <param name="matchedIndices">The found indices asa list.</param>
        public static void GetNearestPoints(GameObject line, Vector3 point,
            out List<Vector3> positionsList, out List<int> matchedIndices)
        {
            LineRenderer lineRenderer = line.GetComponent<LineRenderer>();
            Vector3[] positions = new Vector3[lineRenderer.positionCount];
            lineRenderer.GetPositions(positions);
            positionsList = positions.ToList();

            Vector3[] transformedPositions = new Vector3[positions.Length];
            Array.Copy(sourceArray: positions, destinationArray: transformedPositions,
                length: positions.Length);
            line.transform.TransformPoints(transformedPositions);
            matchedIndices = GetNearestIndices(transformedPositions, point);
        }
    }
}