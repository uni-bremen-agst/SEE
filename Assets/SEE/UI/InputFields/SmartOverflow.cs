using UnityEngine;
using TMPro;

namespace SEE.UI.InputFields
{
    /// <summary>
    /// SmartOverflow automatically toggles a TMP_InputField's text overflow behavior
    /// between <c>Ellipsis</c> and <c>Overflow</c> modes based on focus state:
    ///
    /// - <b>Ellipsis Mode</b>: When the input field is not focused, long text is truncated with '…'.
    /// - <b>Overflow Mode</b>: When the input field is focused, text can scroll freely without truncation.
    ///
    /// This component requires a TMP_InputField and can be attached directly to its GameObject.
    /// No manual event registration is required.
    /// </summary>
    [RequireComponent(typeof(TMP_InputField))]
    public class SmartOverflow : MonoBehaviour
    {
        /// <summary>
        /// Reference to the TMP_InputField component on this GameObject.
        /// </summary>
        private TMP_InputField inputField;

        /// <summary>
        /// Original size of the text RectTransform.
        /// Stored to reset the text field when switching back to Ellipsis mode.
        /// </summary>
        private Vector2 originalSizeDelta;

        /// <summary>
        /// Original anchored position of the text RectTransform.
        /// Stored to reset the text field when switching back to Ellipsis mode.
        /// </summary>
        private Vector2 originalAnchoredPosition;

        /// <summary>
        /// Initializes references, registers focus events, and sets the default mode.
        /// </summary>
        private void Awake()
        {
            inputField = GetComponent<TMP_InputField>();

            originalAnchoredPosition = inputField.textComponent.rectTransform.anchoredPosition;
            originalSizeDelta = inputField.textComponent.rectTransform.sizeDelta;

            inputField.onSelect.AddListener(OnSelect);
            inputField.onDeselect.AddListener(OnDeselect);

            EnableEllipsisMode();
        }

        /// <summary>
        /// Removes event listeners to prevent memory leaks.
        /// </summary>
        private void OnDestroy()
        {
            inputField.onSelect.RemoveListener(OnSelect);
            inputField.onDeselect.RemoveListener(OnDeselect);
        }

        /// <summary>
        /// Event handler called when the input field gains focus.
        /// Switches the text mode to Overflow (scrollable).
        /// </summary>
        /// <param name="text">Current text of the input field (not used).</param>
        private void OnSelect(string text)
        {
            EnableOverflowMode();
        }

        /// <summary>
        /// Event handler called when the input field loses focus.
        /// Switches the text mode back to Ellipsis (compact with …).
        /// </summary>
        /// <param name="text">Current text of the input field (not used).</param>
        private void OnDeselect(string text)
        {
            EnableEllipsisMode();
        }

        /// <summary>
        /// Sets the TMP_Text overflow mode to Overflow (scrollable).
        /// </summary>
        private void EnableOverflowMode()
        {
            inputField.textComponent.overflowMode = TextOverflowModes.Overflow;
        }

        /// <summary>
        /// Sets the TMP_Text overflow mode to Ellipsis (compact with …).
        /// </summary>
        private void EnableEllipsisMode()
        {
            inputField.textComponent.overflowMode = TextOverflowModes.Ellipsis;
            RectTransform rt = inputField.textComponent.rectTransform;
            rt.anchoredPosition = originalAnchoredPosition;
            rt.sizeDelta = originalSizeDelta;

            inputField.ForceLabelUpdate();
        }
    }
}
