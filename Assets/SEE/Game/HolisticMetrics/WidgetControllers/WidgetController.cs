using UnityEngine;

namespace SEE.Game.HolisticMetrics.WidgetControllers
{
    public abstract class WidgetController : MonoBehaviour
    {
        public abstract void Display(MetricValue metricValue);
    }
}