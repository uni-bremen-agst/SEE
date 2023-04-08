using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HSVPicker;
using Michsky.UI.ModernUIPack;
using SEE.Controls;
using SEE.Game;
using SEE.Game.City;
using SEE.Game.UI.ConfigMenu;
using SEE.Game.UI.Menu;
using SEE.Game.UI.RuntimeConfigMenu;
using SEE.GO;
using SEE.Layout.NodeLayouts.Cose;
using SEE.Net.Actions.RuntimeConfig;
using SEE.Utils;
using SimpleFileBrowser;
using Sirenix.Utilities;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using FilePicker = SEE.Game.UI.FilePicker.FilePicker;
using Random = UnityEngine.Random;
using Slider = UnityEngine.UI.Slider;

public class RuntimeTabMenu : TabMenu<ToggleMenuEntry>
{
    public const string RUNTIME_CONFIG_PREFAB_FOLDER = UI_PREFAB_FOLDER + "RuntimeConfigMenu/";
    public const string SETTINGS_OBJECT_PREFAB = RUNTIME_CONFIG_PREFAB_FOLDER + "RuntimeSettingsObject";
    public const string SWITCH_PREFAB = UI_PREFAB_FOLDER + "Input Group - Switch";
    public const string SLIDER_PREFAB = UI_PREFAB_FOLDER + "Input Group - Slider";
    public const string DROPDOWN_PREFAB = UI_PREFAB_FOLDER + "Input Group - Dropdown";
    public const string COLORPICKER_PREFAB = UI_PREFAB_FOLDER + "Input Group - Color Picker";
    public const string STRINGFIELD_PREFAB = RUNTIME_CONFIG_PREFAB_FOLDER + "Input Group - StringInputField";
    public const string BUTTON_PREFAB = RUNTIME_CONFIG_PREFAB_FOLDER + "Button";
    public const string ADD_ELEMENT_BUTTON_PREFAB = RUNTIME_CONFIG_PREFAB_FOLDER + "AddButton";
    public const string REMOVE_ELEMENT_BUTTON_PREFAB = RUNTIME_CONFIG_PREFAB_FOLDER + "RemoveButton";
    public const string PICKER2_PREFAB = RUNTIME_CONFIG_PREFAB_FOLDER + "Picker 2.0";
    protected override string MenuPrefab => RUNTIME_CONFIG_PREFAB_FOLDER + "RuntimeConfigMenuRework_v2";
    protected override string ViewPrefab => RUNTIME_CONFIG_PREFAB_FOLDER + "RuntimeSettingsView";
    protected override string EntryPrefab => RUNTIME_CONFIG_PREFAB_FOLDER + "RuntimeTabButton";

    protected override string ContentPath => "Main Content";
    protected override string ViewListPath => "ViewList";
    protected override string EntryListPath => "TabButtons/TabObjects";
    protected virtual string ConfigButtonListPath => "ConfigButtons/Content";

    protected virtual string CitySwitcherPath => "City Switcher";

    private HorizontalSelector citySwitcher;
    private GameObject configButtonList;

    public int CityIndex;
    private AbstractSEECity city;
    private bool immediateRedraw;

    protected override void StartDesktop()
    {
        base.StartDesktop();
        configButtonList = Content.transform.Find(ConfigButtonListPath).gameObject;
        citySwitcher = Content.transform.Find(CitySwitcherPath).GetComponent<HorizontalSelector>();

        SetupCitySwitcher();
    }

    /// <summary>
    /// Updates the menu and adds listeners.
    /// </summary>
    protected override void OnStartFinished()
    {
        base.OnStartFinished();
        city = RuntimeConfigMenu.GetCity(CityIndex);
        OnEntryAdded += _ => SetMiscAsLastTab();
        OnSyncMethod += methodName => {
            if (methodName == nameof(TriggerImmediateRedraw)) TriggerImmediateRedraw();
        };

        LoadCity();

        // OnSyncField += (widgetName, value) =>
        // {
        //     Debug.LogError("Sync " + RuntimeConfigMenu.GetCity(CityIndex).name + "\t"
        //                    + widgetName.Split("/").Last() + "\t" + value);
        // };
        // OnSyncMethod += widgetName =>
        // {
        //     Debug.LogError("Sync " + RuntimeConfigMenu.GetCity(CityIndex).name + "\t"
        //                    + widgetName.Split("/").Last());
        // };
        // OnSyncPath += (widgetName, value, isAbsolute) =>
        // {
        //     Debug.LogError("SyncPath " + RuntimeConfigMenu.GetCity(CityIndex).name + "\t"
        //                    + widgetName.Split("/").Last() + "\t" + value + "\t" + isAbsolute);
        // };
    }

