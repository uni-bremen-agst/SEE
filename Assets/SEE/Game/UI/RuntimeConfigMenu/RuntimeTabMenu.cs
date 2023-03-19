using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using Michsky.UI.ModernUIPack;
using SEE.Controls;
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
using Random = UnityEngine.Random;
using Slider = UnityEngine.UI.Slider;

public class RuntimeTabMenu : TabMenu<ToggleMenuEntry>
{
    protected const string RUNTIME_CONFIG_PREFAB_FOLDER = UI_PREFAB_FOLDER + "RuntimeConfigMenu/";
    public const string SETTINGS_OBJECT_PREFAB = RUNTIME_CONFIG_PREFAB_FOLDER + "RuntimeSettingsObject";
    public const string SWITCH_PREFAB = UI_PREFAB_FOLDER + "Input Group - Switch";
    public const string SMALLWINDOW_PREFAB = RUNTIME_CONFIG_PREFAB_FOLDER + "RuntimeConfig_SmallConfigWindow";
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

        LoadCity(0);
        SetupCitySwitcher();
    }

    protected virtual void CreateButton(MemberInfo memberInfo)
    {
        if (memberInfo.CustomAttributes.Any(attribute => attribute.AttributeType == typeof(RuntimeButtonAttribute)))
        {
            Transform buttonContent = Content.transform.Find("ConfigButtons/Content");
            GameObject button = PrefabInstantiator.InstantiatePrefab(BUTTON_PREFAB, buttonContent, false);
            button.name = memberInfo.GetCustomAttribute<RuntimeButtonAttribute>().Label;
            ButtonManagerWithIcon buttonManager = button.GetComponent<ButtonManagerWithIcon>();
            buttonManager.buttonText = memberInfo.GetCustomAttribute<RuntimeButtonAttribute>().Label;
            UnityEvent buttonEvent = new();
            buttonEvent.AddListener(() => { City.Invoke(memberInfo.Name, 0); StartCoroutine(ClearAndLoadCity(citySwitcher.index)); });
            buttonManager.clickEvent =  buttonEvent;

            //Debug.Log("\t"+"CreateButton____ "+ memberInfo.Name);
        }
    }

    protected void ClearCity()
    {
        Entries.Reverse().ForEach(RemoveEntry);
        foreach (Transform button in Content.transform.Find("ConfigButtons/Content"))
        {
            Destroyer.Destroy(button.gameObject);
        }
        //TODO Remove Buttons as well: Listener und Buttons muessen zu TabMenu.cs hinzugeuegt werden
        MenuManager.UpdateUI();
    }
    
    public void LoadCity(int i)
    {
        City = GameObject.FindGameObjectsWithTag(Tags.CodeCity)[i].GetComponent<AbstractSEECity>();
        City.GetType().GetMembers().ForEach(memberInfo =>
        {
            if (memberInfo.DeclaringType == typeof(AbstractSEECity) || 
                memberInfo.DeclaringType!.IsSubclassOf(typeof(AbstractSEECity))) 
                CreateSetting(memberInfo, null, City);
        });
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
    private GameObject CreateOrGetViewGameObject(IEnumerable<Attribute> attributes)
    {
        string tabName = attributes.OfType<RuntimeFoldoutAttribute>().FirstOrDefault()?.name ??
                         attributes.OfType<PropertyGroupAttribute>().FirstOrDefault()?.GroupName ??
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
        if (memberInfo.GetAttributes<ObsoleteAttribute>().Count() == 0)
        {
            switch (memberInfo)
            {
                case FieldInfo fieldInfo:
                    if (fieldInfo.IsLiteral || fieldInfo.IsInitOnly) return;
                    CreateSetting(
                        getter: () => fieldInfo.GetValue(obj),
                        name: memberInfo.Name,
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
                        name: memberInfo.Name,
                        parent: parent,
                        setter: changedValue => propertyInfo.SetValue(obj, changedValue),
                        attributes: memberInfo.GetCustomAttributes()
                    );
                    break;
            }
        }
    }

    private void CreateSetting(Func<object> getter, string name, GameObject parent, 
        UnityAction<object> setter = null, IEnumerable<Attribute> attributes = null)
    {
        parent ??= CreateOrGetViewGameObject(attributes ?? Enumerable.Empty<Attribute>()).transform.Find("Content").gameObject;

        object value = getter();
        switch (value)
        {
            case bool bValue:
                CreateSwitch(
                    name: name, 
                    setter: changedValue => setter!(changedValue), 
                    getter: () => (bool) getter(), 
                    parent: parent
                );
                break;
            case int iValue:
                CreateSlider(
                    name: name, 
                    range: attributes?.OfType<RangeAttribute>().ElementAtOrDefault(0), 
                    setter: changedValue => setter!((int) changedValue), 
                    getter: () => (float)(int)getter(), 
                    useRoundValue: true, 
                    parent: parent
                );
                break;
            case uint uiValue:
                CreateSlider(
                    name: name, 
                    range: attributes?.OfType<RangeAttribute>().ElementAtOrDefault(0), 
                    setter: changedValue => setter!((uint) changedValue), 
                    getter: () => (float)(uint)getter(), 
                    useRoundValue: true, 
                    parent: parent
                );
                break;
            case float fValue:
                CreateSlider(
                    name: name, 
                    range: attributes?.OfType<RangeAttribute>().ElementAtOrDefault(0), 
                    setter: changedValue => setter!(changedValue), 
                    getter: () => (float)getter(), 
                    useRoundValue: false, 
                    parent: parent
                );
                break;
            case string sValue:
                CreateStringField(
                    name: name,
                    setter: changedValue => setter!(changedValue),
                    getter: () => (string)getter(),
                    parent: parent
                );
                break;
            case Color cValue:
                CreateColorPicker(
                    name: name, 
                    parent: parent
                );
                break;
            case DataPath dataPath:
                parent = CreateNestedSetting(name, parent);
                FilePicker filePicker = parent.AddComponent<FilePicker>();
                filePicker.DataPathInstance = dataPath;
                filePicker.Label = name;
                filePicker.PickingMode = FileBrowser.PickMode.Files;
                filePicker.OnMenuInitialized += () => AddLayoutElement(parent.transform.Find(name).gameObject);
                break;
            case Enum:
                CreateDropDown(
                    name: name, 
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
                    name: name,
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
                    name: name,
                    parent: parent,
                    setter: null,
                    // TODO: Which attributes? Both or which one?
                    attributes: antennaInfo.GetCustomAttributes().Concat(attributes ?? Enumerable.Empty<Attribute>())
                );
                break;
            // from here on come nested settings
            case HashSet<string> hashSet:
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
                parent = CreateNestedSetting(name, parent);
                foreach (object key in dict.Keys)
                {
                    CreateSetting(
                        getter: () => dict[key],
                        name: key.ToString(),
                        parent: parent,
                        setter: changedValue => dict[key] = changedValue
                    );
                }
                break;
            case IList<string> list:
                parent = CreateNestedSetting( name, parent);
                CreateListSetting( list, parent);
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
                parent = CreateNestedSetting(name, parent);
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
                    Debug.Log("Missing: (Maybe)" + name + " " + value.GetType().GetNiceName());
                }
                parent = CreateNestedSetting(name, parent);
                value.GetType().GetMembers().ForEach(nestedInfo => CreateSetting(nestedInfo, parent, value));
                break;
            default:
                Debug.LogWarning("Missing: " + name + " " + value?.GetType().GetNiceName());
                break;
        }
    } 
    
    private GameObject CreateNestedSetting(string name, GameObject parent)
    {
        GameObject container =
            PrefabInstantiator.InstantiatePrefab(SETTINGS_OBJECT_PREFAB, parent.transform, false);
        container.name = name;
        container.GetComponentInChildren<TextMeshProUGUI>().text = name;
        return container.transform.Find("Content").gameObject;
    }

    private void CreateSlider(string name, RangeAttribute range, UnityAction<float> setter, Func<float> getter, bool useRoundValue, GameObject parent)
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
                
        slider.value = getter();
        slider.onValueChanged.AddListener(setter);
        
        sliderGameObject.AddComponent<Button>().onClick.AddListener(() =>  ShowSmallEditorSlider(name, range, setter,getter() , useRoundValue));
        
        OnUpdateMenuValues += () => slider.value = getter();
    }
    
    private void ShowSmallEditorSlider(string name, RangeAttribute range, UnityAction<float> setter, float value, bool useRoundValue)
    {
        Action<GameObject> createSlider = delegate(GameObject parentObject)
        {
            CreateSlider(name, range, setter, () => value, useRoundValue, parentObject.transform.Find("Content").gameObject);
        };

        ShowSmallEditor(createSlider);
    }

    private void CreateSwitch(string name, UnityAction<bool> setter, Func<bool> getter, GameObject parent)
    {
        GameObject switchGameObject =
            PrefabInstantiator.InstantiatePrefab(SWITCH_PREFAB, parent.transform, false);
        switchGameObject.name = name;
        AddLayoutElement(switchGameObject);
        SwitchManager switchManager = switchGameObject.GetComponentInChildren<SwitchManager>();
        TextMeshProUGUI text = switchGameObject.transform.Find("Label").GetComponent<TextMeshProUGUI>();
        text.text = name;

        switchManager.isOn = getter();
        switchManager.UpdateUI();
        switchManager.OnEvents.AddListener(() => setter(true));
        switchManager.OffEvents.AddListener(() => setter(false));

        switchGameObject.AddComponent<Button>().onClick.AddListener(() => ShowSmallEditorSwitch(name,setter,getter()));
        
        OnUpdateMenuValues += () =>
        {
            switchManager.isOn = getter();
            switchManager.UpdateUI();
        };
    }
    
    private void ShowSmallEditorSwitch(string name, UnityAction<bool> setter, bool value)
    {
        Action<GameObject> createSwitch = delegate(GameObject parentObject)
        {
            CreateSwitch(name, setter, () => value, parentObject.transform.Find("Content").gameObject);
        };
        ShowSmallEditor(createSwitch);
    }

    private void CreateStringField(string name, UnityAction<string> setter, Func<string> getter, GameObject parent)
    {
        GameObject stringGameObject =
            PrefabInstantiator.InstantiatePrefab(STRINGFIELD_PREFAB, parent.transform, false);
        stringGameObject.name = name;
        AddLayoutElement(stringGameObject);
        TextMeshProUGUI text = stringGameObject.transform.Find("Label").GetComponent<TextMeshProUGUI>();
        text.text = name;

        TMP_InputField inputField = stringGameObject.GetComponentInChildren<TMP_InputField>();
        inputField.text = getter();
        inputField.onSelect.AddListener(str => SEEInput.KeyboardShortcutsEnabled = false);
        inputField.onDeselect.AddListener(str => SEEInput.KeyboardShortcutsEnabled = true);
        inputField.onValueChanged.AddListener(setter);
        
        stringGameObject.AddComponent<Button>().onClick.AddListener(() => ShowSmallEditorStringField(name,setter,getter()));

        OnUpdateMenuValues += () => inputField.text = getter();
    }
    
    private void ShowSmallEditorStringField(string name, UnityAction<string> setter, string value)
    {
        Action<GameObject> createStringField = delegate(GameObject parentObject)
        {
            CreateStringField(name, setter, () => value, parentObject.transform.Find("Content").gameObject);
        };
        ShowSmallEditor(createStringField);
    }

    // TODO: Add action
    private void CreateDropDown(string name, UnityAction<int> setter, IEnumerable<string> values, Func<string> getter, GameObject parent)
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
        
        dropDownGameObject.AddComponent<Button>().onClick.AddListener(() => ShowSmallEditorDropDown(name,setter, values, getter()));
        
        // TODO: Set current dropdown item
        // TODO: OnUpdateMenuValues
    }
    
    private void ShowSmallEditorDropDown(string name, UnityAction<int> setter, IEnumerable<string> values, string value)
    {
        Action<GameObject> createDropDown = delegate(GameObject parentObject)
        {
            CreateDropDown(name, setter, values, () => value, parentObject.transform.Find("Content").gameObject);
        };
        ShowSmallEditor(createDropDown);
    }

    // TODO: Add action
    private void CreateColorPicker(string name, GameObject parent)
    {
        parent = CreateNestedSetting("Color Picker: " + name, parent);

        GameObject colorPickerGameObject =
            PrefabInstantiator.InstantiatePrefab(COLORPICKER_PREFAB, parent.transform, false);
        colorPickerGameObject.name = name;
        AddLayoutElement(colorPickerGameObject);
        TextMeshProUGUI text = colorPickerGameObject.transform.Find("Label").GetComponent<TextMeshProUGUI>();
        text.text = name;
        // TODO: Value and setter


        colorPickerGameObject.AddComponent<Button>().onClick.AddListener(() => ShowSmallEditorColorPicker(name));
    }
    
    private void ShowSmallEditorColorPicker(string name)
    {
        Action<GameObject> createColorPicker = delegate(GameObject parentObject)
        {
            CreateColorPicker(name, parentObject.transform.Find("Content").gameObject);
        };
        ShowSmallEditor(createColorPicker);
    }

    private void CreateListSetting(IList<string> list, GameObject parent)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int iCopy = i;
            CreateSetting(
                getter: () => list[iCopy],
                name: i.ToString(),
                parent: parent,
                setter: changedValue => list[iCopy] = changedValue as string
            );
        }
        GameObject AddButton = PrefabInstantiator.InstantiatePrefab(RUNTIME_CONFIG_PREFAB_FOLDER + "AddButton", parent.transform);
        ButtonManagerWithIcon AddButtonManager = AddButton.GetComponent<ButtonManagerWithIcon>();
        AddButtonManager.clickEvent.AddListener(() => { list.Add(""); foreach (Transform child in parent.transform) { Destroyer.Destroy(child.gameObject); } CreateListSetting(list, parent); } );
    }

    private void AddLayoutElement(GameObject gameObject)
    {
        LayoutElement le = gameObject.AddComponent<LayoutElement>();
        le.minHeight = ((RectTransform) gameObject.transform).rect.height;
        le.minWidth = ((RectTransform) gameObject.transform).rect.width;
    }
    
    private void ShowSmallEditor(Action<GameObject> createSettingObject)
    {
        if (!ShowMenu) return;
        
        ToggleMenu();

        GameObject containerGameObject =
            PrefabInstantiator.InstantiatePrefab(SMALLWINDOW_PREFAB, Canvas.transform, false);

        createSettingObject(containerGameObject);

        containerGameObject.transform.Find("CloseButton").GetComponent<Button>().onClick.AddListener(() =>
        {
            Destroyer.Destroy(containerGameObject);
            // TODO Updaten der UI Settings
            OnUpdateMenuValues?.Invoke();
            ToggleMenu();
        });
    }

    private event Action OnUpdateMenuValues;
}