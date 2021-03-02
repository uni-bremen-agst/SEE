using System;
using System.Collections.Generic;
using Michsky.UI.ModernUIPack;
using SEE.GO;
using TMPro;
using UnityEngine;

namespace SEE.Game.UI.ConfigMenu
{
    public enum ComboSelectMode
    {
        Combo,
        Restricted,
    }

    public class ComboSelect : DynamicUIBehaviour
    {
        
        private CustomDropdown _dropdown;
        private TMP_InputField _customInput;
        private TextMeshProUGUI _labelText;

        private Queue<List<string>> _valuesUpdates = new Queue<List<string>>();
        private Queue<string> _valueUpdates = new Queue<string>();
        
        public string label;
        public Action<string> OnValueChange;
        public ComboSelectMode mode = ComboSelectMode.Combo;
        
        public static string CustomInputText = "--Custom Input--";
        
        public List<string> Values
        {
            set => _valuesUpdates.Enqueue(value);
        }

        public string Value
        {
            get => FigureOutValue();
            set => _valueUpdates.Enqueue(value);
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
                foreach (var s in newValues)
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

        void Start()
        {
            MustGetComponentInChild("DropdownCombo/Dropdown", out _dropdown);
            MustGetComponentInChild("DropdownCombo/Input", out _customInput);
            MustGetComponentInChild("Label", out _labelText);
            
            _dropdown.dropdownEvent.AddListener(arg0 =>
            {
                var selectedItem = _dropdown.dropdownItems[arg0].itemName;
                OnValueChange(Value);
                FigureOutInputMode(selectedItem);
            });
            _dropdown.isListItem = true;
            _dropdown.listParent = FindObjectOfType<Canvas>().transform;
            _labelText.text = label;
        }


        void SetToCustomMode(string customValue)
        {
            _dropdown.selectedItemIndex = _dropdown.dropdownItems.FindIndex(item => item.itemName == CustomInputText);
            _dropdown.SetupDropdown();
            _customInput.gameObject.SetActive(true);
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
        }

        void FigureOutInputMode(string value)
        {
            // If the new value is already part of the items in the list, we simply select its index.
            var index = _dropdown.dropdownItems.FindIndex(item => item.itemName == value);
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
            var item = _dropdown.dropdownItems[_dropdown.selectedItemIndex].itemName;
            if (item == CustomInputText)
            {
                return _customInput.text;
            }
            return item;
        }
    }

    public class ComboSelectBuilder
    {
        
        private ComboSelect _comboSelect;
        
        private ComboSelectBuilder(ComboSelect comboSelect)
        {
            _comboSelect = comboSelect;
        }

        public static ComboSelectBuilder Init(GameObject comboSelectHost)
        {
            comboSelectHost.AddComponent<ComboSelect>();
            comboSelectHost.MustGetComponent(out ComboSelect comboSelect);
            return new ComboSelectBuilder(comboSelect);
        }
        
        public ComboSelect Build() => _comboSelect;

        public ComboSelectBuilder SetLabel(string label)
        {
            _comboSelect.label = label;
            return this;
        }

        public ComboSelectBuilder SetOnChangeHandler(Action<string> onChangeHandler)
        {
            _comboSelect.OnValueChange = onChangeHandler;
            return this;
        }

        public ComboSelectBuilder SetAllowedValues(List<String> allowedValues)
        {
            _comboSelect.Values = allowedValues;
            return this;
        }
        
        public ComboSelectBuilder SetDefaultValue(String defaultValue)
        {
            _comboSelect.Value = defaultValue;
            return this;
        }

        public ComboSelectBuilder SetComboSelectMode(ComboSelectMode comboSelectMode)
        {
            _comboSelect.mode = comboSelectMode;
            return this;
        }
    }
}
