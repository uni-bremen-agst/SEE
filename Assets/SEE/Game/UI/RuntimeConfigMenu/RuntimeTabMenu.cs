using System.Linq;
using System.Reflection;
using Michsky.UI.ModernUIPack;
using SEE.Game.City;
using SEE.Game.UI.Menu;
using SEE.Utils;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using UnityEngine;
using UnityEngine.UI;

public class RuntimeTabMenu : TabMenu<ToggleMenuEntry>
{
    protected const string RUNTIME_CONFIG_PREFAB_FOLDER = UI_PREFAB_FOLDER + "RuntimeConfigMenu/";
    public const string SETTINGS_OBJECT_PREFAB = RUNTIME_CONFIG_PREFAB_FOLDER + "RuntimeSettingsObject";
    public const string SWITCH_PREFAB = RUNTIME_CONFIG_PREFAB_FOLDER + "RuntimeSettingsSwitch";
    public const string FILEPICKER_PREFAB = UI_PREFAB_FOLDER + "Input Group - File Picker";
    public const string SLIDER_PREFAB = UI_PREFAB_FOLDER + "Input Group - Slider";

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
        City.GetType().GetFields().ForEach(CreateSetting);
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

        // TODO: Instantiate and setup prefabs based on member info

        GameObject setting = PrefabInstantiator.InstantiatePrefab(SETTINGS_OBJECT_PREFAB, viewContent, false);
        setting.name = memberInfo.Name;
        setting.GetComponentInChildren<Text>().text = memberInfo.Name;
        CreateSettingObject(memberInfo, setting);
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

    private void CreateSettingObject(MemberInfo memberInfo, GameObject setting)
    {
        /*
         * TODO: MembersTypes
         * Relevant: Field, Method, Property
         * Irrelevant: All, Constructor, Custom, Event, NestedType, TypeInfo
         */
        if (memberInfo is FieldInfo fieldInfo)
        {
            object value = fieldInfo.GetValue(City);
            // This is how to set the value:
            // fieldInfo.SetValue(City, value);
            if (value is FilePath)
            {
                GameObject filePickerGameObject =
                    PrefabInstantiator.InstantiatePrefab(FILEPICKER_PREFAB, setting.transform, false);
            }
            else if (value is float f)
            {
                GameObject sliderGameObject =
                    PrefabInstantiator.InstantiatePrefab(SLIDER_PREFAB, setting.transform, false);
                SliderManager sliderManager = sliderGameObject.GetComponentInChildren<SliderManager>();
                Slider slider = sliderGameObject.GetComponentInChildren<Slider>();
                
                sliderManager.usePercent = false;
                sliderManager.useRoundValue = false;
                slider.minValue = 0;
                slider.maxValue = 1;
                
                slider.value = f;
                slider.onValueChanged.AddListener(changedValue=> fieldInfo.SetValue(City, changedValue));
            }
        }
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