using Michsky.UI.ModernUIPack;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace SEE.UI.Drawable
{
    /// <summary>
    /// A controller for a slider that uses a float value.
    /// </summary>
    public class FloatValueSliderController : MonoBehaviour
    {
        /// <summary>
        /// The slider manager.
        /// It contains the slider in manager.mainSlider.
        /// When the value of the main slider changes (meaning the player moved the slider), the value
        /// will be set to the value of this slider.
        /// </summary>
        private SliderManager manager;

        /// <summary>
        /// The tmp text that shows the value of the slider.
        /// </summary>
        private TMP_Text tmpText;

        /// <summary>
        /// Configuration for the decimal places for this slider.
        /// Allows a range from 0 to 4.
        /// </summary>
        [SerializeField, Range(0, 4)]
        private int decimalPlaces = 0;

        /// <summary>
        /// Option to normalize the value of the slider.
        /// </summary>
        [SerializeField]
        private bool valueNormalized = true;

        /// <summary>
        /// Option if the slider should be aligned left.
        /// Otherwise the handler is in the middle of the slider.
        /// </summary>
        [SerializeField]
        private bool sliderLeftAligned = true;

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
            tmpText = GetComponentsInChildren<TMP_Text>()[1];
            manager.mainSlider.onValueChanged.AddListener(SliderChanged);
            tmpText.text = manager.mainSlider.value.ToString("F" + decimalPlaces);

        }

        /// <summary>
        /// Removes the handler of the slider.
        /// </summary>
        private void OnDestroy()
        {
            manager.mainSlider.onValueChanged.RemoveListener(SliderChanged);
        }

        /// <summary>
        /// Handler method for changing the slider value.
        /// </summary>
        /// <param name="newValue">the new selected value</param>
        private void SliderChanged(float newValue)
        {
            /// Normalizes the value of the slider when the <see cref="valueNormalized"/> option is enabled.
            if (valueNormalized)
            {
                newValue = manager.mainSlider.normalizedValue;
            }
            else
            {
                /// Does not normalize the value.
                decimal value = (decimal)manager.mainSlider.value;
                newValue = (float)decimal.Round(value, decimalPlaces);
            }

            /// If the slider should be centered.
            if (!sliderLeftAligned)
            {
                newValue /= 100f;
            }

            /// Assigns the value to the text.
            tmpText.text = manager.mainSlider.value.ToString("F" + decimalPlaces);

            /// Executes the action.
            onValueChanged.Invoke(newValue);
        }

        /// <summary>
        /// Assigns a value to the slider and to the text.
        /// </summary>
        /// <param name="value">the value that should be assigned</param>
        public void AssignValue(float value)
        {
            tmpText.text = value.ToString();
            manager.mainSlider.value = value;
        }

        /// <summary>
        /// Resets the slider to its minimum based on <see cref="sliderLeftAligned"/>.
        /// </summary>
        public void ResetToMin()
        {
            if (sliderLeftAligned)
            {
                manager.mainSlider.value = manager.mainSlider.minValue;
            }
            else
            {
                manager.mainSlider.value = 0f;
            }
        }

        /// <summary>
        /// Gets the current value of the slider.
        /// </summary>
        /// <returns>the slider value.</returns>
        public float GetValue()
        {
            return manager.mainSlider.value;
        }
    }
}
