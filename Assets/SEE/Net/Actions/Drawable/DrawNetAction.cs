using SEE.Controls.Actions.Drawable;
using SEE.Game.Drawable;
using SEE.Game.Drawable.Configurations;
using SEE.UI.Notification;
using UnityEngine;

namespace SEE.Net.Actions.Drawable
{
    /// <summary>
    /// This class is responsible for drawing (<see cref="DrawFreehandAction"/>
    /// or <see cref="DrawShapesAction"/>) a line on the given drawable on all clients.
    /// </summary>
    public class DrawNetAction : DrawableNetAction
    {
        /// <summary>
        /// The line that should be drawn as <see cref="Line"/> object.
        /// </summary>
        public LineConf Line;

        /// <summary>
        /// The constructor of this action. All it does is assign the value you pass it to a field.
        /// </summary>
        /// <param name="drawableID">The id of the drawable on which the line should be drawn.</param>
        /// <param name="parentDrawableID">The id of the drawable parent.</param>
        /// <param name="line">The line that should be drawn.</param>
        public DrawNetAction(string drawableID, string parentDrawableID, LineConf line)
            : base(drawableID, parentDrawableID)
        {
            Line = line;
        }

        /// <summary>
        /// Draws the line on each client.
        /// </summary>
        /// <exception cref="System.Exception">will be thrown, if the <see cref="DrawableID"/> or <see cref="Line"/> don't exists.</exception>
        public override void ExecuteOnClient()
        {
            base.ExecuteOnClient();
            if (Line != null && Line.Id != "")
            {
                GameDrawer.ReDrawLine(Surface, Line);
            }
            else
            {
                throw new System.Exception($"There is no line to draw.");
            }
        }
    }
}