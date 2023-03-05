using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Michsky.UI.ModernUIPack;
using SEE.DataModel;
using SEE.Game;
using SEE.Game.City;
using SEE.Game.UI.ConfigMenu;
using SEE.Game.UI.Menu;
using SEE.Layout.NodeLayouts.Cose;
using SEE.Utils;
using SimpleFileBrowser;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using FilePicker = SEE.Game.UI.FilePicker.FilePicker;
using Object = System.Object;
using Random = UnityEngine.Random;
using Slider = UnityEngine.UI.Slider;

public class RuntimeTabMenu : TabMenu<ToggleMenuEntry>
{
    protected const string RUNTIME_CONFIG_PREFAB_FOLDER = UI_PREFAB_FOLDER + "RuntimeConfigMenu/";
    public const string SETTINGS_OBJECT_PREFAB = RUNTIME_CONFIG_PREFAB_FOLDER + "RuntimeSettingsObject";
    public const string SWITCH_PREFAB = UI_PREFAB_FOLDER + "Input Group - Switch";
    public const string FILEPICKER_PREFAB = UI_PREFAB_FOLDER + "Input Group - File Picker";
    public const string SLIDER_PREFAB = UI_PREFAB_FOLDER + "Input Group - Slider";
    public const string DROPDOWN_PREFAB = UI_PREFAB_FOLDER + "Input Group - Dropdown";
    public const string COLORPICKER_PREFAB = UI_PREFAB_FOLDER + "Input Group - Color Picker";
    public const string STRINGFIELD_PREFAB = RUNTIME_CONFIG_PREFAB_FOLDER + "Input Group - StringInputField";
    public const string BUTTON_PREFAB = RUNTIME_CONFIG_PREFAB_FOLDER + "Button";
    protected override string MenuPrefab => RUNTIME_CONFIG_PREFAB_FOLDER + "RuntimeConfigMenuRework_v2";
    protected override string ViewPrefab => RUNTIME_CONFIG_PREFAB_FOLDER + "RuntimeSettingsView";
    protected override string EntryPrefab => RUNTIME_CONFIG_PREFAB_FOLDER + "RuntimeTabButton";
    
    // is already part of the MenuPrefab
    protected override string ViewListPrefab => null;
    
    // is already part of the MenuPrefab
    protected override string EntryListPrefab => null;
    
    // which sprite should be used as the icon
    protected override string IconSprite => base.IconSprite;
    // TODO: where can be specific parts of the menu be found
    protected override string ViewListPath => "SettingsContentView";
    protected override string ContentPath => "SeeSettingsPanel";
    protected override string EntryListPath => "Tabs/TabObjects";
    
    private HorizontalSelector citySwitcher;

    /// <summary>
    /// The SEE-city.
    /// </summary>
    public AbstractSEECity City { private get; set; }

    /// <summary>
    /// Updates the menu and adds listeners.
    /// </summary>
    protected override void OnStartFinished()
    {
        base.OnStartFinished();
        OnEntryAdded += _ => SetMiscAsLastTab();
        // TODO: Auch Properties und Methoden umsetzen:
        // City.GetType().GetMembers()
        LoadCity(0);
        // City.GetType().GetFields().ForEach(CreateSetting);
        SetupCitySwitcher();
    }

    /// <summary>
    /// Creates the UI for a setting.
    /// </summary>
    /// <param name="memberInfo"></param>
    protected virtual void CreateSetting(FieldInfo fieldInfo)
    {
        // gets the view game object
        GameObject view = GetViewGameObjectHelper(fieldInfo);
        GameObject viewContent = view.transform.Find("Content").gameObject;
        
        CreateSettingObject(fieldInfo, viewContent, City);
    }

