using UnityEngine;

namespace SEE.Game.Drawable.ActionHelpers
{
    /// <summary>
    /// This class provides a method to refresh the UI Canvas it can be necessary to prevent
    /// an error with canvas movement / display issues.
    /// </summary>
    public static class Refresher
    {
        /// <summary>
        /// The UI canvas object. Saved to prevent multiple searches.
        /// </summary>
        private static GameObject uiCanvas;

        /// <summary>
        /// Refreshes the UI canvas, necessary to prevent an error with canvas movement.
        /// </summary>
        public static void UICanvas()
        {
            if (uiCanvas == null)
            {
                uiCanvas = GameObject.Find("UI Canvas");
            }
            uiCanvas.SetActive(false);
            uiCanvas.SetActive(true);
        }
    }
}
