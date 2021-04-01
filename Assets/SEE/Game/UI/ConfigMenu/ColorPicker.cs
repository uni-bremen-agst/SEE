using System;
using System.Collections.Generic;
using Michsky.UI.ModernUIPack;
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

    public class ColorPickerBuilder : UiBuilder<ColorPicker>
    {
        protected override string PrefabPath => "Assets/Prefabs/UI/Input Group - Color Picker.prefab";

        private ColorPickerBuilder(Transform parent) : base(parent)
        {
        }

        public static ColorPickerBuilder Init(Transform parent)
        {
            return new ColorPickerBuilder(parent);
        }

        public ColorPickerBuilder SetLabel(string label)
        {
            Instance.label = label;
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
            Instance.colorPickerControl = colorPickerControl;
            return this;
        }
    }
}
