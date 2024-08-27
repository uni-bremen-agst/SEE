using SEE.Controls.Actions.Drawable;
using SEE.Game.Drawable;
using UnityEngine;

namespace SEE.Net.Actions.Drawable
{
    /// <summary>
    /// This class is responsible for drawing (<see cref="DrawFreehandAction"/>
    /// or <see cref="DrawShapesAction"/>) a line on the given drawable on all clients.
    /// </summary>
    public class DrawingFinishNetAction : DrawableNetAction
    {
        /// <summary>
        /// The line that should be drawn as <see cref="Line"/> object.
        /// </summary>
        public string LineID;

        /// <summary>
        /// Whether the loop option should be activated.
        /// </summary>
        public bool Loop;

        /// <summary>
        /// The fill-out color if the line should be filled out.
        /// </summary>
        public Color FillOutColor;

        /// <summary>
        /// Whether the fill out should be set or not.
        /// </summary>
        public bool FillOutStatus;

        /// <summary>
        /// The constructor of this action. All it does is to assign the value you pass to its fields.
        /// </summary>
        /// <param name="drawableID">The id of the drawable on which the line should be drawn.</param>
        /// <param name="parentDrawableID">The id of the drawable parent.</param>
        /// <param name="lineID">The name of the line.</param>
        /// <param name="loop">Whether the line should activate the loop functionality.</param>
        /// <param name="fillOutColor">The fill out color of the line; null if the line should not filled out.</param>
        public DrawingFinishNetAction(string drawableID, string parentDrawableID, string lineID, bool loop, Color? fillOutColor)
            : base(drawableID, parentDrawableID)
        {
            LineID = lineID;
            Loop = loop;
            if (fillOutColor != null)
            {
                FillOutStatus = true;
                FillOutColor = fillOutColor.Value;
            }
            else
            {
                FillOutStatus = false;
                FillOutColor = Color.clear;
            }
        }

        /// <summary>
        /// Draws the line on each client.
        /// </summary>
        /// <exception cref="System.Exception">will be thrown, if the <see cref="DrawableID"/> or <see cref="Line"/> don't exists.</exception>
        public override void ExecuteOnClient()
        {
            base.ExecuteOnClient();
            if (!string.IsNullOrWhiteSpace(LineID))
            {
                if (FillOutStatus)
                {
                    GameDrawer.FinishDrawing(FindChild(LineID), Loop, FillOutColor);
                }
                else
                {
                    GameDrawer.FinishDrawing(FindChild(LineID), Loop, null);
                }
            }
            else
            {
                throw new System.Exception($"There is no line to draw.");
            }
        }
    }
}
