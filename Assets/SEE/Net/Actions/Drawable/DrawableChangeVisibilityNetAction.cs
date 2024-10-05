using SEE.Game.Drawable;
using SEE.Game.Drawable.Configurations;
using UnityEngine;

namespace SEE.Net.Actions.Drawable
{
    /// <summary>
    /// This class is reponsible for change the drawable visibility on all clients.
    /// </summary>
    public class DrawableChangeVisibilityNetAction : SurfaceNetAction
    {
        /// <summary>
        /// The constructor of this action. All it does is assign the value you pass it to a field.
        /// </summary>
        public DrawableChangeVisibilityNetAction(DrawableConfig config) : base(config)
        {
        }

        /// <summary>
        /// Changes the visibility of the drawable on each client.
        /// </summary>
        public override void ExecuteOnClient()
        {
            base.ExecuteOnClient();
            GameDrawableManager.ChangeVisibility(Surface, DrawableConf.Visibility);
        }
    }
}
