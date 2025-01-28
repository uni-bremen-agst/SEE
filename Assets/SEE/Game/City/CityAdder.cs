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
        /// For this, the object is named accordingly with <paramref name="cityName"/>
        /// and if an <see cref="AbstractSEECity"/> is already present, it will be removed.
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

        #region Reflexion City
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
            FitInitalReflexionCityAsync(reflexionCity).Forget();
        }

        /// <summary>
        /// Ensures that the architecture root is always positioned on the right side
        /// and the implementation root on the left side.
        /// Additionally, the roots are scaled to a ratio of 60 (architecture)
        /// to 40 (implementation).
        /// </summary>
        /// <param name="city">The reflexion city.</param>
        /// <exception cref="ArgumentNullException">If the <paramref name="city"/> is null.</exception>
        private static async UniTask FitInitalReflexionCityAsync(SEEReflexionCity city)
        {
            if (city == null)
            {
                throw new ArgumentNullException(nameof(city));
            }
            /// Waits until the initial city is drawn.
            await UniTask.WaitUntil(() => city.gameObject.IsCodeCityDrawn());

            /// Gets the subroots of a reflexion graph.
            GameObject arch = city.ReflexionGraph.ArchitectureRoot.GameObject(true);
            GameObject impl = city.ReflexionGraph.ImplementationRoot.GameObject(true);

            /// Changes the position of the architecture and implementation roots.
            /// The result is that the architecture root is on the right side,
            /// and the implementation root is on the left side.
            if (arch.transform.position.z < impl.transform.position.z)
            {
                (impl.transform.position, arch.transform.position) = (arch.transform.position, impl.transform.position);
            }

            /// Adjusting the initial size.
            /// The architecture root should occupy approximately 60% of the table,
            /// and the implementation root 40%.
            /// FIXME(#816): Update once branch 816-layouts-for-reflexion-modeling is merged.
            float currentScale = 0.5f;
            float targetArchScale = 0.6f;
            float targetImplScale = 1 - targetArchScale;
            ApplyScale(arch, targetArchScale / currentScale);
            ApplyScale(impl, targetImplScale / currentScale);

            return;

            static void ApplyScale(GameObject obj, float factor)
            {
                Vector3 oldScale = obj.transform.localScale;
                Vector3 newScale = new(oldScale.x, oldScale.y, oldScale.z * factor);
                float diff = oldScale.z - newScale.z;
                diff = diff < 0 ? diff : -diff;
                Vector3 newPosition = obj.transform.position + new Vector3(0, 0, diff) * 1.5f;
                obj.NodeOperator().ResizeTo(newScale, newPosition, 0, reparentChildren: false);
            }
        }
        #endregion
    }
}