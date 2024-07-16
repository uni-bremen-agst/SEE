using SEE.Controls.Actions.Drawable;
using SEE.Game.Drawable;
using SEE.Game.Drawable.Configurations;

namespace SEE.Net.Actions.Drawable
{
    /// <summary>
    /// This class is reponsible for add <see cref="AddImageAction"/> an image to the given drawable on all clients.
    /// </summary>
    public class AddImageNetAction : DrawableNetAction
    {
        /// <summary>
        /// The image that should be added as <see cref="ImageConf"/> object.
        /// </summary>
        public ImageConf Conf;

        /// <summary>
        /// The constructor of this action. All it does is assign the value you pass it to a field.
        /// </summary>
        /// <param name="drawableID">The id of the drawable on which the object should be placed.</param>
        /// <param name="parentDrawableID">The id of the drawable parent.</param>
        /// <param name="imageConf">The image configuration of the image that should be added.</param>
        public AddImageNetAction(string drawableID, string parentDrawableID, ImageConf imageConf)
            : base(drawableID, parentDrawableID)
        {
            Conf = imageConf;
        }

        /// <summary>
        /// Adds the image on each client.
        /// </summary>
        /// <exception cref="System.Exception">will be thrown, if the <see cref="DrawableID"/> or <see cref="Conf.id"/> don't exists.</exception>
        public override void ExecuteOnClient()
        {
            base.ExecuteOnClient();
            if (Conf != null && Conf.Id != "")
            {
                GameImage.RePlaceImage(Surface, Conf);
            }
            else
            {
                throw new System.Exception($"There is no image to add.");
            }
        }
    }
}