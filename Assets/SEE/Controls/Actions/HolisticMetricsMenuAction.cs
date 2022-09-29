using System;
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
        private HolisticMetricsManager holisticMetricsManager;
        
        /// <summary>
        /// This is used in this class to ensure the manager reference is not null. By saving this information in a
        /// variable, we avoid checking that everytime in the Update() method.
        /// </summary>
        private bool managerExists;

        /// <summary>
        /// Tries to get a reference to the HolisticMetricsManager of this scene. The reference will be saved in a
        /// property of this class.
        /// </summary>
        private void Start()
        {
            GameObject holisticMetricsGameObject = GameObject.Find("HolisticMetricsManager");
            if (holisticMetricsGameObject == null)
            {
                // This means that the holistic metrics scene component was not even placed in the scene. In this case,
                // we do not want to also try to get the manager. Therefore, we just return.
                return;
            }
            
            // We do not use a try/catch block because if the holistic metrics root game object
            // ("HolisticMetricsManager") was found but the manager script is not found, this should indeed throw an
            // exception.
            holisticMetricsManager = holisticMetricsGameObject.GetComponent<HolisticMetricsManager>();
            managerExists = true;
        }

        /// <summary>
        /// Checks whether the holistic metrics menu key is pressed and if that is the case, tries to toggle the menu.
        /// </summary>
        private void Update()
        {
            if (managerExists && SEEInput.ToggleHolisticMetricsMenu())
            {
                 holisticMetricsManager.ToggleMenu();
            }
        }
    }
}
