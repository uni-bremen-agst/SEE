using SEE.Game.Drawable;
using SEE.Game.Drawable.Configurations;
using SEE.Net.Actions;
using UnityEngine;

namespace SEE.Net.Actions.Drawable
{
    /// <summary>
    /// This class is reponsible for change the drawable visibility on all clients.
    /// </summary>
    public class DrawableChangeVisibilityNetAction : AbstractNetAction
    {
        /// <summary>
        /// The drawable that should be changed.
        /// </summary>
        public DrawableConfig DrawableConf;

        /// <summary>
        /// The constructor of this action. All it does is assign the value you pass it to a field.
        /// </summary>
        public DrawableChangeVisibilityNetAction(DrawableConfig config)
        {
            DrawableConf = config;
        }

        /// <summary>
        /// Things to execute on the server (none for this class). Necessary because it is abstract
        /// in the superclass.
        /// </summary>
        protected override void ExecuteOnServer()
        {
        }

        /// <summary>
        /// Changes the visibility of the drawable on each client.
        /// </summary>
        protected override void ExecuteOnClient()
        {
            if (!IsRequester())
            {
                GameObject surface = GameFinder.FindDrawableSurface(DrawableConf.ID, DrawableConf.ParentID);
                GameDrawableManager.ChangeVisibility(surface.transform.parent.gameObject, DrawableConf.Visibility);
            }
        }
    }
}