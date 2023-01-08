using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SEE.Game.UI.ConfigMenu;
using SEE.Game.UI.Menu;
using SEE.Utils;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class RuntimeTabMenu : TabMenu<ToggleMenuEntry>
{
    protected const string RUNTIME_CONFIG_PREFAB_FOLDER = UI_PREFAB_FOLDER + "RuntimeConfigMenu/";
    public const string SETTINGS_OBJECT_PREFAB = RUNTIME_CONFIG_PREFAB_FOLDER + "RuntimeSettingsObject";
    public const string SWITCH_PREFAB = RUNTIME_CONFIG_PREFAB_FOLDER + "RuntimeSettingsSwitch";


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
    /// The city settings.
    /// </summary>
    private readonly List<MemberInfo> settings = new();

    /// <summary>
    /// Adds a setting to the menu.
    /// </summary>
    /// <param name="memberInfo"></param>
    public void AddSetting(MemberInfo memberInfo)
    {
        settings.Add(memberInfo);
        OnSettingAdded?.Invoke(memberInfo);
    }

    /// <summary>
    /// Triggers when a setting was added.
    /// </summary>
    public event UnityAction<MemberInfo> OnSettingAdded;

    /// <summary>
    /// Updates the menu and adds listeners.
    /// </summary>
    protected override void OnStartFinished()
    {
        base.OnStartFinished();
        settings.ForEach(CreateSetting);
        OnSettingAdded += CreateSetting;
    }

    /// <summary>
    /// Creates the UI for a setting.
    /// </summary>
    /// <param name="memberInfo"></param>
    protected virtual void CreateSetting(MemberInfo memberInfo)
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
        // gets the view game object
        GameObject view = ViewGameObject(entry);
        Transform viewContent = view.transform.Find("Content");
        
        // TODO: Instantiate and setup prefabs based on member info
        
        GameObject setting = PrefabInstantiator.InstantiatePrefab(SETTINGS_OBJECT_PREFAB, viewContent, false);
        setting.name = memberInfo.Name;
        setting.GetComponentInChildren<Text>().text = memberInfo.Name;

    }
}
