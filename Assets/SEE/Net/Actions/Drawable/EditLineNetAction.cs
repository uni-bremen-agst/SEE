using Assets.SEE.Game.Drawable;
using SEE.Net.Actions;
using SEE.Controls.Actions.Drawable;
using UnityEngine;
using SEE.Game.Drawable.Configurations;
using SEE.Game;
using SEE.Game.Drawable;

namespace SEE.Net.Actions.Drawable
{
    /// <summary>
    /// This class is responsible for changing all values (<see cref="EditAction"/>) of a line on all clients.
    /// </summary>
    public class EditLineNetAction : AbstractNetAction
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
        /// The Line that should be changed. The Line object contains all relevant values to change.
        /// </summary>
        public LineConf Line;

        /// <summary>
        /// The constructor of this action. All it does is assign the value you pass it to a field.
        /// </summary>
        /// <param name="drawableID">The id of the drawable on which the object is located.</param>
        /// <param name="parentDrawableID">The id of the drawable parent.</param>
        /// <param name="line">The line that contains the values to change the associated game object.</param>
        public EditLineNetAction(string drawableID, string parentDrawableID, LineConf line) : base()
        {
            DrawableID = drawableID;
            ParentDrawableID = parentDrawableID;
            Line = line;
        }

        /// <summary>
        /// Things to execute on the server (none for this class). Necessary because it is abstract
        /// in the superclass.
        /// </summary>
        protected override void ExecuteOnServer()
        {

        }

        /// <summary>
        /// Changes the values of the given line on each client.
        /// </summary>
        /// <exception cref="System.Exception">will be thrown, if the <see cref="DrawableID"/> or <see cref="LineName"/> don't exists.</exception>
        protected override void ExecuteOnClient()
        {
            if (!IsRequester())
            {
                GameObject drawable = GameFinder.FindDrawable(DrawableID, ParentDrawableID);
                if (drawable != null && GameFinder.FindChild(drawable, Line.id) != null)
                {
                    GameEdit.ChangeLine(GameFinder.FindChild(drawable, Line.id), Line);
                }
                else
                {
                    throw new System.Exception($"There is no drawable with the ID {DrawableID} or line with the ID {Line.id}.");
                }
            }
        }
    }
}