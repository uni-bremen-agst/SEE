using SEE.Game.HolisticMetrics;
using UnityEngine;

namespace SEE.Controls.Actions
{
    /// <summary>
    /// This class, when the key for the holistic metrics menu is pressed, calls the ToggleMenu() method of the
    /// HolisticMetricsManager (if there is one).
    /// </summary>
    public class HolisticMetricsMenuAction : MonoBehaviour
    {
        /// <summary>
        /// Reference to the holistic metrics manager of this scene (if there is one).
        /// </summary>
        private MenuManager menuManager;

        /// <summary>
        /// Tries to get a reference to the HolisticMetricsManager of this scene. The reference will be saved in a
        /// property of this class.
        /// </summary>
        private void Start()
        {
            menuManager = GameObject.Find("HolisticMetricsManager").GetComponent<MenuManager>();
        }

        /// <summary>
        /// Checks whether the holistic metrics menu key is pressed and if that is the case, tries to toggle the menu.
        /// </summary>
        private void Update()
        {
            if (SEEInput.ToggleHolisticMetricsMenu())
            {
                 menuManager.ToggleMenu();
            }
        }
    }
}
