using Assets.SEE.Game.Drawable;
using Michsky.UI.ModernUIPack;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class LayerSliderController : MonoBehaviour
{
    /// <summary>
    /// The slider manager. 
    /// It contains the slider.
    /// When its value changes (meaning the player moved the slider), the layer of the selected drawable type object
    /// will be set to the value of this slider.
    /// </summary>
    [SerializeField] 
    private SliderManager manager;

    [SerializeField]
    private TMP_Text tmpText;

    [Header("Event")]
    public UnityEvent<int> onValueChanged = new UnityEvent<int>();

    private void Awake()
    {
        manager = GetComponentInChildren<SliderManager>();
        tmpText = GetComponentsInChildren<TMP_Text>()[1];
        manager.mainSlider.onValueChanged.AddListener(SliderChanged);
        manager.mainSlider.minValue = 0;
    }

    private void Start()
    {
        manager.mainSlider.maxValue = ValueHolder.currentOrderInLayer;
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
}
