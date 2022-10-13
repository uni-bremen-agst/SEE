using SEE.Net.Actions.HolisticMetrics;
using UnityEngine;

namespace SEE.Game.HolisticMetrics.Components
{
    public class WidgetAdder : MonoBehaviour
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
                    string boardName = GetComponent<WidgetsManager>().GetTitle();
                    new CreateWidgetNetAction(boardName, widgetConfiguration).Execute();
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
