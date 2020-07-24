using UnityEngine;

namespace SEE.Controls
{
    public class Gizmo : MonoBehaviour
    {
        private bool dragging = false;
        private Plane plane;

        void Update()
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Input.GetMouseButtonDown(0))
            {
                LayerMask layerMask = LayerMask.GetMask("UI");
                dragging = Physics.Raycast(ray, out RaycastHit hitInfo, Mathf.Infinity, layerMask);
                if (dragging)
                {
                    plane = new Plane(hitInfo.normal, hitInfo.point);
                }
            }
            if (Input.GetMouseButtonUp(0))
            {
                dragging = false;
            }
            if (dragging)
            {
                plane.Raycast(ray, out float enter);
                Vector3 point = ray.GetPoint(enter);
                transform.parent.position = point;
            }
        }
    }
}
