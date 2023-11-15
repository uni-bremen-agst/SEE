using UnityEngine.UI;
using UnityEngine;
using TMPro;
using SEE.Controls;
using SEE.Utils;
using SEE.GO;

namespace SEE.UI
{
    /// <summary>
    /// Handles the user interactions with the settings menu.
    /// </summary>
    public class SettingsMenu : PlatformDependentComponent
    {
        /// <summary>
        /// Prefab for the <see cref="SettingsMenu"/>.
        /// </summary>
        private string SettingsPrefab => UIPrefabFolder + "SettingsMenu";

        /// <summary>
        /// The game object instantiated for the <see cref="SettingsPrefab"/>.
        /// </summary>
        private GameObject settingsMenuGameObject;

        /// <summary>
        /// Sets the text of the textbox and adds the onClick event ExitGame to the ExitButton.
        /// </summary>
        protected override void StartDesktop()
        {
            // instantiates the SettingsMenu
            settingsMenuGameObject = PrefabInstantiator.InstantiatePrefab(SettingsPrefab, Canvas.transform, false);
            Button exitButton = settingsMenuGameObject.transform.Find("SettingsPanel/ExitButton").gameObject.MustGetComponent<Button>();
            // adds the ExitGame method to the button
            exitButton.onClick.AddListener(ExitGame);
            // sets the text of the scrollview
            settingsMenuGameObject.transform.Find("KeybindingsPanel/KeybindingsText/Viewport/Content").gameObject.MustGetComponent<TextMeshProUGUI>().text = KeyBindings.GetBindingsText();
        }

        /// <summary>
        /// Toggles the settings panel with the ESC button.
        /// </summary>
        protected override void UpdateDesktop()
        {
            if (SEEInput.ToggleSettings())
            {
                Transform keybindingsPanel = settingsMenuGameObject.transform.Find("KeybindingsPanel");
                GameObject settingsPanel = settingsMenuGameObject.transform.Find("SettingsPanel").gameObject;
                if (keybindingsPanel.gameObject.activeSelf && !settingsPanel.activeSelf)
                {
                    // handles the case where the user is in the KeybindingsPanel but wants to close it
                    keybindingsPanel.gameObject.SetActive(false);
                }                
                else
                {
                    // handles the case where the user wants to open/close the SettingsPanel
                    settingsPanel.SetActive(!settingsPanel.activeSelf);
                }
            }
        }

        /// <summary>
        /// Terminates the application (exits the game).
        /// </summary>
        private void ExitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}