using DynamicPanels;
using SEE.Controls;
using SEE.DataModel;
using SEE.Game.City;
using SEE.GO;
using UnityEngine;
using UnityEngine.UI;
using Valve.VR;

namespace SEE.Game.UI.RuntimeConfigMenu
{
    public class RuntimeConfigMenu : MonoBehaviour
    {
        private SteamVR_Action_Boolean openAction;
        private const string menuPrefab = "Prefabs/UI/RuntimeConfigMenu";
        private GameObject runtimeConfigMenu;
        
        void Awake()
        {
            Debug.Log("Awake Runtime Config Menu");
            
            openAction = SteamVR_Actions._default?.OpenSettingsMenu;
            
            runtimeConfigMenu = Instantiate(Resources.Load<GameObject>(menuPrefab), Vector3.zero, Quaternion.identity);
            runtimeConfigMenu.SetActive(false);
            
            // adds a listener to the debug button
            GameObject debugButton = runtimeConfigMenu.transform.Find("Canvas/DebugButton").gameObject;
            Button debugButtonComponent = debugButton.GetComponent<Button>();
            debugButtonComponent.onClick.AddListener(
                () => SelectCity("StreamingAssets/mini/mini.cfg")
                );
        }

        void Update()
        {
            if ((PlayerSettings.GetInputType() == PlayerInputType.DesktopPlayer && SEEInput.ToggleConfigMenu()) ||
                (PlayerSettings.GetInputType() == PlayerInputType.VRPlayer && openAction != null && openAction.GetStateDown(SteamVR_Input_Sources.Any)))
            {
                runtimeConfigMenu.SetActive(!runtimeConfigMenu.activeSelf);
            }
        }

        private void OnDebugClicked()
        {
            Debug.Log("On Debug Clicked");

        }

        private void SelectCity(string cityPath)
        {
            GameObject implementation = GameObject.FindGameObjectsWithTag(Tags.CodeCity)[0];
            if (implementation)
            {
                SEECity city;
                if (implementation.TryGetComponent(out city))
                {
                    city.ConfigurationPath.RelativePath = cityPath;
                    city.LoadData();
                    city.DrawGraph();
                    city.LoadAndDrawGraph();
                    
                    runtimeConfigMenu.SetActive(false);
                }
            }
        } 
    }
}
