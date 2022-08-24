using SEE.GO;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace SEE.Game
{
    /// <summary>
    /// A mapping of IDs (name) of GameObjects representing nodes or edges
    /// onto those GameObjects.
    /// </summary>
    static class GraphElementIDMap
    {
        /// <summary>
        /// Mapping of IDs (name) of GameObject representing nodes or edges
        /// onto those GameObjects.
        ///
        /// Invariant: mapping[gameObject.name] = gameObject
        /// </summary>
        private static IDictionary<string, GameObject> mapping = new Dictionary<string, GameObject>();

        /// <summary>
        /// Returns the game object with the given <paramref name="ID"/> or null if there is
        /// no such game object.
        /// </summary>
        /// <param name="ID">the ID of the game object to be looked up</param>
        /// <returns>the game object with the given <paramref name="ID"/> or null if there is
        /// no such game object</returns>
        public static GameObject Find(string ID)
        {
            Assert.IsFalse(string.IsNullOrEmpty(ID));
            if (mapping.TryGetValue(ID, out GameObject result))
            {
                Assert.IsNotNull(result, $"Null value for {ID}");
                return result;
            }
            else
            {
                DumpError(ID);
                return null;
            }
        }

        private static void DumpError(string ID)
        {
            Debug.LogError($"ID {ID} not found in {nameof(GraphElementIDMap)}.\n");
            if (mapping.Count == 0)
            {
                Debug.LogError($"{nameof(GraphElementIDMap)} is empty.\n");
            }
            else
            {
                foreach (var entry in mapping)
                {
                    Debug.Log($"  {entry.Key} => {entry.Value.name}\n");
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
        public static void Add(GameObject gameObject)
        {
            Assert.IsNotNull(gameObject);
            Assert.IsFalse(string.IsNullOrEmpty(gameObject.name));
            Assert.IsTrue(gameObject.HasNodeRef() || gameObject.HasEdgeRef());
            mapping.Add(gameObject.name, gameObject);
            Debug.Log($"{nameof(GraphElementIDMap)}.Add({gameObject.name}).\n");
        }

        /// <summary>
        /// Removes <paramref name="gameObject"/> from the mapping.
        ///
        /// If there is no such <paramref name="gameObject"/> in the mapping, nothing
        /// happens.
        /// </summary>
        /// <param name="gameObject">game object to be removed</param>
        public static void Remove(GameObject gameObject)
        {
            Assert.IsNotNull(gameObject);
            Assert.IsFalse(string.IsNullOrEmpty(gameObject.name));
            Assert.IsTrue(gameObject.HasNodeRef() || gameObject.HasEdgeRef());
            mapping.Remove(gameObject.name);
            Debug.Log($"{nameof(GraphElementIDMap)}.Remove({gameObject.name}).\n");
        }
    }
}
