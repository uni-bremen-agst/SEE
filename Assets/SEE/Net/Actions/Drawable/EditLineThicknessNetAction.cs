using SEE.Controls.Actions.Drawable;
using SEE.Game.Drawable;

namespace SEE.Net.Actions.Drawable
{
    /// <summary>
    /// This class is responsible for changing the thickness (<see cref="EditAction"/>)
    /// of a line on all clients.
    /// </summary>
    public class EditLineThicknessNetAction : DrawableNetAction
    {
        /// <summary>
        /// The ID of the line that should be changed
        /// </summary>
        public string LineName;
        /// <summary>
        /// The new thickness for the line.
        /// </summary>
        public float Thickness;

        /// <summary>
        /// The constructor of this action. All it does is assign the value you pass it to a field.
        /// </summary>
        /// <param name="drawableID">The ID of the drawable on which the line is located.</param>
        /// <param name="parentDrawableID">The ID of the drawable parent.</param>
        /// <param name="lineName">The ID of the line that should be changed.</param>
        /// <param name="thickness">The new thickness for the line.</param>
        public EditLineThicknessNetAction(string drawableID, string parentDrawableID, string lineName, float thickness)
            : base(drawableID, parentDrawableID)
        {
            LineName = lineName;
            Thickness = thickness;
        }

        /// <summary>
        /// Changes the thickness of the given line on each client.
        /// </summary>
        /// <exception cref="System.Exception">Will be thrown, if the <see cref="DrawableID"/> or <see cref="LineName"/> don't exists.</exception>
        public override void ExecuteOnClient()
        {
            base.ExecuteOnClient();
            GameEdit.ChangeThickness(FindChild(LineName), Thickness);
        }
    }
}