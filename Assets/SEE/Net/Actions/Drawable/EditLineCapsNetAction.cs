using SEE.Controls.Actions.Drawable;
using SEE.Game.Drawable;
using static SEE.Game.Drawable.ActionHelpers.LineCapPointsCalculator;

namespace SEE.Net.Actions.Drawable
{
    /// <summary>
    /// This class is responsible for changing the line caps (<see cref="EditAction"/>)
    /// of a line on all clients.
    /// </summary>
    public class EditLineCapsNetAction : DrawableNetAction
    {
        /// <summary>
        /// The ID of the line that should be changed.
        /// </summary>
        public string LineName;

        /// <summary>
        /// The new start line cap of the line.
        /// </summary>
        public LineCap StartCap;

        /// <summary>
        /// The new end line cap of the line.
        /// </summary>
        public LineCap EndCap;

        /// <summary>
        /// The constructor of this action. All it does is to assign its parameters to the fields.
        /// </summary>
        /// <param name="drawableID">The ID of the drawable on which the line is located.</param>
        /// <param name="parentDrawableID">The ID of the drawable parent.</param>
        /// <param name="lineName">The ID of the line that should be changed.</param>
        /// <param name="startCap">The new start line cap.</param>
        /// <param name="endCap">The new end line cap.</param>
        public EditLineCapsNetAction(string drawableID, string parentDrawableID, string lineName,
            LineCap startCap, LineCap endCap)
            : base(drawableID, parentDrawableID)
        {
            LineName = lineName;
            StartCap = startCap;
            EndCap = endCap;
        }

        /// <summary>
        /// Changes the line caps of the given line on each client.
        /// </summary>
        /// <exception cref="System.Exception">
        /// Will be thrown, if the <see cref="LineName"/> does not exist.
        /// </exception>
        public override void ExecuteOnClient()
        {
            base.ExecuteOnClient();
            GameEdit.ChangeLineCaps(FindChild(LineName), StartCap, EndCap);
        }
    }
}