using Michsky.UI.ModernUIPack;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace SEE.UI.Drawable
{
    /// <summary>
    /// Provides the controller for the line thickness.
    /// </summary>
    public class ThicknessSliderController : MonoBehaviour
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
        /// The text which shows the slider value.
        /// </summary>
        [SerializeField]
        private TMP_Text tmpText;

        /// <summary>
        /// The unity event which will executed when the slider value changes.
        /// </summary>
        [Header("Event")]
        [FormerlySerializedAs("onValueChanged")]
        public UnityEvent<float> OnValueChanged = new();

        /// <summary>
        /// Initializes the Thickness Slider Controller
        /// and sets the values for the minimum and maximum values.
        /// </summary>
        private void Awake()
        {
            manager = GetComponentInChildren<SliderManager>();
            tmpText = GetComponentsInChildren<TMP_Text>()[1];
            manager.mainSlider.onValueChanged.AddListener(SliderChanged);
            manager.mainSlider.minValue = 0.001f;
            manager.mainSlider.maxValue = 0.3f;
        }

        /// <summary>
        /// Removes the handler for the slider.
        /// </summary>
        private void OnDestroy()
        {
            manager.mainSlider.onValueChanged.RemoveListener(SliderChanged);
        }

        /// <summary>
        /// Executed when the slider value changes.
        /// The new value is added to the text to represent
        /// the slider value, and the associated event is invoked with this value.
        /// </summary>
        /// <param name="newValue">The new slider value.</param>
        private void SliderChanged(float newValue)
        {
            newValue = manager.mainSlider.value;
            tmpText.text = newValue.ToString("F2");
            OnValueChanged.Invoke(newValue);
        }

        /// <summary>
        /// Assigns the value will be assigned to the slider and the text.
        /// </summary>
        /// <param name="value">The value to be assign.</param>
        public void AssignValue(float value)
        {
            tmpText.text = value.ToString("F2");
            manager.mainSlider.value = value;
        }
    }
}
