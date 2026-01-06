using Michsky.UI.ModernUIPack;
using SEE.UI.Notification;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace SEE.UI.Drawable
{
    /// <summary>
    /// This class provides a controller for a combination
    /// of an input field with an up and a down button.
    /// </summary>
    public class InputFieldWithButtons : MonoBehaviour
    {
        /// <summary>
        /// Is the input field of this component.
        /// </summary>
        private TMP_InputField inputField;

        /// <summary>
        /// Is the button up of this component.
        /// </summary>
        private ButtonManagerBasic upBtn;

        /// <summary>
        /// Is the button down of this component.
        /// </summary>
        private ButtonManagerBasic downBtn;

        /// <summary>
        /// Is the current value of the input field.
        /// </summary>
        private float value;

        /// <summary>
        /// The value with wich the up and down button should calculate.
        /// </summary>
        [SerializeField]
        private float upAndDownValue = 0.01f;

        /// <summary>
        /// The minimum value for the input field.
        /// </summary>
        [SerializeField]
        private float minValue = 0.15f;

        /// <summary>
        /// The maximum value for the input field.
        /// </summary>
        [SerializeField]
        private float maxValue = 3f;

        /// <summary>
        /// The action to be executed when the input field is edited.
        /// </summary>
        [Header("Event")]
        [FormerlySerializedAs("onValueChanged")]
        public UnityEvent<float> OnValueChanged = new();

        /// <summary>
        /// The action event for another input field.
        /// When the fields are intended to increase proportionally.
        /// </summary>
        [Header("Event")]
        [FormerlySerializedAs("onProportionalValueChanged")]
        public UnityEvent<float> OnProportionalValueChanged = null;

        /// <summary>
        /// Get and sets up the input field and the buttons.
        /// </summary>
        private void Awake()
        {
            inputField = GetComponentInChildren<TMP_InputField>();
            GameObject up = transform.Find("UpDown").Find("UpBtn").gameObject;
            upBtn = up.GetComponent<ButtonManagerBasic>();

            GameObject down = transform.Find("UpDown").Find("DownBtn").gameObject;
            downBtn = down.GetComponent<ButtonManagerBasic>();
            /// Adds the handler for the normal left click.
            upBtn.clickEvent.AddListener(ClickUp);
            /// Adds the component for the option that the button can be holded (right click).
            up.AddComponent<ButtonHeld>().SetAction(ClickUp);
            /// Adds the handler for the normal left click.
            downBtn.clickEvent.AddListener(ClickDown);
            /// Adds the component for the option that the button can be holded (right click).
            down.AddComponent<ButtonHeld>().SetAction(ClickDown);
            /// Adds a hover tool tip to the buttons.
            up.AddComponent<UIHoverTooltip>().SetMessage("Left mouse button for a single click, " +
                "right mouse button can be held down (performs multiple steps).");
            down.AddComponent<UIHoverTooltip>().SetMessage("Left mouse button for a single click, " +
                "right mouse button can be held down (performs multiple steps).");

            inputField.onEndEdit.AddListener(ValueChanged);
        }

        /// <summary>
        /// The initial onEndEditEvent for the input field.
        /// It displayed the value and invoke the specific onValueChanged Event
        /// </summary>
        /// <param name="newValue">Is the new value for the input field. It must be between the minimum and maximum range.
        /// Otherwise it is set of the respective limit.</param>
        private void ValueChanged(string newValue)
        {
            float oldValue = value;
            newValue = inputField.text.Replace(".", ",");
            /// If the new value can't be parse in a float, then show a notification and
            /// set the default value.
            if (!float.TryParse(newValue, out value))
            {
                ShowNotification.Warn("Wrong format", "The input field only allows float format.");
                value = oldValue;
                inputField.text = value.ToString();
            }

            /// If the value would be less then the <see cref="minValue"/>.
            /// Set to <see cref="minValue"/>.
            if (value < minValue)
            {
                value = minValue;
                inputField.text = value.ToString();
            }

            /// If the value would be greater then the <see cref="maxValue"/>.
            /// Set to <see cref="maxValue"/>.
            if (value > maxValue)
            {
                value = maxValue;
                inputField.text = value.ToString();
            }

            /// If an action for proportional increase is provided, execute it.
            OnProportionalValueChanged?.Invoke(value - oldValue);
            OnValueChanged.Invoke(value);
        }

        /// <summary>
        /// Assigns a value to the input field.
        /// </summary>
        /// <param name="assignValue">The value that should assigned.</param>
        public void AssignValue(float assignValue)
        {
            /// If the value would be less then the <see cref="minValue"/>.
            /// Set to <see cref="minValue"/>.
            if (assignValue < minValue)
            {
                assignValue = minValue;
            }

            /// If the value would be greater then the <see cref="maxValue"/>.
            /// Set to <see cref="maxValue"/>.
            if (assignValue > maxValue)
            {
                assignValue = maxValue;
            }

            value = assignValue;
            inputField.text = value.ToString();
        }

        /// <summary>
        /// OnClick event for the up button.
        /// </summary>
        private void ClickUp()
        {
            /// Assigns the new value.
            /// Increase by <see cref="upAndDownValue"/>.
            value += upAndDownValue;
            value = (float)decimal.Round((decimal)value, GetDecimalPlaces());

            /// If the maximum is exceeded, set it to the maximum.
            if (value > maxValue) { value = maxValue; }

            /// Assigns the value.
            AssignValue(value);

            /// If an action for proportional increase is provided, execute it.
            OnProportionalValueChanged?.Invoke(+upAndDownValue);
            OnValueChanged.Invoke(value);
        }

        /// <summary>
        /// OnClick event for the down button.
        /// </summary>
        private void ClickDown()
        {
            /// Assigns the new value.
            /// Decrease by <see cref="upAndDownValue"/>.
            value -= upAndDownValue;
            value = (float)decimal.Round((decimal)value, GetDecimalPlaces());

            /// If the minimum is undershot, set it to the minimum.
            if (value < minValue) { value = minValue; }

            /// Assigns the value
            AssignValue(value);

            /// If an action for proportional increase is provided, execute it.
            OnProportionalValueChanged?.Invoke(-upAndDownValue);
            OnValueChanged.Invoke(value);
        }

        /// <summary>
        /// Gets the decimal places of <see cref="upAndDownValue"/>.
        /// </summary>
        /// <returns>Tht decimal places of <see cref="upAndDownValue"/>.</returns>
        private int GetDecimalPlaces()
        {
            string text = upAndDownValue.ToString(CultureInfo.InvariantCulture);
            int index = text.IndexOf('.');
            if (index < 0) return 0;
            return text.Length - index - 1;
        }

        /// <summary>
        /// Gets the current value.
        /// </summary>
        /// <returns>The value.</returns>
        public float GetValue()
        {
            return value;
        }

        /// <summary>
        /// Sets the <see cref="upAndDownValue"/>.
        /// </summary>
        /// <param name="value">The new value for <see cref="upAndDownValue"/>.</param>
        public void SetUpAndDownValue(float value)
        {
            upAndDownValue = value;
        }

        /// <summary>
        /// Sets the <see cref="minValue"/>.
        /// </summary>
        /// <param name="minValue">The new minimum value.</param>
        public void SetMinValue(float minValue)
        {
            this.minValue = minValue;
        }

        /// <summary>
        /// Sets the <see cref="maxValue"/>.
        /// </summary>
        /// <param name="maxValue">The new maximum value.</param>
        public void SetMaxValue(float maxValue)
        {
            this.maxValue = maxValue;
        }
    }
}
