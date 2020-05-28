using UnityEngine;

namespace SEE.Utils
{
    /// <summary>
    /// Functions to destroy GameObjects.
    /// </summary>
    public class Destroyer
    {
        /// <summary>
        /// Destroys given game object using UnityEngine.Object when in
        /// game mode or UnityEngine.Object.DestroyImmediate when in editor mode.
        /// </summary>
        /// <param name="gameObject"></param>
        public static void DestroyGameObject(GameObject gameObject)
        {
            // We must use DestroyImmediate when we are in the editor mode.
            if (Application.isPlaying)
            {
                // playing either in a built player or in the player of the editor
                UnityEngine.Object.Destroy(gameObject);
            }
            else
            {
                // game is not played; we are in the editor mode
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        /// <summary>
        /// Destroys given <paramref name="component"/> using UnityEngine.Object when in
        /// game mode or UnityEngine.Object.DestroyImmediate when in editor mode.
        /// </summary>
        /// <param name="component"></param>
        public static void DestroyComponent(Component component)
        {
            // We must use DestroyImmediate when we are in the editor mode.
            if (Application.isPlaying)
            {
                // playing either in a built player or in the player of the editor
                UnityEngine.Object.Destroy(component);
            }
            else
            {
                // game is not played; we are in the editor mode
                UnityEngine.Object.DestroyImmediate(component);
            }
        }
    }
}