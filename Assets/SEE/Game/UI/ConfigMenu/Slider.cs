using System;
using System.Collections.Generic;
using Michsky.UI.ModernUIPack;
using TMPro;
using UnityEngine;

namespace SEE.Game.UI.ConfigMenu
{
    public enum SliderMode
    {
        Integer,
        Float
    }

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
                newValue => OnValueChange(sliderMode == SliderMode.Integer
                                              ? (float)Math.Round(newValue) : newValue));
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

    public class SliderBuilder : BaseUiBuilder<Slider>
    {
        protected override string PrefabPath => "Assets/Prefabs/UI/Input Group - Slider.prefab";

        private SliderBuilder(Transform parent) : base(parent)
        {
        }

        public static SliderBuilder Init(Transform parent)
        {
            return new SliderBuilder(parent);
        }

        public SliderBuilder SetLabel(string label)
        {
            Instance.label = label;
            return this;
        }

        public SliderBuilder SetOnChangeHandler(Action<float> onChangeHandler)
        {
            Instance.OnValueChange = onChangeHandler;
            return this;
        }

        public SliderBuilder SetRange((float Min, float Max) range)
        {
            Instance.range = range;
            return this;
        }

        public SliderBuilder SetDefaultValue(float defaultValue)
        {
            Instance.Value = defaultValue;
            return this;
        }
        public SliderBuilder SetMode(SliderMode sliderMode)
        {
            Instance.sliderMode = sliderMode;
            return this;
        }
    }
}
