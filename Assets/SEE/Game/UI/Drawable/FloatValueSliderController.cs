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
    /// The slider object. When its value changes (meaning the player moved the slider), the layer of the selected line
    /// will be set to the value of this slider.
    /// </summary>
    private Slider slider;

    private TMP_Text tmpText;

    [SerializeField, Range(0, 4)]
    private int decimalPlaces = 0;

    [SerializeField]
    private bool valueNormalized = true;

    [SerializeField]
    private bool sliderLeftAligned = true;

    [Header("Event")]
    public UnityEvent<float> onValueChanged = new UnityEvent<float>();

    private void Awake()
    {
        slider = GetComponentInChildren<Slider>();
        tmpText = GetComponentsInChildren<TMP_Text>()[1];
        tmpText.text = slider.value.ToString("F" + decimalPlaces);
        slider.onValueChanged.AddListener(SliderChanged);
    }

    private void OnDestroy()
    {
        slider.onValueChanged.RemoveListener(SliderChanged);
    }

    private void SliderChanged(float newValue)
    {
        if (valueNormalized)
        {
            newValue = slider.normalizedValue;
        } else
        {
            decimal value = (decimal)slider.value;
            newValue = (float)Decimal.Round(value, decimalPlaces);
        }
        if (!sliderLeftAligned)
        {
            newValue /= 100f;
        }
        tmpText.text = slider.value.ToString("F" + decimalPlaces);
        onValueChanged.Invoke(newValue);
    }

    public void AssignValue(float value)
    {
        tmpText.text = value.ToString();
        slider.value = value;
    }

    public void ResetToMin()
    {
        if (sliderLeftAligned)
        {
            slider.value = slider.minValue;
        } else
        {
            slider.value = 0f;
        }
    }

    public float GetValue()
    {
        return slider.value;
    }
}
