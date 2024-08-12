using SEE.Controls.Actions.Drawable;
using SEE.Game.Drawable;
using UnityEngine;

namespace SEE.Net.Actions.Drawable
{
    /// <summary>
    /// This class is responsible for filling out a line on the given drawable on all clients.
    /// </summary>
    public class DrawingFillOutNetAction : DrawableNetAction
    {
        /// <summary>
        /// The line that should be filled out.
        /// </summary>
        public string LineID;

        /// <summary>
        /// The filled out color, if the line should be filled out.
        /// </summary>
        public Color FillOutColor;

        /// <summary>
        /// The constructor of this action. All it does is assign the value you pass it to a field.
        /// </summary>
        /// <param name="drawableID">The id of the drawable on which the line should be drawn.</param>
        /// <param name="parentDrawableID">The id of the drawable parent.</param>
        /// <param name="lineID">The name of the line.</param>
        /// <param name="fillOutColor">The fill out color of the line; null if the line should not filled out.</param>
        public DrawingFillOutNetAction(string drawableID, string parentDrawableID, string lineID, Color fillOutColor)
            : base(drawableID, parentDrawableID)
        {
            LineID = lineID;
            FillOutColor = fillOutColor;
        }

        /// <summary>
        /// Draws the line on each client.
        /// </summary>
        /// <exception cref="System.Exception">will be thrown, if the <see cref="DrawableID"/> or <see cref="Line"/> don't exists.</exception>
        public override void ExecuteOnClient()
        {
            base.ExecuteOnClient();
            if (LineID != null && LineID != "")
            {
                GameDrawer.FillOut(FindChild(LineID), FillOutColor);
            }
            else
            {
                throw new System.Exception($"There is no line to fill out.");
            }
        }
    }
}
