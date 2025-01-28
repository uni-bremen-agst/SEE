using SEE.Game.City;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.GameObjects
{
    /// <summary>
    /// This component provides a map that assigns each key (tableID) to the respective game object,
    /// on which an <see cref="AbstractSEECity"/> component can be attached.
    /// </summary>
    public class CitiesHolder : MonoBehaviour
    {
        /// <summary>
        /// The map of all tables (keys) and their associated cities
        /// (the game objects assigned to <see cref="AbstractSEECity"/>).
        /// </summary>
        public readonly Dictionary<string, GameObject> Cities;

        /// <summary>
        /// The constructor.
        /// Creates a new map for the table, city pairs.
        /// </summary>
        public CitiesHolder() => Cities = new();

        /// <summary>
        /// Finds the city game object corresponding to the associated <paramref name="tableID"/>.
        /// </summary>
        /// <param name="tableID">The table ID</param>
        /// <returns>The city game object found.</returns>
        /// <exception cref="KeyNotFoundException">If the <paramref name="tableID"/> could not be found"/></exception>
        public GameObject Find(string tableID)
        {
            return Cities[tableID];
        }

        /// <summary>
        /// Checks if a city with the given <paramref name="name"/> already exists.
        /// Case sensitivity is not relevant.
        /// </summary>
        /// <param name="name">The name to be checked.</param>
        /// <returns>True, if already a city with that name exists, otherwise false.</returns>
        public bool IsNameAlreadyUsed(string name)
        {
            foreach(GameObject city in Cities.Values)
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
