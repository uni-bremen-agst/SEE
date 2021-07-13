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

using System;
using System.Collections.Generic;
using Michsky.UI.ModernUIPack;
using TMPro;
using UnityEngine;

namespace SEE.Game.UI.ConfigMenu
{
    public enum ComboSelectMode
    {
        Combo,
        Restricted,
    }

    /// <summary>
    /// The ComboSelect represents an input element that consists of a select/dropdown box to select
    /// from a predefined list of options but also allows the user to enter custom input. A label
    /// is also part of this component.
    ///
    /// Updates made to the selected value and all possible values are batched via queue.
    /// </summary>
    public class ComboSelect : DynamicUIBehaviour
    {
        private static string CustomInputText = "--Custom Input--";

        /// <summary>
        /// The label of the component.
        /// </summary>
        public string label;

        /// <summary>
        /// The event handler that gets invoked when the value of this input changes.
        /// </summary>
        public Action<string> OnValueChange;

        /// <summary>
        /// The mode in which this input operates.
        /// </summary>
        public ComboSelectMode mode = ComboSelectMode.Combo;

        private CustomDropdown _dropdown;
        private TMP_InputField _customInput;
        private TextMeshProUGUI _labelText;
        private Dictaphone _dictaphone;

        private readonly Queue<List<string>> _valuesUpdates = new Queue<List<string>>();
        private readonly Queue<string> _valueUpdates = new Queue<string>();

        /// <summary>
        /// The values (options) of this input.
        /// </summary>
        public List<string> Values
        {
            set => _valuesUpdates.Enqueue(value);
        }

        /// <summary>
        /// The value (currently selected option) of this input.
        /// </summary>
        public string Value
        {
            get => FigureOutValue();
            set => _valueUpdates.Enqueue(value);
        }

        void Awake()
        {
            MustGetComponentInChild("DropdownCombo/Dropdown", out _dropdown);
            MustGetComponentInChild("DropdownCombo/Input", out _customInput);
            MustGetComponentInChild("Label", out _labelText);
            MustGetComponentInChild("DropdownCombo/DictateButton", out _dictaphone);
        }

        void Start()
        {
            _dropdown.dropdownEvent.AddListener(arg0 =>
            {
                string selectedItem = _dropdown.dropdownItems[arg0].itemName;
                OnValueChange(Value);
                FigureOutInputMode(selectedItem);
            });
            _dropdown.isListItem = true;
            _dropdown.listParent = FindObjectOfType<Canvas>().transform;
            _labelText.text = label;

            _dictaphone.OnDictationFinished += text => _customInput.text = text;
        }

        void Update()
        {
            if (_valuesUpdates.Count > 0)
            {
                List<string> newValues = _valuesUpdates.Dequeue();
                _dropdown.dropdownItems.Clear();
                if (mode == ComboSelectMode.Combo)
                {
                    _dropdown.CreateNewItemFast(CustomInputText, null);
                }
                foreach (string s in newValues)
                {
                    _dropdown.CreateNewItemFast(s, null);
                }

                _dropdown.SetupDropdown();
            }

            if (_valueUpdates.Count > 0)
            {
                String newValue = _valueUpdates.Dequeue();
                FigureOutInputMode(newValue);
            }
        }

        void SetToCustomMode(string customValue)
        {
            _dropdown.selectedItemIndex =
                _dropdown.dropdownItems.FindIndex(item => item.itemName == CustomInputText);
            _dropdown.SetupDropdown();
            _customInput.gameObject.SetActive(true);
            _dictaphone.gameObject.SetActive(true);
            if (customValue != null)
            {
                _customInput.text = customValue;
            }
        }

        void SetToFixedMode(int newIndex)
        {
            _dropdown.selectedItemIndex = newIndex;
            _dropdown.SetupDropdown();
            _customInput.gameObject.SetActive(false);
            _dictaphone.gameObject.SetActive(false);
        }

        void FigureOutInputMode(string value)
        {
            // If the new value is already part of the items in the list, we simply select its index.
            int index = _dropdown.dropdownItems.FindIndex(item => item.itemName == value);
            if (index >= 0 && value != CustomInputText)
            {
                SetToFixedMode(index);
            }
            // The item is new, so we set the input to custom mode.
            else
            {
                SetToCustomMode(value == CustomInputText ? null : value);
            }
        }

        string FigureOutValue()
        {
            string item = _dropdown.dropdownItems[_dropdown.selectedItemIndex].itemName;
            if (item == CustomInputText)
            {
                return _customInput.text;
            }
            return item;
        }
    }

    /// <summary>
    /// Instantiates a new combo select game object via prefab and sets the wrapper script.
    /// </summary>
    public class ComboSelectBuilder : UiBuilder<ComboSelect>
    {
        protected override string PrefabPath => "Assets/Prefabs/UI/Input Group - Dropdown.prefab";

        private ComboSelectBuilder(Transform parent) : base(parent)
        {
        }

        public static ComboSelectBuilder Init(Transform parent)
        {
            return new ComboSelectBuilder(parent);
        }

        public ComboSelectBuilder SetLabel(string label)
        {
            Instance.label = label;
            return this;
        }

        public ComboSelectBuilder SetOnChangeHandler(Action<string> onChangeHandler)
        {
            Instance.OnValueChange = onChangeHandler;
            return this;
        }

        public ComboSelectBuilder SetAllowedValues(List<string> allowedValues)
        {
            Instance.Values = allowedValues;
            return this;
        }

        public ComboSelectBuilder SetDefaultValue(string defaultValue)
        {
            Instance.Value = defaultValue;
            return this;
        }

        public ComboSelectBuilder SetComboSelectMode(ComboSelectMode comboSelectMode)
        {
            Instance.mode = comboSelectMode;
            return this;
        }
    }
}
