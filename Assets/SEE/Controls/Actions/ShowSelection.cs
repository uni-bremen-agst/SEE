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
        /// created. Depending upon <paramref name="isOwner"/> one of two different
        /// colors will be used for the outline.
        /// Called when the object is selected.
        /// </summary>
        /// <param name="isOwner">true if a local user initiated this call</param>
        protected override void On(InteractableObject interactableObject, bool isOwner)
        {
            if (!interactable.IsGrabbed)
            {
                if (TryGetComponent(out Outline outline))
                {
                    outline.SetColor(isOwner ? LocalSelectColor : RemoteSelectColor);
                }
                else
                {
                    Outline.Create(gameObject, isOwner ? LocalSelectColor : RemoteSelectColor);
                }
            }
        }

        /// <summary>
        /// If the object is neither grabbed nor hovered over and if it has an outline,
        /// that outline will be removed.
        /// </summary>
        /// <param name="isOwner">true if a local user initiated this call</param>
        protected override void Off(InteractableObject interactableObject, bool isOwner)
        {
            if (!interactable.IsGrabbed && !interactable.IsHovered && TryGetComponent(out Outline outline))
            {
                DestroyImmediate(outline);
            }
        }
    }
}