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
        /// Converts a Vector3 array to a Vector2 array.
        /// </summary>
        /// <param name="vector3">The Vector3 array to converts.</param>
        /// <returns></returns>
        private static Vector2[] castToVector2Array(Vector3[] vector3)
        {
            Vector2[] vector2 = new Vector2[vector3.Length];
            for (int i = 0; i < vector3.Length; i++)
            {
                vector2[i] = new Vector2(vector3[i].x, vector3[i].y);
            }
            return vector2;
        }

        /// <summary>
        /// This method calculates the nearest points of a Vector3 array from a given point
        /// </summary>
        /// <param name="positions">The Vector3 array that holds the positions</param>
        /// <param name="hitPoint">The point from which the nearest points should be determinded.</param>
        /// <returns>all the nearest (same) points from the array</returns>
        public static List<int> GetNearestIndexes(Vector3[] positions, Vector3 hitPoint)
        {
            List<int> matchedIndexes = new();
            Vector2[] vector2 = castToVector2Array(positions);
            Vector2 hitPoint2D = new Vector2(hitPoint.x, hitPoint.y);
            float nearestDistance = float.MaxValue;
            for (int i = 0; i < vector2.Length; i++)
            {
                if (Vector2.Distance(vector2[i], hitPoint2D) < nearestDistance)
                {
                    nearestDistance = Vector2.Distance(vector2[i], hitPoint2D);
                    matchedIndexes = new List<int>();
                    matchedIndexes.Add(i);
                }
                else if (Vector2.Distance(vector2[i], hitPoint2D) == nearestDistance)
                {
                    matchedIndexes.Add(i);
                }
            }
            return matchedIndexes;
        }
    }
}