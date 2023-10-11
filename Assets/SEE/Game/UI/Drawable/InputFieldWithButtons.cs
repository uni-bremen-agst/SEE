using SEE.Game.UI.Notification;
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
        private Button upBtn;
        /// <summary>
        /// Is the displayed down button of this component.
        /// </summary>
        private Button downBtn;
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
        /// Get and sets up the input field and the buttons.
        /// </summary>
        private void Awake()
        {
            inputField = GetComponentInChildren<TMP_InputField>();
            upBtn = transform.Find("UpDown").Find("UpBtn").GetComponent<Button>();
            downBtn = transform.Find("UpDown").Find("DownBtn").GetComponent<Button>();
            upBtn.onClick.AddListener(clickUp);
            downBtn.onClick.AddListener(clickDown);
           // inputField.onSubmit.AddListener(ValueChanged);
           // inputField.onDeselect.AddListener(ValueChanged);
            inputField.onEndEdit.AddListener(ValueChanged);
        }

        /// <summary>
        /// The initial onChangeEvent for the input field.
        /// It displayed the value and invoke the specific onValueChanged Event
        /// </summary>
        /// <param name="newValue">is the new value for the input field. It must be between the minimum and maximum range. Otherwise it is set of the respective limit.</param>
        private void ValueChanged(string newValue)
        {
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
            onValueChanged.Invoke(value);
        }

        /// <summary>
        /// Assigns a value to the input field.
        /// </summary>
        /// <param name="assignValue">The value that should assigned.</param>
        public void AssignValue(float assignValue)
        {
            value = assignValue;
            inputField.text = value.ToString();
        }

        /// <summary>
        /// OnClick event for the up button.
        /// </summary>
        private void clickUp()
        {
            value += upAndDownValue;
            if (value > maxValue) { value = maxValue; }
            AssignValue(value);
        }

        /// <summary>
        /// OnClick event for the down button.
        /// </summary>
        private void clickDown()
        {
            value -= upAndDownValue;
            if (value < minValue) { value = minValue; }
            AssignValue(value);
        }
    }
}