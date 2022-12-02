using Crosstales;
using SEE.Controls;
using SEE.GO;
using SEE.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Valve.VR;

namespace SEE.Game.UI.RuntimeConfigMenu
{
    public class RuntimeConfigMenu : MonoBehaviour
    {
        private SteamVR_Action_Boolean openAction = SteamVR_Actions._default?.OpenSettingsMenu;
        private const string menuPrefabPath = "Prefabs/UI/RuntimeConfigMenu";
        private const string switchPrefabPath = "Prefabs/UI/Input Group - Switch";

        private string settingsPanelName = "SeeSettingPanel";

        private GameObject runtimeConfigMenu;

        private GameObject seeSettingsView;
        
        private GameObject seeSettingsTabs;
        
        private GameObject seeSettingsContentView;

        void Awake()
        {
            Debug.Log("Awake Runtime Config Menu");

            runtimeConfigMenu = PrefabInstantiator.InstantiatePrefab(menuPrefabPath);
            runtimeConfigMenu.name = "RuntimeConfigMenu";
            runtimeConfigMenu.SetActive(false);

            RuntimeConfigMenuUtilities.AddActionToButton(runtimeConfigMenu, "LoadCityButton", 
                () => {
                    Debug.Log("Load City: mini.cfg");
                    RuntimeConfigMenuUtilities.LoadCity("mini/mini.cfg");
                    runtimeConfigMenu.SetActive(false);
                });
            
            RuntimeConfigMenuUtilities.AddActionToButton(runtimeConfigMenu, "ResetCityButton",
                () =>
                {
                    Debug.Log("Reset City.");
                    RuntimeConfigMenuUtilities.ResetCity();
                    runtimeConfigMenu.SetActive(false);
                }
            );

            seeSettingsView = runtimeConfigMenu.transform.Find("SeeSettingsPanel").gameObject;

            seeSettingsTabs = seeSettingsView.transform.Find("Tabs").gameObject;
            
            seeSettingsContentView = seeSettingsView.transform.Find("ContentView").gameObject;
            

            InitSettings();
        }
        
        void Update()
        {
            if ((PlayerSettings.GetInputType() == PlayerInputType.DesktopPlayer && SEEInput.ToggleConfigMenu()) ||
                (PlayerSettings.GetInputType() == PlayerInputType.VRPlayer 
                 && openAction != null && openAction.GetStateDown(SteamVR_Input_Sources.Any)))
            {
                runtimeConfigMenu.SetActive(!runtimeConfigMenu.activeSelf);
            }
        }

        private void InitSettings()
        {
            // 1. TabButton
            // 2. SettingsView
            // 3. SettingsObjects in der SettingsView

            GameObject tabButtonNodes = SetupTabButton("Nodes");
            GameObject settingsViewNodes = SetupSettingsView(tabButtonNodes);
            SetupSettingsNodes(settingsViewNodes);
            // first Tab is visible
            settingsViewNodes.SetActive(true);

            GameObject tabButtonSize= SetupTabButton("Size");
            GameObject settingsViewSize = SetupSettingsView(tabButtonSize);

            GameObject tabButtonColor= SetupTabButton("Color");
            GameObject settingsViewColor = SetupSettingsView(tabButtonColor);
            
            GameObject tabButtonLayout= SetupTabButton("Layout");
            GameObject settingsViewLayout= SetupSettingsView(tabButtonLayout);
            
            
            //
            // 
            //
            // GameObject tableButton = PrefabInstantiator.InstantiatePrefab("Prefabs/UI/RuntimeTableButton");
            // tableButton.transform.parent = tableView.transform;
            // tableButton.name = "DebugButton";
            // Button debugButtonComponent = tableButton.GetComponent<Button>();
            // UnityAction action = () =>
            // {
            //     seeTables.SetActive(false);
            //     seeSettingsPanel.SetActive(true);
            // };
            //
            // debugButtonComponent.onClick.AddListener(action);
        }

        private void SetupSettingsNodes(GameObject settingsViewNodes)
        {
            GameObject nodesettingTest = PrefabInstantiator.InstantiatePrefab("Prefabs/UI/RuntimeSettingsObject", settingsViewNodes.transform.Find("Content"), false);
            nodesettingTest.GetComponentInChildren<Text>().text = "TEST";
        }

        private GameObject SetupTabButton(string tabName)
        {
            GameObject tabButton = PrefabInstantiator.InstantiatePrefab("Prefabs/UI/RuntimeTabButton", seeSettingsTabs.transform.Find("TabObjects"), false);
            tabButton.name = tabName;
            // tabButton.GetComponentInChildren<TextMeshProUGUI>().name = name;
            // tabButton.GetComponentInChildren<TextMeshProUGUI>().SetText(name);

            //TODO name für TabButton
            return tabButton;
        }
        
        private GameObject SetupSettingsView(GameObject tabButton)
        {
            GameObject settingsView = PrefabInstantiator.InstantiatePrefab("Prefabs/UI/RuntimeSettingsView", seeSettingsContentView.transform, false);
            settingsView.name = tabButton.name + "View";
            tabButton.GetComponent<TabButtonSwitcher>().Tab = settingsView;
            return settingsView;
        }




    }
}
