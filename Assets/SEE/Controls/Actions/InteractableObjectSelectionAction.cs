using UnityEngine;

namespace SEE.Controls.Actions
{
    /// <summary>
    /// Abstract super class of behaviours to be triggered when an interactable
    /// object is being selected/deselected.
    /// </summary>
    public abstract class InteractableObjectSelectionAction : InteractableObjectAction
    {
        /// <summary>
        /// Registers On() and Off() for the respective selection events.
        /// </summary>
        protected virtual void OnEnable()
        {
            if (interactable != null)
            {
                interactable.SelectIn += On;
                interactable.SelectOut += Off;
                interactable.GrabOut += GrabOff;
            }
            else
            {
                Debug.LogError($"InteractableObjectSelectionAction.OnEnable for {name} has NO interactable.\n");
            }
        }

        /// <summary>
        /// Unregisters On() and Off() from the respective selection events.
        /// </summary>
        protected virtual void OnDisable()
        {
            if (interactable != null)
            {
                interactable.SelectIn -= On;
                interactable.SelectOut -= Off;
                interactable.GrabOut -= GrabOff;
            }
            else
            {
                Debug.LogError($"InteractableObjectSelectionAction.OnDisable for {name} has NO interactable.\n");
            }
        }

        /// <summary>
        /// Called when the object is selected.
        /// </summary>
        /// <param name="interactableObject">the object being selected</param>
        /// <param name="isInitiator">true if a local user initiated this call</param>
        protected abstract void On(InteractableObject interactableObject, bool isInitiator);

        /// <summary>
        /// Called when the object is no longer selected.
        /// </summary>
        /// <param name="interactableObject">the object being selected</param>
        /// <param name="isInitiator">true if a local user initiated this call</param>
        protected abstract void Off(InteractableObject interactableObject, bool isInitiator);

        /// <summary>
        /// Called when the object is no longer grabbed.
        /// </summary>
        /// <param name="interactableObject">the object being grabbed</param>
        /// <param name="isInitiator">true if a local user initiated this call</param>
        protected abstract void GrabOff(InteractableObject interactableObject, bool isInitiator);
    }
}