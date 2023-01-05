using System.Collections;
using System.Collections.Generic;
using SEE.Game.UI.Menu;
using UnityEngine;

public class RuntimeTabMenu : TabMenu<ToggleMenuEntry>
{
    protected const string RUNTIME_CONFIG_PREFAB_FOLDER = UI_PREFAB_FOLDER + "RuntimeCOnfigMenu/";
    
    protected override string MenuPrefab => RUNTIME_CONFIG_PREFAB_FOLDER + "RuntimeConfigMenu";
    protected override string ViewPrefab => RUNTIME_CONFIG_PREFAB_FOLDER + "RuntimeSettingsObject";
    protected override string EntryPrefab => RUNTIME_CONFIG_PREFAB_FOLDER + "RuntimeTabButton";
    
    // is already part of the MenuPrefab
    protected override string ViewListPrefab => null;
    
    // is already part of the MenuPrefab
    protected override string EntryListPrefab => null;
    
    // which sprite should be used as the icon
    protected override string IconSprite => base.IconSprite;
    // TODO: where can be specific parts of the menu be found
    protected override string ViewListPath => base.ViewListPath;
    protected override string IconTitlePath => base.IconTitlePath;
    protected override string CloseButtonPath => base.CloseButtonPath;
    protected override string ContentPath => base.ContentPath;
    protected override string EntryListPath => base.EntryListPath;
    
    // TODO: Add Setting to View
}
