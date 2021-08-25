// Copyright 2021 Ruben Smidt
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS
// BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
// IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR
// IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using Michsky.UI.ModernUIPack;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace SEE.Game.UI.ConfigMenu
{
    public enum SliderMode
    {
        Integer,
        Float
    }

    /// <summary>
    /// The wrapper component for a numerical slider. It comes with a label that is displayed
    /// next to the input.
    ///
    /// This slider can be operated either with floats or integers.
    /// </summary>
    public class Slider : DynamicUIBehaviour
    {
        /// <summary>
        /// The label of this component.
        /// </summary>
        public string Label;

        /// <summary>
        /// The operational range of the slider.
        /// </summary>
        public (float Min, float Max) Range = (0f, 10f);

        /// <summary>
        /// The slider mode.
        /// </summary>
        public SliderMode SliderMode = SliderMode.Float;

        private SliderManager sliderManager;
        private TextMeshProUGUI label;

        private readonly Queue<float> valueUpdates = new Queue<float>();

        /// <summary>
        /// Requests an external value update.
        /// </summary>
        public float Value
        {
            set => valueUpdates.Enqueue(value);
        }
        private void Start()
        {
            MustGetComponentInChild("Label", out label);
            label.text = Label;

            MustGetComponentInChild("Slider", out sliderManager);
            sliderManager.mainSlider.onValueChanged.AddListener(
                newValue => OnValueChange(SliderMode == SliderMode.Integer
                                              ? (float)Math.Round(newValue) : newValue));
            sliderManager.mainSlider.minValue = Range.Min;
            sliderManager.mainSlider.maxValue = Range.Max;
            sliderManager.usePercent = false;
            sliderManager.useRoundValue = SliderMode == SliderMode.Integer;
        }
        private void Update()
        {
            if (valueUpdates.Count > 0)
            {
                float newValue = valueUpdates.Dequeue();
                sliderManager.mainSlider.value = newValue;
            }
        }

        /// <summary>
        /// The event handler that gets invoked when the value changes.
        /// </summary>
        public Action<float> OnValueChange { get; set; }
    }

    /// <summary>
    /// Instantiates a new slider game object via prefab and sets the wrapper script.
    /// </summary>
    public class SliderBuilder : UIBuilder<Slider>
    {
        protected override string PrefabPath => "Prefabs/UI/Input Group - Slider";

        private SliderBuilder(Transform parent) : base(parent)
        {
        }

        public static SliderBuilder Init(Transform parent)
        {
            return new SliderBuilder(parent);
        }

        public SliderBuilder SetLabel(string label)
        {
            Instance.Label = label;
            return this;
        }

        public SliderBuilder SetOnChangeHandler(Action<float> onChangeHandler)
        {
            Instance.OnValueChange = onChangeHandler;
            return this;
        }

        public SliderBuilder SetRange((float Min, float Max) range)
        {
            Instance.Range = range;
            return this;
        }

        public SliderBuilder SetDefaultValue(float defaultValue)
        {
            Instance.Value = defaultValue;
            return this;
        }
        public SliderBuilder SetMode(SliderMode sliderMode)
        {
            Instance.SliderMode = sliderMode;
            return this;
        }
    }
}
