using Michsky.UI.ModernUIPack;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace SEE.Game.UI.Drawable
{
    /// <summary>
    /// The controller for the rotation slider.
    /// </summary>
    public class RotationSliderController : MonoBehaviour
    {
        /// <summary>
        /// The slider manager. 
        /// It contains the slider in manager.mainSlider.
        /// When the value of the main slider changes (meaning the player moved the slider), the value
        /// will be set to the value of this slider.
        /// </summary>
        [SerializeField]
        private SliderManager manager;

        /// <summary>
        /// The input field that show's the value of the slider.
        /// The rotation can also be changed through the input field.
        /// </summary>
        [SerializeField]
        private TMP_InputField inputField;

        /// <summary>
        /// Action that is executed when the value of the slider changes.
        /// </summary>
        [Header("Event")]
        public UnityEvent<float> onValueChanged = new();

        /// <summary>
        /// Initializes the slider controller.
        /// </summary>
        private void Awake()
        {
            manager = GetComponentInChildren<SliderManager>();
            inputField = GetComponentInChildren<TMP_InputField>();
            /// Adds the slider change method
            manager.mainSlider.onValueChanged.AddListener(SliderChanged);
            /// Adds the input field change method.
            inputField.onEndEdit.AddListener(InputChanged);
            /// Minimum degree
            manager.mainSlider.minValue = 0f;
            /// Maximum degree
            manager.mainSlider.maxValue = 359.9f;
        }

        /// <summary>
        /// Removes the handler of the slider and the input field.
        /// </summary>
        private void OnDestroy()
        {
            manager.mainSlider.onValueChanged.RemoveListener(SliderChanged);
            inputField.onEndEdit.RemoveListener(InputChanged);
        }

        /// <summary>
        /// Handler method for changing the slider value.
        /// </summary>
        /// <param name="newValue">the new selected value</param>
        private void SliderChanged(float newValue)
        {
            newValue = manager.mainSlider.value;
            inputField.text = newValue.ToString("F1");
            onValueChanged.Invoke(newValue);
        }

        /// <summary>
        /// Handler for changing the input of the input field.
        /// It invokes a <see cref="SliderChanged"/>.
        /// </summary>
        /// <param name="text">The new value.</param>
        private void InputChanged(string text)
        {
            text = text.Replace(",", ".");
            /// Try to parse the text into a float.
            if (float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out float value))
            {
                manager.mainSlider.value = value;
                SliderChanged(value);
            }
            else
            {
                Debug.Log("The given text is no degree format.");
            }
        }

        /// <summary>
        /// Assigns a value to the slider and to the input field.
        /// </summary>
        /// <param name="value">the value that should be assigned</param>
        public void AssignValue(float value)
        {
            inputField.text = value.ToString();
            manager.mainSlider.value = value;
        }
    }
}
