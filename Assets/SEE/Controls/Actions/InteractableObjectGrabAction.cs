using UnityEngine;

namespace SEE.Controls.Actions
{
    /// <summary>
    /// Abstract super class of behaviours to be triggered when an interactable
    /// object is being grabbed/released.
    /// </summary>
    public abstract class InteractableObjectGrabAction : InteractableObjectAction
    {
        /// <summary>
        /// Registers On() and Off() for the respective grabbing events.
        /// </summary>
        protected virtual void OnEnable()
        {
            if (interactable != null)
            {
                interactable.GrabIn += On;
                interactable.GrabOut += Off;
            }
            else
            {
                Debug.LogError($"InteractableObjectGrabAction.OnEnable for {name} has NO interactable.\n");
            }
        }

        /// <summary>
        /// Unregisters On() and Off() from the respective grabbing events.
        /// </summary>
        protected virtual void OnDisable()
        {
            if (interactable != null)
            {
                interactable.GrabIn -= On;
                interactable.GrabOut -= Off;
            }
            else
            {
                Debug.LogError($"InteractableObjectGrabAction.OnDisable for {name} has NO interactable.\n");
            }
        }

        /// <summary>
        /// Called when the object is grabbed.
        /// </summary>
        /// <param name="interactableObject">the object being grabbed</param>
        /// <param name="isInitiator">true if a local user initiated this call</param>
        protected abstract void On(InteractableObject interactableObject, bool isInitiator);

        /// <summary>
        /// Called when the object is no longer grabbed.
        /// </summary>
        /// <param name="interactableObject">the object being grabbed</param>
        /// <param name="isInitiator">true if a local user initiated this call</param>
        protected abstract void Off(InteractableObject interactableObject, bool isInitiator);
    }
}