using SEE.Game.Drawable;
using SEE.Game.Drawable.Configurations;
using UnityEngine;

namespace SEE.Net.Actions.Drawable
{
    /// <summary>
    /// This class is reponsible for change the drawable surface color on all clients.
    /// </summary>
    public class DrawableChangeColorNetAction : SurfaceNetAction
    {
        /// <summary>
        /// The constructor of this action. All it does is assign the value you pass it to a field.
        /// </summary>
        public DrawableChangeColorNetAction(DrawableConfig config) : base(config)
        {
        }

        /// <summary>
        /// Changes the color of the drawable surface on each client.
        /// </summary>
        public override void ExecuteOnClient()
        {
            base.ExecuteOnClient();
            GameDrawableManager.ChangeColor(Surface, DrawableConf.Color);
        }
    }
}