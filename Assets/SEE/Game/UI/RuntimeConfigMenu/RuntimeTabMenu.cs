using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Michsky.UI.ModernUIPack;
using SEE.DataModel;
using SEE.Game;
using SEE.Game.City;
using SEE.Game.UI.FilePicker;
using SEE.Game.UI.Menu;
using SEE.Utils;
using SimpleFileBrowser;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Object = System.Object;
using Random = UnityEngine.Random;

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
    protected virtual void CreateSetting(MemberInfo memberInfo)
    {
        // gets the view game object
        GameObject view = GetViewGameObjectHelper(memberInfo);
        Transform viewContent = view.transform.Find("Content");
        // Debug.Log("\t"+"CreateSetting____ "+ memberInfo.Name);
        // TODO: Instantiate and setup prefabs based on member info

        GameObject setting = PrefabInstantiator.InstantiatePrefab(SETTINGS_OBJECT_PREFAB, viewContent, false);
        setting.name = memberInfo.Name;
        setting.GetComponentInChildren<TextMeshProUGUI>().text = memberInfo.Name;
        GameObject settingContent = setting.transform.Find("Content").gameObject;
        CreateSettingObject(memberInfo, settingContent, City);
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

    private void CreateSettingObject(MemberInfo memberInfo, GameObject parent, object obj)
    {
        /*
         * TODO: MembersTypes
         * Relevant: Field, Method, Property
         * Irrelevant: All, Constructor, Custom, Event, NestedType, TypeInfo
         */
        
        if (memberInfo is FieldInfo fieldInfo)
        {
            Debug.Log("Create " + memberInfo.Name + " (" + fieldInfo.GetValue(obj).GetType() + ")");
            object value = fieldInfo.GetValue(obj);
            if (value is bool b)
            {
                CreateSwitch(
                    name: memberInfo.Name, 
                    setter: changedValue => fieldInfo.SetValue(obj, changedValue), 
                    value: b, 
                    parent: parent
                    );
            }
            else if (value is int i)
            {
                CreateSlider(
                    name: memberInfo.Name, 
                    range: memberInfo.GetAttributes().OfType<RangeAttribute>().ElementAtOrDefault(0), 
                    setter: changedValue => fieldInfo.SetValue(obj, (int) changedValue), 
                    value: i, 
                    useRoundValue: true, 
                    parent: parent
                    );
            }
            else if (value is uint ui)
            {
                CreateSlider(
                    name: memberInfo.Name, 
                    range: memberInfo.GetAttributes().OfType<RangeAttribute>().ElementAtOrDefault(0), 
                    setter: changedValue => fieldInfo.SetValue(obj, (int) changedValue), 
                    value: ui, 
                    useRoundValue: true, 
                    parent: parent
                );
            }
            else if (value is float f)
            {
                CreateSlider(
                    name: memberInfo.Name, 
                    range: memberInfo.GetAttributes().OfType<RangeAttribute>().ElementAtOrDefault(0), 
                    setter: changedValue => fieldInfo.SetValue(obj, changedValue), 
                    value: f, 
                    useRoundValue: false, 
                    parent: parent
                );
            }
            else if (value is string s)
            {
                CreateStringField(
                    name: memberInfo.Name,
                    setter: changedValue => fieldInfo.SetValue(obj, changedValue),
                    value: s,
                    parent: parent
                    );
            }
            else if (value is Color c)
            {
                CreateColorPicker(
                    name: memberInfo.Name, 
                    parent: parent
                    );
            }
            else if (value is DataPath dataPath)
            {
                FilePicker filePicker = parent.AddComponent<FilePicker>();
                filePicker.DataPathInstance = dataPath;
                filePicker.Label = memberInfo.Name;
                filePicker.PickingMode = FileBrowser.PickMode.Files;
            }
            else if (value.GetType().IsEnum)
            {
                CreateDropDown(fieldInfo.Name, parent);
            }
            else if (value is VisualAttributes visualAttribute)
            {
                // TODO: Is this right?
                value.GetType().GetMembers().ForEach(attribute => CreateSettingObject(attribute, parent, value));
            }
            else if (value is IEnumerable enumerable)
            {
                GameObject container =
                    PrefabInstantiator.InstantiatePrefab(SETTINGS_OBJECT_PREFAB, parent.transform, false);
                container.name = fieldInfo.Name;
                container.GetComponentInChildren<TextMeshProUGUI>().text = fieldInfo.Name;

                GameObject content = container.transform.Find("Content").gameObject;
                // TODO: Correct handling of each enumerable type?
                if (enumerable is IEnumerable<KeyValuePair<string,VisualNodeAttributes>> visualDict)
                {
                    foreach (KeyValuePair<string, VisualNodeAttributes> visual in visualDict)
                    {
                        GameObject nodeType = PrefabInstantiator.InstantiatePrefab(SETTINGS_OBJECT_PREFAB, content.transform, false);
                        nodeType.name = visual.Key;
                        nodeType.GetComponentInChildren<TextMeshProUGUI>().text = visual.Key;
                        visual.Value.GetType().GetMembers().ForEach(nestedMember => CreateSettingObject(
                            nestedMember, nodeType.transform.Find("Content").gameObject, visual.Value)
                        );
                    }
                }
                else if (enumerable is IEnumerable<KeyValuePair<string, ColorRange>> colorDict)
                {
                    foreach (KeyValuePair<string, ColorRange> visual in colorDict)
                    {
                        GameObject nodeType = PrefabInstantiator.InstantiatePrefab(SETTINGS_OBJECT_PREFAB, content.transform, false);
                        nodeType.name = visual.Key;
                        nodeType.GetComponentInChildren<TextMeshProUGUI>().text = visual.Key;
                        visual.Value.GetType().GetMembers().ForEach(nestedMember => CreateSettingObject(
                            nestedMember, nodeType.transform.Find("Content").gameObject, visual.Value)
                        );
                    }
                }
                else if (enumerable is IEnumerable<string> hashSet)
                {
                    foreach(string str in hashSet)
                    {
                        str.GetType().GetMembers().ForEach(info => CreateSettingObject(info, content, value));   
                    }
                }
                else
                {
                    Debug.Log("Missing Enumerable: " + fieldInfo.Name + " "  + enumerable.GetType());
                }
            }
            else
            {
                Debug.Log("Missing " + fieldInfo.Name + " (" + value.GetType() + ")" + " ");
            }
        } else if (memberInfo is MethodInfo methodInfo)
        {
            // TODO: Buttons für Methoden
        }
        // TODO: Some fields don't implement VisualAttributes (or LayoutSettings)
        // CoseGraphSettings, LabelSettings, AntennaSettings

    }

    private void CreateSlider(string name, RangeAttribute range, UnityAction<float> setter, float value, bool useRoundValue, GameObject parent)
    {
        range ??= new RangeAttribute(0, 2);
        
        GameObject sliderGameObject =
            PrefabInstantiator.InstantiatePrefab(SLIDER_PREFAB, parent.transform, false);
        AddLayoutElement(sliderGameObject);
        SliderManager sliderManager = sliderGameObject.GetComponentInChildren<SliderManager>();
        UnityEngine.UI.Slider slider = sliderGameObject.GetComponentInChildren<UnityEngine.UI.Slider>();
        TextMeshProUGUI text = sliderGameObject.transform.Find("Label").GetComponent<TextMeshProUGUI>();
        text.text = name;
                
        sliderManager.usePercent = false;
        sliderManager.useRoundValue = useRoundValue;
        slider.minValue = range.min;
        slider.maxValue = range.max;
                
        slider.value = value;
        slider.onValueChanged.AddListener(setter);
    }

    private void CreateSwitch(string name, UnityAction<bool> setter, bool value, GameObject parent)
    {
        GameObject switchGameObject =
            PrefabInstantiator.InstantiatePrefab(SWITCH_PREFAB, parent.transform, false);
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
    private void CreateStringField(string name, UnityAction<string> setter, string value, GameObject parent)
    {
        GameObject stringGameObject =
            PrefabInstantiator.InstantiatePrefab(STRINGFIELD_PREFAB, parent.transform, false);
        AddLayoutElement(stringGameObject);
        TextMeshProUGUI text = stringGameObject.transform.Find("Label").GetComponent<TextMeshProUGUI>();
        text.text = name;

        TMP_InputField inputField = stringGameObject.GetComponentInChildren<TMP_InputField>();
        inputField.text = value;
        inputField.onValueChanged.AddListener(setter);
    }

    // TODO: Add action
    private void CreateDropDown(string name, GameObject parent)
    {
        GameObject dropDownGameObject =
            PrefabInstantiator.InstantiatePrefab(DROPDOWN_PREFAB, parent.transform, false);
        AddLayoutElement(dropDownGameObject);
        TextMeshProUGUI text = dropDownGameObject.transform.Find("Label").GetComponent<TextMeshProUGUI>();
        text.text = name;
        // TODO: value and setter
    }

    // TODO: Add action
    private void CreateColorPicker(string name, GameObject parent)
    {
        GameObject colorPickerGameObject =
            PrefabInstantiator.InstantiatePrefab(COLORPICKER_PREFAB, parent.transform, false);
        AddLayoutElement(colorPickerGameObject);
        TextMeshProUGUI text = colorPickerGameObject.transform.Find("Label").GetComponent<TextMeshProUGUI>();
        text.text = name;
        // TODO: Value and setter
    }
    
    /// <summary>
    /// Sets the misc button as the last in the tab list.
    /// </summary>
    protected virtual void SetMiscAsLastTab()
    {
        ToggleMenuEntry miscEntry = Entries.FirstOrDefault(entry => entry.Title == "Misc");
        if (miscEntry != null) EntryGameObject(miscEntry).transform.SetAsLastSibling();
    }

    private void AddLayoutElement(GameObject go)
    {
        LayoutElement le = go.AddComponent<LayoutElement>();
        le.minHeight = ((RectTransform) go.transform).rect.height;
        le.minWidth = ((RectTransform) go.transform).rect.width;
    }
    

}