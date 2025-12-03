using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SEE.Utils.Extensions
{
    /// <summary>
    /// Provides extension methods for <see cref="TMP_InputField"/>/> to simplify UI state handling,
    /// such as coloring transitions and highlighting validation errors.
    /// </summary>
    public static class TMPInputFieldExtensions
    {
        /// <summary>
        /// Sets all transition-related colors (normal, highlighted, pressed, disabled, selected)
        /// of the <see cref="TMP_InputField"/> to the specified <paramref name="color"/>.
        /// </summary>
        /// <param name="inputField">The input field whose color transitions will be updated.</param>
        /// <param name="color">The color applied to all transition states.</param>
        public static void SetAllTransitionColors(this TMP_InputField inputField, Color color)
        {
            if (inputField == null)
            {
                return;
            }

            ColorBlock cb = inputField.colors;

            cb.normalColor = color;
            cb.highlightedColor = color;
            cb.pressedColor = color;
            cb.disabledColor = color;
            cb.selectedColor = color;

            cb.colorMultiplier = 1f;
            cb.fadeDuration = 0.1f;

            inputField.colors = cb;
        }

        /// <summary>
        /// Applies a visual error state to the input field by coloring all transition states red,
        /// and highlighting the associated label (if found) in red as well.
        /// </summary>
        /// <param name="inputField">The input field to visually mark as invalid.</param>
        public static void SetErrorState(this TMP_InputField inputField)
        {
            SetAllTransitionColors(inputField, Color.red);
            SetLabelColor(inputField, Color.red);
        }

        /// <summary>
        /// Resets the input field's visual appearance to its normal state by clearing
        /// all transition colors and restoring the default label color.
        /// </summary>
        /// <param name="inputField">The input field whose visuals will be reset.</param>
        public static void SetNormalState(this TMP_InputField inputField)
        {
            SetAllTransitionColors(inputField, Color.clear);
            SetLabelColor(inputField, Color.white);
        }

        /// <summary>
        /// Sets the color of the label associated with this input field.
        /// The label is searched as a sibling named "Label" under the same parent.
        /// </summary>
        /// <param name="inputField">The input field whose label color will be updated.</param>
        /// <param name="color">The new color for the label.</param>
        private static void SetLabelColor(TMP_InputField inputField, Color color)
        {
            Transform parent = inputField.transform.parent;
            if (parent != null && parent.Find("Label") != null)
            {
                TextMeshProUGUI label = parent.Find("Label").GetComponent<TextMeshProUGUI>();
                label.color = color;
            }
        }
    }
}