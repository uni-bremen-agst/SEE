using System;
using System.Linq;
using SEE.Game.City;
using UnityEngine;

namespace SEE.Game.HolisticMetrics.Metrics
{
    /// <summary>
    /// This could be any holistic metric (a metric that is calculated on the entire code city, not on individual
    /// nodes). If you want to implement a new metric, just implement this class. Then the new metric will automatically
    /// be available for adding to a board in the holistic metrics menu in the game. If you need help implementing this
    /// class, you could, for example, refer to the NumberOfNodeTypes metric - that is pretty much as simple as it gets.
    /// </summary>
    internal abstract class Metric : MonoBehaviour
    {
        /// <summary>
        /// If you want to implement a new metric, simply implement this method in the new class. This method will be
        /// called to retrieve the value you want to display, so just do whatever calculations you need to do and then
        /// return the value as a MetricValue. If you want to display a single value, use the class <see cref="MetricValueRange"/>,
        /// if you want to display multiple values (for example on a bar chart), use <see cref="MetricValueCollection"/>.
        /// </summary>
        /// <param name="city">The code city for which the metric should be calculated.</param>
        /// <returns>The calculated metric value</returns>
        internal virtual MetricValue Refresh(AbstractSEECity city)
        {
            if (city == null)
            {
                throw new ArgumentNullException(nameof(city));
            }
            if (city.LoadedGraph == null)
            {
                throw new Exception($"The city {city.name} does not have a loaded graph.");
            }
            return null;
        }

        /// <summary>
        /// Returns all available <see cref="Metric"/> implementations.
        /// </summary>
        /// <returns>The types of all available <see cref="Metric"/> implementations.</returns>
        internal static Type[] GetTypes()
        {
            return AppDomain
                .CurrentDomain
                .GetAssemblies()
                .SelectMany(domainAssembly => domainAssembly.GetTypes())
                .Where(type => type.IsSubclassOf(typeof(Metric)))
                .ToArray();
        }
    }
}
