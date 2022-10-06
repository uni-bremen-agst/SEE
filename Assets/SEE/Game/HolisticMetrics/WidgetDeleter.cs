using UnityEngine;

namespace SEE.Game.HolisticMetrics
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
                    transform.parent.GetComponent<BoardController>().DeleteWidget(gameObject);
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