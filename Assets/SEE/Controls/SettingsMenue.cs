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

    public class OpenSettings : MonoBehaviour
    {
        //The text box, which will display the text
        public TMP_Text keybindingsText;
        // Start is called before the first frame update
        //Sets the text of the textbox
        void Start()
        {
            keybindingsText.text = KeyBindings.ShowBindings();
        }

        //The settings panel which will be activated
        public GameObject settings;
        // Update is called once per frame
        //Activates the settings panel with the esc button
        void Update()
        {
            if (SEEInput.ActivateSettings())
            {
                settings.SetActive(true);
            }
        }

        //When this method is called, the application will be terminated
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