using SEE.Utils;
using UnityEngine;

namespace SEE.Controls.Actions
{
    public class SelectAction : MonoBehaviour
    {
        private void Update()
        {
            bool isMouseOverGUI = Raycasting.IsMouseOverGUI();

            bool unselectAll = Input.GetKeyDown(KeyCode.U);
            bool select = Input.GetMouseButtonDown(0);
            bool toggle = Input.GetKey(KeyCode.LeftControl);

            if (unselectAll)
            {
                InteractableObject.UnselectAll(true);
            }
            else if (select && !isMouseOverGUI)
            {
                InteractableObject obj = null;
                if (Raycasting.RaycastInteractableObject(out _, out InteractableObject o) != HitGraphElement.None)
                {
                    obj = o;
                }

                if (toggle)
                {
                    obj?.SetSelect(!obj.IsSelected, true);
                }
                else
                {
                    InteractableObject.UnselectAll(true);
                    obj?.SetSelect(true, true);
                }
            }
        }
    }
}
