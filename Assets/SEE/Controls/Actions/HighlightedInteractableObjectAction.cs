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
        /// Color of the initial outline before being activated.
        /// </summary>
        protected Color initialOutlineColor = Color.black;

        /// <summary>
        /// Initializes this component by creating an outline, if necessary.
        /// </summary>
        /// <remarks>This method is called by Unity only if the Object is active.
        /// This function is called just after the object is enabled. This happens when
        /// a MonoBehaviour instance is created, such as when a level is loaded or
        /// a GameObject with the script component is instantiated.
        /// It is called after <see cref="Awake"/> and before <see cref="Start"/>.</remarks>
        protected virtual void OnEnable()
        {
            if (!TryGetComponent(out outline))
            {
                outline = Outline.Create(gameObject, initialOutlineColor);
            }
        }

        /// <summary>
        /// Remembers the original value of <see cref="outline.OutlineColor"/> in
        /// <see cref="initialOutlineColor"/> and sets it to either <see cref="LocalOutlineColor"/>
        /// or <see cref="RemoteOutlineColor"/> depending upon <paramref name="isInitiator"/>.
        /// </summary>
        /// <param name="isInitiator">true if the caller is the initiator, that is, the one
        /// triggering this action, rather than just a proxy propagating an action
        /// through the network to other connected clients</param>
        protected void SetInitialAndNewOutlineColor(bool isInitiator)
        {
            initialOutlineColor = outline.OutlineColor;
            SetOutlineColor(isInitiator);
        }

        /// <summary>
        /// Sets <see cref="outline.OutlineColor"/> to either <see cref="LocalOutlineColor"/>
        /// or <see cref="RemoteOutlineColor"/> depending upon <paramref name="isInitiator"/>.
        /// </summary>
        /// <param name="isInitiator">true if the caller is the initiator, that is, the one
        /// triggering this action, rather than just a proxy propagating an action
        /// through the network to other connected clients</param>
        protected void SetOutlineColor(bool isInitiator)
        {
            if (outline)
            {
                outline.OutlineColor = isInitiator ? LocalOutlineColor : RemoteOutlineColor;
            }
        }

        /// <summary>
        /// Resets <see cref="outline.OutlineColor"/> to <see cref="initialOutlineColor"/>.
        /// </summary>
        protected void ResetOutlineColor()
        {
            outline.OutlineColor = initialOutlineColor;
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
