using UnityEngine.UI;
using UnityEngine;
using TMPro;
using SEE.Controls;
using SEE.Utils;
using SEE.GO;

namespace SEE.UI
{
    /// <summary>
    /// Handles the actions in the
    /// settings prefab.
    /// </summary>
    public class SettingsMenu : PlatformDependentComponent
    {
        /// <summary>
        /// Prefab for the SettingsMenu.
        /// </summary>
        protected virtual string SettingsPrefab => UIPrefabFolder + "SettingsMenu";

        /// <summary>
        /// The SettingsMenu game object.
        /// </summary>
        protected GameObject SettingsMenuGameObject;

        /// <summary>
        /// Sets the text of the textbox and adds the onClick event ExitGame to the ExitButton.
        /// </summary>
        protected override void StartDesktop()
        {
            // instantiates the SettingsMenu
            SettingsMenuGameObject = PrefabInstantiator.InstantiatePrefab(SettingsPrefab, Canvas.transform, false);
            Button exitButton = SettingsMenuGameObject.transform.Find("SettingsPanel/ExitButton").gameObject.MustGetComponent<Button>();
            // adds the ExitGame method to the button
            exitButton.onClick.AddListener(ExitGame);
            // sets the text of the scrollview
            SettingsMenuGameObject.transform.Find("KeybindingsPanel/KeybindingsText/Viewport/Content").gameObject.MustGetComponent<TextMeshProUGUI>().text = KeyBindings.GetBindingsText();
        }

        /// <summary>
        /// Toggles the settings panel with the esc button.
        /// </summary>
        protected override void UpdateDesktop()
        {
            // handels the case, that the user is in the KeybindingsPanel but wants to close it
            if (SEEInput.ToggleSettings() && SettingsMenuGameObject.transform.Find("KeybindingsPanel").gameObject.activeSelf == true && SettingsMenuGameObject.transform.Find("SettingsPanel").gameObject.activeSelf == false)
            {
                SettingsMenuGameObject.transform.Find("KeybindingsPanel").gameObject.SetActive(false);
            }
            // handels the case where to user wants to open/close the SettingsPanel
            else if (SEEInput.ToggleSettings())
            {
                SettingsMenuGameObject.transform.Find("SettingsPanel").gameObject.SetActive(!SettingsMenuGameObject.transform.Find("SettingsPanel").gameObject.activeSelf);
            }
        }

        /// <summary>
        /// When this method is called, the application will be terminated.
        /// </summary>
        public void ExitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}