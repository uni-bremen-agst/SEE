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
        public readonly List<(GameObject city, GameObject table)> Cities;

        /// <summary>
        /// The constructor.
        /// Creates an new list for the cities, table pairs.
        /// </summary>
        CitiesHolder() => Cities = new();

        /// <summary>
        /// Finds the city game object corresponding to the associated <paramref name="tableID"/>.
        /// </summary>
        /// <param name="tableID">The table ID</param>
        /// <returns>The city game object, it found, otherwise null.</returns>
        public GameObject Find(string tableID)
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

        /// <summary>
        /// Checks if a city with the given <paramref name="name"/> already exists.
        /// Case sensitivity is not relevant.
        /// </summary>
        /// <param name="name">The name to be checked.</param>
        /// <returns>True, if already a city with that name exists, otherwise false.</returns>
        public bool IsNameAlreadyUsed(string name)
        {
            foreach((GameObject city, _) in Cities)
            {
                if (city.name.ToLower() == name.ToLower())
                {
                    return true;
                }
            }
            return false;
        }
    }
}
