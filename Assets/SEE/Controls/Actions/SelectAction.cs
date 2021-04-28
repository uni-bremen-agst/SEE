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
        /// Deselects all currently selected interactable objects if the user requests
        /// us to do so.
        /// Additionally, if the left mouse button is pressed and no GUI is in the way
        /// (let O be the currently hovered graph element at this point):
        /// either
        ///   1) if the user wants us to toggle, O will be selected if it was not
        ///      selected or unselected if it was
        /// or
        ///   2) all currently selected objects are unselected and O becomes selected.
        /// </summary>
        private void Update()
        {
            if (SEEInput.Unselect())
            {
                InteractableObject.UnselectAll(true);
            }
            else if (SEEInput.Select())
            {
                InteractableObject obj = null;
                if (Raycasting.RaycastInteractableObject(out _, out InteractableObject o) != HitGraphElement.None)
                {
                    obj = o;
                }

                if (Input.GetKey(KeyCode.LeftControl))
                {
                    if (obj != null)
                    {
                        obj.SetSelect(!obj.IsSelected, true);
                    }
                }
                else
                {
                    InteractableObject.ReplaceSelection(obj, true);
                }
            }
        }
    }
}
