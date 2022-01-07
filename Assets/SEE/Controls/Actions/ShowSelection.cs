using SEE.Controls.Interactables;
using UnityEngine;

namespace SEE.Controls.Actions
{
    /// <summary>
    /// Draws or modifies, respectively, an outline around a selected game object and makes it opaque.
    /// </summary>
    public class ShowSelection : InteractableObjectSelectionAction
    {
        /// <summary>
        /// The local selection color of the outline.
        /// </summary>
        private static readonly Color LocalSelectColor = Utils.ColorPalette.Viridis(0.8f);

        /// <summary>
        /// The remote selection color of the outline.
        /// </summary>
        private static readonly Color RemoteSelectColor = Utils.ColorPalette.Viridis(0.6f);

        /// <summary>
        /// Outline of the game object being selected.
        /// </summary>
        private Outline outline;

        /// <summary>
        /// The AlphaEnforcer that ensures that the selected game object always has the correct
        /// alpha value.
        /// </summary>
        private AlphaEnforcer enforcer;

        /// <summary>
        /// Color of the node before being selected.
        /// </summary>
        private Color initialColor = Color.black;

        /// <summary>
        /// Alpha value of the node before being hovered over.
        /// </summary>
        private float initialAlpha = 1f;

        /// <summary>
        /// Initializes this component by creating an outline and AlphaEnforcer, if necessary.
        /// </summary>
        private void Start()
        {
            if (!TryGetComponent(out outline))
            {
                outline = Outline.Create(gameObject, initialColor);
            }

            if (!TryGetComponent(out enforcer))
            {
                enforcer = gameObject.AddComponent<AlphaEnforcer>();
                enforcer.TargetAlpha = initialAlpha;
            }
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
            if (!interactable.IsGrabbed)
            {
                initialColor = outline.OutlineColor;
                outline.OutlineColor = isInitiator ? LocalSelectColor : RemoteSelectColor;
                if (isInitiator)
                {
                    initialAlpha = enforcer.TargetAlpha;
                    enforcer.TargetAlpha = 1f;
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
            if (!interactable.IsHovered && !interactable.IsGrabbed)
            {
                outline.OutlineColor = initialColor;
                if (isInitiator)
                {
                    enforcer.TargetAlpha = initialAlpha;
                }
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
                outline.OutlineColor = isInitiator ? LocalSelectColor : RemoteSelectColor;
                if (isInitiator)
                {
                    enforcer.TargetAlpha = 1f;
                }
            }
        }
    }
}