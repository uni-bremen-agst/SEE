using UnityEngine;

namespace SEE.Utils
{
    /// <summary>
    /// Provides extensions for GameObjects retrieving their size attributes.
    /// </summary>
    public static class GameObjectSize
    {
        /// <summary>
        /// Returns the size of the given <paramref name="gameObject"/>.
        /// </summary>
        /// <param name="gameObject"></param>
        /// <returns>size of given <paramref name="gameObject"/></returns>
        public static Vector3 Size(this GameObject gameObject)
        {
            Renderer renderer = gameObject.GetComponent<Renderer>();
            return renderer.bounds.size;
        }
    }
}