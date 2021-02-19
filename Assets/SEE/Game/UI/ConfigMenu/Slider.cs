using System;
using System.Collections.Generic;
using Michsky.UI.ModernUIPack;
using SEE.GO;
using TMPro;
using UnityEngine;

namespace SEE.Game.UI.ConfigMenu
{
    public enum SliderMode
    {
        Integer,
        Float
    }

    [RequireComponent(typeof(SliderManager))]
    public class Slider : DynamicUIBehaviour
    {
        public String label;
        public (float Min, float Max) range = (0f, 10f);
        public SliderMode sliderMode = SliderMode.Float;

        private SliderManager _sliderManager;
        private TextMeshProUGUI _label;

        private readonly Queue<float> _valueUpdates = new Queue<float>();

        public float Value {
            set => _valueUpdates.Enqueue(value);
        }

        void Start()
        {
            MustGetComponentInChild("Label", out _label);
            _label.text = label;

            MustGetComponentInChild("Slider", out _sliderManager);
            _sliderManager.mainSlider.onValueChanged.AddListener(
                newValue => OnValueChange(newValue));
            _sliderManager.mainSlider.minValue = range.Min;
            _sliderManager.mainSlider.maxValue = range.Max;
            _sliderManager.usePercent = false;
            _sliderManager.useRoundValue = sliderMode == SliderMode.Integer;
        }

        void Update()
        {
            if (_valueUpdates.Count > 0)
            {
                float newValue = _valueUpdates.Dequeue();
                _sliderManager.mainSlider.value = newValue;
            }
        }

        public Action<float> OnValueChange { get; set; }
    }

    public class SliderBuilder
    {
        private readonly Slider _slider;

        private SliderBuilder(Slider slider)
        {
            _slider = slider;
        }

        public static SliderBuilder Init(GameObject sliderHost)
        {
            sliderHost.AddComponent<Slider>();
            sliderHost.MustGetComponent(out Slider slider);
            return new SliderBuilder(slider);
        }

        public Slider Build() => _slider;

        public SliderBuilder SetLabel(string label)
        {
            _slider.label = label;
            return this;
        }

        public SliderBuilder SetOnChangeHandler(Action<float> onChangeHandler)
        {
            _slider.OnValueChange = onChangeHandler;
            return this;
        }

        public SliderBuilder SetRange((float Min, float Max) range)
        {
            _slider.range = range;
            return this;
        }

        public SliderBuilder SetDefaultValue(float defaultValue)
        {
            _slider.Value = defaultValue;
            return this;
        }
        public SliderBuilder SetMode(SliderMode sliderMode)
        {
            _slider.sliderMode = sliderMode;
            return this;
        }
    }
}
