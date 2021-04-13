using UnityEngine;

namespace SEE.Controls.Actions
{
    /// <summary>
    /// For performance reasons, this script exists separately from <see cref="MappingAction"/>. TODO(torben): They may simple be combined in the future.
    /// </summary>
    public class MappingGrabUpdateAction : MonoBehaviour
    {
        [SerializeField] private MappingAction mappingAction;

        private void Start()
        {
            InteractableObject.AnyGrabIn += AnyGrabIn;
            InteractableObject.AnyGrabOut += AnyGrabOut;
            enabled = false;
        }

        private void OnDestroy()
        {
            InteractableObject.AnyGrabIn -= AnyGrabIn;
            InteractableObject.AnyGrabOut -= AnyGrabOut;
        }

        private void Update() => mappingAction.UpdateGrabbed();

        private void AnyGrabIn(InteractableObject interactableObject, bool isOwner) => enabled = true;
        private void AnyGrabOut(InteractableObject interactableObject, bool isOwner) => enabled = InteractableObject.GrabbedObjects.Count > 0;
    }
}
