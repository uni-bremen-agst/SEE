using UnityEngine;

namespace SEE.Utils
{
    /// <summary>
    /// Provides extensions for GameObjects retrieving their size attributes.
    /// </summary>
    public static class GameObjectSize
    {
        /// <summary>
        /// Returns the size of the given <paramref name="gameObject"/> in world space.
        /// </summary>
        /// <param name="gameObject"></param>
        /// <returns>size of given <paramref name="gameObject"/></returns>
        public static Vector3 WorldSpaceScale(this GameObject gameObject)
        {
            return gameObject.transform.lossyScale;
        }
    }
}