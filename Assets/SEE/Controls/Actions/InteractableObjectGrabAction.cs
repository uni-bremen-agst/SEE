﻿using UnityEngine;

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
                Debug.LogErrorFormat("InteractableObjectGrabAction.OnEnable for {0} has NO interactable.\n", name);
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
                Debug.LogErrorFormat("InteractableObjectGrabAction.OnDisable for {0} has NO interactable.\n", name);
            }
        }

        /// <summary>
        /// Called when the object is grabbed.
        /// </summary>
        /// <param name="isOwner">true if a local user initiated this call</param>
        protected abstract void On(bool isOwner);

        /// <summary>
        /// Called when the object is no longer grabbed.
        /// </summary>
        /// <param name="isOwner">true if a local user initiated this call</param>
        protected abstract void Off(bool isOwner);
    }
}