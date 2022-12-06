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

        private GameObject runtimeConfigMenu;

        private GameObject seeTables;

        private GameObject codeCityLoader;

        private GameObject seeSettingsView;
        
        private GameObject seeSettingsTabs;
        
        private GameObject seeSettingsContentView;

        void Awake()
        {
            Debug.Log("Awake Runtime Config Menu");

            runtimeConfigMenu = PrefabInstantiator.InstantiatePrefab(menuPrefabPath);
            runtimeConfigMenu.name = "RuntimeConfigMenu";
            runtimeConfigMenu.SetActive(false);



            seeSettingsView = runtimeConfigMenu.transform.Find("SeeSettingsPanel").gameObject;

            seeSettingsTabs = seeSettingsView.transform.Find("Tabs").gameObject;
            
            seeSettingsContentView = seeSettingsView.transform.Find("ContentView").gameObject;

            seeTables = runtimeConfigMenu.transform.Find("SeeTables").gameObject;

            codeCityLoader = runtimeConfigMenu.transform.Find("CodeCityLoader").gameObject;

            InitSettings();
        }
        
        void Update()
        {
            if ((PlayerSettings.GetInputType() == PlayerInputType.DesktopPlayer && SEEInput.ToggleConfigMenu()) ||
                (PlayerSettings.GetInputType() == PlayerInputType.VRPlayer 
                 && openAction != null && openAction.GetStateDown(SteamVR_Input_Sources.Any)))
            {
                // Menu is always displayed from the top
                // Should be changed as soon as backwards navigation has been added to each individual menu
                seeSettingsView.SetActive(false);
                codeCityLoader.SetActive(false);
                seeTables.SetActive(true);

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
            SetupSettingsSize(settingsViewSize);

            GameObject tabButtonColor= SetupTabButton("Color");
            GameObject settingsViewColor = SetupSettingsView(tabButtonColor);
            SetupSettingsColor(settingsViewColor);

            GameObject tabButtonLayout= SetupTabButton("Layout");
            GameObject settingsViewLayout= SetupSettingsView(tabButtonLayout);
            SetupSettingsLayout(settingsViewLayout);

        }

        private GameObject SetupTabButton(string tabName)
        {
            GameObject tabButton = PrefabInstantiator.InstantiatePrefab("Prefabs/UI/RuntimeTabButton", seeSettingsTabs.transform.Find("TabObjects"), false);
            tabButton.name = tabName;
            tabButton.GetComponentInChildren<TextMeshProUGUI>().text = tabName;
            return tabButton;
        }
        
        private GameObject SetupSettingsView(GameObject tabButton)
        {
            GameObject settingsView = PrefabInstantiator.InstantiatePrefab("Prefabs/UI/RuntimeSettingsView", seeSettingsContentView.transform, false);
            settingsView.name = tabButton.name + "View";
            tabButton.GetComponent<TabButtonSwitcher>().Tab = settingsView;
            return settingsView;
        }

        private void SetupSettingsNodes(GameObject settingsViewNodes)
        {
            GameObject nodeSettingTest = PrefabInstantiator.InstantiatePrefab("Prefabs/UI/RuntimeSettingsObject", settingsViewNodes.transform.Find("Content"), false);
            nodeSettingTest.GetComponentInChildren<Text>().text = "TestSetting Nodes";
        }

        private void SetupSettingsSize(GameObject settingsViewSize)
        {
            GameObject sizeSettingTest = PrefabInstantiator.InstantiatePrefab("Prefabs/UI/RuntimeSettingsObject", settingsViewSize.transform.Find("Content"), false);
            sizeSettingTest.GetComponentInChildren<Text>().text = "TestSetting Size";
        }

        private void SetupSettingsColor(GameObject settingsViewColor)
        {
            GameObject colorSettingTest = PrefabInstantiator.InstantiatePrefab("Prefabs/UI/RuntimeSettingsObject", settingsViewColor.transform.Find("Content"), false);
            colorSettingTest.GetComponentInChildren<Text>().text = "TestSetting Color";
        }

        private void SetupSettingsLayout(GameObject settingsViewLayout)
        {
            GameObject layoutSettingTest = PrefabInstantiator.InstantiatePrefab("Prefabs/UI/RuntimeSettingsObject", settingsViewLayout.transform.Find("Content"), false);
            layoutSettingTest.GetComponentInChildren<Text>().text = "TestSetting Layout";
        }

    }
}
