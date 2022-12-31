using SEE.GO;
using UnityEngine;

namespace SEE.Controls.Actions
{
    /// <summary>
    /// Common abstract superclass of all actions relating to a game object that
    /// has an InteractableObject component attached to them.
    /// </summary>
    public abstract class InteractableObjectAction : MonoBehaviour
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