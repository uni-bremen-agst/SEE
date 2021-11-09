using UnityEngine;

namespace SEE.Controls.Actions
{
    /// <summary>
    /// Draws an outline for a selected object.
    /// </summary>
    public class ShowSelection : InteractableObjectSelectionAction
    {
        /// <summary>
        /// The local selection color of the outline.
        /// </summary>
        private readonly static Color LocalSelectColor = Utils.ColorPalette.Viridis(0.4f);

        /// <summary>
        /// The remote selection color of the outline.
        /// </summary>
        private readonly static Color RemoteSelectColor = Utils.ColorPalette.Viridis(0.6f);

        /// <summary>
        /// If the object is not grabbed, a selection outline will be
        /// created. Depending upon <paramref name="isInitiator"/> one of two different
        /// colors will be used for the outline.
        /// Called when the object is selected.
        /// </summary>
        /// <param name="isInitiator">true if a local user initiated this call</param>
        protected override void On(InteractableObject interactableObject, bool isInitiator)
        {
            if (!interactable.IsGrabbed)
            {
                if (TryGetComponent(out Outline outline))
                {
                    outline.SetColor(isInitiator ? LocalSelectColor : RemoteSelectColor);
                }
                else
                {
                    Outline.Create(gameObject, isInitiator ? LocalSelectColor : RemoteSelectColor);
                }
            }
        }

        /// <summary>
        /// If the object is neither grabbed nor hovered over and if it has an outline,
        /// that outline will be removed.
        /// </summary>
        /// <param name="isInitiator">true if a local user initiated this call</param>
        protected override void Off(InteractableObject interactableObject, bool isInitiator)
        {
            if (!interactable.IsHovered && !interactable.IsGrabbed && TryGetComponent(out Outline outline))
            {
                DestroyImmediate(outline);
            }
        }

        /// <summary>
        /// If the object is no longer grabbed, but selected, the outline color is changed.
        /// </summary>
        /// <param name="interactableObject">The ungrabbed object.</param>
        /// <param name="isInitiator">true if a local user initiated this call</param>
        protected override void GrabOff(InteractableObject interactableObject, bool isInitiator)
        {
            if (interactable.IsSelected)
            {
                GetComponent<Outline>().SetColor(isInitiator ? LocalSelectColor : RemoteSelectColor);
            }
        }
    }
}