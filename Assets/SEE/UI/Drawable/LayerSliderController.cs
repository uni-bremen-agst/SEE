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
        /// The slider manager containing the actual slider.
        /// </summary>
        [SerializeField]
        private SliderManager manager;

        /// <summary>
        /// The text displaying the current slider value.
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
        /// Whether this controller has already initialized its UI references
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
        /// This method is idempotent so that the controller can also be used before
        /// Unity invokes <see cref="Awake"/>.
        /// </summary>
        /// <exception cref="MissingComponentException">
        /// Thrown if one of the required slider controls cannot be found.
        /// </exception>
        private void Initialize()
        {
            if (initialized)
            {
                return;
            }

            manager = GetComponentInChildren<SliderManager>(true);
            TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(true);

            if (manager == null)
            {
                throw new MissingComponentException(
                    $"{nameof(LayerSliderController)} requires a {nameof(SliderManager)}.");
            }

            if (manager.mainSlider == null)
            {
                throw new MissingComponentException(
                    $"{nameof(SliderManager)} requires a slider.");
            }

            if (texts.Length < 2)
            {
                throw new MissingComponentException(
                    $"{nameof(LayerSliderController)} requires the text displaying the layer value.");
            }

            tmpText = texts[1];

            manager.mainSlider.onValueChanged.AddListener(SliderChanged);
            manager.mainSlider.minValue = 0;

            initialized = true;
        }

        /// <summary>
        /// Sets the default maximum order in layer after initialization.
        /// </summary>
        private void Start()
        {
            Initialize();
            manager.mainSlider.maxValue = ValueHolder.MaxOrderInLayer;
        }

        /// <summary>
        /// Removes the slider handler when this controller is destroyed.
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

            initialized = false;
        }

        /// <summary>
        /// Handles changes of the slider value.
        /// </summary>
        /// <param name="newValue">The newly selected value.</param>
        private void SliderChanged(float newValue)
        {
            newValue = manager.mainSlider.value;
            tmpText.text = ((int)newValue).ToString();
            OnValueChanged.Invoke((int)newValue);
        }

        /// <summary>
        /// Assigns a value to the slider and its displayed text.
        /// </summary>
        /// <param name="value">The value that should be assigned.</param>
        public void AssignValue(int value)
        {
            Initialize();

            tmpText.text = value.ToString();
            manager.mainSlider.value = value;
        }

        /// <summary>
        /// Assigns a new maximum value to the slider.
        /// </summary>
        /// <param name="value">The new maximum value.</param>
        public void AssignMaxOrder(int value)
        {
            Initialize();

            manager.mainSlider.maxValue = value;
            manager.UpdateUI();
        }
    }
}
