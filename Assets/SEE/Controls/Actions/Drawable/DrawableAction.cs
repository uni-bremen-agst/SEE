using UnityEngine;

namespace SEE.Controls.Actions.Drawable
{
    /// <summary>
    /// Super class of all actions dealing with drawables. Provides the
    /// common features of all drawable actions.
    /// </summary>
    public abstract class DrawableAction : AbstractPlayerAction
    {
        /// <summary>
        ///  The canvas on which the drawables are displayed.
        ///  Set by lazy initialization.
        /// </summary>
        /// <remarks>There can only be one canvas in the scene.</remarks>
        private static GameObject canvas;

        /// <summary>
        /// The canvas on which the drawables are displayed.
        /// </summary>
        protected static GameObject Canvas
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
    }
}