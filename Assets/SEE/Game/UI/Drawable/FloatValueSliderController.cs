using Assets.SEE.Game.Drawable;
using Michsky.UI.ModernUIPack;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System;

public class FloatValueSliderController : MonoBehaviour
{
    /// <summary>
    /// The slider manager. It contains the slider in manager.mainSlider.
    /// When the value of the main slider changes (meaning the player moved the slider), the value
    /// will be set to the value of this slider.
    /// </summary>
    private SliderManager manager;

    /// <summary>
    /// The tmp text that show's the value of the slider.
    /// </summary>
    private TMP_Text tmpText;

    /// <summary>
    /// Configuration for the decimal places for this slider.
    /// </summary>
    [SerializeField, Range(0, 4)]
    private int decimalPlaces = 0;

    /// <summary>
    /// Option to normalized the value of the slider.
    /// </summary>
    [SerializeField]
    private bool valueNormalized = true;

    /// <summary>
    /// Option if the slider should aligned left.
    /// Otherwise the handler is in the middle of the slider.
    /// </summary>
    [SerializeField]
    private bool sliderLeftAligned = true;

    /// <summary>
    /// On value changed action for the slider.
    /// </summary>
    [Header("Event")]
    public UnityEvent<float> onValueChanged = new UnityEvent<float>();

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
    /// Removes the handler for the slider.
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
        if (valueNormalized)
        {
            newValue = manager.mainSlider.normalizedValue;
        } else
        {
            decimal value = (decimal)manager.mainSlider.value;
            newValue = (float)Decimal.Round(value, decimalPlaces);
        }
        if (!sliderLeftAligned)
        {
            newValue /= 100f;
        }
        tmpText.text = manager.mainSlider.value.ToString("F" + decimalPlaces);
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
    /// Resets the slider to it's minimum.
    /// </summary>
    public void ResetToMin()
    {
        if (sliderLeftAligned)
        {
            manager.mainSlider.value = manager.mainSlider.minValue;
        } else
        {
            manager.mainSlider.value = 0f;
        }
    }

    /// <summary>
    /// Get's the current value of the slider.
    /// </summary>
    /// <returns>the slider value.</returns>
    public float GetValue()
    {
        return manager.mainSlider.value;
    }
}
