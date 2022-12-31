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
            GameObject prefab = LoadPrefab(prefabName);
            GameObject result = UnityEngine.Object.Instantiate(prefab, parent, instantiateInWorldSpace);
            if (result == null)
            {
                throw new Exception($"Prefab {prefabName} could not be instantiated.");
            }
            return result;
        }

        /// <summary>
        /// Loads and returns the <paramref name="prefabName"/> located in the Resources folder.
        /// If it cannot be loaded, an exception is thrown.
        /// 
        /// Note: Unlike <see cref="InstantiatePrefab(string, Transform, bool)"/>, this method only
        /// loads, but does not instantiate the prefab.
        /// </summary>
        /// <param name="prefabName">the path of the prefab relative to the Resources folder</param>
        /// <returns>the loaded prefab; will never be null</returns>
        /// <exception cref="Exception">thrown if the prefab cannot be loaded</exception>
        public static GameObject LoadPrefab(string prefabName)
        {
            GameObject prefab = Resources.Load<GameObject>(prefabName);
            if (prefab == null)
            {
                throw new Exception($"Prefab {prefabName} not found.");
            }
            return prefab;
        }
    }
}
