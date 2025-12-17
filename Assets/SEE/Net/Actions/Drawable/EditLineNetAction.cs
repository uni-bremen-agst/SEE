using SEE.Controls.Actions.Drawable;
using SEE.Game.Drawable.Configurations;
using SEE.Game.Drawable;
using UnityEngine;

namespace SEE.Net.Actions.Drawable
{
    /// <summary>
    /// This class is responsible for changing all values (<see cref="EditAction"/>) of a line on all clients.
    /// </summary>
    public class EditLineNetAction : DrawableNetAction
    {
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
        public EditLineNetAction(string drawableID, string parentDrawableID, LineConf line)
            : base(drawableID, parentDrawableID)
        {
            Line = line;
        }

        /// <summary>
        /// Changes the values of the given line on each client.
        /// </summary>
        /// <exception cref="System.Exception">will be thrown, if the <see cref="DrawableID"/>
        /// or <see cref="Line"/> don't exists.</exception>
        public override void ExecuteOnClient()
        {
            base.ExecuteOnClient();
            GameEdit.ChangeLine(FindChild(Line.ID), Line);
        }
    }
}