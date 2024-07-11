using System.Collections;
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
        /// Refreshes the UI canvas, necessary to prevent an error with canvas movement.
        /// </summary>
        public static void UICanvas()
        {
            GameObject UICanvas = GameObject.Find("UI Canvas");
            UICanvas.SetActive(false);
            UICanvas.SetActive(true);
        }
    }
}