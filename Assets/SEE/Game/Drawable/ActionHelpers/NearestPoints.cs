using SEE.Controls.Actions;
using SEE.Game;
using SEE.GO;
using SEE.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace SEE.Game.Drawable.ActionHelpers
{
    /// <summary>
    /// This class provides methods to calculate the nearest points.
    /// </summary>
    public static class NearestPoints
    {
        /// <summary>
        /// This method calculates the nearest points of a Vector3 array from a given point
        /// </summary>
        /// <param name="positions">The Vector3 array that holds the positions</param>
        /// <param name="hitPoint">The point from which the nearest points should be determinded.</param>
        /// <returns>all the nearest (same) points from the array</returns>
        public static List<int> GetNearestIndexes(Vector3[] positions, Vector3 hitPoint)
        {
            List<int> matchedIndexes = new();
            float nearestDistance = float.MaxValue;
            for (int i = 0; i < positions.Length; i++)
            {
                if (Vector3.Distance(positions[i], hitPoint) < nearestDistance)
                {
                    nearestDistance = Vector3.Distance(positions[i], hitPoint);
                    matchedIndexes = new List<int>();
                    matchedIndexes.Add(i);
                } else if (Vector3.Distance(positions[i], hitPoint) == nearestDistance)
                {
                    matchedIndexes.Add(i);
                }
            }
            return matchedIndexes;
        }

        /// <summary>
        /// Calculates the nearest point of a given point .
        /// </summary>
        /// <param name="node">Contains the line renderer in child on that the nearest point 
        /// of the given point should be found.</param>
        /// <param name="point">The point on that the nearest point should be found.</param>
        /// <returns></returns>
        public static Vector3 GetNearestPoint(GameObject node, Vector3 point)
        {
            LineRenderer renderer = node.GetComponentInChildren<LineRenderer>();
            Vector3[] positions = new Vector3[renderer.positionCount];
            renderer.GetPositions(positions);
            Vector3[] transformedPositions = new Vector3[positions.Length];
            Array.Copy(sourceArray: positions, destinationArray: transformedPositions, length: positions.Length);
            node.transform.TransformPoints(transformedPositions);

            List<int> indexes = NearestPoints.GetNearestIndexes(transformedPositions, point);
            return transformedPositions[indexes[0]];
        }
    }
}