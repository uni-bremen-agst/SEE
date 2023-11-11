using UnityEngine.UI;
using UnityEngine;
using TMPro;
using SEE.Controls;
using SEE.Utils;

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
            Button exitButton = SettingsMenuGameObject.transform.GetChild(0).transform.GetChild(3).GetComponent<Button>();
            // adds the ExitGame method to the button
            exitButton.onClick.AddListener(ExitGame);
            // sets the text of the scrollview
            SettingsMenuGameObject.transform.GetChild(1).transform.GetChild(2).transform.GetChild(0).transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = KeyBindings.GetBindingsText();
        }

        /// <summary>
        /// Toggles the settings panel with the esc button.
        /// </summary>
        protected override void UpdateDesktop()
        {
            if (SEEInput.ToggleSettings() && SettingsMenuGameObject.transform.GetChild(1).gameObject.activeSelf == true && SettingsMenuGameObject.transform.GetChild(0).gameObject.activeSelf == false)
            {
                SettingsMenuGameObject.transform.GetChild(1).gameObject.SetActive(false);
            }
            else if (SEEInput.ToggleSettings())
            {
                SettingsMenuGameObject.transform.GetChild(0).gameObject.SetActive(!SettingsMenuGameObject.transform.GetChild(0).gameObject.activeSelf);
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