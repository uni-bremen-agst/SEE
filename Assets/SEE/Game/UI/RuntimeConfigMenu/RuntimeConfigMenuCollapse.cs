using UnityEngine;

namespace SEE.Game.UI.RuntimeConfigMenu
{
    public class RuntimeConfigMenuCollapse : MonoBehaviour
    {
        /// <summary>
        ///     Are the settings collapsed
        /// </summary>
        private bool _settingVisibility = true;

        /// <summary>
        ///     All settings of the SettingObject are shown/hidden
        /// </summary>
        /// <see cref="_settingVisibility" />
        public void OnClickCollapse()
        {
            _settingVisibility = !_settingVisibility;
            transform.parent.parent.Find("Content").gameObject.SetActive(_settingVisibility);

            // change rotation when pressed
            if (_settingVisibility) transform.Find("Icon").transform.Rotate(0, 0, -90);
            else transform.Find("Icon").transform.Rotate(0, 0, 90);
        }
    }
}