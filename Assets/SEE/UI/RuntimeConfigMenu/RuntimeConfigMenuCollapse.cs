using UnityEngine;

namespace SEE.UI.RuntimeConfigMenu
{
    /// <summary>
    /// Can be used to collapse the nested settings of a 'SettingsObject' in the <see cref="RuntimeTabMenu"/>.
    /// </summary>
    public class RuntimeConfigMenuCollapse : MonoBehaviour
    {
        /// <summary>
        /// Is the setting collapsed.
        /// </summary>
        private bool visibility = true;

        /// <summary>
        /// Gets the value of <see cref="visibility"/>.
        /// </summary>
        public bool Visibility => visibility;

        /// <summary>
        /// Parent game object that contains all setting objects.
        /// </summary>
        public GameObject MainContentContainer;

        /// <summary>
        /// Collapse icon.
        /// </summary>
        public GameObject CollapseIcon;

        /// <summary>
        /// Toggles the visibility of the nested settings.
        /// <see cref="visibility" />
        /// </summary>
        public void OnClickCollapse()
        {
            visibility = !visibility;

            // hide setting objects
            MainContentContainer.gameObject.SetActive(visibility);

            // change icon rotation when pressed
            if (visibility)
            {
                CollapseIcon.transform.Rotate(0, 0, -90);
            }
            else
            {
                CollapseIcon.transform.Rotate(0, 0, 90);
            }
        }
    }
}
