using SEE.Utils;
using UnityEngine;

namespace SEE.Controls.Actions
{
    /// <summary>
    /// Provides the ability to select graph elements (nodes or edges). 
    /// This components is intended to be added to instances of a player
    /// object. Generally, it will be added to prefabs for such player
    /// objects.
    /// </summary>
    public class SelectAction : MonoBehaviour
    {
        /// <summary>
        /// Deselects all currently selected interactable objects if the user requests.
        /// If the left mouse button is pressed and no GUI is in the way:
        /// if a graph element (node or edge) is hit, it will be 
        /// 
        /// 
        /// Selects a particular object that is currently being hovered over if the
        /// user presses the left mouse button and no GUI is in the way. Unselects
        /// it will be selected.
        /// </summary>
        private void Update()
        {
            if (SEEInput.Unselect())
            {
                InteractableObject.UnselectAll(true);
            }
            else if (Input.GetMouseButtonDown(0) && !Raycasting.IsMouseOverGUI())
            {
                InteractableObject obj = null;
                if (Raycasting.RaycastInteractableObject(out _, out InteractableObject o) != HitGraphElement.None)
                {
                    obj = o;
                }

                if (Input.GetKey(KeyCode.LeftControl))
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
