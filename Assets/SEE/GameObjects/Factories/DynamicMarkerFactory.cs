using System;
using SEE.Game;
using SEE.Utils;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.GO.Factories
{
    /// <summary>
    /// A factory for dynamic markers.
    /// </summary>
    /// <remarks>These will be used for marking edit conflicts in a branch city, for instance.
    /// They are used by HighlightEffect.</remarks>
    internal static class DynamicMarkerFactory
    {
        /// <summary>
        /// Name of the file containing the prefab for dynamic markers.
        /// </summary>
        private const string dynamicMarkerPrefabFile = "Prefabs/DynamicMarker";

        /// <summary>
        /// A mapping of game objects representing a code city onto a prefab for all
        /// dynamic markers in this code city.
        /// Each code city has its own prefab because code cities have different portals.
        /// </summary>
        private static readonly Dictionary<GameObject, GameObject> dynamicMarkerPrefabs = new();

        /// <summary>
        /// Returns a dynamic marker that can be used in the code city the given <paramref name="gameObject"/>
        /// belongs to. The portal of the marker will be the one of the code city.
        /// </summary>
        /// <param name="gameObject">The game object for which to obtain a dynamic marker.</param>
        /// <returns>Dynamic marker.</returns>
        /// <exception cref="Exception">Thrown if <paramref name="gameObject"/> is not contained
        /// in a code city or if the marker's prefab cannot be loaded.</exception>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="gameObject"/> is null.</exception>
        public static GameObject GetMarker(GameObject gameObject)
        {
            if (gameObject == null)
            {
                throw new ArgumentNullException(nameof(gameObject));
            }
            GameObject codeCity = gameObject.GetCodeCity();
            if (codeCity == null)
            {
                throw new Exception($"{gameObject.name} is not contained in a code city.");
            }
            if (dynamicMarkerPrefabs.TryGetValue(codeCity, out GameObject dynamicMarkerPrefab))
            {
                return dynamicMarkerPrefab;
            }
            else
            {
                // Instantiation per code city. Each code has its own portal.
                dynamicMarkerPrefab = PrefabInstantiator.LoadPrefab(dynamicMarkerPrefabFile);
                if (dynamicMarkerPrefab == null)
                {
                    throw new Exception($"Cannot load prefab from file {dynamicMarkerPrefabFile}\n");
                }
                else
                {
                    // We do not want to change the prefab file if we set the portal.
                    dynamicMarkerPrefab.hideFlags = HideFlags.DontSave;
                    dynamicMarkerPrefab.name = "DynamicMarker_" + codeCity.name;
                    dynamicMarkerPrefab.tag = Tags.Decoration;

                    Portal.GetDimensions(codeCity, out Vector2 leftFrontCorner, out Vector2 rightBackCorner);
                    Portal.SetPortal(dynamicMarkerPrefab, leftFrontCorner, rightBackCorner);
                    // Portal.SetInfinitePortal(dynamicMarkerPrefab);

                    dynamicMarkerPrefabs[codeCity] = dynamicMarkerPrefab;
                    return dynamicMarkerPrefab;
                }
            }
        }

        /// <summary>
        /// Removes the marker prefab corresponding to <paramref name="codeCity"/>
        /// and destroys it.
        /// </summary>
        /// <param name="codeCity">The code city for which to remove the prefab.</param>
        public static void DestroyMarkerPrefab(GameObject codeCity)
        {
            if (dynamicMarkerPrefabs != null && dynamicMarkerPrefabs.TryGetValue(codeCity, out GameObject prefab))
            {
                dynamicMarkerPrefabs.Remove(codeCity);
                Destroyer.Destroy(prefab);
            }
        }

#if UNITY_EDITOR
        /// <summary>
        /// Destroys all entries in <see cref="dynamicMarkerPrefabs"/> and clears it. This might be needed
        /// if a user has created dynamic markers in the Unity Editor, in which case the initially loaded
        /// prefab is still lurking around.
        /// </summary>
        /// <remarks>Called by Unity. Runs right when play mode is entered.</remarks>
        [UnityEditor.InitializeOnEnterPlayMode]
        private static void ClearDynamicMarkerPrefabs()
        {
            Debug.Log($"Removing {dynamicMarkerPrefabs.Count} dynamic marker prefabs.\n");
            foreach (GameObject prefab in dynamicMarkerPrefabs.Values)
            {
                Destroyer.Destroy(prefab);
            }
            dynamicMarkerPrefabs.Clear();
        }
#endif
    }
}
