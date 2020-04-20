using UnityEngine;

namespace SEE.Controls
{
    public class Selection3DAction : SelectionAction
    {
        protected override Vector3 Origin()
        {
            return gameObject.transform.position;
        }

        protected override bool Detect(out RaycastHit hitInfo)
        {
            return Physics.Raycast(gameObject.transform.position, selectionDevice.Value, out hitInfo, Mathf.Infinity);
        }
    }
}
