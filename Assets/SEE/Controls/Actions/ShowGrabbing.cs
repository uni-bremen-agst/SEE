using UnityEngine;

namespace SEE.Controls.Actions
{
    public class ShowGrabbing : InteractableObjectGrabAction
    {
        /// <summary>
        /// The local grabbing color of the outline.
        /// </summary>
        private readonly static Color LocalGrabColor = Utils.ColorPalette.Viridis(0.8f);

        /// <summary>
        /// The remote grabbing color of the outline.
        /// </summary>
        private readonly static Color RemoteGrabColor = Utils.ColorPalette.Viridis(0.0f);

        protected override void On(InteractableObject interactableObject, bool isInitiator)
        {
            if (TryGetComponent(out Outline outline))
            {
                outline.SetColor(isInitiator ? LocalGrabColor : RemoteGrabColor);
            }
            else
            {
                Outline.Create(gameObject, isInitiator ? LocalGrabColor : RemoteGrabColor);
            }
        }

        protected override void Off(InteractableObject interactableObject, bool isInitiator)
        {
            if (!interactable.IsHovered && !interactable.IsSelected && TryGetComponent(out Outline outline))
            {
                DestroyImmediate(outline);
            }
        }
    }
}