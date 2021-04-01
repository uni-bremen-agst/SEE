using SEE.GO;
using UnityEngine;
using Valve.VR;
using PlayerSettings = SEE.Controls.PlayerSettings;
namespace SEE.Game.UI.ConfigMenu
{
    public class ConfigMenuFactory : DynamicUIBehaviour
    {
        private const string ConfigMenuPrefabPath = "Assets/Prefabs/UI/ConfigMenu.prefab";
        private readonly SteamVR_Action_Boolean _openAction =
            SteamVR_Actions._default.OpenSettingsMenu;
        private readonly SteamVR_Input_Sources _inputSource = SteamVR_Input_Sources.Any;

        private GameObject _configMenuPrefab;
        private ConfigMenu _configMenu;
        private bool _isModPressed;

        private void Awake()
        {
            _configMenuPrefab = MustLoadPrefabAtPath(ConfigMenuPrefabPath);
            Instantiate(_configMenuPrefab).MustGetComponent(out _configMenu);
            // _configMenu.OnInstanceChangeRequest.AddListener(instance => );
        }

        private void ReplaceMenu(EditableInstance newInstance)
        {
            Destroy(_configMenu.gameObject);
            Instantiate(_configMenuPrefab).MustGetComponent(out _configMenu);
            _configMenu.CurrentlyEditing = newInstance;
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
