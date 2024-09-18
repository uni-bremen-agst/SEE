using SEE.Game.Drawable;
using SEE.Game.Drawable.Configurations;

namespace SEE.Net.Actions.Drawable
{
    /// <summary>
    /// This class is reponsible for change the drawable lighting on all clients.
    /// </summary>
    public class DrawableChangeLightingNetAction : SurfaceNetAction
    {
        /// <summary>
        /// The constructor of this action. All it does is assign the value you pass it to a field.
        /// </summary>
        public DrawableChangeLightingNetAction(DrawableConfig config) : base(config)
        {
        }

        /// <summary>
        /// Changes the lighting of the drawable on each client.
        /// </summary>
        public override void ExecuteOnClient()
        {
            base.ExecuteOnClient();
            GameDrawableManager.ChangeLighting(Surface, DrawableConf.Lighting);
        }
    }
}
