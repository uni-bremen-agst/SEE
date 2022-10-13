using System;
using UnityEngine;

namespace SEE.Game.HolisticMetrics.WidgetControllers
{
    internal abstract class WidgetController : MonoBehaviour
    {
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
        
        internal abstract void Display(MetricValue metricValue);
    }
}
