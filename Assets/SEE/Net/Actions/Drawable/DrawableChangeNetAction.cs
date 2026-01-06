using SEE.Game.Drawable;
using SEE.Game.Drawable.Configurations;
using UnityEngine;

namespace SEE.Net.Actions.Drawable
{
    /// <summary>
    /// This class is reponsible for change the drawable values on all clients.
    /// </summary>
    public class DrawableChangeNetAction : SurfaceNetAction
    {
        /// <summary>
        /// The constructor of this action. All it does is assign the value you pass it to a field.
        /// </summary>
        public DrawableChangeNetAction(DrawableConfig config) : base(config)
        {
        }

        /// <summary>
        /// Changes the values of the drawable on each client.
        /// </summary>
        /// /// <exception cref="System.Exception">Will be thrown, if the <see cref="DrawableConf"/> don't exists.</exception>
        public override void ExecuteOnClient()
        {
            base.ExecuteOnClient();
            if (DrawableConf != null && Surface != null)
            {
                GameDrawableManager.Change(Surface, DrawableConf);
            }
            else
            {
                throw new System.Exception($"There is no drawable with the ID {DrawableConf.ID}.");
            }
        }
    }
}