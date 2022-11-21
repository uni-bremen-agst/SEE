using System;
using SEE.Game.HolisticMetrics.Components;
using UnityEngine;

namespace SEE.Game.HolisticMetrics.WidgetControllers
{
    /// <summary>
    /// For every holistic metrics widget prefab, there needs to be a WidgetController that is attached to it. It takes
    /// care of displaying values on that widget and has an ID that identifies that widget.
    /// </summary>
    internal abstract class WidgetController : MonoBehaviour
    {
        private WidgetMover mover;

        /// <summary>
        /// The field that saves the ID of this widget.
        /// </summary>
        private Guid? id;
        
        /// <summary>
        /// The unique ID of this widget. It can be set once, then it is fixed.
        /// </summary>
        internal Guid ID
        {
            get => id.GetValueOrDefault();
            set => id ??= value;  // In C# 9 this could be replaced by "init;".
        }
        
        /// <summary>
        /// Calling this method will display the given MetricValue on the widget that the WidgetController is attached
        /// to.
        /// </summary>
        /// <param name="metricValue">The MetricValue to display</param>
        internal abstract void Display(MetricValue metricValue);

        /// <summary>
        /// Toggles that this widget can be moved.
        /// </summary>
        /// <param name="enable">Whether the moving should be enabled or disabled</param>
        internal void ToggleMoving(bool enable)
        {
            if (enable)
            {
                mover = gameObject.AddComponent<WidgetMover>();
            }
            else
            {
                Destroy(mover);
            }
        }

        /// <summary>
        /// Destroys all child GameObjects of a given Transform.
        /// </summary>
        /// <param name="parent">The transform whose children are to be destroyed</param>
        protected static void DestroyChildren(Transform parent)
        {
            for (int i = parent.childCount -1; i >= 0; i--)
            {
                Destroy(parent.GetChild(i).gameObject);
            }
        }
    }
}