    protected virtual void CreateButton(MethodInfo methodInfo, AbstractSEECity city)
    {
        RuntimeButtonAttribute buttonAttribute = methodInfo.GetCustomAttributes().OfType<RuntimeButtonAttribute>().FirstOrDefault();
        // only methods with the button attribute
        if (buttonAttribute == null) return;
        // only methods with no parameters
        if (methodInfo.GetParameters().Length > 0) return;

        GameObject button = PrefabInstantiator.InstantiatePrefab(BUTTON_PREFAB, configButtonList.transform, false);
        button.name = methodInfo.GetCustomAttribute<RuntimeButtonAttribute>().Label;
        ButtonManagerWithIcon buttonManager = button.GetComponent<ButtonManagerWithIcon>();
        buttonManager.buttonText = methodInfo.GetCustomAttribute<RuntimeButtonAttribute>().Label;

        buttonManager.clickEvent.AddListener(() =>
        {
            methodInfo.Invoke(city, null); // calls the method
            OnUpdateMenuValues?.Invoke(); // updates the menu
        });
        buttonManager.clickEvent.AddListener(() =>
        {
            UpdateCityMethodNetAction netAction = new();
            netAction.CityIndex = CityIndex;
            netAction.MethodName = methodInfo.Name;
            netAction.Execute();
        });
        OnSyncMethod += methodName =>
        {
            if (methodName == methodInfo.Name)
            {
                methodInfo.Invoke(city, null); // calls the method
                OnUpdateMenuValues?.Invoke(); // updates the menu  
            }
        };
    }

    private void LoadCity()
    {
        city.GetType().GetMembers().ForEach(memberInfo =>
        {
            if (memberInfo.DeclaringType == typeof(AbstractSEECity) ||
                memberInfo.DeclaringType!.IsSubclassOf(typeof(AbstractSEECity)))
                CreateSetting(memberInfo, null, city);
        });
        city.GetType().GetMethods().ForEach(methodInfo => CreateButton(methodInfo, city));
        SelectEntry(Entries.First(entry => entry.Title != "Misc"));
    }

    private void SetupCitySwitcher()
    {
        citySwitcher.itemList.Clear();
        citySwitcher.defaultIndex = CityIndex;
        RuntimeConfigMenu.GetCities().ForEach(city => citySwitcher.CreateNewItem(city.name));
        citySwitcher.SetupSelector();
        citySwitcher.selectorEvent.AddListener(index =>
        {
            OnSwitchCity?.Invoke(index);
            citySwitcher.index = CityIndex;
            citySwitcher.UpdateUI();
        });
    }

    /// <summary>
    /// Returns the view game object.
    /// Adds an entry if necessary.
    /// </summary>
    /// <param name="attributes"></param>
    /// <returns></returns>
    private GameObject CreateOrGetViewGameObject(IEnumerable<Attribute> attributes)
    {
        string tabName = attributes.OfType<RuntimeFoldoutAttribute>().FirstOrDefault()?.name ??
                         "Misc";
        ToggleMenuEntry entry = Entries.FirstOrDefault(entry => entry.Title == tabName);
        // adds an entry (tab + view) if necessary
        if (entry == null)
        {
            entry = new ToggleMenuEntry(
                entryAction: () => { },
                exitAction: () => { },
                tabName,
                description: $"Settings for {tabName}",
                entryColor: Random.ColorHSV(), // TODO: Color
                icon: Icon, // TODO: Icon
                enabled: true
            );
            AddEntry(entry);
        }

        return ViewGameObject(entry);
    }

