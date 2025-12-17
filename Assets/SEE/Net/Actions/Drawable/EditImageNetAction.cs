using SEE.Controls.Actions.Drawable;
using SEE.Game.Drawable;
using SEE.Game.Drawable.Configurations;

namespace SEE.Net.Actions.Drawable
{
    /// <summary>
    /// This class is responsible for changing all values (<see cref="EditAction"/>) of a
    /// image on all clients.
    /// </summary>
    public class EditImageNetAction : DrawableNetAction
    {
        /// <summary>
        /// The Image that should be changed. The Image object contains all relevant values to change.
        /// </summary>
        public ImageConf Image;

        /// <summary>
        /// The constructor of this action. All it does is assign the value you pass it to a field.
        /// </summary>
        /// <param name="drawableID">The ID of the drawable on which the object is located.</param>
        /// <param name="parentDrawableID">The ID of the drawable parent.</param>
        /// <param name="image">The image configuration that contains the values to change the associated game object.</param>
        public EditImageNetAction(string drawableID, string parentDrawableID, ImageConf image)
            : base(drawableID, parentDrawableID)
        {
            Image = image;
        }

        /// <summary>
        /// Changes the values of the given image configuration on each client.
        /// </summary>
        /// <exception cref="System.Exception">Will be thrown, if the drawable or if image don't exists.</exception>
        public override void ExecuteOnClient()
        {
            base.ExecuteOnClient();
            GameEdit.ChangeImage(FindChild(Image.ID), Image);
        }
    }
}