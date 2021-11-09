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
    /// Specifies what a user can do with the combo box.
    /// </summary>
    public enum ComboSelectMode
    {
        /// <summary>
        /// The user can select a value of a predefined set of values or
        /// enter a completely new value.
        /// </summary>
        Combo,
        /// <summary>
        /// The user can only select a value of a predefined set of values.
        /// </summary>
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
        /// <summary>
        /// The initial input of the combo selection if nothing has been selected yet.
        /// </summary>
        private const string CustomInputText = "--Custom Input--";

        /// <summary>
        /// The label of the component.
        /// </summary>
        public string Label;

        /// <summary>
        /// The event handler that gets invoked when the value of this input changes.
        /// </summary>
        public Action<string> OnValueChange;

        /// <summary>
        /// The mode in which this input operates.
        /// </summary>
        public ComboSelectMode Mode = ComboSelectMode.Combo;

        /// <summary>
        /// The component of type <see cref="CustomDropdown"/> in the prefab
        /// <see cref="ComboSelectBuilder.PrefabPath"/> for the child game object
        /// 'DropdownCombo/Dropdown'. This drop down allows a user to select one
        /// item of a predefined set of values.
        /// </summary>
        private CustomDropdown dropdown;

        /// <summary>
        /// The component of type <see cref="TMP_InputField"/> in the prefab
        /// <see cref="ComboSelectBuilder.PrefabPath"/> for the child game object
        /// 'DropdownCombo/Input'. This input field allows a user to set the
        /// value directly (by typing or dictating it).
        /// </summary>
        private TMP_InputField customInput;

        /// <summary>
        /// The component of type <see cref="TextMeshProUGUI"/> in the child named
        /// 'Label' in the prefab <see cref="ComboSelectBuilder.PrefabPath"/>.
        /// This represents the label of the combo box.
        /// </summary>
        private TextMeshProUGUI labelText;

        /// <summary>
        /// The component of type <see cref="Dictaphone"/> in the prefab
        /// <see cref="ComboSelectBuilder.PrefabPath"/> for the child game object
        /// 'DropdownCombo/DictateButton'. This element represents a kind of
        /// button to turn on/off dictation of the input value.
        /// </summary>
        private Dictaphone dictaphone;

        private readonly Queue<List<string>> valuesUpdates = new Queue<List<string>>();
        private readonly Queue<string> valueUpdates = new Queue<string>();

        /// <summary>
        /// The values (options) of this input.
        /// </summary>
        public List<string> Values
        {
            set => valuesUpdates.Enqueue(value);
        }

        /// <summary>
        /// The value (currently selected option) of this input.
        /// </summary>
        public string Value
        {
            get => FigureOutValue();
            set => valueUpdates.Enqueue(value);
        }

        private void Awake()
        {
            MustGetComponentInChild("DropdownCombo/Dropdown", out dropdown);
            MustGetComponentInChild("DropdownCombo/Input", out customInput);
            MustGetComponentInChild("Label", out labelText);
            MustGetComponentInChild("DropdownCombo/DictateButton", out dictaphone);
        }

        private void Start()
        {
            dropdown.dropdownEvent.AddListener(selectedIndex =>
            {
                string selectedItem = dropdown.dropdownItems[selectedIndex].itemName;
                OnValueChange(Value);
                FigureOutInputMode(selectedItem);
            });
            dropdown.isListItem = true;
            dropdown.listParent = FindCanvas(gameObject);
            labelText.text = Label;

            dictaphone.OnDictationFinished += text => customInput.text = text;
        }

        private void Update()
        {
            if (valuesUpdates.Count > 0)
            {
                List<string> newValues = valuesUpdates.Dequeue();
                dropdown.dropdownItems.Clear();
                if (Mode == ComboSelectMode.Combo)
                {
                    dropdown.CreateNewItemFast(CustomInputText, null);
                }
                foreach (string s in newValues)
                {
                    dropdown.CreateNewItemFast(s, null);
                }

                dropdown.SetupDropdown();
            }

            if (valueUpdates.Count > 0)
            {
                String newValue = valueUpdates.Dequeue();
                FigureOutInputMode(newValue);
            }
        }

        private void SetToCustomMode(string customValue)
        {
            dropdown.selectedItemIndex =
                dropdown.dropdownItems.FindIndex(item => item.itemName == CustomInputText);
            dropdown.SetupDropdown();
            customInput.gameObject.SetActive(true);
            dictaphone.gameObject.SetActive(true);
            if (customValue != null)
            {
                customInput.text = customValue;
            }
        }

        private void SetToFixedMode(int newIndex)
        {
            dropdown.selectedItemIndex = newIndex;
            dropdown.SetupDropdown();
            customInput.gameObject.SetActive(false);
            dictaphone.gameObject.SetActive(false);
        }

        private void FigureOutInputMode(string value)
        {
            // If the new value is already part of the items in the list, we simply select its index.
            int index = dropdown.dropdownItems.FindIndex(item => item.itemName == value);
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

        private string FigureOutValue()
        {
            string item = dropdown.dropdownItems[dropdown.selectedItemIndex].itemName;
            if (item == CustomInputText)
            {
                return customInput.text;
            }
            return item;
        }
    }

    /// <summary>
    /// Instantiates a new combo select game object via prefab and sets the wrapper script.
    /// </summary>
    public class ComboSelectBuilder : UIBuilder<ComboSelect>
    {
        protected override string PrefabPath => "Prefabs/UI/Input Group - Dropdown";

        private ComboSelectBuilder(Transform parent) : base(parent)
        {
        }

        public static ComboSelectBuilder Init(Transform parent)
        {
            return new ComboSelectBuilder(parent);
        }

        public ComboSelectBuilder SetLabel(string label)
        {
            Instance.Label = label;
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
            Instance.Mode = comboSelectMode;
            return this;
        }
    }
}
