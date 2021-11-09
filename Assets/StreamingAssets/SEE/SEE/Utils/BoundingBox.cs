using SEE.GO;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.Utils
{
    /// <summary>
    /// Calculation of bounding boxes containing game objects.
    /// </summary>
    public static class BoundingBox
    {
        /// <summary>
        /// Returns the bounding box (2D rectangle) enclosing all given <paramref name="gameObjects"/>
        /// in terms of world space.
        /// 
        /// Precondition: All <paramref name="gameObjects"/> have a renderer component attached to them.
        /// </summary>
        /// <param name="gameObjects">the list of objects that are enclosed in the resulting bounding box</param>
        /// <param name="leftLowerCorner">the left lower front corner (x axis in 3D space) of the bounding box</param>
        /// <param name="rightUpperCorner">the right lower back corner (z axis in 3D space) of the bounding box</param>
        public static void Get(ICollection<GameObject> gameObjects, out Vector2 leftLowerCorner, out Vector2 rightUpperCorner)
        {
            if (gameObjects.Count == 0)
            {
                leftLowerCorner = Vector2.zero;
                rightUpperCorner = Vector2.zero;
            }
            else
            {
                leftLowerCorner = new Vector2(Mathf.Infinity, Mathf.Infinity);
                rightUpperCorner = new Vector2(Mathf.NegativeInfinity, Mathf.NegativeInfinity);

                foreach (GameObject go in gameObjects)
                {
                    Vector3 extent = go.WorldSpaceScale() / 2.0f;
                    // Note: position denotes the center of the object
                    Vector3 position = go.transform.position;
                    {
                        // x co-ordinate of lower left corner
                        float x = position.x - extent.x;
                        if (x < leftLowerCorner.x)
                        {
                            leftLowerCorner.x = x;
                        }
                    }
                    {
                        // z co-ordinate of lower left corner
                        float z = position.z - extent.z;
                        if (z < leftLowerCorner.y)
                        {
                            leftLowerCorner.y = z;
                        }
                    }
                    {   // x co-ordinate of upper right corner
                        float x = position.x + extent.x;
                        if (x > rightUpperCorner.x)
                        {
                            rightUpperCorner.x = x;
                        }
                    }
                    {
                        // z co-ordinate of upper right corner
                        float z = position.z + extent.z;
                        if (z > rightUpperCorner.y)
                        {
                            rightUpperCorner.y = z;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Returns the maximal y co-ordinate of all given <paramref name="gameObjects"/> in world space.
        /// </summary>
        /// <param name="gameObjects">the game objects whose maximal y co-ordinate is requested</param>
        /// <returns>maximal y co-ordinate</returns>
        public static float GetRoof(ICollection<GameObject> gameObjects)
        {
            float result = float.NegativeInfinity;
            foreach (GameObject gameObject in gameObjects)
            {
                if (gameObject.transform.position.y > result)
                {
                    result = gameObject.transform.position.y + gameObject.WorldSpaceScale().y / 2.0f;
                }
            }
            return result;
        }
    }
}