using SEE.Net.Actions;
using SEE.Controls.Actions.Drawable;
using UnityEngine;
using SEE.Game.Drawable;

namespace SEE.Net.Actions.Drawable
{
    /// <summary>
    /// This class is responsible for changing the loop (<see cref="EditAction"/>) of a line on all clients.
    /// </summary>
    public class EditLineLoopNetAction : AbstractNetAction
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
        /// The id of the line that should be changed
        /// </summary>
        public string LineName;
        /// <summary>
        /// The new loop value for the line.
        /// </summary>
        public bool Loop;

        /// <summary>
        /// The constructor of this action. All it does is assign the value you pass it to a field.
        /// </summary>
        /// <param name="drawableID">The id of the drawable on which the object is located.</param>
        /// <param name="parentDrawableID">The id of the drawable parent.</param>
        /// <param name="lineName">The id of the line that should be changed.</param>
        /// <param name="loop">The new loop value for the line.</param>
        public EditLineLoopNetAction(string drawableID, string parentDrawableID, string lineName, bool loop) : base()
        {
            DrawableID = drawableID;
            ParentDrawableID = parentDrawableID;
            LineName = lineName;
            Loop = loop;
        }

        /// <summary>
        /// Things to execute on the server (none for this class). Necessary because it is abstract
        /// in the superclass.
        /// </summary>
        protected override void ExecuteOnServer()
        {

        }

        /// <summary>
        /// Changes the loop of the given line on each client.
        /// </summary>
        /// <exception cref="System.Exception">will be thrown, if the <see cref="DrawableID"/> or <see cref="LineName"/> don't exists.</exception>
        protected override void ExecuteOnClient()
        {
            if (!IsRequester())
            {
                GameObject drawable = GameFinder.FindDrawable(DrawableID, ParentDrawableID);
                if (drawable != null && GameFinder.FindChild(drawable, LineName) != null)
                {
                    GameEdit.ChangeLoop(GameFinder.FindChild(drawable, LineName), Loop);
                }
                else
                {
                    throw new System.Exception($"There is no drawable with the ID {DrawableID} or line with the ID {LineName}.");
                }
            }
        }
    }
}