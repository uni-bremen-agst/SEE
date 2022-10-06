using UnityEngine;

namespace SEE.Game.HolisticMetrics
{
    public class WidgetPositioner : MonoBehaviour
    {
        private static WidgetConfiguration widgetConfiguration;

        private static bool positioningDone;
        
        internal static void Setup(WidgetConfiguration widgetConfigurationParam)
        {
            widgetConfiguration = widgetConfigurationParam;
            positioningDone = false;
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
                    positioningDone = true;
                }
            }
        }

        private void Update()
        {
            if (positioningDone)
            {
                Destroy(this);    
            }
        }
    }
}
