using Assets.SEE.Game.Drawable;
using SEE.Game.Drawable;
using SEE.Game.Drawable.Configurations;
using SEE.Net.Actions;
using UnityEngine;

namespace Assets.SEE.Net.Actions.Drawable
{
    /// <summary>
    /// This class is reponsible for change the drawable description on all clients.
    /// </summary>
    public class DrawableChangeDescriptionNetAction : AbstractNetAction
    {
        /// <summary>
        /// The drawable that should be changed.
        /// </summary>
        public DrawableConfig DrawableConf;

        /// <summary>
        /// The constructor of this action. All it does is assign the value you pass it to a field.
        /// </summary>
        public DrawableChangeDescriptionNetAction(DrawableConfig config)
        {
            this.DrawableConf = config;
        }

        /// <summary>
        /// Things to execute on the server (none for this class). Necessary because it is abstract
        /// in the superclass.
        /// </summary>
        protected override void ExecuteOnServer()
        {
        }

        /// <summary>
        /// Changes the description of the drawable on each client.
        /// </summary>
        protected override void ExecuteOnClient()
        {
            if (!IsRequester())
            {
                GameObject surface = GameFinder.FindDrawableSurface(DrawableConf.ID, DrawableConf.ParentID);
                GameDrawableManager.ChangeDescription(surface.transform.parent.gameObject, DrawableConf.Description);
            }
        }
    }
}