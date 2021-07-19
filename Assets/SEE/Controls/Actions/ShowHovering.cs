using UnityEngine;

namespace SEE.Controls.Actions
{
    /// <summary>
    /// Draws an outline around a game object being hovered over.
    /// </summary>
    public class ShowHovering : InteractableObjectHoveringAction
    {
        /// <summary>
        /// The local hovering color of the outline.
        /// </summary>
        protected static readonly Color LocalHoverColor = Utils.ColorPalette.Viridis(0.0f);

        /// <summary>
        /// The remote hovering color of the outline.
        /// </summary>
        protected static readonly Color RemoteHoverColor = Utils.ColorPalette.Viridis(0.2f);

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
            if (!interactable.IsSelected && !interactable.IsGrabbed)
            {
                if (TryGetComponent(out Outline outline))
                {
                    outline.SetColor(isInitiator ? LocalHoverColor : RemoteHoverColor);
                }
                else
                {
                    Outline.Create(gameObject, isInitiator ? LocalHoverColor : RemoteHoverColor);
                }
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
            if (!interactable.IsSelected && !interactable.IsGrabbed && TryGetComponent(out Outline outline))
            {
                DestroyImmediate(outline);
            }
        }

        protected override void SelectOff(InteractableObject interactableObject, bool isInitiator)
        {
            if (interactable.IsHovered && !interactable.IsGrabbed)
            {
                GetComponent<Outline>().SetColor(isInitiator ? LocalHoverColor : RemoteHoverColor);
            }
        }

        protected override void GrabOff(InteractableObject interactableObject, bool isInitiator)
        {
            if (interactable.IsHovered && !interactable.IsSelected)
            {
                GetComponent<Outline>().SetColor(isInitiator ? LocalHoverColor : RemoteHoverColor);
            }
        }
    }
}