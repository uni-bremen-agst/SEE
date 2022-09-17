using SEE.GO;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Sirenix.Utilities;
using UnityEngine;
using UnityEngine.Assertions;

namespace SEE.Game
{
    /// <summary>
    /// A mapping of IDs (name) of GameObjects representing nodes or edges
    /// onto those GameObjects.
    /// </summary>
    internal static class GraphElementIDMap
    {
        /// <summary>
        /// Mapping of IDs (name) of GameObject representing nodes or edges
        /// onto those GameObjects.
        ///
        /// Invariant: mapping[gameObject.name] = gameObject
        /// </summary>
        internal static IDictionary<string, GameObject> mapping = new Dictionary<string, GameObject>();

        /// <summary>
        /// Returns the game object with the given <paramref name="ID"/> or null if there is
        /// no such game object.
        /// </summary>
        /// <param name="ID">the ID of the game object to be looked up</param>
        /// <returns>the game object with the given <paramref name="ID"/> or null if there is
        /// no such game object</returns>
        internal static GameObject Find(string ID)
        {
            Assert.IsFalse(string.IsNullOrEmpty(ID));
            if (mapping.TryGetValue(ID, out GameObject result))
            {
                Assert.IsNotNull(result, $"Null value for {ID}");
                return result;
            }
            else
            {
                //DumpError(ID);
                return null;
            }
        }

        /// <summary>
        /// Emits the <paramref name="ID"/> and all current entries of the map.
        /// </summary>
        /// <remarks>Used for debugging when <paramref name="ID"/> cannot be found in the map.</remarks>
        /// <param name="ID">the ID not found in the map</param>
        private static void DumpError(string ID)
        {
            Debug.LogError($"ID {ID} not found in {nameof(GraphElementIDMap)}.\n");
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
        /// <param name="gameObject">game object to be added</param>
        /// <exception cref="ArgumentException">thrown if there is already a game object with
        /// this ID (name attribute)</exception>
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
        /// <param name="gameObjects">game objects to be added</param>
        internal static void Add(IEnumerable<GameObject> gameObjects)
        {
            foreach (GameObject gameEdge in gameObjects)
            {
                Add(gameEdge);
            }
        }

        /// <summary>
        /// Removes <paramref name="gameObject"/> from the mapping.
        ///
        /// If there is no such <paramref name="gameObject"/> in the mapping, nothing
        /// happens.
        /// </summary>
        /// <param name="gameObject">game object to be removed</param>
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