    protected virtual void CreateButton(MemberInfo memberInfo)
    {
        if (memberInfo.CustomAttributes.Any(attribute => attribute.AttributeType == typeof(RuntimeButtonAttribute)))
        {
            Transform buttonContent = Content.transform.Find("ConfigButtons/Content");
            GameObject button = PrefabInstantiator.InstantiatePrefab(BUTTON_PREFAB, buttonContent, false);
            button.name = memberInfo.Name;
            ButtonManagerWithIcon buttonManager = button.GetComponent<ButtonManagerWithIcon>();
            buttonManager.buttonText = memberInfo.Name;
            UnityEvent buttonEvent = new UnityEvent();
            buttonEvent.AddListener(() => City.Invoke(memberInfo.Name, 0));
            buttonManager.clickEvent =  buttonEvent;

            //Debug.Log("\t"+"CreateButton____ "+ memberInfo.Name);
        }
    }
        

    protected void ClearCity()
    {
        Entries.Reverse().ForEach(RemoveEntry);
        foreach (Transform button in Content.transform.Find("ConfigButtons/Content"))
        {
            Destroy(button.gameObject);
        }
        //TODO Remove Buttons as well: Listener und Buttons muessen zu TabMenu.cs hinzugeuegt werden
    }
    
    public void LoadCity(int i)
    {
        City = GameObject.FindGameObjectsWithTag(Tags.CodeCity)[i].GetComponent<AbstractSEECity>();
        City.GetType().GetFields().ForEach(CreateSetting);
        City.GetType().GetMethods().ForEach(CreateButton);
        SelectEntry(Entries.First(entry => entry.Title != "Misc"));
    } 
    
    public List<GameObject> GetAllCities()
    {
        List<GameObject> seeCities = GameObject.FindGameObjectsWithTag(Tags.CodeCity).ToList();
        return seeCities;
    } 
    
    private void SetupCitySwitcher()
    {
        citySwitcher =  GameObject.Find("Horizontal Selector").GetComponent<HorizontalSelector>();
        citySwitcher.name = "CitySwitcher";
        citySwitcher.itemList.Clear();
        List<GameObject> seeCities = GetAllCities();
        foreach (GameObject city in seeCities)
        {
            citySwitcher.CreateNewItem(city.GetComponent<AbstractSEECity>().name);
        }
        citySwitcher.defaultIndex = 0;
        citySwitcher.SetupSelector();
        citySwitcher.selectorEvent.AddListener(index =>
        {
            StartCoroutine(ClearAndLoadCity(index));
        });
    }
    
    /// <summary>
    /// Clear and load city.
    /// Delays the loading of a city by one frame since destroying GameObject is not immediate.
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    IEnumerator ClearAndLoadCity(int index)
    {
        ClearCity();
        yield return 0;
        LoadCity(index);
    }

