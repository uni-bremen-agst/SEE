using System.IO;
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
            // Instantiating the metrics manager here makes development on the branch for the holistic metrics
            // significantly easier for me, because this means the object is always in the scene when playing, but it
            // is not in the scene file and I don't have to resolve merge conflicts in that file when it is being
            // changed by merging master changes into the branch. When I am done developing, what needs to be done is:
            // TODO: Put HolisticMetricsManager in the scene per default, then only get a reference to it here.
            string pathToPrefab = Path.Combine(
                "Prefabs", 
                "HolisticMetrics", 
                "SceneComponents", 
                "HolisticMetricsManager");
            GameObject holisticMetricsPrefab = Resources.Load<GameObject>(pathToPrefab);
            GameObject holisticMetricsManager = Instantiate(holisticMetricsPrefab);
            menuManager = holisticMetricsManager.GetComponent<MenuManager>();
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
