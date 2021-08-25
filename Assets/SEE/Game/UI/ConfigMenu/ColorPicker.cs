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
    /// <summary>
    /// The wrapper script for a color picker. This component doesn't actually own the color picker
    /// component nor renders it. This script communicates with a controller script to show/hide and
    /// manipulate the value of a singleton color picker game object.
    /// </summary>
    public class ColorPicker : DynamicUIBehaviour
    {
        private TextMeshProUGUI labelText;
        private ButtonManagerBasicWithIcon buttonManager;

        private readonly Queue<Color> valueUpdates = new Queue<Color>();

        /// <summary>
        /// The controller script of the singleton color picker.
        /// </summary>
        public ColorPickerControl ColorPickerControl;

        /// <summary>
        /// The label of the component.
        /// </summary>
        public string Label;

        public Action<Color> OnValueChange;

        /// <summary>
        /// Holds the latest selected color.
        /// This can come from user input or from programmatic changes and is useful for displaying
        /// the selected color.
        /// </summary>
        public Color LatestSelectedColor
        {
            get;
            private set;
        }

        /// <summary>
        /// The color that should be displayed by the color picker.
        /// </summary>
        public Color Value { set => valueUpdates.Enqueue(value); }

        private void Update()
        {
            HandleQueuedUpdates();
        }

        private void Start()
        {
            MustGetComponentInChild("Label", out labelText);
            labelText.text = Label;

            MustGetComponentInChild("Trigger", out buttonManager);
            buttonManager.clickEvent.AddListener(() => ColorPickerControl.RequestControl(this));

            HandleQueuedUpdates();
        }

        private void HandleQueuedUpdates()
        {
            if (valueUpdates.Count > 0)
            {
                Color newColor = valueUpdates.Dequeue();
                LatestSelectedColor = newColor;
                ReflectColorUpdate();
                ColorPickerControl.AskForColorUpdate(this, newColor);
            }
        }

        private void ReflectColorUpdate()
        {
            buttonManager.normalText.text = ColorUtility.ToHtmlStringRGB(LatestSelectedColor);
            buttonManager.normalImage.color = LatestSelectedColor;
        }

        public void OnPickerHostColorChange(Color color)
        {
            LatestSelectedColor = color;
            ReflectColorUpdate();
            OnValueChange(color);
        }
    }

    /// <summary>
    /// Instantiates a new color picker game object via prefab and sets the wrapper script.
    /// </summary>
    public class ColorPickerBuilder : UIBuilder<ColorPicker>
    {
        protected override string PrefabPath => "Prefabs/UI/Input Group - Color Picker";

        private ColorPickerBuilder(Transform parent) : base(parent)
        {
        }

        public static ColorPickerBuilder Init(Transform parent)
        {
            return new ColorPickerBuilder(parent);
        }

        public ColorPickerBuilder SetLabel(string label)
        {
            Instance.Label = label;
            return this;
        }

        public ColorPickerBuilder SetOnChangeHandler(Action<Color> onChangeHandler)
        {
            Instance.OnValueChange = onChangeHandler;
            return this;
        }

        public ColorPickerBuilder SetDefaultValue(Color defaultValue)
        {
            Instance.Value = defaultValue;
            return this;
        }

        public ColorPickerBuilder SetColorPickerControl(ColorPickerControl colorPickerControl)
        {
            Instance.ColorPickerControl = colorPickerControl;
            return this;
        }
    }
}
