using UnityEngine;

namespace SEE.Controls
{
    /// <summary>
    /// Toggles the browser on and off with the key F4.
    /// </summary>
    public class DialogueCanvas : MonoBehaviour
    {
        /// <summary>
        /// The browser, which will be toggled.
        /// </summary>
        public GameObject Browser;

        /// <summary>
        /// When the key F4 is pressed,
        /// the browser and its buttons
        /// will be activated when they
        /// are deactivated and vice versa.
        /// </summary>
        private void Update()
        {
            if (SEEInput.ToggleBrowser())
            {
                //disables/enables the Browser
                Browser.SetActive(!Browser.activeSelf);
                //disables/enables the Buttons
                foreach (Transform child in gameObject.transform)
                {
                    child.gameObject.SetActive(!child.gameObject.activeSelf);
                }
            }
        }
    }
}
