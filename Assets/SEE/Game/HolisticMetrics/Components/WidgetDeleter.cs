using SEE.Game.HolisticMetrics.WidgetControllers;
using SEE.Net.Actions.HolisticMetrics;
using UnityEngine;

namespace SEE.Game.HolisticMetrics.Components
{
    public class WidgetDeleter : MonoBehaviour
    {
        private static bool deletionDone;

        internal static void Setup()
        {
            deletionDone = false;
        }
        
        private void OnMouseUp()
        {
            if (Camera.main != null)
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out _))
                {
                    deletionDone = true;
                    new DeleteWidgetNetAction(
                        transform.parent.GetComponent<WidgetsManager>().GetTitle(), 
                        GetComponent<WidgetController>().ID)
                        .Execute();
                }
            }
        }

        private void Update()
        {
            if (deletionDone)
            {
                Destroy(this);
            }
        }
    }
}
