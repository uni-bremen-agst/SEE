using Michsky.UI.ModernUIPack;
using SEE.Controls;
using SEE.Game.City;
using SEE.GO;
using SEE.Utils;
using System.Collections.Generic;
using SEE.Game.UI.Menu;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Valve.VR;

namespace SEE.Game.UI.RuntimeConfigMenu
{
    public class RuntimeConfigMenu : MonoBehaviour
    {
        // paths of prefabs
        private const string PREFAB_FOLDER =  "Prefabs/UI/RuntimeConfigMenu/";
        public const string MENU_PREFAB_PATH = PREFAB_FOLDER + "RuntimeConfigMenu";
        public const string TAB_BUTTON_PREFAB_PATH = PREFAB_FOLDER + "RuntimeTabButton";
        public const string SETTINGS_VIEW_PREFAB_PATH = PREFAB_FOLDER + "RuntimeSettingsView";
        public const string SETTINGS_OBJECT_PREFAB_PATH = PREFAB_FOLDER + "RuntimeSettingsObject";
        public const string SWITCH_PREFAB_PATH = PREFAB_FOLDER + "RuntimeSettingsSwitch";
        public const string TABLE_BUTTON_PREFAB_PATH = PREFAB_FOLDER + "RuntimeTableButton";
    
        private SteamVR_Action_Boolean openAction = SteamVR_Actions._default?.OpenSettingsMenu;
        private GameObject runtimeConfigMenu;

        private GameObject seeTables;

        private GameObject codeCityLoader;

        private GameObject seeSettingsView;
        
        private GameObject seeSettingsTabs;
        
        private static GameObject seeSettingsContentView;

        private static SEECity city;

        private RuntimeTabMenu Menu;
        
        void Start()
        {
            Menu = gameObject.AddComponent<RuntimeTabMenu>();
            Menu.Title = "Tab Menu Titel";
            Menu.Description = "Ich bin die Beschreibung.";
            Menu.HideAfterSelection = false;

            Menu.AddEntry(new ToggleMenuEntry(() => { },()=>{},  "Ferdinand", "Ich bin der Ferdinand.", Color.blue));
            Menu.AddEntry(new ToggleMenuEntry(() => { },()=>{},  "Mahmoud", "Ich bin der Mahmoud.", Color.cyan));
            Menu.AddEntry(new ToggleMenuEntry(() => { },()=>{},  "Thilo", "Ich bin der Thilo.", Color.yellow));
            
            Menu.AddEntry(new ToggleMenuEntry(() => { },()=>{},  "Ferdinand1", "Ich bin der Ferdinand.", Color.blue));
            Menu.AddEntry(new ToggleMenuEntry(() => { },()=>{},  "Mahmoud1", "Ich bin der Mahmoud.", Color.cyan));
            Menu.AddEntry(new ToggleMenuEntry(() => { },()=>{},  "Thilo1", "Ich bin der Thilo.", Color.yellow));
            
            Menu.AddEntry(new ToggleMenuEntry(() => { },()=>{},  "Ferdinand2", "Ich bin der Ferdinand.", Color.blue));
            Menu.AddEntry(new ToggleMenuEntry(() => { },()=>{},  "Mahmoud2", "Ich bin der Mahmoud.", Color.cyan));
            Menu.AddEntry(new ToggleMenuEntry(() => { },()=>{},  "Thilo2", "Ich bin der Thilo.", Color.yellow));
        }
        
        void Update()
        {
            if (SEEInput.ToggleConfigMenu())
            {
                Menu.ToggleMenu();
            }
        }

        // Creating the Menu without the individual settings at Awake
        private void InitMenu()
        {
            // 1. TabButton
            // 2. SettingsView
            // 3. SettingsObjects in der SettingsView

            GameObject tabButtonNodes = SetupTabButton("Nodes");
            GameObject settingsViewNodes = SetupSettingsView(tabButtonNodes);
            // first Tab is visible
            settingsViewNodes.SetActive(true);

            GameObject tabButtonSize= SetupTabButton("Size");
            GameObject settingsViewSize = SetupSettingsView(tabButtonSize);

            GameObject tabButtonColor= SetupTabButton("Color");
            GameObject settingsViewColor = SetupSettingsView(tabButtonColor);

            GameObject tabButtonLayout= SetupTabButton("Layout");
            GameObject settingsViewLayout= SetupSettingsView(tabButtonLayout);

        }

