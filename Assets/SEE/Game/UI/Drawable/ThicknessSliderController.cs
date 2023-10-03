using Assets.SEE.Game.Drawable;
using Michsky.UI.ModernUIPack;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class ThicknessSliderController : MonoBehaviour
{
    /// <summary>
    /// The slider object. When its value changes (meaning the player moved the slider), the layer of the selected line
    /// will be set to the value of this slider.
    /// </summary>
    [SerializeField]
    private Slider slider;

    [SerializeField]
    private TMP_Text tmpText;

    [Header("Event")]
    public UnityEvent<float> onValueChanged = new UnityEvent<float>();

    private void Awake()
    {
        slider = GetComponentInChildren<Slider>();
        tmpText = GetComponentsInChildren<TMP_Text>()[1];
        slider.onValueChanged.AddListener(SliderChanged);
        slider.minValue = 0.001f;
        slider.maxValue = 0.1f;//0.04f;
    }

    private void OnDestroy()
    {
        slider.onValueChanged.RemoveListener(SliderChanged);
    }

    private void SliderChanged(float newValue)
    {
        newValue = slider.value;
        tmpText.text = newValue.ToString("F2");
        onValueChanged.Invoke(newValue);
    }

    public void AssignValue(float value)
    {
        tmpText.text = value.ToString("F2");
        slider.value = value;
    }
}
