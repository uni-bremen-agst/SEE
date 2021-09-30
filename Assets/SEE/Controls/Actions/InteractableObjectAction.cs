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
        protected InteractableObject interactable;

        /// <summary>
        /// Sets <see cref="interactable"/>. In case of failure, this action will be disabled.
        /// </summary>
        protected virtual void Awake()
        {
            if (!gameObject.TryGetComponent<InteractableObject>(out interactable))
            {
                Debug.LogWarning($"Game object {name} has no {nameof(InteractableObject)} component.\n");
                enabled = false;
            }
        }
    }
}