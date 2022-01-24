using DG.Tweening;
using SEE.Controls.Interactables;
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
        /// The animation sequence to be played while the gameObject is being hovered over.
        /// </summary>
        private Sequence hoverAnimation;

        /// <summary>
        /// Initializes this component by creating an outline and AlphaEnforcer as
        /// well as a <see cref="hoverAnimation"/>.
        /// </summary>
        protected override void Start()
        {
            base.Start();
            hoverAnimation = DOTween.Sequence();
            // The following animation is equivalent to SetAlpha() when played forward and equivalent to
            // ResetAlpha() when played backward.
            hoverAnimation.Append(DOTween.To(() => enforcer.TargetAlpha, x => enforcer.TargetAlpha = x, 1f, 0.5f));
            hoverAnimation.SetAutoKill(false);
            hoverAnimation.SetLink(gameObject, LinkBehaviour.KillOnDestroy);
            hoverAnimation.Pause();
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
                SetInitialAndNewOutlineColor(isInitiator);
                if (isInitiator)
                {
                    // Replaces SetAlpha().
                    hoverAnimation.PlayForward();
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
                ResetOutlineColor();
                if (isInitiator)
                {
                    // Replaces ResetAlpha().
                    hoverAnimation.PlayBackwards();
                }
            }
        }

        protected void SelectOff(InteractableObject interactableObject, bool isInitiator)
        {
            if (interactable.IsHovered && !interactable.IsGrabbed)
            {
                SetOutlineColor(isInitiator);
                if (isInitiator)
                {
                    // Replaces SetAlpha().
                    hoverAnimation.PlayForward();
                }
            }
        }

        protected void GrabOff(InteractableObject interactableObject, bool isInitiator)
        {
            if (interactable.IsHovered && !interactable.IsSelected)
            {
                SetOutlineColor(isInitiator);
                if (isInitiator)
                {
                    // Replaces SetAlpha().
                    hoverAnimation.PlayForward();
                }
            }
        }

        /// <summary>
        /// Registers On() and Off() for the respective hovering events.
        /// </summary>
        protected virtual void OnEnable()
        {
            if (interactable != null)
            {
                interactable.HoverIn += On;
                interactable.HoverOut += Off;
                interactable.SelectOut += SelectOff;
                interactable.GrabOut += GrabOff;
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
            if (interactable != null)
            {
                interactable.HoverIn -= On;
                interactable.HoverOut -= Off;
                interactable.SelectOut -= SelectOff;
                interactable.GrabOut -= GrabOff;
            }
            else
            {
                Debug.LogErrorFormat("ShowHovering.OnDisable for {0} has NO interactable.\n", name);
            }
        }
    }
}