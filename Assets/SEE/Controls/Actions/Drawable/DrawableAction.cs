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
        ///  The drawable surface on which the drawables are displayed.
        ///  Set by lazy initialization.
        /// </summary>
        private static GameObject surface;

        /// <summary>
        /// The surface on which the drawables are displayed.
        /// </summary>
        protected static GameObject Surface
        {
            get
            {
                if (surface == null)
                {
                    surface = GameObject.Find("UI Canvas");
                }
                return surface;
            }
        }
    }
}