using SEE.Controls.Actions.Drawable;
using SEE.Game.Drawable;
using UnityEngine;

namespace SEE.Net.Actions.Drawable
{
    /// <summary>
    /// This class is responsible for changing the fill-out color (<see cref="EditAction"/>)
    /// of a line on all clients.
    /// </summary>
    public class EditLineFillOutColorNetAction : DrawableNetAction
    {
        /// <summary>
        /// The id of the line that should be changed.
        /// </summary>
        public string LineName;

        /// <summary>
        /// The new color for the line.
        /// </summary>
        public Color Color;

        /// <summary>
        /// The constructor of this action. All it does is to assign its parameters to the field.
        /// </summary>
        /// <param name="drawableID">The id of the drawable on which the line is located.</param>
        /// <param name="parentDrawableID">The id of the drawable parent.</param>
        /// <param name="lineName">The id of the line that should be changed.</param>
        /// <param name="color">The new color for the line.</param>
        public EditLineFillOutColorNetAction(string drawableID, string parentDrawableID, string lineName, Color color)
            : base(drawableID, parentDrawableID)
        {
            LineName = lineName;
            Color = color;
        }

        /// <summary>
        /// Changes the secondary color of the given line on each client.
        /// </summary>
        /// <exception cref="System.Exception">will be thrown, if the <see cref="LineName"/> does not exists.</exception>
        public override void ExecuteOnClient()
        {
            base.ExecuteOnClient();
            GameEdit.ChangeFillOutColor(FindChild(LineName), Color);
        }
    }
}
