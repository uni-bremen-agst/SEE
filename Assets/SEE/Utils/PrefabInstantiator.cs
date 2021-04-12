using System;
using UnityEngine;

namespace SEE.Utils
{
    /// <summary>
    /// Allows to instantiate prefabs at run-time located in the Resources folder.
    /// </summary>
    public static class PrefabInstantiator
    {
        /// <summary>
        /// Instantiates and returns the <paramref name="prefabName"/> located in the Resources folder.
        /// The prefab becomes the child of <paramref name="parent"/> and is instantiated in world space
        /// if and only if <paramref name="instantiateInWorldSpace"/>.
        /// If the prefab cannot be instantiated, an exception is thrown.
        /// </summary>
        /// <param name="prefabName">the path of the prefab relative to the Resources folder</param>
        /// <param name="parent">the parent of the instantiate prefab; can be null</param>
        /// <param name="instantiateInWorldSpace">whether the prefab should be instantiated in world space</param>
        /// <returns>the instantiated prefab; will never be null</returns>
        public static GameObject InstantiatePrefab(string prefabName, Transform parent = null, bool instantiateInWorldSpace = true)
        {
            GameObject prefab = Resources.Load<GameObject>(prefabName);
            if (prefab == null)
            {
                throw new Exception($"Prefab {prefabName} not found.");
            }
            GameObject result = UnityEngine.Object.Instantiate(prefab, parent, instantiateInWorldSpace);
            if (result == null)
            {
                throw new Exception($"Prefab {prefabName} could not be instantiated.");
            }
            return result;
        }
    }
}
