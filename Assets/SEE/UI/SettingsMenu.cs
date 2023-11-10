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
        /// Prefab for the Settingsmenu.
        /// </summary>
        protected virtual string SettingsPrefab => UIPrefabFolder + "SettingsMenu";

        /// <summary>
        /// The SettingsMenu game object.
        /// </summary>
        protected GameObject Settingsmenu;

        /// <summary>
        /// Sets the text of the textbox and adds the onClick event ExitGame to the ExitButton.
        /// </summary>
        protected override void StartDesktop()
        {
            // instantiates the SettingsMenu
            Settingsmenu = PrefabInstantiator.InstantiatePrefab(SettingsPrefab, Canvas.transform, false);
            Button exitButton = Settingsmenu.transform.GetChild(0).transform.GetChild(3).GetComponent<Button>();
            // adds the ExitGame method to the button
            exitButton.onClick.AddListener(ExitGame);
            var keybindingsText = Settingsmenu.transform.GetChild(1).transform.GetChild(2).transform.GetChild(0).transform.GetChild(0);
            // sets the text of the scrollview
            keybindingsText.GetComponent<TextMeshProUGUI>().text = KeyBindings.ShowBindings();
        }

        /// <summary>
        /// Toggles the settings panel with the esc button.
        /// </summary>
        protected override void UpdateDesktop()
        {
            if (Settingsmenu.transform.GetChild(1).gameObject.activeSelf == true && Settingsmenu.transform.GetChild(0).gameObject.activeSelf == false && SEEInput.ToggleSettings())
            {
                Settingsmenu.transform.GetChild(1).gameObject.SetActive(false);
            }
            else if (SEEInput.ToggleSettings())
            {
                Settingsmenu.transform.GetChild(0).gameObject.SetActive(!Settingsmenu.transform.GetChild(0).gameObject.activeSelf);
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