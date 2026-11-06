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
        /// Whether the slider controller has already initialized its UI references
        /// and event handlers.
        /// </summary>
        private bool initialized;

        /// <summary>
        /// Initializes the slider controller.
        /// </summary>
        private void Awake()
        {
            Initialize();
        }

        /// <summary>
        /// Initializes the UI references and event handlers of this slider controller.
        /// This method is idempotent so that the controller can also be initialized
        /// before Unity invokes <see cref="Awake"/>.
        /// </summary>
        /// <exception cref="MissingComponentException">
        /// Thrown if the required slider manager, slider, or input field cannot be found.
        /// </exception>
        private void Initialize()
        {
            if (initialized)
            {
                return;
            }

            manager = GetComponentInChildren<SliderManager>();
            inputField = GetComponentInChildren<TMP_InputField>();

            if (manager == null)
            {
                throw new MissingComponentException(
                    $"{nameof(RotationSliderController)} requires a {nameof(SliderManager)}.");
            }

            if (manager.mainSlider == null)
            {
                throw new MissingComponentException(
                    $"{nameof(SliderManager)} requires a slider.");
            }

            if (inputField == null)
            {
                throw new MissingComponentException(
                    $"{nameof(RotationSliderController)} requires a {nameof(TMP_InputField)}.");
            }

            manager.mainSlider.onValueChanged.AddListener(SliderChanged);
            inputField.onValueChanged.AddListener(InputChanged);

            manager.mainSlider.minValue = 0.0f;
            manager.mainSlider.maxValue = 359.9f;

            initialized = true;
        }

        /// <summary>
        /// Removes the handlers of the slider and input field.
        /// </summary>
        private void OnDestroy()
        {
            if (!initialized)
            {
                return;
            }

            if (manager != null && manager.mainSlider != null)
            {
                manager.mainSlider.onValueChanged.RemoveListener(SliderChanged);
            }

            if (inputField != null)
            {
                inputField.onValueChanged.RemoveListener(InputChanged);
            }

            initialized = false;
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
            Initialize();

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
