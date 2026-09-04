using SEE.UI.Menu.Drawable;
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
        /// The surface on which the drawables are displayed.
        /// </summary>
        protected GameObject Surface { get; set; }

        /// <summary>
        /// Initializes the shared action and menu lifecycles for drawable actions.
        /// </summary>
        protected DrawableAction()
        {
            DrawableActionLifecycle.Initialize();
            DrawableMenuLifecycle.Initialize();
        }
    }
}
