using Michsky.UI.ModernUIPack;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace SEE.UI.Drawable
{
    /// <summary>
    /// A controller for a slider that uses an int value.
    /// </summary>
    public class IntValueSliderController : MonoBehaviour
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
        /// Action that is executed when the value of the slider changes.
        /// </summary>
        [Header("Event")]
        [FormerlySerializedAs("onValueChanged")]
        public UnityEvent<int> OnValueChanged = new();

        /// <summary>
        /// Initializes the slider controller.
        /// </summary>
        private void Awake()
        {
            manager = GetComponentInChildren<SliderManager>();
            tmpText = GetComponentsInChildren<TMP_Text>()[1];
            tmpText.text = manager.mainSlider.value.ToString();
            manager.mainSlider.onValueChanged.AddListener(SliderChanged);
            manager.mainSlider.wholeNumbers = true;
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
            newValue = manager.mainSlider.value;
            tmpText.text = ((int)newValue).ToString();
            OnValueChanged.Invoke((int)newValue);
        }

        /// <summary>
        /// Assigns a value to the slider and to the text.
        /// </summary>
        /// <param name="value">the value that should be assigned</param>
        public void AssignValue(int value)
        {
            tmpText.text = value.ToString();
            manager.mainSlider.value = value;
        }

        /// <summary>
        /// Resets the slider to its minimum.
        /// </summary>
        public void ResetToMin()
        {
            manager.mainSlider.value = manager.mainSlider.minValue;
        }

        /// <summary>
        /// Gets the current value of the slider.
        /// </summary>
        /// <returns>the slider value.</returns>
        public int GetValue()
        {
            return (int)manager.mainSlider.value;
        }
    }
}
