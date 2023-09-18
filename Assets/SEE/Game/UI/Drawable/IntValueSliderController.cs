using Assets.SEE.Game.Drawable;
using Michsky.UI.ModernUIPack;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class IntValueSliderController : MonoBehaviour
{
    /// <summary>
    /// The slider object. When its value changes (meaning the player moved the slider), the layer of the selected line
    /// will be set to the value of this slider.
    /// </summary>
    private Slider slider;

    private TMP_Text tmpText;

    [Header("Event")]
    public UnityEvent<int> onValueChanged = new UnityEvent<int>();

    private void Awake()
    {
        slider = GetComponentInChildren<Slider>();
        tmpText = GetComponentsInChildren<TMP_Text>()[1];
        tmpText.text = slider.value.ToString();
        slider.onValueChanged.AddListener(SliderChanged);
        slider.wholeNumbers = true;
    }

    private void OnDestroy()
    {
        slider.onValueChanged.RemoveListener(SliderChanged);
    }

    private void SliderChanged(float newValue)
    {
        newValue = slider.value;
        tmpText.text = ((int)newValue).ToString();
        onValueChanged.Invoke((int)newValue);
    }

    public void AssignValue(int value)
    {
        tmpText.text = value.ToString();
        slider.value = value;
    }
    public void ResetToMin()
    {
        slider.value = slider.minValue;
    }

    public int GetValue()
    {
        return (int)slider.value;
    }
}
