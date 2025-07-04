using Cysharp.Threading.Tasks;
using SEE.GO;
using SEE.Utils;
using System;
using UnityEngine;

namespace SEE.Game.City
{
    /// <summary>
    /// This class manages the addition of cities and their required components.
    /// </summary>
    public static class CityAdder
    {
        /// <summary>
        /// Prepares the <paramref name="cityHolder"/> object for adding a new city.
        /// The <paramref name="cityHolder"/> will be named <paramref name="cityName"/>.
        /// If an <see cref="AbstractSEECity"/> is already present, it will be removed.
        /// </summary>
        /// <param name="cityHolder">The game object to which the city should be attached.</param>
        /// <param name="cityName">The name for the city.</param>
        private static async UniTask PrepareCityHolderAsync(GameObject cityHolder, string cityName)
        {
            cityHolder.name = cityName;
            /// Delete existing <see cref="AbstractSEECity"/> component.
            if (cityHolder.TryGetComponent(out AbstractSEECity existingCity))
            {
                Destroyer.Destroy(existingCity);
                await UniTask.WaitUntil(() => existingCity == null);
            }
        }

        /// <summary>
        /// Creates and loads an initial reflexion city.
        /// </summary>
        /// <param name="cityHolder">The game object to which the city should be attached.</param>
        /// <param name="cityName">The name for the city.</param>
        public static async UniTask CreateReflexionCityAsync(GameObject cityHolder, string cityName)
        {
            await PrepareCityHolderAsync(cityHolder, cityName);
            SEEReflexionCity reflexionCity = cityHolder.AddComponent<SEEReflexionCity>();
            cityHolder.AddComponent<ReflexionVisualization>();
            cityHolder.AddComponent<EdgeMeshScheduler>();
            reflexionCity.LoadInitial(cityHolder.name);
            reflexionCity.DrawGraph();
        }
    }
}
