using SEE.Controls.Actions.Drawable;
using SEE.Game.Drawable;

namespace SEE.Net.Actions.Drawable
{
    /// <summary>
    /// This class is responsible for changing the loop (<see cref="EditAction"/>) of a line
    /// on all clients.
    /// </summary>
    public class EditLineLoopNetAction : DrawableNetAction
    {
        /// <summary>
        /// The ID of the line that should be changed
        /// </summary>
        public string LineName;
        /// <summary>
        /// The new loop value for the line.
        /// </summary>
        public bool Loop;

        /// <summary>
        /// The constructor of this action. All it does is assign the value you pass it to a field.
        /// </summary>
        /// <param name="drawableID">The ID of the drawable on which the object is located.</param>
        /// <param name="parentDrawableID">The ID of the drawable parent.</param>
        /// <param name="lineName">The ID of the line that should be changed.</param>
        /// <param name="loop">The new loop value for the line.</param>
        public EditLineLoopNetAction(string drawableID, string parentDrawableID, string lineName, bool loop)
            : base(drawableID, parentDrawableID)
        {
            LineName = lineName;
            Loop = loop;
        }

        /// <summary>
        /// Changes the loop of the given line on each client.
        /// </summary>
        /// <exception cref="System.Exception">Will be thrown, if the <see cref="DrawableID"/>
        /// or <see cref="LineName"/> don't exists.</exception>
        public override void ExecuteOnClient()
        {
            base.ExecuteOnClient();
            GameEdit.ChangeLoop(FindChild(LineName), Loop);
        }
    }
}