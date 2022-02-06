using UnityEngine;

namespace SEE.Controls.Actions
{
    /// <summary>
    /// Draws or modifies, respectively, an outline around a selected game object
    /// and makes it opaque.
    /// </summary>
    public class ShowSelection : HighlightedInteractableObjectAction
    {
        /// <summary>
        /// Initializes the local and remote outline color.
        /// </summary>
        static ShowSelection()
        {
            LocalOutlineColor = Utils.ColorPalette.Viridis(0.8f);
            RemoteOutlineColor = Utils.ColorPalette.Viridis(0.6f);
        }

        /// <summary>
        /// If the object is not grabbed, a selection outline will be
        /// created. Depending upon <paramref name="isInitiator"/> one of two different
        /// colors will be used for the outline.
        /// Called when the object is selected.
        /// </summary>
        /// <param name="isInitiator">true if a local user initiated this call</param>
        protected override void On(InteractableObject interactableObject, bool isInitiator)
        {
            if (!Interactable.IsGrabbed)
            {
                SetOutlineColorAndAlpha(isInitiator);
            }
        }

        /// <summary>
        /// If the object is neither grabbed nor hovered over and if it has an outline,
        /// that outline will be removed.
        /// </summary>
        /// <param name="isInitiator">true if a local user initiated this call</param>
        protected override void Off(InteractableObject interactableObject, bool isInitiator)
        {
            if (!Interactable.IsHovered && !Interactable.IsGrabbed)
            {
                ResetOutlineColorAndAlpha(isInitiator);
            }
        }

        /// <summary>
        /// Registers On() and Off() for the respective selection events.
        /// </summary>
        protected virtual void OnEnable()
        {
            if (Interactable != null)
            {
                Interactable.SelectIn += On;
                Interactable.SelectOut += Off;
                Interactable.GrabOut += GrabOff;
            }
            else
            {
                Debug.LogError($"ShowSelection.OnEnable for {name} has NO interactable.\n");
            }
        }

        /// <summary>
        /// Unregisters On() and Off() from the respective selection events.
        /// </summary>
        protected virtual void OnDisable()
        {
            if (Interactable != null)
            {
                Interactable.SelectIn -= On;
                Interactable.SelectOut -= Off;
                Interactable.GrabOut -= GrabOff;
            }
            else
            {
                Debug.LogError($"ShowSelection.OnDisable for {name} has NO interactable.\n");
            }
        }

        /// <summary>
        /// If the object is no longer grabbed, but selected, the outline color and alpha
        /// value are reset to their original values.
        /// </summary>
        /// <param name="interactableObject">The ungrabbed object.</param>
        /// <param name="isInitiator">true if a local user initiated this call</param>
        protected void GrabOff(InteractableObject interactableObject, bool isInitiator)
        {
            if (Interactable.IsSelected)
            {
                ResetOutlineColorAndAlpha(isInitiator);
            }
        }
    }
}