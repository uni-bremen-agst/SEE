using System.Linq;
using SEE.IDE;
using SEE.Utils;
using UnityEngine;
using SEE.Audio;
using SEE.GO;
using SEE.XR;

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
    /// When there is no interaction taken by the user, any pending selection of
    /// the IDE will be checked.
    /// </summary>
    private void Update()
    {
      if (SEEInput.Unselect())
      {
        InteractableObject.UnselectAll(true);
        AudioManagerImpl.EnqueueSoundEffect(IAudioManager.SoundEffect.DropSound);
      }
      else if (SEEInput.Select() || XRSEEActions.SelectedFlag)
      {
        InteractableObject obj = null;
        if (Raycasting.RaycastInteractableObjectBase(out RaycastHit hit, out InteractableObjectBase o)
                && o is InteractableObject
                && ((InteractableObject)o).GraphElemRef.Elem != null
                && o.IsInteractable(o.PartiallyInteractable ? hit.point : null))
        {
          obj = ((InteractableObject)o);
          if ( obj.CompareTag("Node"))
          {
            ShowEdgesOfSelectedNode showEdgesOfSelectedNode = new ShowEdgesOfSelectedNode();
            Debug.Log($"ShowEdgesOfSelectedNode: Selected {showEdgesOfSelectedNode.ShowEdgesHere(obj)}");
            
          }
        }
        if (Input.GetKey(KeyCode.LeftControl) || (SceneSettings.InputType == PlayerInputType.VRPlayer && XRSEEActions.SelectedFlag))
        {
          if (obj != null)
          {
            obj.SetSelect(!obj.IsSelected, true);
            XRSEEActions.SelectedFlag = false;
          }
        }
        else
        {
          InteractableObject.ReplaceSelection(obj, true);
          XRSEEActions.SelectedFlag = false;
        }
      }
      else if (IDEIntegration.Instance != null && IDEIntegration.Instance.PendingSelectionsAction())
      {
        InteractableObject.UnselectAll(false);

        foreach (InteractableObject elem in IDEIntegration.Instance.PopPendingSelections().Where(e => e != null))
        {
          elem.SetSelect(true, true);
        }
      }
    }
  }
}
