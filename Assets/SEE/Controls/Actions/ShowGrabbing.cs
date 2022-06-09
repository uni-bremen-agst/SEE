using UnityEngine;

namespace SEE.Controls.Actions
{
    /// <summary>
    /// Draws or modifies, respectively, an outline around a game object being grabbed and
    /// makes it opaque.
    /// </summary>
    internal class ShowGrabbing : HighlightedInteractableObjectAction
    {
        /// <summary>
        /// Initializes the local and remote outline color.
        /// </summary>
        static ShowGrabbing()
        {
            LocalOutlineColor = Utils.ColorPalette.Viridis(1.0f);
            RemoteOutlineColor = Utils.ColorPalette.Viridis(0.0f);
        }

        /// <summary>
        /// Called when the object is grabbed. Remembers the current color
        /// of the outline in <see cref="initialColor"/> and sets the outline's
        /// color to <see cref="LocalOutlineColor"/> or <see cref="RemoteOutlineColor"/>,
        /// respectively, depending upon <paramref name="isInitiator"/>.
        /// If <paramref name="isInitiator"/>, the <see cref="enforcer"/>'s
        /// current <see cref="TargetAlpha"/> is saved and then set to 1.
        /// </summary>
        /// <param name="interactableObject">the object being grabbed</param>
        /// <param name="isInitiator">true if a local user initiated this call</param>
        protected override void On(InteractableObject interactableObject, bool isInitiator)
        {
            SetOutlineColor(isInitiator);
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
            if (!Interactable.IsHovered && !Interactable.IsSelected)
            {
                ResetOutlineColor();
            }
        }

        /// <summary>
        /// Registers On() and Off() for the respective grabbing events.
        /// </summary>
        protected virtual void OnEnable()
        {
            if (Interactable != null)
            {
                Interactable.GrabIn += On;
                Interactable.GrabOut += Off;
            }
            else
            {
                Debug.LogError($"ShowGrabbing.OnEnable for {name} has NO interactable.\n");
            }
        }

        /// <summary>
        /// Unregisters On() and Off() from the respective grabbing events.
        /// </summary>
        protected virtual void OnDisable()
        {
            if (Interactable != null)
            {
                Interactable.GrabIn -= On;
                Interactable.GrabOut -= Off;
            }
            else
            {
                Debug.LogError($"ShowGrabbing.OnDisable for {name} has NO interactable.\n");
            }
        }
    }
}