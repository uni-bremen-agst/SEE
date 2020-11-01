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
        /// Registers Show() and Hide() for the respective hovering events.
        /// </summary>
        protected virtual void OnEnable()
        {
            if (interactable != null)
            {
                interactable.HoverIn += Show;
                interactable.HoverOut += Hide;
            }
            else
            {
                Debug.LogErrorFormat("ShowLabel.OnEnable for {0} has NO interactable.\n", name);
            }
        }

        /// <summary>
        /// Unregisters Show() and Hide() from the respective hovering events.
        /// </summary>
        protected virtual void OnDisable()
        {
            if (interactable != null)
            {
                interactable.HoverIn -= Show;
                interactable.HoverOut -= Hide;
            }
            else
            {
                Debug.LogErrorFormat("ShowLabel.OnDisable for {0} has NO interactable.\n", name);
            }
        }

        /// <summary>
        /// Called when the object is hovered over.
        /// </summary>
        /// <param name="isOwner">true if a local user initiated this call</param>
        protected abstract void Show(bool isOwner);

        /// <summary>
        /// Called when the object is no longer being hovered over.
        /// </summary>
        /// <param name="isOwner">true if a local user initiated this call</param>
        protected abstract void Hide(bool isOwner);
    }
}