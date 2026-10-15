using SEE.Extensions;
using UnityEngine;

namespace SEE.Controls.Modifiers
{
    /// <summary>
    /// Common abstract superclass of all modifiers relating to a game object that
    /// has an <see cref="InteractableObject"/> component attached to them.
    /// A modifier is one that modifies the appearance (e.g.,
    /// by an outline) or shows additional information (e.g., erosion icons)
    /// for a picked interactable object.
    /// </summary>
    public abstract class InteractableObjectModifier : MonoBehaviour
    {
        /// <summary>
        /// The interactable component attached to the same object as a sibling of this action.
        /// </summary>
        protected InteractableObject Interactable;

        /// <summary>
        /// Sets <see cref="Interactable"/>. In case of failure, this action will be disabled.
        /// </summary>
        protected virtual void Awake()
        {
            if (!gameObject.TryGetComponentOrLog(out Interactable))
            {
                enabled = false;
            }
        }
    }
}
