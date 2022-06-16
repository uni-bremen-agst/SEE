using DG.Tweening;
using SEE.Utils;
using UnityEngine;

namespace SEE.Controls.Actions
{
    /// <summary>
    /// Draws or modifies, respectively, an outline around a game object being hovered over and makes it opaque.
    /// </summary>
    internal class ShowHovering : HighlightedInteractableObjectAction
    {
        /// <summary>
        /// Initializes the local and remote outline color.
        /// </summary>
        static ShowHovering()
        {
            LocalOutlineColor = ColorPalette.Viridis(0.4f);
            RemoteOutlineColor = ColorPalette.Viridis(0.2f);
        }

        /// <summary>
        /// If the object is neither selected nor grabbed, a hovering outline will be
        /// created. Depending upon <paramref name="isInitiator"/> one of two different
        /// colors will be used for the outline.
        /// Called when the object is hovered over.
        /// </summary>
        /// <param name="interactableObject">the object being hovered over (not used here)</param>
        /// <param name="isInitiator">true if a local user initiated this call</param>
        protected override void On(InteractableObject interactableObject, bool isInitiator)
        {
            if (!Interactable.IsSelected && !Interactable.IsGrabbed)
            {
                SetInitialAndNewOutlineColor(isInitiator);
            }
        }

        /// <summary>
        /// If the object is neither selected nor grabbed and if it has an outline,
        /// this outline will be destroyed.
        /// Called when the object is no longer being hovered over.
        /// </summary>
        /// <param name="interactableObject">the object being hovered over (not used here)</param>
        /// <param name="isInitiator">true if a local user initiated this call</param>
        protected override void Off(InteractableObject interactableObject, bool isInitiator)
        {
            //FIXME: Outline color is not correctly set if we hover off while a node is selected
            if (!Interactable.IsSelected && !Interactable.IsGrabbed)
            {
                ResetOutlineColor();
            }
        }

        protected void SelectOff(InteractableObject interactableObject, bool isInitiator)
        {
            if (Interactable.IsHovered && !Interactable.IsGrabbed)
            {
                SetOutlineColor(isInitiator);
            }
        }

        protected void GrabOff(InteractableObject interactableObject, bool isInitiator)
        {
            if (Interactable.IsHovered && !Interactable.IsSelected)
            {
                SetOutlineColor(isInitiator);
            }
        }

        /// <summary>
        /// Registers On() and Off() for the respective hovering events.
        /// </summary>
        protected virtual void OnEnable()
        {
            if (Interactable != null)
            {
                Interactable.HoverIn += On;
                Interactable.HoverOut += Off;
                Interactable.SelectOut += SelectOff;
                Interactable.GrabOut += GrabOff;
            }
            else
            {
                Debug.LogErrorFormat("ShowHovering.OnEnable for {0} has NO interactable.\n", name);
            }
        }

        /// <summary>
        /// Unregisters On() and Off() from the respective hovering events.
        /// </summary>
        protected virtual void OnDisable()
        {
            if (Interactable != null)
            {
                Interactable.HoverIn -= On;
                Interactable.HoverOut -= Off;
                Interactable.SelectOut -= SelectOff;
                Interactable.GrabOut -= GrabOff;
            }
            else
            {
                Debug.LogErrorFormat("ShowHovering.OnDisable for {0} has NO interactable.\n", name);
            }
        }
    }
}