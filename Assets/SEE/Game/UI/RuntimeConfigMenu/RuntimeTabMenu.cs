using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
    public const string RUNTIME_CONFIG_PREFAB_FOLDER = UI_PREFAB_FOLDER + "RuntimeConfigMenu/";
    public const string SETTINGS_OBJECT_PREFAB = RUNTIME_CONFIG_PREFAB_FOLDER + "RuntimeSettingsObject";
    public const string SWITCH_PREFAB = UI_PREFAB_FOLDER + "Input Group - Switch";
    public const string SLIDER_PREFAB = UI_PREFAB_FOLDER + "Input Group - Slider";
    public const string DROPDOWN_PREFAB = UI_PREFAB_FOLDER + "Input Group - Dropdown";
    public const string COLORPICKER_PREFAB = UI_PREFAB_FOLDER + "Input Group - Color Picker";
    public const string STRINGFIELD_PREFAB = RUNTIME_CONFIG_PREFAB_FOLDER + "Input Group - StringInputField";
    public const string BUTTON_PREFAB = RUNTIME_CONFIG_PREFAB_FOLDER + "Button";
    public const string ADD_ELEMENT_BUTTON_PREFAB = RUNTIME_CONFIG_PREFAB_FOLDER + "AddButton";
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
    /// <param name="attributes"></param>
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
                    getter: () => (bool) getter(), 
                    parent: parent
                );
                break;
            case int:
                CreateSlider(
                    settingName: settingName, 
                    range: attributeArray.OfType<RangeAttribute>().ElementAtOrDefault(0), 
                    setter: changedValue => setter!((int) changedValue), 
                    getter: () => (int)getter(), 
                    useRoundValue: true, 
                    parent: parent
                );
                break;
            case uint:
                CreateSlider(
                    settingName: settingName, 
                    range: attributeArray.OfType<RangeAttribute>().ElementAtOrDefault(0), 
                    setter: changedValue => setter!((uint) changedValue), 
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
                    getter: () => (string) getter(),
                    parent: parent
                );
                break;
            case Color:
                CreateColorPicker(
                    settingName: settingName, 
                    parent: parent
                );
                break;
            case DataPath dataPath:
                parent = CreateNestedSetting(settingName, parent);
                FilePicker filePicker = parent.AddComponent<FilePicker>();
                filePicker.DataPathInstance = dataPath;
                filePicker.Label = settingName;
                filePicker.PickingMode = FileBrowser.PickMode.Files;
                filePicker.OnMenuInitialized += () => AddLayoutElement(parent.transform.Find(settingName).gameObject);
                break;
            case Enum:
                CreateDropDown(
                    settingName: settingName, 
                    setter: changedValue => setter!(Enum.ToObject(value.GetType(), changedValue)),
                    values: value.GetType().GetEnumNames(),
                    getter: () => (string) getter(),
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
                GameObject addButton = PrefabInstantiator.InstantiatePrefab(ADD_ELEMENT_BUTTON_PREFAB, parent.transform);
                addButton.name = "AddElementButton";
                ButtonManagerWithIcon addButtonManager = addButton.GetComponent<ButtonManagerWithIcon>();
                addButtonManager.clickEvent.AddListener(() => {
                    list.Add(""); 
                    UpdateListChildren(list, parent);
                    addButtonManager.transform.SetAsLastSibling();
                });
                UpdateListChildren(list, parent);
                addButtonManager.transform.SetAsLastSibling();
                OnUpdateMenuValues += () => {
                    UpdateListChildren(list, parent);
                    addButtonManager.transform.SetAsLastSibling();
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

    private void CreateSlider(string settingName, RangeAttribute range, UnityAction<float> setter, Func<float> getter, bool useRoundValue, GameObject parent)
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
                
        slider.value = getter();
        slider.onValueChanged.AddListener(setter);

        RuntimeSmallEditorButton smallEditorButton = sliderGameObject.AddComponent<RuntimeSmallEditorButton>();
        smallEditorButton.OnShowMenuChanged += () => ShowMenu = !smallEditorButton.ShowMenu;
        
        OnUpdateMenuValues += () => slider.value = getter();
    }

    private void CreateSwitch(string settingName, UnityAction<bool> setter, Func<bool> getter, GameObject parent)
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
        switchManager.OffEvents.AddListener(() => setter(false));

        RuntimeSmallEditorButton smallEditorButton = switchGameObject.AddComponent<RuntimeSmallEditorButton>();
        smallEditorButton.OnShowMenuChanged += () => ShowMenu = !smallEditorButton.ShowMenu;
        
        OnUpdateMenuValues += () =>
        {
            switchManager.isOn = getter();
            switchManager.UpdateUI();
        };
    }

    private void CreateStringField(string settingName, UnityAction<string> setter, Func<string> getter, GameObject parent)
    {
        GameObject stringGameObject =
            PrefabInstantiator.InstantiatePrefab(STRINGFIELD_PREFAB, parent.transform, false);
        stringGameObject.name = settingName;
        AddLayoutElement(stringGameObject);
        TextMeshProUGUI text = stringGameObject.transform.Find("Label").GetComponent<TextMeshProUGUI>();
        text.text = settingName;

        TMP_InputField inputField = stringGameObject.GetComponentInChildren<TMP_InputField>();
        inputField.text = getter();
        inputField.onSelect.AddListener(str => SEEInput.KeyboardShortcutsEnabled = false);
        inputField.onDeselect.AddListener(str => SEEInput.KeyboardShortcutsEnabled = true);
        inputField.onValueChanged.AddListener(setter);
        
        RuntimeSmallEditorButton smallEditorButton = stringGameObject.AddComponent<RuntimeSmallEditorButton>();
        smallEditorButton.OnShowMenuChanged += () => ShowMenu = !smallEditorButton.ShowMenu;

        OnUpdateMenuValues += () => inputField.text = getter();
    }

    // TODO: Add action
    private void CreateDropDown(string settingName, UnityAction<int> setter, IEnumerable<string> values, Func<string> getter, GameObject parent)
    {
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
        dropdown.listParent = GameObject.Find("UI Canvas").transform;
        dropdown.dropdownEvent.AddListener(setter);
        values.ForEach(s => dropdown.CreateNewItem(s, null));
        dropdown.SetupDropdown();
        
        RuntimeSmallEditorButton smallEditorButton = dropDownGameObject.AddComponent<RuntimeSmallEditorButton>();
        smallEditorButton.OnShowMenuChanged += () => ShowMenu = !smallEditorButton.ShowMenu;
        
        // TODO: Set current dropdown item
        // TODO: OnUpdateMenuValues
    }

    // TODO: Add action
    private void CreateColorPicker(string settingName, GameObject parent)
    {
        parent = CreateNestedSetting("Color Picker: " + settingName, parent);

        GameObject colorPickerGameObject =
            PrefabInstantiator.InstantiatePrefab(COLORPICKER_PREFAB, parent.transform, false);
        colorPickerGameObject.name = settingName;
        AddLayoutElement(colorPickerGameObject);
        TextMeshProUGUI text = colorPickerGameObject.transform.Find("Label").GetComponent<TextMeshProUGUI>();
        text.text = settingName;
        // TODO: Value and setter

        RuntimeSmallEditorButton smallEditorButton = colorPickerGameObject.AddComponent<RuntimeSmallEditorButton>();
        smallEditorButton.OnShowMenuChanged += () => ShowMenu = !smallEditorButton.ShowMenu;
    }

    private void UpdateListChildren(IList<string> list, GameObject parent)
    {
        // removes superfluous children
        foreach (Transform child in parent.transform)
        {
            if (child.name != "AddElementButton")
            {
                int index = Int32.Parse(child.name);
                if (index >= list.Count) Destroyer.Destroy(child.gameObject);
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
    
    private void AddLayoutElement(GameObject gameObject)
    {
        LayoutElement le = gameObject.AddComponent<LayoutElement>();
        le.minHeight = ((RectTransform) gameObject.transform).rect.height;
        le.minWidth = ((RectTransform) gameObject.transform).rect.width;
    }

    private event Action OnUpdateMenuValues;
}