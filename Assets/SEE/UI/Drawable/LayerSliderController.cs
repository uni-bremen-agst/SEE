using Michsky.UI.ModernUIPack;
using SEE.Game.Drawable;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace SEE.UI.Drawable
{
    /// <summary>
    /// The slider controller for the order in layer.
    /// </summary>
    public class LayerSliderController : MonoBehaviour
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
        /// The tmp text that show's the value of the slider.
        /// </summary>
        [SerializeField]
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
            manager.mainSlider.onValueChanged.AddListener(SliderChanged);
            manager.mainSlider.minValue = 0;
        }

        /// <summary>
        /// Sets the slider maximum to the current maximum order in layer value.
        /// </summary>
        private void Start()
        {
            manager.mainSlider.maxValue = ValueHolder.MaxOrderInLayer;
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
        /// <param name="newValue">The new selected value.</param>
        private void SliderChanged(float newValue)
        {
            newValue = manager.mainSlider.value;
            tmpText.text = ((int)newValue).ToString();
            OnValueChanged.Invoke((int)newValue);
        }

        /// <summary>
        /// Assigns a value to the slider and to the text.
        /// </summary>
        /// <param name="value">The value that should be assigned.</param>
        public void AssignValue(int value)
        {
            tmpText.text = value.ToString();
            manager.mainSlider.value = value;
        }

        /// <summary>
        /// Assigns a new max value to the slider.
        /// </summary>
        /// <param name="value">The new maximum value that should be assigned.</param>
        public void AssignMaxOrder(int value)
        {
            manager.mainSlider.maxValue = value;
            manager.UpdateUI();
        }
    }
}
