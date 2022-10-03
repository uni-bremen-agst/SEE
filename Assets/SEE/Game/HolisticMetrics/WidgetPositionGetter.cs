using UnityEngine;

namespace SEE.Game.HolisticMetrics
{
    public class WidgetPositionGetter : MonoBehaviour
    {
        private WidgetConfiguration widgetConfiguration;
        
        internal void Setup(WidgetConfiguration widgetConfigurationParam)
        {
            widgetConfiguration = widgetConfigurationParam;
        }
        
        private void OnMouseUp()
        {
            if (Camera.main != null)
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    // Create the widget
                    Vector3 localPoint = transform.InverseTransformPoint(hit.point);
                    widgetConfiguration.Position = localPoint;
                    GetComponent<BoardController>().AddMetric(widgetConfiguration);
                }
            }
            Destroy(this);
        }
    }
}
