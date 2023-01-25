using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Michsky.UI.ModernUIPack;
using SEE.DataModel;
using SEE.Game;
using SEE.Game.City;
using SEE.Game.UI.Menu;
using SEE.Layout.NodeLayouts.Cose;
using SEE.Utils;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
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
    protected override string MenuPrefab => RUNTIME_CONFIG_PREFAB_FOLDER + "RuntimeConfigMenuRework";
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
    protected override string ContentPath => "SeeSettingsPanel/MainContent";
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
        Debug.Log("\t"+"CreateSetting____ "+ memberInfo.Name);
        // TODO: Instantiate and setup prefabs based on member info

        GameObject setting = PrefabInstantiator.InstantiatePrefab(SETTINGS_OBJECT_PREFAB, viewContent, false);
        setting.name = memberInfo.Name;
        setting.GetComponentInChildren<TextMeshProUGUI>().text = memberInfo.Name;
        GameObject settingContent = setting.transform.Find("Content").gameObject;
        CreateSettingObject(memberInfo, settingContent, City);
    }

    protected void ClearCity()
    {
        Entries.Reverse().ForEach(RemoveEntry);
    }
    
    public void LoadCity(int i)
    {
        City = GameObject.FindGameObjectsWithTag(Tags.CodeCity)[i].GetComponent<AbstractSEECity>();
        City.GetType().GetFields().ForEach(CreateSetting);
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
            // Title = citySwitcher.itemList[index].itemTitle;
            ClearCity();
            LoadCity(index);
        });
    }

    /// <summary>
    /// Returns the view game object.
    /// Adds an entry if necessary.
    /// </summary>
    /// <param name="memberInfo"></param>
    /// <returns></returns>
    private GameObject GetViewGameObjectHelper(MemberInfo memberInfo)
    {
        string tabName = memberInfo.GetCustomAttributes().OfType<TabGroupAttribute>().FirstOrDefault()?.TabName ??
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
            object value = fieldInfo.GetValue(obj);
            // This is how to set the value:
            // fieldInfo.SetValue(City, value);

            // TODO: Add action
            if (value is FilePath)
            {
                CreateFilePicker(fieldInfo, parent);
            }
            else if (value is int i)
            {
                CreateSlider(memberInfo, changedValue => fieldInfo.SetValue(obj, (int) changedValue), i, true, parent);
            }
            else if (value is float f)
            {
                CreateSlider(memberInfo, changedValue => fieldInfo.SetValue(obj, changedValue), f, false, parent);
            }
            else if (value is bool b)
            {
                CreateSwitch(memberInfo, changedValue => fieldInfo.SetValue(obj, changedValue), b, parent);
            }
            // TODO: Add action
            else if (value is string s)
            {
                CreateStringField(memberInfo, parent);
            }
            // TODO: Add action
            else if (value is Color c)
            {
                CreateColorPicker(memberInfo, parent);
            }
            // TODO: Add action
            else if (value is UInt32 ui)
            {
                CreateSlider(memberInfo, changedValue => fieldInfo.SetValue(obj, (int)changedValue), ((int) ui), true, parent);
            }
            // TODO: Add action
            else if (value is   
                    NodeShapes or
                    NodeLayoutKind or
                    EdgeLayoutKind or
                    PropertyKind)
            {
                CreateDropDown(memberInfo, parent);
            }
            else if (value is NodeTypeVisualsMap nodeTypeVisualsMap)
            {
                /*nodeTypeVisualsMap.Values.ForEach(nodeType =>
                    nodeType.GetType().GetMembers().ForEach(nestedMember => CreateSettingObject(nestedMember, parent, nodeType)));*/
                var enumerator = nodeTypeVisualsMap.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    GameObject nodeType = PrefabInstantiator.InstantiatePrefab(SETTINGS_OBJECT_PREFAB, parent.transform, false);
                    nodeType.name = enumerator.Current.Key;
                    nodeType.GetComponentInChildren<TextMeshProUGUI>().text = enumerator.Current.Key;
                    enumerator.Current.Value.GetType().GetMembers().ForEach(nestedMember => CreateSettingObject(nestedMember, nodeType.transform.Find("Content").gameObject, enumerator.Current.Value));
                }
            }
            // list values with nested settings
            else if (value is 
                     NodeLayoutAttributes or 
                     EdgeLayoutAttributes or 
                     CoseGraphAttributes or 
                     EdgeSelectionAttributes or 
                     ErosionAttributes or 
                     BoardAttributes or
                     AntennaAttributes or
                     LabelAttributes or
                     ColorProperty)
            {
                value.GetType().GetMembers().ForEach(attribute => CreateSettingObject(attribute, parent, value));
            }
            else if (value is HashSet<string> hs)
            {
                foreach(string str in hs)
                {
                    str.GetType().GetMembers().ForEach(stringvalue => CreateSettingObject(stringvalue, parent, value));   
                }
            }
            else if (value is List<string> l)
            {
                foreach (string str in l)
                {
                    str.GetType().GetMembers().ForEach(stringvalue => CreateSettingObject(stringvalue, parent, value));
                }
            }
            else if (value is Dictionary<string, NodeShapes> dictshapes)
            {
                foreach (string key in dictshapes.Keys)
                {
                    dictshapes[key].GetType().GetMembers().ForEach(nodeshape => CreateSettingObject(nodeshape, parent, value));
                }
            }
            else if (value is Dictionary<string, NodeLayoutKind> dictkind)
            {
                foreach (string key in dictkind.Keys)
                {
                    dictkind[key].GetType().GetMembers().ForEach(kind => CreateSettingObject(kind, parent, value));
                }
            }
            else if (value is Dictionary<string, Boolean> dictbool)
            {
                foreach (string key in dictbool.Keys)
                {
                    dictbool[key].GetType().GetMembers().ForEach(boolean => CreateSettingObject(boolean, parent, value));
                }
            }
            else if (value is ColorMap cm)
            {
                var enumerator = cm.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    GameObject colorRange = PrefabInstantiator.InstantiatePrefab(SETTINGS_OBJECT_PREFAB, parent.transform, false);
                    colorRange.name = enumerator.Current.Key;
                    colorRange.GetComponentInChildren<TextMeshProUGUI>().text = enumerator.Current.Key;
                    enumerator.Current.Value.GetType().GetMembers().ForEach(nestedMember => CreateSettingObject(nestedMember, colorRange.transform.Find("Content").gameObject, enumerator.Current.Value));
                }
            }
            else
            {
                Debug.Log("Unknown Setting Type: " + memberInfo.ToString());
            }
        } else if (memberInfo is MethodInfo methodInfo)
        {
            // TODO: Buttons für Methoden
        }
    }

    private void CreateSlider(MemberInfo memberInfo, Action<float> setter, float value, bool useRoundValue, GameObject parent)
    {
        GameObject sliderGameObject =
            PrefabInstantiator.InstantiatePrefab(SLIDER_PREFAB, parent.transform, false);
        RangeAttribute range = memberInfo.GetAttributes().OfType<RangeAttribute>().ElementAtOrDefault(0) 
                               ?? new RangeAttribute(0, 2);
        SliderManager sliderManager = sliderGameObject.GetComponentInChildren<SliderManager>();
        Slider slider = sliderGameObject.GetComponentInChildren<Slider>();
        TextMeshProUGUI text = sliderGameObject.transform.Find("Label").GetComponent<TextMeshProUGUI>();
        text.text = memberInfo.Name;
                
        sliderManager.usePercent = false;
        sliderManager.useRoundValue = useRoundValue;
        slider.minValue = range.min;
        slider.maxValue = range.max;
                
        slider.value = value;
        slider.onValueChanged.AddListener(changedValue=>
        {
            setter(changedValue);
            if (City is SEECity seeCity) seeCity.ReDrawGraph();
        });
    }

    private void CreateSwitch(MemberInfo memberInfo, Action<bool> setter, bool value, GameObject parent)
    {
        GameObject switchGameObject =
            PrefabInstantiator.InstantiatePrefab(SWITCH_PREFAB, parent.transform, false);
        SwitchManager switchManager = switchGameObject.GetComponentInChildren<SwitchManager>();
        TextMeshProUGUI text = switchGameObject.transform.Find("Label").GetComponent<TextMeshProUGUI>();
        text.text = memberInfo.Name;

        switchManager.isOn = value;
        switchManager.OnEvents.AddListener(() =>
        {
            setter(true);
            if (City is SEECity seeCity) seeCity.ReDrawGraph();
        });
        switchManager.OffEvents.AddListener(() =>
        {
            setter(false);
            if (City is SEECity seeCity) seeCity.ReDrawGraph();
        });
    }

    // TODO: Add action
    private void CreateFilePicker(MemberInfo memberInfo, GameObject parent)
    {
        GameObject filePickerGameObject =
            PrefabInstantiator.InstantiatePrefab(FILEPICKER_PREFAB, parent.transform, false);
        TextMeshProUGUI text = filePickerGameObject.transform.Find("Label").GetComponent<TextMeshProUGUI>();
        text.text = memberInfo.Name;
    }

    // TODO: Replace with actual string field prefab
    // TODO: Add action
    private void CreateStringField(MemberInfo memberInfo, GameObject parent)
    {
        GameObject switchGameObject =
            PrefabInstantiator.InstantiatePrefab(SWITCH_PREFAB, parent.transform, false);
        TextMeshProUGUI text = switchGameObject.transform.Find("Label").GetComponent<TextMeshProUGUI>();
        text.text = memberInfo.Name;
    }

    // TODO: Add action
    private void CreateDropDown(MemberInfo memberInfo, GameObject parent)
    {
        GameObject dropDownGameObject =
            PrefabInstantiator.InstantiatePrefab(DROPDOWN_PREFAB, parent.transform, false);
        TextMeshProUGUI text = dropDownGameObject.transform.Find("Label").GetComponent<TextMeshProUGUI>();
        text.text = memberInfo.Name;
    }

    // TODO: Add action
    private void CreateColorPicker(MemberInfo  memberInfo, GameObject parent)
    {
        GameObject colorPickerGameObject =
            PrefabInstantiator.InstantiatePrefab(COLORPICKER_PREFAB, parent.transform, false);
        TextMeshProUGUI text = colorPickerGameObject.transform.Find("Label").GetComponent<TextMeshProUGUI>();
        text.text = memberInfo.Name;
    }
    
    /// <summary>
    /// Sets the misc button as the last in the tab list.
    /// </summary>
    protected virtual void SetMiscAsLastTab()
    {
        ToggleMenuEntry miscEntry = Entries.FirstOrDefault(entry => entry.Title == "Misc");
        if (miscEntry != null) EntryGameObject(miscEntry).transform.SetAsLastSibling();
    }
    

}