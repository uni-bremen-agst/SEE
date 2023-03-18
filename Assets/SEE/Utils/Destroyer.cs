using SEE.Game;
using SEE.GO;
using UnityEngine;

namespace SEE.Utils
{
    /// <summary>
    /// Functions to destroy game objects in game or editor mode.
    /// They will also maintain the consistency of <see cref="GraphElementIDMap"/>.
    /// </summary>
    public static class Destroyer
    {
        /// <summary>
        /// Destroys given <paramref name="gameObject"/> using <see cref="UnityEngine.Object.Destroy(Object)"/>
        /// when in play mode (effective only after the current Update loop of Unity) or
        /// <see cref="UnityEngine.Object.DestroyImmediate(Object)"/> when in editor mode
        /// (with immediate effect).
        ///
        /// Note: This method will recurse into the children of <paramref name="gameObject"/>
        /// if <paramref name="recurseIntoChildren"/> is <c>true</c>.
        /// The <paramref name="gameObject"/> is removed from <see cref="GraphElementIDMap"/>
        /// if it represents a node or edge.
        /// </summary>
        /// <param name="gameObject">game object to be destroyed (generally, but not necessarily
        /// representing a node or edge)</param>
        /// <param name="recurseIntoChildren">Whether to also destroy the <paramref name="gameObject"/>'s
        /// children.</param>
        public static void Destroy(GameObject gameObject, bool recurseIntoChildren = true)
        {
            if (gameObject != null)
            {
                if (gameObject.IsNode() || gameObject.IsEdge())
                {
                    GraphElementIDMap.Remove(gameObject);
                }
                if (recurseIntoChildren)
                {
                    // We cannot traverse the children and destroy them at the same time.
                    // We first need to collect all children.
                    GameObject[] allChildren = new GameObject[gameObject.transform.childCount];
                    int i = 0;
                    foreach (Transform child in gameObject.transform)
                    {
                        allChildren[i++] = child.gameObject;
                    }

                    // Now we can destroy the children.
                    foreach (GameObject child in allChildren)
                    {
                        Destroy(child.gameObject);
                    }
                }
                // We must use DestroyImmediate when we are in the editor mode.
                if (Application.isPlaying)
                {
                    // playing either in a built player or in the player of the editor
                    Object.Destroy(gameObject);
                }
                else
                {
                    // game is not played; we are in the editor mode
                    Object.DestroyImmediate(gameObject);
                }
            }
        }

        /// <summary>
        /// Destroys given <paramref name="component"/> using <see cref="UnityEngine.Object.Destroy(Object)"/>
        /// when in play mode or <see cref="UnityEngine.Object.DestroyImmediate(Object)"/> when in editor mode.
        /// </summary>
        /// <param name="component">component to be destroyed</param>
        public static void Destroy(Component component)
        {
            if (component != null)
            {
                // We must use DestroyImmediate when we are in the editor mode.
                if (Application.isPlaying)
                {
                    // playing either in a built player or in the player of the editor
                    Object.Destroy(component);
                }
                else
                {
                    // game is not played; we are in the editor mode
                    Object.DestroyImmediate(component);
                }
            }
        }
    }
}