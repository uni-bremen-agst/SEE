using Michsky.UI.ModernUIPack;
using SEE.UI.Notification;
using System.Globalization;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace SEE.UI.Drawable
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
        [FormerlySerializedAs("onValueChanged")]
        public UnityEvent<float> OnValueChanged = new();

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
            inputField.onValueChanged.AddListener(InputChanged);
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
            inputField.onValueChanged.RemoveListener(InputChanged);
        }

        /// <summary>
        /// Handler method for changing the slider value.
        /// </summary>
        /// <param name="newValue">The new selected value.</param>
        private void SliderChanged(float newValue)
        {
            newValue = manager.mainSlider.value;
            if (newValue % 1 == 0)
            {
                inputField.text = newValue.ToString();
            }
            else
            {
                inputField.text = newValue.ToString("F1");
            }
            OnValueChanged.Invoke(newValue);
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
            if (text.Last() != '.' && float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out float value))
            {
                manager.mainSlider.value = value;
                SliderChanged(value);
            }
            else
            {
                if (text.Last() != '.')
                {
                    ShowNotification.Warn("Wrong format!", "The given text is no degree format.");
                }
            }
        }

        /// <summary>
        /// Assigns a value to the slider and to the input field.
        /// </summary>
        /// <param name="value">The value that should be assigned.</param>
        public void AssignValue(float value)
        {
            if (value % 1 == 0)
            {
                inputField.text = value.ToString();
            }
            else
            {
                inputField.text = value.ToString("F1");
            }
            manager.mainSlider.value = value;
        }
    }
}
