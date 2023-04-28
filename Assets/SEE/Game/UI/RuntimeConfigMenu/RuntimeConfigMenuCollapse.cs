using UnityEngine;

namespace SEE.Game.UI.RuntimeConfigMenu
{
    /// <summary>
    /// Can be used to collapse the nested settings of a 'SettingsObject' in the <see cref="RuntimeTabMenu"/>.
    /// </summary>
    public class RuntimeConfigMenuCollapse : MonoBehaviour
    {
        /// <summary>
        ///     Is the setting collapsed.
        /// </summary>
        private bool visibility = true;

        /// <summary>
        ///     Toggles the visibility of the nested settings.
        /// </summary>
        /// <see cref="visibility" />
        public void OnClickCollapse()
        {
            visibility = !visibility;
            transform.parent.parent.Find("Content").gameObject.SetActive(visibility);

            // changes icon rotation when pressed
            if (visibility) transform.Find("Icon").transform.Rotate(0, 0, -90);
            else transform.Find("Icon").transform.Rotate(0, 0, 90);
        }
    }
}