    /// <summary>
    /// Sets the misc button as the last in the tab list.
    /// </summary>
    protected virtual void SetMiscAsLastTab()
    {
        ToggleMenuEntry miscEntry = Entries.FirstOrDefault(entry => entry.Title == "Misc");
        if (miscEntry != null) EntryGameObject(miscEntry).transform.SetAsLastSibling();
    }

    private void CreateSetting(MemberInfo memberInfo, GameObject parent, object obj)
    {
        if (memberInfo.GetCustomAttributes().Any(attribute => attribute is ObsoleteAttribute)) return;
        switch (memberInfo)
        {
            case FieldInfo fieldInfo:
                if (fieldInfo.IsLiteral || fieldInfo.IsInitOnly) return;
                CreateSetting(
                    getter: () => fieldInfo.GetValue(obj),
                    settingName: memberInfo.Name,
                    parent: parent,
                    setter: changedValue => fieldInfo.SetValue(obj, changedValue),
                    attributes: memberInfo.GetCustomAttributes()
                );
                break;
            case PropertyInfo propertyInfo:
                if (propertyInfo.GetMethod == null || propertyInfo.SetMethod == null ||
                    !propertyInfo.CanRead || !propertyInfo.CanWrite) return;
                CreateSetting(
                    getter: () => propertyInfo.GetValue(obj),
                    settingName: memberInfo.Name,
                    parent: parent,
                    setter: changedValue => propertyInfo.SetValue(obj, changedValue),
                    attributes: memberInfo.GetCustomAttributes()
                );
                break;
        }
    }

