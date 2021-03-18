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
                if (Raycasting.RaycastInteractableObject(out RaycastHit raycastHit, out InteractableObject o) != HitGraphElement.None)
                {
                    Transform hoveredTransform = raycastHit.transform;
                    // parentTransform walks up the game-object hierarchy toward the
                    // containing CityTransform. If the CityTransform is reached, we
                    // know that hoveredTransform is part of the CityTransform, thus,
                    // belongs to the city, we are dealing with.
                    Transform parentTransform = hoveredTransform;
                    do
                    {
                        if (parentTransform == transform)
                        {
                            obj = o;
                            break;
                        }
                        else
                        {
                            parentTransform = parentTransform.parent;
                        }
                    } while (parentTransform != null);
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
