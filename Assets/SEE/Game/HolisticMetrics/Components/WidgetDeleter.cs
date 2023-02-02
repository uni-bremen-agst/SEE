using SEE.Game.HolisticMetrics.Metrics;
using SEE.Game.HolisticMetrics.WidgetControllers;
using SEE.Utils;
using UnityEngine;

namespace SEE.Game.HolisticMetrics.Components
{
    /// <summary>
    /// This component can be attached to a widget. It will listen for left clicks on the widget and when it notices a
    /// left click, it will delete that widget.
    /// </summary>
    public class WidgetDeleter : MonoBehaviour
    {
        private bool hasDeletion;
        
        /// <summary>
        /// When the mouse is clicked on the widget and then it is released again, we will delete the widget on which
        /// the cursor was clicked and then delete all WidgetDeleter instances.
        /// </summary>
        private void OnMouseUp()
        {
            if (!Raycasting.IsMouseOverGUI())
            {
                hasDeletion = true;
            }
        }

        internal bool GetDeletion(out WidgetConfig config)
        {
            config = ConfigManager.GetWidgetConfig(GetComponent<WidgetController>(), GetComponent<Metric>());
            return hasDeletion;
        }
    }
}
