using Assets.SEE.Game.Drawable;
using Michsky.UI.ModernUIPack;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Globalization;

public class RotationSliderController : MonoBehaviour
{
    /// <summary>
    /// The slider object. When its value changes (meaning the player moved the slider), the layer of the selected line
    /// will be set to the value of this slider.
    /// </summary>
    [SerializeField]
    private Slider slider;

    [SerializeField]
    private TMP_InputField inputField;

    [Header("Event")]
    public UnityEvent<float> onValueChanged = new UnityEvent<float>();

    private void Awake()
    {
        slider = GetComponentInChildren<Slider>();
        inputField = GetComponentInChildren<TMP_InputField>();
        slider.onValueChanged.AddListener(SliderChanged);
        inputField.onEndEdit.AddListener(InputChanged);
        slider.minValue = 0f;
        slider.maxValue = 359.9f;
    }

    private void OnDestroy()
    {
        slider.onValueChanged.RemoveListener(SliderChanged);
        inputField.onEndEdit.RemoveListener(InputChanged);
    }

    private void SliderChanged(float newValue)
    {
        newValue = slider.value;
        inputField.text = newValue.ToString("F1");
        onValueChanged.Invoke(newValue);
    }

    private void InputChanged(string text)
    {
        text = text.Replace(",", ".");
        if (float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out float value))
        {
            slider.value = value;
            SliderChanged(value);
        } else
        {
            Debug.Log("The given text is no degree format.");
        }
    }

    public void AssignValue(float value)
    {
        inputField.text = value.ToString();
        slider.value = value;
    }
}
