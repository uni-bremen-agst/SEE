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
        protected Outline Outline;

        /// <summary>
        /// Color of the initial outline before being activated.
        /// </summary>
        protected Color InitialOutlineColor = Color.black;

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
            if (!TryGetComponent(out Outline))
            {
                Outline = Outline.Create(gameObject, InitialOutlineColor);
            }
        }

        /// <summary>
        /// Remembers the original value of <see cref="outline.OutlineColor"/> in
        /// <see cref="InitialOutlineColor"/> and sets it to either <see cref="LocalOutlineColor"/>
        /// or <see cref="RemoteOutlineColor"/> depending upon <paramref name="isInitiator"/>.
        /// </summary>
        /// <param name="isInitiator">True if the caller is the initiator, that is, the one
        /// triggering this action, rather than just a proxy propagating an action
        /// through the network to other connected clients.</param>
        protected void SetInitialAndNewOutlineColor(bool isInitiator)
        {
            InitialOutlineColor = Outline.OutlineColor;
            SetOutlineColor(isInitiator);
        }

        /// <summary>
        /// Sets <see cref="outline.OutlineColor"/> to either <see cref="LocalOutlineColor"/>
        /// or <see cref="RemoteOutlineColor"/> depending upon <paramref name="isInitiator"/>.
        /// </summary>
        /// <param name="isInitiator">True if the caller is the initiator, that is, the one
        /// triggering this action, rather than just a proxy propagating an action
        /// through the network to other connected clients.</param>
        protected void SetOutlineColor(bool isInitiator)
        {
            if (Outline)
            {
                Outline.OutlineColor = isInitiator ? LocalOutlineColor : RemoteOutlineColor;
            }
        }

        /// <summary>
        /// Resets <see cref="outline.OutlineColor"/> to <see cref="InitialOutlineColor"/>.
        /// </summary>
        protected void ResetOutlineColor()
        {
            if (Outline)
            {
                Outline.OutlineColor = InitialOutlineColor;
            }
        }

        /// <summary>
        /// Called when the object gets activated.
        /// </summary>
        /// <param name ="interactableObject" >The object being activated.</param>
        /// <param name="isInitiator">True if the caller is the initiator, that is, the one
        /// triggering this action, rather than just a proxy propagating an action
        /// through the network to other connected clients.</param>
        protected abstract void On(InteractableObject interactableObject, bool isInitiator);

        /// <summary>
        /// Called when the object gets deactivated.
        /// </summary>
        /// <param name ="interactableObject" >The object being deactivated.</param>
        /// <param name="isInitiator">True if the caller is the initiator, that is, the one
        /// triggering this action, rather than just a proxy propagating an action
        /// through the network to other connected clients.</param>
        protected abstract void Off(InteractableObject interactableObject, bool isInitiator);
    }
}
