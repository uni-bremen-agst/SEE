using UnityEngine;

namespace SEE.Controls.Actions
{
    /// <summary>
    /// Abstract super class of behaviours to be triggered when an interactable
    /// object is being hovered on/off.
    /// </summary>
    public abstract class InteractableObjectHoveringAction : InteractableObjectAction
    {
        /// <summary>
        /// Registers On() and Off() for the respective hovering events.
        /// </summary>
        protected virtual void OnEnable()
        {
            if (interactable != null)
            {
                interactable.HoverIn += On;
                interactable.HoverOut += Off;
                interactable.SelectOut += SelectOff;
                interactable.GrabOut += GrabOff;
            }
            else
            {
                Debug.LogErrorFormat("InteractableObjectHoveringAction.OnEnable for {0} has NO interactable.\n", name);
            }
        }

        /// <summary>
        /// Unregisters On() and Off() from the respective hovering events.
        /// </summary>
        protected virtual void OnDisable()
        {
            if (interactable != null)
            {
                interactable.HoverIn -= On;
                interactable.HoverOut -= Off;
                interactable.SelectOut -= SelectOff;
                interactable.GrabOut -= GrabOff;
            }
            else
            {
                Debug.LogErrorFormat("InteractableObjectHoveringAction.OnDisable for {0} has NO interactable.\n", name);
            }
        }

        /// <summary>
        /// Called when the object is hovered over.
        /// </summary>
        /// <param name="interactableObject">the object being hovered over</param>
        /// <param name="isInitiator">true if a local user initiated this call</param>
        protected abstract void On(InteractableObject interactableObject, bool isInitiator);

        /// <summary>
        /// Called when the object is no longer being hovered over.
        /// </summary>
        /// <param name="interactableObject">the object being hovered over</param>
        /// <param name="isInitiator">true if a local user initiated this call</param>
        protected abstract void Off(InteractableObject interactableObject, bool isInitiator);

        /// <summary>
        /// Called when the object is no longer selected.
        /// </summary>
        /// <param name="interactableObject">the object being selected</param>
        /// <param name="isInitiator">true if a local user initiated this call</param>
        protected abstract void SelectOff(InteractableObject interactableObject, bool isInitiator);

        /// <summary>
        /// Called when the object is no longer grabbed.
        /// </summary>
        /// <param name="interactableObject">the object being grabbed</param>
        /// <param name="isInitiator">true if a local user initiated this call</param>
        protected abstract void GrabOff(InteractableObject interactableObject, bool isInitiator);
    }
}