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
        /// This method calculates the metric based on the currently loaded code cities
        /// (TODO: Calculate and display separately for each city)
        /// and then displays the new value on the Widget property using some method implemented in the widget's Script.
        /// </summary>
        internal abstract void Refresh();

        /// <summary>
        /// When this Metric is started, it will register with the CanvasController so that the CanvasController knows
        /// this instance and can call its Refresh() method. Also it will retrieve a reference to the WidgetController
        /// that should be attached to the same GameObject this Metric is attached to.
        /// </summary>
        public void Start()
        {
            transform.parent.GetComponent<CanvasController>().Register(this);
            WidgetController = transform.GetComponent<WidgetController>();
        }
    }
}