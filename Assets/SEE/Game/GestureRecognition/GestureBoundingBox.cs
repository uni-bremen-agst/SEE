using System;
using UnityEngine;

namespace SEE.Game.GestureRecognition
{
    /// <summary>
    /// Class that describes the bounding box of a gesture. 
    /// </summary>
    public class GestureBoundingBox
    {
        
        public Vector3 leftTop, leftBottom, rightBottom, rightTop, center;
        public float width, height;
        
        
        /// <summary>Generates a bounding box of a given array of gesture points.</summary>
        /// <param name="vertices">The gesture points</param>
        /// <returns>The bounding box.</returns>
        public static GestureBoundingBox Get(Vector3[] vertices)
        {
            float minX = float.PositiveInfinity,
                maxX = float.NegativeInfinity,
                minZ = float.PositiveInfinity,
                maxZ = float.NegativeInfinity;
            float y = vertices[0].y;
            foreach (var v in vertices)
            {
                minX = Math.Min(minX, v.x);
                minZ = Math.Min(minZ, v.z);
                maxX = Math.Max(maxX, v.x);
                maxZ = Math.Max(maxZ, v.z);
            }

            GestureBoundingBox box = new GestureBoundingBox();
            box.leftBottom = new Vector3(minX, y, minZ);
            box.rightBottom = new Vector3(maxX, y, minZ);
            box.leftTop = new Vector3(minX, y, maxZ);
            box.rightTop = new Vector3(maxX, y, maxZ);
            box.width = box.rightBottom.x - box.leftBottom.x;
            box.height = box.leftTop.z - box.leftBottom.z;
            box.center = new Vector3(box.leftBottom.x + box.width / 2, y
                , box.leftBottom.z + box.height / 2);
            return box;
        }
    }
}