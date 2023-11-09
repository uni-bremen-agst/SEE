using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace SEE.Controls
{
    /// <summary>
    /// Handles the actions in the
    /// settings canvas.
    /// </summary>
    public class SettingsMenue : MonoBehaviour
    {
        /// <summary>
        /// The text box, which will display the text.
        /// </summary>
        public TMP_Text keybindingsText;

        /// <summary>
        /// The settings panel which will be toggled.
        /// </summary>
        public GameObject settingsPanel;

        /// <summary>
        /// The keybindings panel which will be toggled.
        /// </summary>
        public GameObject keybindingsPanel;

        /// <summary>
        /// Start is called before the first frame update.
        /// Sets the text of the textbox.
        /// </summary>
        void Start()
        {
            keybindingsText.text = KeyBindings.ShowBindings();
        }

        /// <summary>
        /// Update is called once per frame.
        /// Activates the settings panel with the esc button.
        /// </summary>
        void Update()
        {
            if (keybindingsPanel.activeSelf == true && settingsPanel.activeSelf == false && SEEInput.ToggleSettings())
            {
                keybindingsPanel.SetActive(false);
            }
            else if (SEEInput.ToggleSettings())
            {
                settingsPanel.SetActive(!settingsPanel.activeSelf);
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