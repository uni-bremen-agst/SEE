using SEE.Game.City;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.GameObjects
{
    /// <summary>
    /// This component provides a list of all game objects to which
    /// an <see cref="AbstractSEECity"/> component can be attached.
    /// </summary>
    public class CitiesHolder : MonoBehaviour
    {
        /// The list of game objects
        public readonly static List<(GameObject city, GameObject table)> Cities = new();

        /// <summary>
        /// Finds the City game object corresponding to the associated <paramref name="tableID"/>.
        /// </summary>
        /// <param name="tableID">The table ID</param>
        /// <returns>The city game object, it found, otherwise null.</returns>
        public static GameObject Find(string tableID)
        {
            GameObject result = null;
            foreach((GameObject city, GameObject table) in Cities)
            {
                if (table.name == tableID)
                {
                    result = city;
                }
            }
            return result;
        }
    }
}