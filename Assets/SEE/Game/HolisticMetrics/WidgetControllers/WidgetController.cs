using System;
using UnityEngine;

namespace SEE.Game.HolisticMetrics.WidgetControllers
{
    internal abstract class WidgetController : MonoBehaviour
    {
        internal Guid ID;
        
        internal abstract void Display(MetricValue metricValue);
    }
}
