using SEE.Game.Drawable;
using SEE.Game.Drawable.Configurations;
using UnityEngine;

namespace SEE.Net.Actions.Drawable
{
    /// <summary>
    /// This class is reponsible for change the drawable values on all clients.
    /// </summary>
    public class DrawableChangeNetAction : AbstractNetAction
    {
        /// <summary>
        /// The configuration which holds all data.
        /// </summary>
        public DrawableConfig DrawableConf;

        /// <summary>
        /// The constructor of this action. All it does is assign the value you pass it to a field.
        /// </summary>
        public DrawableChangeNetAction(DrawableConfig config)
        {
            DrawableConf = config;
        }

        /// <summary>
        /// Changes the values of the drawable on each client.
        /// </summary>
        /// /// <exception cref="System.Exception">will be thrown, if the <see cref="DrawableConf"/> don't exists.</exception>
        public override void ExecuteOnClient()
        {
            if (DrawableConf != null && GameFinder.FindDrawableSurface(DrawableConf.ID, DrawableConf.ParentID) != null)
            {
                GameObject surface = GameFinder.FindDrawableSurface(DrawableConf.ID, DrawableConf.ParentID);
                GameDrawableManager.Change(surface, DrawableConf);
            }
            else
            {
                throw new System.Exception($"There is no drawable with the ID {DrawableConf.ID}.");
            }
        }
    }
}