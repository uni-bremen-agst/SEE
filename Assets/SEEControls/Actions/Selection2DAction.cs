using UnityEngine;

namespace SEE.Controls
{
    public class Selection2DAction : SelectionAction
    {
        protected override Vector3 Origin()
        {
            return MainCamera.transform.position;
        }

        protected override bool Detect(out RaycastHit hitInfo)
        {
            Ray ray = MainCamera.ScreenPointToRay(selectionDevice.Value);
            //Debug.LogFormat("2d direction={0} at origin {1} and direction {2} camera {3}\n", 
            //                selectionDevice.Value, ray.origin, ray.direction, MainCamera.transform.position);
            //Debug.DrawRay(ray.origin, ray.direction * 10000, Color.yellow);
            return Physics.Raycast(ray, out hitInfo);
        }
    }
}