    private void CreateSetting(Func<object> getter, string settingName, GameObject parent,
        UnityAction<object> setter = null, IEnumerable<Attribute> attributes = null)
    {
        Attribute[] attributeArray = attributes as Attribute[] ?? attributes?.ToArray() ?? Array.Empty<Attribute>();
        parent ??= CreateOrGetViewGameObject(attributeArray).transform.Find("Content").gameObject;

        object value = getter();
        switch (value)
        {
            case bool:
                CreateSwitch(
                    settingName: settingName,
                    setter: changedValue => setter!(changedValue),
                    getter: () => (bool)getter(),
                    parent: parent
                );
                break;
            case int:
                CreateSlider(
                    settingName: settingName,
                    range: attributeArray.OfType<RangeAttribute>().ElementAtOrDefault(0),
                    setter: changedValue => setter!((int)changedValue),
                    getter: () => (int)getter(),
                    useRoundValue: true,
                    parent: parent
                );
                break;
            case uint:
                CreateSlider(
                    settingName: settingName,
                    range: attributeArray.OfType<RangeAttribute>().ElementAtOrDefault(0),
                    setter: changedValue => setter!((uint)changedValue),
                    getter: () => (uint)getter(),
                    useRoundValue: true,
                    parent: parent
                );
                break;
            case float:
                CreateSlider(
                    settingName: settingName,
                    range: attributeArray.OfType<RangeAttribute>().ElementAtOrDefault(0),
                    setter: changedValue => setter!(changedValue),
                    getter: () => (float)getter(),
                    useRoundValue: false,
                    parent: parent
                );
                break;
            case string:
                CreateStringField(
                    settingName: settingName,
                    setter: changedValue => setter!(changedValue),
                    getter: () => (string)getter(),
                    parent: parent
                );
                break;
            case Color:
                CreateColorPicker(
                    settingName: settingName,
                    parent: parent,
                    setter: changedValue => setter!(changedValue),
                    getter: () => (Color)getter()
                );
                break;
            case DataPath dataPath:
                parent = CreateNestedSetting(settingName, parent);
                FilePicker filePicker = parent.AddComponent<FilePicker>();
                filePicker.DataPathInstance = dataPath;
                filePicker.Label = settingName;
                filePicker.PickingMode = FileBrowser.PickMode.Files;
                filePicker.OnMenuInitialized += () => AddLayoutElement(parent.transform.Find(settingName).gameObject);

                filePicker.OnChangedDropdown += () =>
                {
                    UpdateIntCityFieldNetAction netAction = new();
                    netAction.Value = (int)dataPath.Root;
                    netAction.CityIndex = CityIndex;
                    netAction.WidgetPath = filePicker.gameObject.FullName() + "/" + settingName;
                    netAction.Execute();
                };

                filePicker.OnChangedPath += () =>
                {
                    UpdatePathCityFieldNetAction netAction = new();
                    netAction.IsAbsolute = dataPath.Root == DataPath.RootKind.Absolute;
                    netAction.Value = netAction.IsAbsolute ? dataPath.AbsolutePath : dataPath.RelativePath;
                    netAction.CityIndex = CityIndex;
                    netAction.WidgetPath = filePicker.gameObject.FullName() + "/" + settingName;
                    netAction.Execute();
                };

                OnSyncPath += (widgetPath, newValue, isAbsolute) =>
                {
                    if (widgetPath == filePicker.gameObject.FullName() + "/" + settingName)
                    {
                        filePicker.ChangePath(newValue, isAbsolute);
                    }
                };

                OnSyncField += (widgetPath, newValue) =>
                {
                    if (widgetPath == filePicker.gameObject.FullName() + "/" + settingName)
                    {
                        filePicker.ChangeDropdown((int)newValue);
                    }
                };

                break;
            case Enum:
                CreateDropDown(
                    settingName: settingName,
                    setter: changedValue => setter!(Enum.ToObject(value.GetType(), changedValue)),
                    values: value.GetType().GetEnumNames(),
                    getter: () => getter().ToString(),
                    parent: parent
                );
                break;
            // from here on come nested settings
            case NodeTypeVisualsMap:
            case ColorMap:
                FieldInfo mapInfo = value.GetType().GetField("map", BindingFlags.Instance | BindingFlags.NonPublic)!;
                CreateSetting(
                    getter: () => mapInfo.GetValue(value),
                    settingName: settingName,
                    parent: parent,
                    setter: null,
                    // TODO: Which attributes? Both or which one?
                    attributes: mapInfo.GetCustomAttributes().Concat(attributes ?? Enumerable.Empty<Attribute>())
                );
                break;
            case AntennaAttributes:
                FieldInfo antennaInfo = value.GetType().GetField("AntennaSections")!;
                CreateSetting(
                    getter: () => antennaInfo.GetValue(value),
                    settingName: settingName,
                    parent: parent,
                    setter: null,
                    // TODO: Which attributes? Both or which one?
                    attributes: antennaInfo.GetCustomAttributes().Concat(attributes ?? Enumerable.Empty<Attribute>())
                );
                break;
            // from here on come nested settings
            case HashSet<string>:
                // TODO: How to edit the strings?
                // TODO: Currently only works on first edit
                /*
            parent = CreateNestedSetting( name, parent);
            int strIndex = 0;
            foreach (string str in hashSet)
            {
                CreateSetting(
                    value: str,
                    name: strIndex.ToString(),
                    parent: parent,
                    setter: changedValue =>
                    {
                        hashSet.Remove(str); 
                        hashSet.Add((string)changedValue);
                    },
                    attributes: null
                );
                strIndex++;
            }
            */
                break;
            case IDictionary dict:
                parent = CreateNestedSetting(settingName, parent);
                UpdateDictChildren(parent, dict);
                OnUpdateMenuValues += () => UpdateDictChildren(parent, dict);
                break;
            case IList<string> list:
                parent = CreateNestedSetting(settingName, parent);
                // Buttons erstellen
                GameObject addButton = PrefabInstantiator.InstantiatePrefab(ADD_ELEMENT_BUTTON_PREFAB, parent.transform);
                GameObject removeButton = PrefabInstantiator.InstantiatePrefab(REMOVE_ELEMENT_BUTTON_PREFAB, parent.transform);
                removeButton.name = "RemoveElementButton";
                ButtonManagerWithIcon removeButtonManager = removeButton.GetComponent<ButtonManagerWithIcon>();
                addButton.name = "AddElementButton";
                ButtonManagerWithIcon addButtonManager = addButton.GetComponent<ButtonManagerWithIcon>();
                // Listener AddButton
                addButtonManager.clickEvent.AddListener(() =>
                {
                    list.Add("");
                    UpdateListChildren(list, parent);
                    addButtonManager.transform.SetAsLastSibling();
                    removeButtonManager.transform.SetAsLastSibling();
                });
                // Listener RemoveButton
                removeButtonManager.clickEvent.AddListener(() =>
                {
                    list.RemoveAt(list.Count - 1);
                    UpdateListChildren(list, parent);
                    addButtonManager.transform.SetAsLastSibling();
                    removeButtonManager.transform.SetAsLastSibling();
                });
                // Update
                UpdateListChildren(list, parent);
                addButton.transform.SetAsLastSibling();
                removeButton.transform.SetAsLastSibling();
                OnUpdateMenuValues += () =>
                {
                    UpdateListChildren(list, parent);
                    addButton.transform.SetAsLastSibling();
                    removeButton.transform.SetAsLastSibling();
                };
                break;
            // confirmed types where the nested fields should be edited
            case ColorRange:
            case ColorProperty:
            case CoseGraphAttributes:
            case VisualNodeAttributes:
            case NodeLayoutAttributes:
            case EdgeLayoutAttributes:
            case EdgeSelectionAttributes:
            case ErosionAttributes:
            case BoardAttributes:
                parent = CreateNestedSetting(settingName, parent);
                value.GetType().GetMembers().ForEach(nestedInfo => CreateSetting(nestedInfo, parent, value));
                break;
            // unconfirmed types where the nested fields should be edited
            case VisualAttributes:
            case ConfigIO.PersistentConfigItem:
            case LabelAttributes:
                if (value.GetType() != typeof(VisualAttributes)
                    && value.GetType() != typeof(ConfigIO.PersistentConfigItem)
                    && value.GetType() != typeof(LabelAttributes)
                   )
                {
                    Debug.Log("Missing: (Maybe)" + settingName + " " + value.GetType().GetNiceName());
                }
                parent = CreateNestedSetting(settingName, parent);
                value.GetType().GetMembers().ForEach(nestedInfo => CreateSetting(nestedInfo, parent, value));
                break;
            default:
                Debug.LogWarning("Missing: " + settingName + " " + value?.GetType().GetNiceName());
                break;
        }
    }

