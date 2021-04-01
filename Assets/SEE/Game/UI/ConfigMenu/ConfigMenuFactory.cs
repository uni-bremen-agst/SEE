using SEE.GO;
using UnityEngine;
using Valve.VR;
using PlayerSettings = SEE.Controls.PlayerSettings;
namespace SEE.Game.UI.ConfigMenu
{
    public class ConfigMenuFactory : DynamicUIBehaviour
    {
        private static readonly EditableInstance DefaultInstanceToEdit = EditableInstance.Implementation;
        private static readonly string ConfigMenuPrefabPath = "Assets/Prefabs/UI/ConfigMenu.prefab";

        private readonly SteamVR_Action_Boolean _openAction =
            SteamVR_Actions._default.OpenSettingsMenu;
        private readonly SteamVR_Input_Sources _inputSource = SteamVR_Input_Sources.Any;

        private GameObject _configMenuPrefab;
        private ConfigMenu _configMenu;
        private bool _isModPressed;

        private void Awake()
        {
            _configMenuPrefab = MustLoadPrefabAtPath(ConfigMenuPrefabPath);
            BuildConfigMenu(DefaultInstanceToEdit);
        }

        private void BuildConfigMenu(EditableInstance instanceToEdit)
        {
            Instantiate(_configMenuPrefab).MustGetComponent(out _configMenu);
            _configMenu.CurrentlyEditing = instanceToEdit;
            _configMenu.OnInstanceChangeRequest.AddListener(ReplaceMenu);
        }

        private void ReplaceMenu(EditableInstance newInstance)
        {
            Destroy(_configMenu.gameObject);
            BuildConfigMenu(newInstance);
        }

        private void Update()
        {
            if (PlayerSettings.GetInputType() == PlayerInputType.DesktopPlayer)
                HandleDesktopUpdate();
            else if (PlayerSettings.GetInputType() == PlayerInputType.VRPlayer)
                HandleVRUpdate();
        }
        private void HandleDesktopUpdate()
        {
            if (Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift))
                _isModPressed = true;
            if (Input.GetKeyUp(KeyCode.LeftShift) || Input.GetKeyUp(KeyCode.RightShift))
                _isModPressed = false;

            if (_isModPressed && Input.GetKeyUp(KeyCode.Escape))
                _configMenu.Toggle();
        }

        private void HandleVRUpdate()
        {
            if (_openAction.GetStateDown(_inputSource))
                _configMenu.Toggle();
        }
    }
}