    /// <summary>
    /// Returns the view game object.
    /// Adds an entry if necessary.
    /// </summary>
    /// <param name="memberInfo"></param>
    /// <returns></returns>
    private GameObject GetViewGameObjectHelper(MemberInfo memberInfo)
    {
        string tabName = memberInfo.GetCustomAttributes().OfType<RuntimeFoldoutAttribute>().FirstOrDefault()?.name ??
                         memberInfo.GetCustomAttributes().OfType<PropertyGroupAttribute>().FirstOrDefault()?.GroupName ??
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

    private static void CreateSettingObject(FieldInfo fieldInfo, GameObject parent, object obj)
    {
        object value = fieldInfo.GetValue(obj);
        if (value is bool b)
        {
            CreateSwitch(
                name: fieldInfo.Name, 
                setter: changedValue => fieldInfo.SetValue(obj, changedValue), 
                value: b, 
                parent: parent
                );
        }
        else if (value is int i)
        {
            CreateSlider(
                name: fieldInfo.Name, 
                range: fieldInfo.GetAttributes().OfType<RangeAttribute>().ElementAtOrDefault(0), 
                setter: changedValue => fieldInfo.SetValue(obj, (int) changedValue), 
                value: i, 
                useRoundValue: true, 
                parent: parent
                );
        }
        else if (value is uint ui)
        {
            CreateSlider(
                name: fieldInfo.Name, 
                range: fieldInfo.GetAttributes().OfType<RangeAttribute>().ElementAtOrDefault(0), 
                setter: changedValue => fieldInfo.SetValue(obj, (int) changedValue), 
                value: ui, 
                useRoundValue: true, 
                parent: parent
            );
        }
        else if (value is float f)
        {
            CreateSlider(
                name: fieldInfo.Name, 
                range: fieldInfo.GetAttributes().OfType<RangeAttribute>().ElementAtOrDefault(0), 
                setter: changedValue => fieldInfo.SetValue(obj, changedValue), 
                value: f, 
                useRoundValue: false, 
                parent: parent
            );
        }
        else if (value is string s)
        {
            CreateStringField(
                name: fieldInfo.Name,
                setter: changedValue => fieldInfo.SetValue(obj, changedValue),
                value: s,
                parent: parent
                );
        }
        else if (value is Color c)
        {
            CreateColorPicker(
                name: fieldInfo.Name, 
                parent: parent
                );
        }
        else if (value is DataPath dataPath)
        {
            parent = CreateNestedSettingsObject(fieldInfo.Name, parent);
            FilePicker filePicker = parent.AddComponent<FilePicker>();
            filePicker.DataPathInstance = dataPath;
            filePicker.Label = fieldInfo.Name;
            filePicker.PickingMode = FileBrowser.PickMode.Files;
            filePicker.OnMenuInitialized += () =>
            {
                GameObject filePickerGO = parent.transform.Find(fieldInfo.Name).gameObject;
                AddLayoutElement(filePickerGO);
            };
        }
        else if (value.GetType().IsEnum)
        {
            // TODO: Does the setter work?
            CreateDropDown(
                name: fieldInfo.Name, 
                setter: changedValue => fieldInfo.SetValue(obj, Enum.ToObject(value.GetType(), changedValue)),
                values: value.GetType().GetEnumNames(),
                value: value.ToString(),
                parent: parent
            );
        }
        // from here on come nested settings
        else if (value is IEnumerable<string> stringEnumerable)
        {
            parent = CreateNestedSettingsObject(fieldInfo.Name, parent);
            foreach (string str in stringEnumerable)
            {
                CreateStringField(
                    name: fieldInfo.Name,
                    setter: changedValue => fieldInfo.SetValue(obj, changedValue),
                    value: str,
                    parent: parent
                );
            }
        }
        else if (value is IEnumerable<KeyValuePair<string, VisualNodeAttributes>> visualNodeMap)
        {
            parent = CreateNestedSettingsObject(fieldInfo.Name, parent);
            foreach (KeyValuePair<string, VisualNodeAttributes> visualNodePair in visualNodeMap)
            {
                GameObject nestedParent = CreateNestedSettingsObject(visualNodePair.Key, parent);
                visualNodePair.Value.GetType().GetFields().ForEach(nestedInfo =>
                    CreateSettingObject(nestedInfo, nestedParent, visualNodePair.Value));
            }
        }
        else if (value is IEnumerable<KeyValuePair<string, ColorRange>> colorMap)
        {
            parent = CreateNestedSettingsObject(fieldInfo.Name, parent);
            foreach (KeyValuePair<string, ColorRange> colorPair in colorMap)
            {
                GameObject nestedParent = CreateNestedSettingsObject(colorPair.Key, parent);
                colorPair.Value.GetType().GetFields().ForEach(nestedInfo =>
                    CreateSettingObject(nestedInfo, nestedParent, colorPair.Value));
            }
        }
        else if (value is IEnumerable<KeyValuePair<string, bool>> boolMap)
        {
            parent = CreateNestedSettingsObject(fieldInfo.Name, parent);
            foreach (KeyValuePair<string, bool> boolPair in boolMap)
            {
                GameObject nestedParent = CreateNestedSettingsObject(boolPair.Key, parent);
                boolPair.Value.GetType().GetFields().ForEach(nestedInfo =>
                    CreateSettingObject(nestedInfo, nestedParent, boolPair.Value));
            }
        }
        else if (value is IEnumerable<KeyValuePair<string, NodeLayoutKind>> layoutKindMap)
        {
            parent = CreateNestedSettingsObject(fieldInfo.Name, parent);
            foreach (KeyValuePair<string, NodeLayoutKind> layoutKindPair in layoutKindMap)
            {
                GameObject nestedParent = CreateNestedSettingsObject(layoutKindPair.Key, parent);
                layoutKindPair.Value.GetType().GetFields().ForEach(nestedInfo =>
                    CreateSettingObject(nestedInfo, nestedParent, layoutKindPair.Value));
            }
        }
        else if (value is IEnumerable<KeyValuePair<string, NodeShapes>> nodeShapesMap)
        {
            parent = CreateNestedSettingsObject(fieldInfo.Name, parent);
            foreach (KeyValuePair<string, NodeShapes> nodeShapesPair in nodeShapesMap)
            {
                GameObject nestedParent = CreateNestedSettingsObject(nodeShapesPair.Key, parent);
                nodeShapesPair.Value.GetType().GetFields().ForEach(nestedInfo =>
                    CreateSettingObject(nestedInfo, nestedParent, nodeShapesPair.Value));
            }
        }
        else if (value is VisualAttributes or ConfigIO.PersistentConfigItem 
                 or LabelAttributes or CoseGraphAttributes)
        {
            parent = CreateNestedSettingsObject(fieldInfo.Name, parent);
            value.GetType().GetFields().ForEach(nestedInfo => CreateSettingObject(nestedInfo, parent, value));
        }
        else if (value is IEnumerable) {
            Type entryType = value.GetType().GetInterface(typeof(IEnumerable<>).Name).GetGenericArguments()[0];
            parent = CreateNestedSettingsObject(
                    "Missing<" + entryType.GetNiceName() + ">: " + fieldInfo.Name, parent);
            Debug.LogWarning("Missing: " + fieldInfo.Name + "(" + value.GetType().GetNiceName() 
                             + ", " + "Enumerable<" + entryType.GetNiceName() + ">"
                             + ")");
        }
        else
        {
            parent = CreateNestedSettingsObject("Missing: " + fieldInfo.Name + " (" + value.GetType().GetNiceName() + ")",
                parent);
            Debug.LogWarning("Missing: " + fieldInfo.Name + "(" + value.GetType().GetNiceName() +")");
        }
    }
    private static GameObject CreateNestedSettingsObject(string name, GameObject parent)
    {
        GameObject container =
            PrefabInstantiator.InstantiatePrefab(SETTINGS_OBJECT_PREFAB, parent.transform, false);
        container.name = name;
        container.GetComponentInChildren<TextMeshProUGUI>().text = name;
        return container.transform.Find("Content").gameObject;
    }

    private static void CreateSlider(string name, RangeAttribute range, UnityAction<float> setter, float value, bool useRoundValue, GameObject parent)
    {
        range ??= new RangeAttribute(0, 2);

        GameObject sliderGameObject =
            PrefabInstantiator.InstantiatePrefab(SLIDER_PREFAB, parent.transform, false);
        sliderGameObject.name = name;
        AddLayoutElement(sliderGameObject);
        SliderManager sliderManager = sliderGameObject.GetComponentInChildren<SliderManager>();
        Slider slider = sliderGameObject.GetComponentInChildren<Slider>();
        TextMeshProUGUI text = sliderGameObject.transform.Find("Label").GetComponent<TextMeshProUGUI>();
        text.text = name;
                
        sliderManager.usePercent = false;
        sliderManager.useRoundValue = useRoundValue;
        slider.minValue = range.min;
        slider.maxValue = range.max;
                
        slider.value = value;
        slider.onValueChanged.AddListener(setter);
    }

    private static void CreateSwitch(string name, UnityAction<bool> setter, bool value, GameObject parent)
    {
        GameObject switchGameObject =
            PrefabInstantiator.InstantiatePrefab(SWITCH_PREFAB, parent.transform, false);
        switchGameObject.name = name;
        AddLayoutElement(switchGameObject);
        SwitchManager switchManager = switchGameObject.GetComponentInChildren<SwitchManager>();
        TextMeshProUGUI text = switchGameObject.transform.Find("Label").GetComponent<TextMeshProUGUI>();
        text.text = name;

        switchManager.isOn = value;
        switchManager.OnEvents.AddListener(() => setter(true));
        switchManager.OffEvents.AddListener(() => setter(false));
    }

    // TODO: Replace with actual string field prefab
    // TODO: Add action
    private static void CreateStringField(string name, UnityAction<string> setter, string value, GameObject parent)
    {
        GameObject stringGameObject =
            PrefabInstantiator.InstantiatePrefab(STRINGFIELD_PREFAB, parent.transform, false);
        stringGameObject.name = name;
        AddLayoutElement(stringGameObject);
        TextMeshProUGUI text = stringGameObject.transform.Find("Label").GetComponent<TextMeshProUGUI>();
        text.text = name;

        TMP_InputField inputField = stringGameObject.GetComponentInChildren<TMP_InputField>();
        inputField.text = value;
        inputField.onValueChanged.AddListener(setter);
    }

    // TODO: Add action
    private static void CreateDropDown(string name, UnityAction<int> setter, IEnumerable<string> values, string value, GameObject parent)
    {
        GameObject dropDownGameObject =
            PrefabInstantiator.InstantiatePrefab(DROPDOWN_PREFAB, parent.transform, false);
        dropDownGameObject.name = name;
        AddLayoutElement(dropDownGameObject);
        TextMeshProUGUI text = dropDownGameObject.transform.Find("Label").GetComponent<TextMeshProUGUI>();
        text.text = name;
        // TODO: value and setter

        CustomDropdown dropdown = dropDownGameObject.transform.Find("DropdownCombo/Dropdown").GetComponent<CustomDropdown>();
        TMP_InputField customInput = dropDownGameObject.transform.Find("DropdownCombo/Input").GetComponent<TMP_InputField>();
        Dictaphone dictaphone = dropDownGameObject.transform.Find("DropdownCombo/DictateButton").GetComponent<Dictaphone>();
        
        customInput.gameObject.SetActive(false);
        dictaphone.gameObject.SetActive(false);
        
        dropdown.isListItem = true;
        dropdown.listParent = GameObject.Find("UI Canvas").transform;
        dropdown.dropdownEvent.AddListener(setter);
        values.ForEach(s => dropdown.CreateNewItem(s, null));
        dropdown.SetupDropdown();
    }

    // TODO: Add action
    private static void CreateColorPicker(string name, GameObject parent)
    {
        parent = CreateNestedSettingsObject("Color Picker: " + name, parent);

        GameObject colorPickerGameObject =
            PrefabInstantiator.InstantiatePrefab(COLORPICKER_PREFAB, parent.transform, false);
        colorPickerGameObject.name = name;
        AddLayoutElement(colorPickerGameObject);
        TextMeshProUGUI text = colorPickerGameObject.transform.Find("Label").GetComponent<TextMeshProUGUI>();
        text.text = name;
        // TODO: Value and setter
    }

    private static void AddLayoutElement(GameObject gameObject)
    {
        LayoutElement le = gameObject.AddComponent<LayoutElement>();
        le.minHeight = ((RectTransform) gameObject.transform).rect.height;
        le.minWidth = ((RectTransform) gameObject.transform).rect.width;
    }
    

}