using System.Collections.Generic;
using UnityEngine;

namespace SEE.Game.HolisticMetrics.Metrics
{
    /// <summary>
    /// This could be any holistic metric (a metric that is calculated on the entire code city, not on individual
    /// nodes). If you want to implement a new metric, just implement this class. Then the new metric will automatically
    /// be available for adding to a board in the holistic metrics menu in the game.
    /// </summary>
    internal abstract class Metric : MonoBehaviour
    {
        /// <summary>
        /// The ICollection of all graph elements currently present in the scene. This is managed by the class
        /// GraphElementIDMap. When this class is being implemented, this field does not need to be used, but it might
        /// come in handy.
        /// </summary>
        protected static readonly ICollection<GameObject> GraphElements = 
            GraphElementIDMap.MappingForHolisticMetrics.Values;

        /// <summary>
        /// TODO: Calculate for a specified code city, not for all code cities. Would probably require a new parameter.
        /// If you want to implement a new metric, simply implement this method in the new class. This method will be
        /// called to retrieve the value you want to display, so just do whatever calculations you need to do and then
        /// return the value as a MetricValue. If you want to display a single value, use the class MetricValueRange,
        /// if you want to display multiple values (for example on a bar chart), use MetricValueCollection.
        /// </summary>
        /// <returns>The calculated metric value</returns>
        internal abstract MetricValue Refresh();
    }
}