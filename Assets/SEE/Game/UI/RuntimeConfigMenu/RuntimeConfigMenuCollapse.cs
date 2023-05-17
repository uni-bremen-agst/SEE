using UnityEngine;

namespace SEE.Game.UI.RuntimeConfigMenu
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
        /// Parent game object that contains all setting objects
        /// </summary>
        public GameObject mainContentContainer;

        /// <summary>
        /// Collapse icon
        /// </summary>
        public GameObject collapseIcon;

        /// <summary>
        /// Toggles the visibility of the nested settings.
        /// </summary>
        /// <see cref="visibility" />
        public void OnClickCollapse()
        {
            visibility = !visibility;

            // hide setting objects
            mainContentContainer.gameObject.SetActive(visibility);

            // change icon rotation when pressed
            if (visibility) collapseIcon.transform.Rotate(0, 0, -90);
            else collapseIcon.transform.Rotate(0, 0, 90);
        }
    }
}