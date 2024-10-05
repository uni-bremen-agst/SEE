using UnityEngine;

namespace SEE.UI
{
    /// <summary>
    /// Offers the game object for the UI canvas.
    /// </summary>
    internal static class UICanvas
    {
        /// <summary>
        ///  The canvas on which the drawable menus are displayed.
        ///  Set by lazy initialization.
        /// </summary>
        private static GameObject canvas;

        /// <summary>
        /// The canvas on which the drawable menus are displayed.
        /// </summary>
        internal static GameObject Canvas
        {
            get
            {
                if (canvas == null)
                {
                    canvas = GameObject.Find("UI Canvas");
                }
                return canvas;
            }
        }

        /// <summary>
        /// Refreshes the UI canvas, necessary to prevent an error with canvas movement.
        /// </summary>
        public static void Refresh()
        {
            Canvas.SetActive(false);
            Canvas.SetActive(true);
        }
    }
}