    private GameObject CreateNestedSetting(string settingName, GameObject parent)
    {
        GameObject container =
            PrefabInstantiator.InstantiatePrefab(SETTINGS_OBJECT_PREFAB, parent.transform, false);
        container.name = settingName;
        container.GetComponentInChildren<TextMeshProUGUI>().text = settingName;
        return container.transform.Find("Content").gameObject;
    }

    private void CreateSlider(string settingName, RangeAttribute range, UnityAction<float> setter, Func<float> getter,
        bool useRoundValue, GameObject parent, bool recursive = false)
    {
        range ??= new RangeAttribute(0, 2);

        GameObject sliderGameObject =
            PrefabInstantiator.InstantiatePrefab(SLIDER_PREFAB, parent.transform, false);
        sliderGameObject.name = settingName;
        AddLayoutElement(sliderGameObject);
        SliderManager sliderManager = sliderGameObject.GetComponentInChildren<SliderManager>();
        Slider slider = sliderGameObject.GetComponentInChildren<Slider>();
        TextMeshProUGUI text = sliderGameObject.transform.Find("Label").GetComponent<TextMeshProUGUI>();
        text.text = settingName;

        sliderManager.usePercent = false;
        sliderManager.useRoundValue = useRoundValue;
        slider.minValue = range.min;
        slider.maxValue = range.max;

        RuntimeSliderManager endEditManager = slider.gameObject.AddComponent<RuntimeSliderManager>();

        slider.value = getter();
        endEditManager.OnEndEdit += () => setter(slider.value);

        endEditManager.OnEndEdit += () =>
        {
            UpdateFloatCityFieldNetAction action = new();
            action.CityIndex = CityIndex;
            action.WidgetPath = sliderGameObject.FullName();
            action.Value = slider.value;
            action.Execute();
        };
        endEditManager.OnEndEdit += CheckImmediateRedraw;

        OnSyncField += (widgetPath, value) =>
        {
            if (widgetPath == sliderGameObject.FullName())
            {
                setter((float)value);
                slider.value = (float)value;
                sliderManager.UpdateUI();
            }
        };

        OnUpdateMenuValues += () =>
        {
            slider.value = getter();
            sliderManager.UpdateUI();
        };


        if (!recursive)
        {
            RuntimeSmallEditorButton smallEditorButton = sliderGameObject.AddComponent<RuntimeSmallEditorButton>();
            smallEditorButton.OnShowMenuChanged += () =>
            {
                immediateRedraw = smallEditorButton.ShowMenu;
                ShowMenu = !smallEditorButton.ShowMenu;
                OnUpdateMenuValues?.Invoke();
            };
            smallEditorButton.CreateWidget = smallEditor =>
                CreateSlider(settingName, range, setter, getter, useRoundValue, smallEditor, true);
        }
    }

