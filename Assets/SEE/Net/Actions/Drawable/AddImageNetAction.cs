using SEE.Net.Actions;
using System.Collections;
using UnityEngine;
using SEE.Controls.Actions.Drawable;
using Assets.SEE.Game.Drawable;
using SEE.Game.Drawable.Configurations;
using SEE.Game.Drawable;

namespace SEE.Net.Actions.Drawable
{
    /// <summary>
    /// This class is reponsible for add <see cref="AddImageAction"/> a image to the given drawable on all clients.
    /// </summary>
    public class AddImageNetAction : AbstractNetAction
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
        /// The image that should be add as <see cref="ImageConf"/> object.
        /// </summary>
        public ImageConf ImageConf;

        /// <summary>
        /// The constructor of this action. All it does is assign the value you pass it to a field.
        /// </summary>
        /// <param name="drawableID">The id of the drawable on which the object is located.</param>
        /// <param name="parentDrawableID">The id of the drawable parent.</param>
        /// <param name="imageConf">The image configuration of the image that should be displayed.</param>
        public AddImageNetAction(string drawableID, string parentDrawableID, ImageConf imageConf)
        {
            this.DrawableID = drawableID;
            this.ParentDrawableID = parentDrawableID;
            this.ImageConf = imageConf;
        }

        /// <summary>
        /// Things to execute on the server (none for this class). Necessary because it is abstract
        /// in the superclass.
        /// </summary>
        protected override void ExecuteOnServer()
        {
        }
        /// <summary>
        /// Adds the image on each client.
        /// </summary>
        /// <exception cref="System.Exception">will be thrown, if the <see cref="DrawableID"/> or <see cref="TextName"/> don't exists.</exception>
        protected override void ExecuteOnClient()
        {
            if (!IsRequester())
            {
                GameObject drawable = GameFinder.FindDrawable(DrawableID, ParentDrawableID);
                if (drawable == null)
                {
                    throw new System.Exception($"There is no drawable with the ID {DrawableID}.");
                }

                if (ImageConf != null && ImageConf.id != "")
                {
                    GameImage.RePlaceImage(drawable, ImageConf);
                }
                else
                {
                    throw new System.Exception($"There is no text to write.");
                }
            }
        }
    }
}