using System;
using System.Collections.Generic;
using Michsky.UI.ModernUIPack;
using SEE.GO;
using TMPro;
using UnityEngine;

namespace SEE.Game.UI.ConfigMenu
{
    public class ColorPicker : DynamicUIBehaviour
    {
        private TextMeshProUGUI _labelText;
        private ButtonManagerBasicWithIcon _buttonManager;

        private readonly Queue<Color> _valueUpdates = new Queue<Color>();

        public ColorPickerControl colorPickerControl;
        public string label;
        public Action<Color> OnValueChange;

        public Color LatestSelectedColor {
            get;
            private set;
        }

        public Color Value { set => _valueUpdates.Enqueue(value); }

        void Update()
        {
            HandleQueuedUpdates();
        }

        void Start()
        {
            MustGetComponentInChild("Label", out _labelText);
            _labelText.text = label;

            MustGetComponentInChild("Trigger", out _buttonManager);
            _buttonManager.clickEvent.AddListener(() => colorPickerControl.RequestControl(this));

            HandleQueuedUpdates();
        }

        private void HandleQueuedUpdates()
        {
            if (_valueUpdates.Count > 0)
            {
                Color newColor = _valueUpdates.Dequeue();
                LatestSelectedColor = newColor;
                ReflectColorUpdate();
                colorPickerControl.AskForColorUpdate(this, newColor);
            }
        }

        private void ReflectColorUpdate()
        {
            _buttonManager.normalText.text = ColorUtility.ToHtmlStringRGB(LatestSelectedColor);
            _buttonManager.normalImage.color = LatestSelectedColor;
        }

        public void OnPickerHostColorChange(Color color)
        {
            LatestSelectedColor = color;
            ReflectColorUpdate();
            OnValueChange(color);
        }
    }

    public class ColorPickerBuilder
    {

        private ColorPicker _colorPicker;

        private ColorPickerBuilder(ColorPicker colorPicker)
        {
            _colorPicker = colorPicker;
        }

        public static ColorPickerBuilder Init(GameObject colorPickerHost)
        {
            colorPickerHost.AddComponent<ColorPicker>();
            colorPickerHost.MustGetComponent(out ColorPicker colorPicker);
            return new ColorPickerBuilder(colorPicker);
        }

        public ColorPicker Build() => _colorPicker;

        public ColorPickerBuilder SetLabel(string label)
        {
            _colorPicker.label = label;
            return this;
        }

        public ColorPickerBuilder SetOnChangeHandler(Action<Color> onChangeHandler)
        {
            _colorPicker.OnValueChange = onChangeHandler;
            return this;
        }

        public ColorPickerBuilder SetDefaultValue(Color defaultValue)
        {
            _colorPicker.Value = defaultValue;
            return this;
        }

        public ColorPickerBuilder SetColorPickerControl(ColorPickerControl colorPickerControl)
        {
            _colorPicker.colorPickerControl = colorPickerControl;
            return this;
        }
    }
}