    private void CreateSwitch(string settingName, UnityAction<bool> setter, Func<bool> getter, GameObject parent,
        bool recursive = false)
    {
        GameObject switchGameObject =
            PrefabInstantiator.InstantiatePrefab(SWITCH_PREFAB, parent.transform, false);
        switchGameObject.name = settingName;
        AddLayoutElement(switchGameObject);
        SwitchManager switchManager = switchGameObject.GetComponentInChildren<SwitchManager>();
        TextMeshProUGUI text = switchGameObject.transform.Find("Label").GetComponent<TextMeshProUGUI>();
        text.text = settingName;

        switchManager.isOn = getter();
        switchManager.UpdateUI();
        switchManager.OnEvents.AddListener(() => setter(true));
        switchManager.OnEvents.AddListener(() =>
        {
            UpdateBoolCityFieldNetAction action = new();
            action.CityIndex = CityIndex;
            action.WidgetPath = switchGameObject.FullName();
            action.Value = true;
            action.Execute();
        });
        switchManager.OnEvents.AddListener(CheckImmediateRedraw);
        switchManager.OffEvents.AddListener(() => setter(false));
        switchManager.OffEvents.AddListener(() =>
        {
            UpdateBoolCityFieldNetAction action = new();
            action.CityIndex = CityIndex;
            action.WidgetPath = switchGameObject.FullName();
            action.Value = false;
            action.Execute();
        });
        switchManager.OffEvents.AddListener(CheckImmediateRedraw);

        OnSyncField += (widgetPath, value) =>
        {
            if (widgetPath == switchGameObject.FullName())
            {
                setter((bool)value);
                switchManager.isOn = (bool)value;
                switchManager.UpdateUI();
            }
        };

        OnUpdateMenuValues += () =>
        {
            switchManager.isOn = getter();
            switchManager.UpdateUI();
        };

        if (!recursive)
        {
            RuntimeSmallEditorButton smallEditorButton = switchGameObject.AddComponent<RuntimeSmallEditorButton>();
            smallEditorButton.OnShowMenuChanged += () =>
            {
                immediateRedraw = smallEditorButton.ShowMenu;
                ShowMenu = !smallEditorButton.ShowMenu;
                OnUpdateMenuValues?.Invoke();
            };
            smallEditorButton.CreateWidget = smallEditor =>
                CreateSwitch(settingName, setter, getter, smallEditor, true);
        }



    }

    private void CreateStringField(string settingName, UnityAction<string> setter, Func<string> getter,
        GameObject parent, bool recursive = false)
    {
        GameObject stringGameObject =
            PrefabInstantiator.InstantiatePrefab(STRINGFIELD_PREFAB, parent.transform, false);
        stringGameObject.name = settingName;
        AddLayoutElement(stringGameObject);
        TextMeshProUGUI text = stringGameObject.transform.Find("Label").GetComponent<TextMeshProUGUI>();
        text.text = settingName;

        TMP_InputField inputField = stringGameObject.GetComponentInChildren<TMP_InputField>();
        inputField.text = getter();
        inputField.onSelect.AddListener(_ => SEEInput.KeyboardShortcutsEnabled = false);
        inputField.onDeselect.AddListener(_ => SEEInput.KeyboardShortcutsEnabled = true);
        inputField.onEndEdit.AddListener(setter);
        inputField.onEndEdit.AddListener(changedValue =>
        {
            UpdateStringCityFieldNetAction action = new();
            action.CityIndex = CityIndex;
            action.WidgetPath = stringGameObject.FullName();
            action.Value = changedValue;
            action.Execute();
        });
        inputField.onEndEdit.AddListener(_ => CheckImmediateRedraw());

        OnUpdateMenuValues += () => inputField.text = getter();

        OnSyncField += (widgetPath, value) =>
        {
            if (widgetPath == stringGameObject.FullName())
            {
                setter(value as string);
                inputField.text = value as string;
            }
        };


        if (!recursive)
        {
            RuntimeSmallEditorButton smallEditorButton = stringGameObject.AddComponent<RuntimeSmallEditorButton>();
            smallEditorButton.OnShowMenuChanged += () =>
            {
                immediateRedraw = smallEditorButton.ShowMenu;
                ShowMenu = !smallEditorButton.ShowMenu;
                OnUpdateMenuValues?.Invoke();
            };
            smallEditorButton.CreateWidget = smallEditor =>
                CreateStringField(settingName, setter, getter, smallEditor, true);
        }
    }

