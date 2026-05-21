using UnityEngine;

namespace SEE.Controls
{
    /// <summary>
    /// Toggles the children of the game object this component is attached to.
    /// </summary>
    public abstract class ToggleChildren : MonoBehaviour
    {
        /// <summary>
        /// Toggles the children of the game object this component is attached to
        /// when the toggle condition is met.
        /// </summary>
        private void Update()
        {
            if (ToggleCondition())
            {
                foreach (Transform child in gameObject.transform)
                {
                    child.gameObject.SetActive(!child.gameObject.activeSelf);
                }
            }
        }

        /// <summary>
        /// If true, the children should be toggled.
        /// </summary>
        /// <returns>Whether the children should be toggled.</returns>
        protected abstract bool ToggleCondition();
    }
}
