using SEE.Controls.Interactables;
using UnityEngine;

namespace SEE.Controls.Actions
{
    public abstract class HighlightedInteractableObjectAction : InteractableObjectAction
    {
        /// <summary>
        /// The local color of the outline. Is expected to be assigned by the static
        /// constructor of a subclass.
        /// </summary>
        protected static Color LocalOutlineColor;

        /// <summary>
        /// The remote color of the outline. Is expected to be assigned by the static
        /// constructor of a subclass.
        /// </summary>
        protected static Color RemoteOutlineColor;

        /// <summary>
        /// Outline of the game object being activated.
        /// </summary>
        protected Outline outline;

        /// <summary>
        /// The AlphaEnforcer that ensures that the selected game object always has the correct
        /// alpha value.
        /// </summary>
        protected AlphaEnforcer enforcer;

        /// <summary>
        /// Color of the initial outline before being activated.
        /// </summary>
        protected Color initialOutlineColor = Color.black;

        /// <summary>
        /// Alpha value of the node before being activated.
        /// </summary>
        protected float initialAlpha = 1f;

        /// <summary>
        /// Initializes this component by creating an outline and AlphaEnforcer, if necessary.
        /// </summary>
        protected virtual void Start()
        {
            if (!TryGetComponent(out outline))
            {
                outline = Outline.Create(gameObject, initialOutlineColor);
            }

            if (!TryGetComponent(out enforcer))
            {
                enforcer = gameObject.AddComponent<AlphaEnforcer>();
                enforcer.TargetAlpha = initialAlpha;
            }
        }

        protected void SetInitialAndNewOutlineColor(bool isInitiator)
        {
            initialOutlineColor = outline.OutlineColor;
            SetOutlineColor(isInitiator);
        }

        protected void SetOutlineColor(bool isInitiator)
        {
            outline.OutlineColor = isInitiator ? LocalOutlineColor : RemoteOutlineColor;
        }

        protected void ResetOutlineColor()
        {
            outline.OutlineColor = initialOutlineColor;
        }

        protected void SetAlpha(bool isInitiator)
        {
            if (isInitiator)
            {
                initialAlpha = enforcer.TargetAlpha;
                enforcer.TargetAlpha = 1f;
            }
        }

        protected void ResetAlpha(bool isInitiator)
        {
            if (isInitiator)
            {
                enforcer.TargetAlpha = initialAlpha;
            }
        }

        protected void SetOutlineColorAndAlpha(bool isInitiator)
        {
            SetInitialAndNewOutlineColor(isInitiator);
            SetAlpha(isInitiator);
        }

        protected void ResetOutlineColorAndAlpha(bool isInitiator)
        {
            ResetOutlineColor();
            ResetAlpha(isInitiator);
        }

        /// <summary>
        /// Called when the object gets activated.
        /// </summary>
        /// <param name ="interactableObject" >the object being activated</param>
        /// <param name="isInitiator">true if the caller is the initiator, that is, the one
        /// triggering this action, rather than just a proxy propagating an action
        /// through the network to other connected clients</param>
        protected abstract void On(InteractableObject interactableObject, bool isInitiator);

        /// <summary>
        /// Called when the object gets deactivated.
        /// </summary>
        /// <param name ="interactableObject" >the object being deactivated</param>
        /// <param name="isInitiator">true if the caller is the initiator, that is, the one
        /// triggering this action, rather than just a proxy propagating an action
        /// through the network to other connected clients</param>
        protected abstract void Off(InteractableObject interactableObject, bool isInitiator);
    }
}
