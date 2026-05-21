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
        /// <b>IMPORTANT:</b> The given <paramref name="gameObject"/> will only be destroyed AFTER the current
        /// frame of Unity is finished. This means that comparing this <paramref name="gameObject"/> to
        /// null in the same frame will still return false until the frame is over.
        /// Use `await UniTask.WaitForEndOfFrame();` if you want to wait for the destruction to take effect.
        ///
        /// Note: This method will recurse into the children of <paramref name="gameObject"/>
        /// if <paramref name="recurseIntoChildren"/> is true.
        /// The <paramref name="gameObject"/> is removed from <see cref="GraphElementIDMap"/>
        /// if it represents a node or edge.
        /// </summary>
        /// <param name="gameObject">Game object to be destroyed (generally, but not necessarily
        /// representing a node or edge).</param>
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
                        Destroy(child, recurseIntoChildren);
                    }
                }
                /// Make sure that we do not run into an endless recursion. We really
                /// want to call <see cref="Destroy(Object)"></see> and not
                /// <see cref="Destroy(GameObject, bool)"/>.
                Destroy(gameObject as Object);
            }
        }

        /// <summary>
        /// Destroys given <paramref name="object"/> using <see cref="UnityEngine.Object.Destroy(Object)"/>
        /// when in play mode or <see cref="UnityEngine.Object.DestroyImmediate(Object)"/> when in editor mode.
        /// </summary>
        /// <param name="object">Object to be destroyed.</param>
        public static void Destroy(Object @object)
        {
            if (@object != null)
            {
                // We must use DestroyImmediate when we are in the editor mode.
                if (Application.isPlaying)
                {
                    // playing either in a built player or in the player of the editor
                    Object.Destroy(@object);
                }
                else
                {
                    // game is not played; we are in the editor mode
                    Object.DestroyImmediate(@object);
                }
            }
        }

        /// <summary>
        /// If <paramref name="gameObject"/> has a component of type <typeparamref name="T"/>,
        /// this component will be destroyed. If there is no such component, nothing happens.
        /// </summary>
        /// <typeparam name="T">type of component to be destroyed</typeparam>
        /// <param name="gameObject">Game object whose component is to be destroyed.</param>
        public static void Destroy<T>(this GameObject gameObject) where T : Component
        {
            if (gameObject.TryGetComponent(out T component))
            {
                Destroy(component);
            }
        }
    }
}