        // Adding the settings to the Menu after a city was loaded
        public static void InitSettings()
        {
            foreach (Transform settingsview in seeSettingsContentView.transform)
            {
                foreach(Transform setting in settingsview.Find("Content"))
                {
                    Destroy(setting.gameObject);
                }
            }

            if(city != null)
            {
                // TODO after adding the settings to the menu, the vertical layout group only puts the elements in order
                //      after switching to another tab
                //      already tried: Canvas.ForceUpdateCanvases() and LayoutRebuilder.ForceRebuildLayoutImmediate()
                SetupSettingsNodes();
                
                SetupSettingsSize();

                SetupSettingsColor();

                SetupSettingsLayout();
            }
            
        }

        // Tabs
        private GameObject SetupTabButton(string tabName)
        {
            GameObject tabButton = PrefabInstantiator.InstantiatePrefab(TAB_BUTTON_PREFAB_PATH, seeSettingsTabs.transform.Find("TabObjects"), false);
            tabButton.name = tabName;
            tabButton.GetComponentInChildren<TextMeshProUGUI>().text = tabName;
            return tabButton;
        }
        
        // Views
        private GameObject SetupSettingsView(GameObject tabButton)
        {
            GameObject settingsView = PrefabInstantiator.InstantiatePrefab(SETTINGS_VIEW_PREFAB_PATH, seeSettingsContentView.transform, false);
            settingsView.name = tabButton.name + "View";
            tabButton.GetComponent<TabButtonSwitcher>().Tab = settingsView;
            return settingsView;
        }

        // Settings
        private static void SetupSettingsNodes()
        {
            foreach(var type in city.NodeTypes)
            {
                // Display NodeType
                GameObject nodeType = PrefabInstantiator.InstantiatePrefab(SETTINGS_OBJECT_PREFAB_PATH, seeSettingsContentView.transform.Find("NodesView").transform.Find("Content"), false);
                nodeType.GetComponentInChildren<Text>().text = type.Key;
                nodeType.GetComponentInChildren<Text>().fontStyle = FontStyle.Bold;

                // Display Visibility of NodeType
                GameObject nodeTypeVisibility = PrefabInstantiator.InstantiatePrefab(SWITCH_PREFAB_PATH, seeSettingsContentView.transform.Find("NodesView").transform.Find("Content"), false);
                nodeTypeVisibility.GetComponentInChildren<SwitchManager>().isOn = type.Value.IsRelevant;
                //nodeTypeVisibility.GetComponentInChildren<TextMeshProUGUI>().fontSize = nodeType.GetComponent<Text>().fontSize;
                //nodeTypeVisibility.GetComponentInChildren<TextMeshProUGUI>().color = nodeType.GetComponent<Text>().color;
                nodeTypeVisibility.GetComponentInChildren<TextMeshProUGUI>().text = "Is Relevant?";
                nodeTypeVisibility.GetComponentInChildren<Button>().onClick.AddListener(() => changeVisibility(type));

                // Display Shape of NodeType
                GameObject nodeTypeShape = PrefabInstantiator.InstantiatePrefab(SETTINGS_OBJECT_PREFAB_PATH, seeSettingsContentView.transform.Find("NodesView").transform.Find("Content"), false);
                nodeTypeShape.GetComponentInChildren<Text>().text = type.Value.Shape.ToString();
            }
        }

        private static void SetupSettingsSize()
        {
            GameObject sizeSettingTest = PrefabInstantiator.InstantiatePrefab(SETTINGS_OBJECT_PREFAB_PATH, seeSettingsContentView.transform.Find("SizeView").transform.Find("Content"), false);
            sizeSettingTest.GetComponentInChildren<Text>().text = "TestSetting Size";
        }

        private static void SetupSettingsColor()
        {
            GameObject colorSettingTest = PrefabInstantiator.InstantiatePrefab(SETTINGS_OBJECT_PREFAB_PATH, seeSettingsContentView.transform.Find("ColorView").transform.Find("Content"), false);
            colorSettingTest.GetComponentInChildren<Text>().text = "TestSetting Color";
        }

        private static void SetupSettingsLayout()
        {
            GameObject layoutSettingTest = PrefabInstantiator.InstantiatePrefab(SETTINGS_OBJECT_PREFAB_PATH, seeSettingsContentView.transform.Find("LayoutView").transform.Find("Content"), false);
            layoutSettingTest.GetComponentInChildren<Text>().text = "TestSetting Layout";
        }

        public static void SetSEECity(SEECity newCity)
        {
            city = newCity;
        }

        private static void changeVisibility(KeyValuePair<string, VisualNodeAttributes> type)
        {
            type.Value.IsRelevant = !type.Value.IsRelevant;
            city.ReDrawGraph();
        }

    }
}
