using Assets.SEE.Net.Actions.Drawable;
using SEE.Game.Drawable;
using SEE.Game.Drawable.Configurations;
using UnityEngine;

namespace SEE.Net.Actions.Drawable
{
    /// <summary>
    /// This class is reponsible for change the drawable description on all clients.
    /// </summary>
    public class DrawableChangeDescriptionNetAction : SurfaceNetAction
    {
        /// <summary>
        /// The drawable that should be changed.
        /// </summary>
        public DrawableConfig DrawableConf;

        /// <summary>
        /// The constructor of this action. All it does is assign the value you pass it to a field.
        /// </summary>
        public DrawableChangeDescriptionNetAction(DrawableConfig config) : base(config)
        {
            DrawableConf = config;
        }

        /// <summary>
        /// Changes the description of the drawable on each client.
        /// </summary>
        public override void ExecuteOnClient()
        {
            base.ExecuteOnClient();
            GameDrawableManager.ChangeDescription(Surface, DrawableConf.Description);
        }
    }
}
