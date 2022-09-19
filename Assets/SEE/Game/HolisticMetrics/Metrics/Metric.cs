using System.Collections.Generic;
using SEE.Game.HolisticMetrics.WidgetControllers;
using UnityEngine;

namespace SEE.Game.HolisticMetrics.Metrics
{
    /// <summary>
    /// This could be any holistic metric (a metric that is calculated on the entire code city, not on individual nodes)
    /// </summary>
    public abstract class Metric : MonoBehaviour
    {
        /// <summary>
        /// The WidgetController that should be attached to the same widget that this Metric is attached to. Use this to
        /// display your metric's value.
        /// </summary>
        protected WidgetController WidgetController;

        /// <summary>
        /// The ICollection of all graph elements currently present in the scene. This is managed by the class
        /// GraphElementIDMap.
        /// </summary>
        protected static ICollection<GameObject> GraphElements = GraphElementIDMap.MappingForHolisticMetrics.Values;

        /// <summary>
        /// This method calculates the metric based on the currently loaded code cities
        /// (TODO: Calculate and display separately for each city)
        /// and then displays the new value on the Widget property using the Display() method in the widget's
        /// controller.
        /// </summary>
        internal abstract void Refresh();

        /// <summary>
        /// When this Metric is started, it will register with the CanvasController so that the CanvasController knows
        /// this instance and can call its Refresh() method. Also it will retrieve a reference to the WidgetController
        /// that should be attached to the same GameObject this Metric is attached to.
        /// </summary>
        public void Start()
        {
            // Try to get the CanvasController from the parent GameObject.
            CanvasController canvasController = transform.parent.GetComponent<CanvasController>();
            if (canvasController is null)
            {
                // If getting the CanvasController from the parent GameObject did not succeed, that probably means that
                // the widget has not been attached to the MetricsCanvas as a child.
                Debug.LogWarning("Widgets should be added to the scene as children of the MetricsCanvas.");
                
                // Now we try to find the CanvasController by searching the entire scene for the MetricsCanvas.
                canvasController = GameObject.Find("enemy").GetComponent<CanvasController>();
            }
            canvasController.Register(this);
            WidgetController = transform.GetComponent<WidgetController>();
        }
    }
}