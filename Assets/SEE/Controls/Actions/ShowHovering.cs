using SEE.Controls.Interactables;
using SEE.Utils;
using UnityEngine;

namespace SEE.Controls.Actions
{
    /// <summary>
    /// Draws an outline around a game object being hovered over and makes it opaque.
    /// </summary>
    public class ShowHovering : InteractableObjectHoveringAction
    {
        /// <summary>
        /// The local hovering color of the outline.
        /// </summary>
        private static readonly Color LocalHoverColor = ColorPalette.Viridis(0.4f);

        /// <summary>
        /// The remote hovering color of the outline.
        /// </summary>
        private static readonly Color RemoteHoverColor = ColorPalette.Viridis(0.2f);

        /// <summary>
        /// Color of the node's outline before being hovered over.
        /// </summary>
        private Color initialColor = Color.black;

        /// <summary>
        /// Alpha value of the node before being hovered over.
        /// </summary>
        private float initialAlpha = 1f;

        /// <summary>
        /// Outline of the game object being hovered over.
        /// </summary>
        private Outline outline;

        /// <summary>
        /// The material of the game object being hovered over.
        /// </summary>
        private AlphaEnforcer enforcer;

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
                initialColor = outline.OutlineColor;
                outline.OutlineColor = isInitiator ? LocalHoverColor : RemoteHoverColor;
                if (isInitiator)
                {
                    initialAlpha = enforcer.TargetAlpha;
                    enforcer.TargetAlpha = 1f;
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
            //FIXME: Outline color is not correctly set if we hover off while a node is selected
            if (!interactable.IsSelected && !interactable.IsGrabbed)
            {
                outline.OutlineColor = initialColor;
                if (isInitiator)
                {
                    enforcer.TargetAlpha = initialAlpha;
                }
            }
        }

        protected override void SelectOff(InteractableObject interactableObject, bool isInitiator)
        {
            if (interactable.IsHovered && !interactable.IsGrabbed)
            {
                outline.OutlineColor = isInitiator ? LocalHoverColor : RemoteHoverColor;
                if (isInitiator)
                {
                    enforcer.TargetAlpha = 1f;
                }
            }
        }

        protected override void GrabOff(InteractableObject interactableObject, bool isInitiator)
        {
            if (interactable.IsHovered && !interactable.IsSelected)
            {
                outline.OutlineColor = isInitiator ? LocalHoverColor : RemoteHoverColor;
                if (isInitiator)
                {
                    enforcer.TargetAlpha = 1f;
                }
            }
        }
    }
}