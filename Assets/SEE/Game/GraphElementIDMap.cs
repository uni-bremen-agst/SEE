using SEE.GO;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace SEE.Game
{
    /// <summary>
    /// A mapping of IDs (name) of GameObjects representing nodes or edges onto those GameObjects.
    /// </summary>
    internal static class GraphElementIDMap
    {
        /// <summary>
        /// Mapping of IDs (name) of GameObject representing nodes or edges
        /// onto those GameObjects.
        ///
        /// Invariant: mapping[gameObject.name] = gameObject
        /// </summary>
        private static readonly IDictionary<string, GameObject> mapping = new Dictionary<string, GameObject>();

#if UNITY_EDITOR
        [UnityEditor.InitializeOnEnterPlayMode]
        /// <summary>
        /// Resets the mapping. As a consequence, all game objects  representing nodes or edges
        /// must re-added to the mapping when the game is being started.
        ///
        /// We always want to start with a clean mapping when entering play mode in
        /// the Unity editor. Otherwise, the mapping could contain game objects from the last
        /// game session which are no longer valid.
        ///
        /// This method is called when entering play mode in the Unity editor. When the game
        /// is run from a build, this method does not need to be called, because the mapping
        /// would be initialized from scratch anyway due to the static initialization of
        /// <see cref="mapping"/>.
        /// </summary>
        private static void ResetMapping()
        {
            mapping.Clear();
        }
#endif

        /// <summary>
        /// Returns the game object with the given <paramref name="id"/> or null if there is
        /// no such game object if <paramref name="mustFindElement"/> is false.
        /// If <paramref name="mustFindElement"/> is true and no corresponding graph element
        /// can be found, <see cref="KeyNotFoundException"/> is thrown.
        /// </summary>
        /// <param name="id">The ID of the game object to be looked up.</param>
        /// <param name="mustFindElement">If true, an exception will be thrown if the element could not be found.
        /// Otherwise, <c>null</c> will be returned.</param>
        /// <returns>The game object with the given <paramref name="id"/> or null if there is
        /// no such game object.</returns>
        /// <exception cref="KeyNotFoundException">Thrown if <paramref name="mustFindElement"/> is
        /// true and a graph element with the given <paramref name="id"/> cannot be found.</exception>
        internal static GameObject Find(string id, bool mustFindElement = false)
        {
            Assert.IsFalse(string.IsNullOrEmpty(id));
            if (mapping.TryGetValue(id, out GameObject result))
            {
                Assert.IsNotNull(result, $"Null value for {id}");
                return result;
            }
            else if (mustFindElement)
            {
                throw new KeyNotFoundException($"Element with ID '{id}' was not found.");
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Returns whether there is a game object with the given <paramref name="id"/>
        /// in this map.
        /// </summary>
        /// <param name="id">The ID of the game object to be looked up.</param>
        /// <returns>Whether there is a game object with the given <paramref name="id"/> in this map.</returns>
        internal static bool Has(string id)
        {
            Assert.IsFalse(string.IsNullOrEmpty(id));
            return mapping.ContainsKey(id);
        }

        /// <summary>
        /// Emits the <paramref name="id"/> and all current entries of the map.
        /// </summary>
        /// <remarks>Used for debugging when <paramref name="id"/> cannot be found in the map.</remarks>
        /// <param name="id">The ID not found in the map.</param>
        private static void DumpError(string id)
        {
            Debug.LogError($"ID {id} not found in {nameof(GraphElementIDMap)}.\n");
            if (mapping.Count == 0)
            {
                Debug.LogError($"{nameof(GraphElementIDMap)} is empty.\n");
            }
            else
            {
                Dump();
            }
        }

        /// <summary>
        /// Dumps the content of this map into the console.
        /// </summary>
        internal static void Dump()
        {
            foreach (var entry in mapping)
            {
                if (entry.Value != null)
                {
                    Debug.Log($"  {entry.Key} => {entry.Value.name}\n");
                }
                else
                {
                    Debug.LogError($"  {entry.Key} => NULL (The GameObject has been destroyed but you are still trying to access it.)\n");
                }
            }
        }

        /// <summary>
        /// Adds <paramref name="gameObject"/> to the mapping where <paramref name="gameObject.name"/>
        /// is used as key.
        ///
        /// Assumption: <paramref name="gameObject"/> represents a graph node or edge.
        /// </summary>
        /// <param name="gameObject">Game object to be added.</param>
        /// <exception cref="ArgumentException">Thrown if there is already a game object with
        /// this ID (name attribute).</exception>
        internal static void Add(GameObject gameObject)
        {
            Assert.IsNotNull(gameObject);
            Assert.IsFalse(string.IsNullOrEmpty(gameObject.name));
            Assert.IsTrue(gameObject.IsNode() || gameObject.IsEdge());
            mapping.Add(gameObject.name, gameObject);
        }

        /// <summary>
        /// Adds all <paramref name="gameObjects"/> to the mapping using
        /// <see cref="Add(GameObject)"/>.
        /// </summary>
        /// <param name="gameObjects">Game objects to be added.</param>
        internal static void Add(IEnumerable<GameObject> gameObjects)
        {
            foreach (GameObject gameEdge in gameObjects)
            {
                Add(gameEdge);
            }
        }

        /// <summary>
        /// Adds <paramref name="gameObject"/> to the mapping where <paramref name="gameObject.name"/>
        /// is used as key, removing any game object present in the mapping under the same name.
        ///
        /// Assumption: <paramref name="gameObject"/> represents a graph node or edge.
        /// </summary>
        /// <param name="gameObject">Game object to be added.</param>
        internal static void Update(GameObject gameObject)
        {
            Remove(gameObject);
            Add(gameObject);
        }

        /// <summary>
        /// Adds all <paramref name="gameObjects"/> to the mapping, removing those which were already present,
        /// using <see cref="Update(GameObject)"/>.
        /// </summary>
        /// <param name="gameObjects">Game objects to be updated.</param>
        internal static void Update(IEnumerable<GameObject> gameObjects)
        {
            foreach (GameObject gameObject in gameObjects)
            {
                Update(gameObject);
            }
        }

        /// <summary>
        /// Removes <paramref name="gameObject"/> from the mapping.
        ///
        /// If there is no such <paramref name="gameObject"/> in the mapping, nothing
        /// happens.
        /// </summary>
        /// <param name="gameObject">Game object to be removed.</param>
        internal static void Remove(GameObject gameObject)
        {
            Assert.IsNotNull(gameObject);
            Assert.IsFalse(string.IsNullOrEmpty(gameObject.name));
            Assert.IsTrue(gameObject.IsNode() || gameObject.IsEdge());
            mapping.Remove(gameObject.name);
        }

        /// <summary>
        /// Clears the content of this map.
        /// </summary>
        internal static void Clear()
        {
            mapping.Clear();
        }
    }
}