    // TODO: Add action
    private void CreateDropDown(string settingName, UnityAction<int> setter, IEnumerable<string> values,
        Func<string> getter, GameObject parent, bool recursive = false)
    {
        string[] valueArray = values as string[] ?? values.ToArray();

        GameObject dropDownGameObject =
            PrefabInstantiator.InstantiatePrefab(DROPDOWN_PREFAB, parent.transform, false);
        dropDownGameObject.name = settingName;
        AddLayoutElement(dropDownGameObject);
        TextMeshProUGUI text = dropDownGameObject.transform.Find("Label").GetComponent<TextMeshProUGUI>();
        text.text = settingName;
        // TODO: value and setter

        CustomDropdown dropdown = dropDownGameObject.transform.Find("DropdownCombo/Dropdown").GetComponent<CustomDropdown>();
        TMP_InputField customInput = dropDownGameObject.transform.Find("DropdownCombo/Input").GetComponent<TMP_InputField>();
        Dictaphone dictaphone = dropDownGameObject.transform.Find("DropdownCombo/DictateButton").GetComponent<Dictaphone>();

        customInput.gameObject.SetActive(false);
        dictaphone.gameObject.SetActive(false);

        dropdown.isListItem = true;
        dropdown.listParent = Canvas.transform;
        dropdown.selectedItemIndex = Array.IndexOf(valueArray, getter());
        valueArray.ForEach(s => dropdown.CreateNewItemFast(s, null));

        dropdown.SetupDropdown();

        dropdown.dropdownEvent.AddListener(setter);
        dropdown.dropdownEvent.AddListener(changedValue =>
        {
            UpdateIntCityFieldNetAction action = new();
            action.CityIndex = CityIndex;
            action.WidgetPath = dropDownGameObject.FullName();
            action.Value = changedValue;
            action.Execute();
        });
        dropdown.dropdownEvent.AddListener(_ => CheckImmediateRedraw());

        OnSyncField += (widgetPath, value) =>
        {
            if (widgetPath == dropDownGameObject.FullName())
            {
                setter((int)value);
                dropdown.ChangeDropdownInfo((int)value);
            }
        };

        OnUpdateMenuValues += () => dropdown.ChangeDropdownInfo(Array.IndexOf(valueArray, getter()));

        if (!recursive)
        {
            RuntimeSmallEditorButton smallEditorButton = dropDownGameObject.AddComponent<RuntimeSmallEditorButton>();
            smallEditorButton.OnShowMenuChanged += () =>
            {
                immediateRedraw = smallEditorButton.ShowMenu;
                ShowMenu = !smallEditorButton.ShowMenu;
                OnUpdateMenuValues?.Invoke();
            };
            smallEditorButton.CreateWidget = smallEditor =>
                CreateDropDown(settingName, setter, valueArray, getter, smallEditor, true);
        }
    }

