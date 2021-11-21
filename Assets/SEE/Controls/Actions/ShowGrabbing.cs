using SEE.Controls.Interactables;
using UnityEngine;

namespace SEE.Controls.Actions
{
    /// <summary>
    /// Draws an outline around a game object being grabbed and makes it opaque.
    /// </summary>
    public class ShowGrabbing : InteractableObjectGrabAction
    {
        /// <summary>
        /// The local grabbing color of the outline.
        /// </summary>
        private static readonly Color LocalGrabColor = Utils.ColorPalette.Viridis(1.0f);

        /// <summary>
        /// The remote grabbing color of the outline.
        /// </summary>
        private static readonly Color RemoteGrabColor = Utils.ColorPalette.Viridis(0.0f);

        /// <summary>
        /// Outline of the game object being grabbed.
        /// </summary>
        private Outline outline;

        /// <summary>
        /// The material of the game object being selected.
        /// </summary>
        private AlphaEnforcer enforcer;
        
        /// <summary>
        /// Color of the node before being grabbed.
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

        protected override void On(InteractableObject interactableObject, bool isInitiator)
        {
            initialColor = outline.OutlineColor;
            outline.OutlineColor = isInitiator ? LocalGrabColor : RemoteGrabColor;
            if (isInitiator)
            {
                initialAlpha = enforcer.TargetAlpha;
                enforcer.TargetAlpha = 1f;
            }
        }

        protected override void Off(InteractableObject interactableObject, bool isInitiator)
        {
            if (!interactable.IsHovered && !interactable.IsSelected)
            {
                outline.OutlineColor = initialColor;
                if (isInitiator)
                {
                    enforcer.TargetAlpha = initialAlpha;
                }
            }
        }
    }
}