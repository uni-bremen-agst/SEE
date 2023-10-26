using Assets.SEE.Game.Drawable;
using SEE.Game.Drawable.Configurations;
using SEE.Game;
using System.Collections;
using UnityEngine;
using SEE.Game.Drawable;

namespace SEE.Net.Actions.Drawable
{
    /// <summary>
    /// This class is responsible for changing all values (<see cref="EditAction"/>) of a image on all clients.
    /// </summary>
    public class EditImageNetAction : AbstractNetAction
    {
        /// <summary>
        /// The id of the drawable on which the object is located
        /// </summary>
        public string DrawableID;
        /// <summary>
        /// The id of the drawable parent
        /// </summary>
        public string ParentDrawableID;
        /// <summary>
        /// The Image that should be changed. The Image object contains all relevant values to change.
        /// </summary>
        public ImageConf Image;

        /// <summary>
        /// The constructor of this action. All it does is assign the value you pass it to a field.
        /// </summary>
        /// <param name="drawableID">The id of the drawable on which the object is located.</param>
        /// <param name="parentDrawableID">The id of the drawable parent.</param>
        /// <param name="image">The image configuration that contains the values to change the associated game object.</param>
        public EditImageNetAction(string drawableID, string parentDrawableID, ImageConf image) : base()
        {
            DrawableID = drawableID;
            ParentDrawableID = parentDrawableID;
            Image = image;
        }

        /// <summary>
        /// Things to execute on the server (none for this class). Necessary because it is abstract
        /// in the superclass.
        /// </summary>
        protected override void ExecuteOnServer()
        {

        }

        /// <summary>
        /// Changes the values of the given image configuration on each client.
        /// </summary>
        /// <exception cref="System.Exception">will be thrown, if the <see cref="DrawableID"/> or <see cref="LineName"/> don't exists.</exception>
        protected override void ExecuteOnClient()
        {
            if (!IsRequester())
            {
                GameObject drawable = GameFinder.Find(DrawableID, ParentDrawableID);
                if (drawable != null && GameFinder.FindChild(drawable, Image.id) != null)
                {
                    GameObject imageObj = GameFinder.FindChild(drawable, Image.id);
                    GameEdit.ChangeImage(imageObj, Image);
                }
                else
                {
                    throw new System.Exception($"There is no drawable with the ID {DrawableID} or line with the ID {Image.id}.");
                }
            }
        }
    }
}