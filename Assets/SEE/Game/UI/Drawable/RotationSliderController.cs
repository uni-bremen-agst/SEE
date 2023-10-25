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
    /// The slider manager. It contains the slider. 
    /// When the value of the slider changes (meaning the player moved the slider), the rotation of the selected drawable type object
    /// will be set to the value of this slider.
    /// </summary>
    [SerializeField]
    private SliderManager manager;

    [SerializeField]
    private TMP_InputField inputField;

    [Header("Event")]
    public UnityEvent<float> onValueChanged = new UnityEvent<float>();

    private void Awake()
    {
        manager = GetComponentInChildren<SliderManager>();
        inputField = GetComponentInChildren<TMP_InputField>();
        manager.mainSlider.onValueChanged.AddListener(SliderChanged);
        inputField.onEndEdit.AddListener(InputChanged);
        manager.mainSlider.minValue = 0f;
        manager.mainSlider.maxValue = 359.9f;
    }

    private void OnDestroy()
    {
        manager.mainSlider.onValueChanged.RemoveListener(SliderChanged);
        inputField.onEndEdit.RemoveListener(InputChanged);
    }

    private void SliderChanged(float newValue)
    {
        newValue = manager.mainSlider.value;
        inputField.text = newValue.ToString("F1");
        onValueChanged.Invoke(newValue);
    }

    private void InputChanged(string text)
    {
        text = text.Replace(",", ".");
        if (float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out float value))
        {
            manager.mainSlider.value = value;
            SliderChanged(value);
        } else
        {
            Debug.Log("The given text is no degree format.");
        }
    }

    public void AssignValue(float value)
    {
        inputField.text = value.ToString();
        manager.mainSlider.value = value;
    }
}
