using SEE.Controls.Actions.Drawable;
using SEE.Game.Drawable;
using SEE.Game.Drawable.Configurations;
using SEE.UI.Notification;
using UnityEngine;

namespace SEE.Net.Actions.Drawable
{
    /// <summary>
    /// This class is responsible for add a point to a line on the given drawable on all clients.
    /// </summary>
    public class DrawingNetAction : DrawableNetAction
    {
        /// <summary>
        /// The line that should be drawn as <see cref="Line"/> object.
        /// </summary>
        public string LineID;

        /// <summary>
        /// The position to be added.
        /// </summary>
        public Vector3 Position;

        /// <summary>
        /// The index on which the position should be added.
        /// </summary>
        public int Index;

        /// <summary>
        /// The constructor of this action. All it does is assign the value you pass it to a field.
        /// </summary>
        /// <param name="drawableID">The id of the drawable on which the line should be drawn.</param>
        /// <param name="parentDrawableID">The id of the drawable parent.</param>
        /// <param name="lineID">The name of the line.</param>
        /// <param name="position">The position to add.</param>
        /// <param name="index">The index on which the position should be added.</param>
        public DrawingNetAction(string drawableID, string parentDrawableID, string lineID, Vector3 position, int index)
            : base(drawableID, parentDrawableID)
        {
            LineID = lineID;
            Position = position;
            Index = index;
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
                GameDrawer.DrawPoint(FindChild(LineID), Position, Index);
            }
            else
            {
                throw new System.Exception($"There is no line to draw.");
            }
        }
    }
}