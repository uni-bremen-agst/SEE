using System;
using System.Collections.Generic;
using Michsky.UI.ModernUIPack;
using TMPro;
using UnityEngine;

public class DropdownControlledInput : MonoBehaviour, IControlledInput
{
    public string fieldName;
    public CustomDropdown dropdown;
    public TMP_InputField customInput;

    public static string CustomInputText = "--Custom Input--";

    public List<string> Values
    {
        set
        {
            dropdown.dropdownItems.Clear();
            dropdown.CreateNewItemFast(CustomInputText, null);
            foreach (var s in value)
            {
                dropdown.CreateNewItemFast(s, null);
            }

            dropdown.SetupDropdown();
        }
    }

    void Start()
    {
        dropdown = GetComponentInChildren<CustomDropdown>();
        dropdown.dropdownEvent.AddListener(arg0 =>
        {
            var selectedItem = dropdown.dropdownItems[arg0].itemName;
            OnValueChange(Value);
            FigureOutInputMode(selectedItem);
        });
        dropdown.isListItem = true;
        dropdown.listParent = FindObjectOfType<Canvas>().transform;
        EnsureCustomInput();
    }

    public Action<string> OnValueChange { get; set; }

    void SetToCustomMode(string customValue)
    {
        dropdown.selectedItemIndex = dropdown.dropdownItems.FindIndex(item => item.itemName == CustomInputText);
        dropdown.SetupDropdown();
        customInput.gameObject.SetActive(true);
        if (customValue != null)
        {
            Debug.Log(customInput.text);
            customInput.text = customValue;
        }
    }

    void SetToFixedMode(int newIndex)
    {
        dropdown.selectedItemIndex = newIndex;
        dropdown.SetupDropdown();
        customInput.gameObject.SetActive(false);
    }

    void EnsureCustomInput()
    {
        if (!customInput)
        {
            customInput = GetComponentInChildren<TMP_InputField>();
        }
    }

    void FigureOutInputMode(string value)
    {
        // If the new value is already part of the items in the list, we simply select its index.
        var index = dropdown.dropdownItems.FindIndex(item => item.itemName == value.Replace("Metric.", ""));
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
        var item = dropdown.dropdownItems[dropdown.selectedItemIndex].itemName;
        if (item == CustomInputText)
        {
            return customInput.text;
        }
        return item;
    }

    public string Value
    {
        get => FigureOutValue();
        set => FigureOutInputMode(value);
    }
}