using System.Collections;
using UnityEngine;
using SEE.Controls.Actions.Drawable;
using SEE.Game.Drawable.Configurations;
using SEE.Game;
using Assets.SEE.Game.Drawable;
using SEE.Game.Drawable;

namespace SEE.Net.Actions.Drawable
{
    /// <summary>
    /// This class is reponsible for writing <see cref="WriteTextAction"/> a text on the given drawable on all clients.
    /// </summary>
    public class WriteTextNetAction : AbstractNetAction
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
        /// The text that should be written as <see cref="Text"/> object.
        /// </summary>
        public TextConf Text;

        /// <summary>
        /// The constructor of this action. All it does is assign the value you pass it to a field.
        /// </summary>
        /// <param name="drawableID">The id of the drawable on which the object is located.</param>
        /// <param name="parentDrawableID">The id of the drawable parent.</param>
        /// <param name="text">The text that should be written.</param>
        public WriteTextNetAction(string drawableID, string parentDrawableID, TextConf text)
        {
            this.DrawableID = drawableID;
            this.ParentDrawableID = parentDrawableID;
            this.Text = text;
        }

        /// <summary>
        /// Things to execute on the server (none for this class). Necessary because it is abstract
        /// in the superclass.
        /// </summary>
        protected override void ExecuteOnServer()
        {
        }
        /// <summary>
        /// Writes the text on each client.
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

                if (Text != null && Text.id != "")
                {
                    GameTexter.ReWriteText(drawable, Text);
                }
                else
                {
                    throw new System.Exception($"There is no text to write.");
                }
            }
        }
    }
}