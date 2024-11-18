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
        public readonly static List<GameObject> Cities = new();

        /// <summary>
        /// Ensures that the list is reset at each start.
        /// </summary>
        private void Awake()
        {
            Cities.Clear();
        }
    }
}