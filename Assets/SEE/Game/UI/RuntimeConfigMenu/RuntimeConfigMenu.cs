using SEE.Controls;
using SEE.GO;
using SEE.Utils;
using UnityEngine;
using Valve.VR;

namespace SEE.Game.UI.RuntimeConfigMenu
{
    public class RuntimeConfigMenu : MonoBehaviour
    {
        private SteamVR_Action_Boolean openAction = SteamVR_Actions._default?.OpenSettingsMenu;
        private const string menuPrefabPath = "Prefabs/UI/RuntimeConfigMenu";
        private const string switchPrefabPath = "Prefabs/UI/Input Group - Switch";

        private GameObject runtimeConfigMenu;

        void Awake()
        {
            Debug.Log("Awake Runtime Config Menu");

            runtimeConfigMenu = PrefabInstantiator.InstantiatePrefab(menuPrefabPath);
            runtimeConfigMenu.name = "RuntimeConfigMenu";
            runtimeConfigMenu.SetActive(false);

            RuntimeConfigMenuUtilities.AddActionToButton(runtimeConfigMenu, "Canvas/LoadCityButton", 
                () => {
                    RuntimeConfigMenuUtilities.LoadCity("mini/mini.cfg");
                    runtimeConfigMenu.SetActive(false);
                });
            
            RuntimeConfigMenuUtilities.AddActionToButton(runtimeConfigMenu, "Canvas/ResetCityButton",
                () =>
                {
                    RuntimeConfigMenuUtilities.ResetCity();
                    runtimeConfigMenu.SetActive(false);
                }
            );
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


    }
}
