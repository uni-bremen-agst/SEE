using SEE.Game.HolisticMetrics.Metrics;
using SEE.Game.HolisticMetrics.WidgetControllers;
using SEE.Utils;
using UnityEngine;

namespace SEE.Game.HolisticMetrics.ActionHelpers
{
    /// <summary>
    /// This component can be attached to a widget. It will listen for left clicks on the widget and when it notices a
    /// left click, it will delete that widget.
    /// </summary>
    public class WidgetDeleter : MonoBehaviour
    {
        /// <summary>
        /// Whether the user has clicked on the widget with the mouse.
        /// </summary>
        private bool hasDeletion;

        /// <summary>
        /// When the mouse is clicked on the widget and then it is released again, we will delete the widget on which
        /// the cursor was clicked and then delete all WidgetDeleter instances.
        /// </summary>
        private void OnMouseUp()
        {
            // TODO: We need an interaction for VR, too.
            if (!Raycasting.IsMouseOverGUI())
            {
                hasDeletion = true;
            }
        }

        /// <summary>
        /// Fetches the player's selection of which widget to delete.
        /// </summary>
        /// <param name="config">The config of the widget to delete.</param>
        /// <returns>The value of <see cref="hasDeletion"/>.</returns>
        internal bool GetDeletion(out WidgetConfig config)
        {
            config = ConfigManager.GetWidgetConfig(GetComponent<WidgetController>(), GetComponent<Metric>());
            return hasDeletion;
        }
    }
}
