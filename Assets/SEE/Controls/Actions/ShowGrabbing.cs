using SEE.Controls.Interactables;
using UnityEngine;

namespace SEE.Controls.Actions
{
    /// <summary>
    /// Draws or modifies, respectively, an outline around a game object being grabbed and makes it opaque.
    ///
    /// FIXME: An outline is meanwhile added for every node, hence, outlining the
    /// grabbed object makes no visibile difference. We need another way to highlight
    /// a grabbed object.
    ///
    /// FIXME: <see cref="ShowGrabbing"/>,  <see cref="ShowSelection"/>, and <see cref="ShowHovering"/>
    /// are all so similiar to each other. We should consider to remove this redundancy.
    /// </summary>
    internal class ShowGrabbing : InteractableObjectGrabAction
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
        /// The AlphaEnforcer that ensures that the grabbed game object always has the correct
        /// alpha value.
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

        /// <summary>
        /// Called when the object is grabbed. Remembers the current color
        /// of the outline in <see cref="initialColor"/> and sets the outline's
        /// color to <see cref="LocalGrabColor"/> or <see cref="RemoteGrabColor"/>,
        /// respectively, depending upon <paramref name="isInitiator"/>.
        /// If <paramref name="isInitiator"/>, the <see cref="enforcer"/>'s
        /// current <see cref="TargetAlpha"/> is saved and then set to 1.
        /// </summary>
        /// <param name="interactableObject">the object being grabbed</param>
        /// <param name="isInitiator">true if a local user initiated this call</param>
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

        /// <summary>
        /// Called when the object is no longer grabbed. Resets the outline's
        /// color and the <see cref="enforcer"/>'s <see cref="TargetAlpha"/>
        /// to their original values before the object was grabbed.
        /// </summary>
        /// <param name="interactableObject">the object being grabbed</param>
        /// <param name="isInitiator">true if a local user initiated this call</param>
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