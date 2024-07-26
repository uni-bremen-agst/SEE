using SEE.Controls;
using TMPro;
using UnityEngine;

namespace SEE.UI.Drawable
{
    /// <summary>
    /// Locks the shortcuts as long as input is entered in an input field.
    /// Used in the menu's prefabs.
    /// </summary>
    [RequireComponent(typeof(TMP_InputField))]
    public class InputFieldEventTriggerController : MonoBehaviour
    {
        /// <summary>
        /// The input field being edited.
        /// </summary>
        private TMP_InputField inputField;

        /// <summary>
        /// Initializes the component.
        /// </summary>
        private void Awake()
        {
            inputField = GetComponent<TMP_InputField>();
            inputField.onDeselect.AddListener(ActivateInput);
            inputField.onSelect.AddListener(DeactivateInput);
        }

        /// <summary>
        /// Deactivates the keyboard shortcuts of SEE.
        /// </summary>
        /// <param name="hex">Simply a placeholder, not used.
        /// However, must be specified as AddListener expects a string UnityAction.</param>
        private void DeactivateInput(string hex)
        {
            SEEInput.KeyboardShortcutsEnabled = false;
        }

        /// <summary>
        /// Enables the keyboard shortcuts of SEE.
        /// </summary>
        /// <param name="hex">Simply a placeholder, not used.
        /// However, must be specified as AddListener expects a string UnityAction.</param>
        private void ActivateInput(string hex)
        {
            SEEInput.KeyboardShortcutsEnabled = true;
        }

        /// <summary>
        /// When the menu will be destroyed, enable the shortcuts.
        /// </summary>
        private void OnDestroy()
        {
            SEEInput.KeyboardShortcutsEnabled = true;
        }
    }
}
