using SEE.Controls.Actions.Drawable;
using SEE.Game.Drawable;

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
        /// The constructor of this action. All it does is assign the value you pass it to a field.
        /// </summary>
        /// <param name="drawableID">The id of the drawable on which the line should be drawn.</param>
        /// <param name="parentDrawableID">The id of the drawable parent.</param>
        /// <param name="lineID">The name of the line.</param>
        public DrawingFinishNetAction(string drawableID, string parentDrawableID, string lineID, bool loop)
            : base(drawableID, parentDrawableID)
        {
            LineID = lineID;
            Loop = loop;
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
                GameDrawer.FinishDrawing(FindChild(LineID), Loop);
            }
            else
            {
                throw new System.Exception($"There is no line to draw.");
            }
        }
    }
}