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
    private SliderManager manager;

    private TMP_Text tmpText;

    [Header("Event")]
    public UnityEvent<int> onValueChanged = new UnityEvent<int>();

    private void Awake()
    {
        manager = GetComponentInChildren<SliderManager>();
        tmpText = GetComponentsInChildren<TMP_Text>()[1];
        tmpText.text = manager.mainSlider.value.ToString();
        manager.mainSlider.onValueChanged.AddListener(SliderChanged);
        manager.mainSlider.wholeNumbers = true;
    }

    private void OnDestroy()
    {
        manager.mainSlider.onValueChanged.RemoveListener(SliderChanged);
    }

    private void SliderChanged(float newValue)
    {
        newValue = manager.mainSlider.value;
        tmpText.text = ((int)newValue).ToString();
        onValueChanged.Invoke((int)newValue);
    }

    public void AssignValue(int value)
    {
        tmpText.text = value.ToString();
        manager.mainSlider.value = value;
    }
    public void ResetToMin()
    {
        manager.mainSlider.value = manager.mainSlider.minValue;
    }

    public int GetValue()
    {
        return (int)manager.mainSlider.value;
    }
}