    // TODO: Add action
    private void CreateColorPicker(string settingName, GameObject parent, UnityAction<Color> setter, Func<Color> getter, bool recursive = false)
    {
        parent = CreateNestedSetting("Color Picker: " + settingName, parent);
        GameObject colorPickerGameObject =
            PrefabInstantiator.InstantiatePrefab(PICKER2_PREFAB, parent.transform, false);
        colorPickerGameObject.name = settingName;
        AddLayoutElement(colorPickerGameObject);
        HSVPicker.ColorPicker colorPicker = colorPickerGameObject.GetComponent<HSVPicker.ColorPicker>();
        colorPicker.CurrentColor = getter();
        colorPicker.onValueChanged.AddListener(setter);
        colorPicker.onValueChanged.AddListener(_ => CheckImmediateRedraw());
        BoxSlider boxSlider = colorPickerGameObject.GetComponentInChildren<BoxSlider>();
        boxSlider.onValueChanged.AddListener((f, f1) =>
        {
            UpdateColorCityFieldNetAction action = new();
            action.CityIndex = CityIndex;
            action.WidgetPath = colorPickerGameObject.FullName();
            action.Value = getter();
            action.Execute();
        });
        boxSlider.onValueChanged.AddListener((_, _) => CheckImmediateRedraw());
        OnSyncField += (widgetPath, value) =>
        {
            if (widgetPath == colorPickerGameObject.FullName())
            {
                setter((Color)value);
                colorPicker.CurrentColor = getter();
            }
        };
        OnUpdateMenuValues += () =>
        {
            colorPicker.CurrentColor = getter();
        };
        /* TODO: What is the purpose of this?
        if (!recursive)
        {
            OnUpdateMenuValues?.Invoke();
        };
        */
        if (!recursive)
        {
            RuntimeSmallEditorButton smallEditorButton = colorPickerGameObject.AddComponent<RuntimeSmallEditorButton>();
            smallEditorButton.OnShowMenuChanged += () =>
            {
                immediateRedraw = smallEditorButton.ShowMenu;
                ShowMenu = !smallEditorButton.ShowMenu;
                OnUpdateMenuValues?.Invoke();
            };
            smallEditorButton.CreateWidget = smallEditor =>
                CreateColorPicker(settingName, smallEditor, setter, getter, true);
        }

    }

    private void UpdateListChildren(IList<string> list, GameObject parent)
    {
        // removes superfluous children
        foreach (Transform child in parent.transform)
        {
            if (Int32.TryParse(child.name, out int index))
            {
                if (index >= list.Count)
                    Destroyer.Destroy(child.gameObject);
            }
        }
        // creates needed children
        for (int i = 0; i < list.Count; i++)
        {
            if (parent.transform.Find(i.ToString()) == null)
            {
                int iCopy = i;
                CreateSetting(
                    getter: () => list[iCopy],
                    settingName: i.ToString(),
                    parent: parent,
                    setter: changedValue => list[iCopy] = changedValue as string
                );
            }
        }
    }

    private void UpdateDictChildren(GameObject parent, IDictionary dict)
    {
        // removes children that aren't in the dictionary any more
        foreach (Transform child in parent.transform)
        {
            if (!dict.Contains(child.name)) Destroyer.Destroy(child);
        }
        // goes through all dictionary keys
        foreach (object key in dict.Keys)
        {
            // creates a child if it doesn't exist yet
            if (parent.transform.Find(key.ToString()) == null)
            {
                CreateSetting(
                    getter: () => dict[key],
                    settingName: key.ToString(),
                    parent: parent,
                    setter: changedValue => dict[key] = changedValue
                );
            }
        }
    }

    private void AddLayoutElement(GameObject go)
    {
        LayoutElement le = go.AddComponent<LayoutElement>();
        le.minWidth = ((RectTransform)go.transform).rect.width;
        le.minHeight = ((RectTransform)go.transform).rect.height;
    }

    private void CheckImmediateRedraw()
    {
        if (!immediateRedraw) return;
        TriggerImmediateRedraw();
        UpdateCityMethodNetAction netAction = new();
        netAction.CityIndex = CityIndex;
        netAction.MethodName = nameof(TriggerImmediateRedraw);
        netAction.Execute();
    }

    private void TriggerImmediateRedraw()
    {
        // TODO: How to redraw the city?
        ((SEECity)city).LoadData();
        city.Invoke(nameof(SEECity.ReDrawGraph), 0);
    }

    public Action<string, object> OnSyncField;
    public Action<string> OnSyncMethod;
    public Action<string, string, bool> OnSyncPath;
    public event Action OnUpdateMenuValues;

    public event Action<int> OnSwitchCity;
}
