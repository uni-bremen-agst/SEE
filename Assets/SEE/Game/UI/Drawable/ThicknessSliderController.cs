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
    /// The slider manager. It contains the slider
    /// When its value changes (meaning the player moved the slider), the thickness of the selected line
    /// will be set to the value of this slider.
    /// </summary>
    [SerializeField]
    private SliderManager manager;

    [SerializeField]
    private TMP_Text tmpText;

    [Header("Event")]
    public UnityEvent<float> onValueChanged = new UnityEvent<float>();

    private void Awake()
    {
        manager = GetComponentInChildren<SliderManager>();
        tmpText = GetComponentsInChildren<TMP_Text>()[1];
        manager.mainSlider.onValueChanged.AddListener(SliderChanged);
        manager.mainSlider.minValue = 0.001f;
        manager.mainSlider.maxValue = 0.1f;//0.04f;
    }

    private void OnDestroy()
    {
        manager.mainSlider.onValueChanged.RemoveListener(SliderChanged);
    }

    private void SliderChanged(float newValue)
    {
        newValue = manager.mainSlider.value;
        tmpText.text = newValue.ToString("F2");
        onValueChanged.Invoke(newValue);
    }

    public void AssignValue(float value)
    {
        tmpText.text = value.ToString("F2");
        manager.mainSlider.value = value;
    }
}
