using Assets.SEE.Game.Drawable;
using Assets.SEE.Game;
using SEE.Game.Drawable.Configurations;
using SEE.Game;
using System.Collections;
using UnityEngine;

namespace SEE.Net.Actions.Drawable
{
    /// <summary>
    /// This class is responsible for changing all values (<see cref="EditAction"/>) of a text on all clients.
    /// </summary>
    public class EditTextNetAction : AbstractNetAction
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
        /// The Text that should be changed. The Text object contains all relevant values to change.
        /// </summary>
        public TextConf Text;

        /// <summary>
        /// The constructor of this action. All it does is assign the value you pass it to a field.
        /// </summary>
        /// <param name="drawableID">The id of the drawable on which the object is located.</param>
        /// <param name="parentDrawableID">The id of the drawable parent.</param>
        /// <param name="text">The text that contains the values to change the associated game object.</param>
        public EditTextNetAction(string drawableID, string parentDrawableID, TextConf text) : base()
        {
            DrawableID = drawableID;
            ParentDrawableID = parentDrawableID;
            Text = text;
        }

        /// <summary>
        /// Things to execute on the server (none for this class). Necessary because it is abstract
        /// in the superclass.
        /// </summary>
        protected override void ExecuteOnServer()
        {

        }

        /// <summary>
        /// Changes the values of the given text on each client.
        /// </summary>
        /// <exception cref="System.Exception">will be thrown, if the <see cref="DrawableID"/> or <see cref="LineName"/> don't exists.</exception>
        protected override void ExecuteOnClient()
        {
            if (!IsRequester())
            {
                GameObject drawable = GameDrawableFinder.Find(DrawableID, ParentDrawableID);
                if (drawable != null && GameDrawableFinder.FindChild(drawable, Text.id) != null)
                {
                    GameObject textObj = GameDrawableFinder.FindChild(drawable, Text.id);
                    GameEdit.ChangeText(textObj, Text);
                }
                else
                {
                    throw new System.Exception($"There is no drawable with the ID {DrawableID} or line with the ID {Text.id}.");
                }
            }
        }
    }
}