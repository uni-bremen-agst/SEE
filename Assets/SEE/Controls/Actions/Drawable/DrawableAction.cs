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
    }
}
