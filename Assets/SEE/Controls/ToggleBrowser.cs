using UnityEngine;

namespace SEE.Controls
{
    /// <summary>
    /// Toggles the browser on and off if the user requests so.
    /// </summary>
    /// <remarks>This component is assumed to be attached to the browser canvas.</remarks>
    public class ToggleBrowser : MonoBehaviour
    {
        /// <summary>
        /// The browser, which will be toggled.
        /// </summary>
        /// <remarks>This field will be set in the inspector and is assumed
        /// to refer to the browser to be toggled.</remarks>
        [Tooltip("The Internet browser. A game object with a Browser component attached to it.")]
        public GameObject Browser;

        /// <summary>
        /// Toggles (activates/deactivates) the browser and and all immediate children
        /// of the game object this component is attached to, if the user requests so.
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
