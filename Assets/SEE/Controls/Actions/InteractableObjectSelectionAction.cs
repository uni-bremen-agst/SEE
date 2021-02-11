﻿using UnityEngine;

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
            }
            else
            {
                Debug.LogErrorFormat("InteractableObjectSelectionAction.OnEnable for {0} has NO interactable.\n", name);
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
            }
            else
            {
                Debug.LogErrorFormat("InteractableObjectSelectionAction.OnDisable for {0} has NO interactable.\n", name);
            }
        }

        /// <summary>
        /// Called when the object is selected.
        /// </summary>
        /// <param name="interactableObject">the object being selected</param>
        /// <param name="isOwner">true if a local user initiated this call</param>
        protected abstract void On(InteractableObject interactableObject, bool isOwner);

        /// <summary>
        /// Called when the object is no longer selected.
        /// </summary>
        /// <param name="interactableObject">the object being selected</param>
        /// <param name="isOwner">true if a local user initiated this call</param>
        protected abstract void Off(InteractableObject interactableObject, bool isOwner);
    }
}