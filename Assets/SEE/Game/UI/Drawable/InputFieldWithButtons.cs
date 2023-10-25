using Michsky.UI.ModernUIPack;
using SEE.Game.UI.Notification;
using System;
using System.Collections;
using System.Xml;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Assets.SEE.Game.UI.Drawable
{
    /// <summary>
    /// This class manages a combination of an input field with a up and a down button.
    /// </summary>
    public class InputFieldWithButtons : MonoBehaviour
    {
        /// <summary>
        /// Is the displayed input field of this component.
        /// </summary>
        private TMP_InputField inputField;
        /// <summary>
        /// Is the displayed up button of this component.
        /// </summary>
        private ButtonManagerBasic upBtn;
        /// <summary>
        /// Is the displayed down button of this component.
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
        /// An onValueChange event for the input field.
        /// </summary>
        [Header("Event")]
        public UnityEvent<float> onValueChanged = new UnityEvent<float>();

        /// <summary>
        /// An onValueChange event for another input field.
        /// </summary>
        [Header("Event")]
        public UnityEvent<float> onProportionalValueChanged = null;

        /// <summary>
        /// Get and sets up the input field and the buttons.
        /// </summary>
        private void Awake()
        {
            inputField = GetComponentInChildren<TMP_InputField>();
            upBtn = transform.Find("UpDown").Find("UpBtn").GetComponent<ButtonManagerBasic>();
            downBtn = transform.Find("UpDown").Find("DownBtn").GetComponent<ButtonManagerBasic>();
            upBtn.clickEvent.AddListener(clickUp);
            transform.Find("UpDown").Find("UpBtn").gameObject.AddComponent<ButtonHolded>().SetAction(clickUp);
            downBtn.clickEvent.AddListener(clickDown);
            transform.Find("UpDown").Find("DownBtn").gameObject.AddComponent<ButtonHolded>().SetAction(clickDown);
            inputField.onEndEdit.AddListener(ValueChanged);
        }

        /// <summary>
        /// The initial onEndEditEvent for the input field.
        /// It displayed the value and invoke the specific onValueChanged Event
        /// </summary>
        /// <param name="newValue">is the new value for the input field. It must be between the minimum and maximum range. Otherwise it is set of the respective limit.</param>
        private void ValueChanged(string newValue)
        {
            float oldValue = value;
            newValue = inputField.text;
            if (!float.TryParse(newValue, out value))
            {
                ShowNotification.Warn("Wrong format", "The input field only allows float format.");
                value = 0.5f;
            }
            if (value < minValue)
            {
                value = minValue;
                inputField.text = value.ToString();
            }
            if (value > maxValue)
            {
                value = maxValue;
                inputField.text = value.ToString();
            }
            if(onProportionalValueChanged != null)
            {
                onProportionalValueChanged.Invoke(value - oldValue);
            }
            onValueChanged.Invoke(value);
        }

        /// <summary>
        /// Assigns a value to the input field.
        /// </summary>
        /// <param name="assignValue">The value that should assigned.</param>
        public void AssignValue(float assignValue)
        {
            if (assignValue < minValue)
            {
                assignValue = minValue;
            }
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
        private void clickUp()
        {
            value += upAndDownValue;
            value = (float)Decimal.Round((decimal)value, 2);
            if (value > maxValue) { value = maxValue; }
            AssignValue(value);
            if (onProportionalValueChanged != null)
            {
                onProportionalValueChanged.Invoke(+upAndDownValue);
            }
            onValueChanged.Invoke(value);
        }

        /// <summary>
        /// OnClick event for the down button.
        /// </summary>
        private void clickDown()
        {
            value -= upAndDownValue;
            value = (float)Decimal.Round((decimal)value, 2);
            if (value < minValue) { value = minValue; }
            AssignValue(value);
            if (onProportionalValueChanged != null)
            {
                onProportionalValueChanged.Invoke(-upAndDownValue);
            }
            onValueChanged.Invoke(value);
        }

        /// <summary>
        /// Gets the current value.
        /// </summary>
        /// <returns>The value.</returns>
        public float GetValue()
        {
            return value;
        }
    }
}