using SEE.Game.Drawable;
using static SEE.Game.Drawable.GameDrawer;
using SEE.Controls.Actions.Drawable;
using UnityEngine;
using SEE.Game.Drawable.Configurations;

namespace SEE.Net.Actions.Drawable
{
    /// <summary>
    /// This class is responsible for drawing (<see cref="DrawOnAction"/>) a line on the given drawable on all clients.
    /// </summary>
    public class DrawOnNetAction : AbstractNetAction
    {
        /// <summary>
        /// The id of the drawable on which the object is located
        /// </summary>
        public string DrawableID;

        /// <summary>
        /// The id of the drawable parent
        /// </summary>
        public string ParentDrawableID;

        /// <summary>
        /// The line that should be drawn as <see cref="Line"/> object.
        /// </summary>
        public LineConf Line;

        /// <summary>
        /// The constructor of this action. All it does is assign the value you pass it to a field.
        /// </summary>
        /// <param name="drawableID">The id of the drawable on which the object is located.</param>
        /// <param name="parentDrawableID">The id of the drawable parent.</param>
        /// <param name="line">The line that should be drawn.</param>
        public DrawOnNetAction(string drawableID, string parentDrawableID, LineConf line)
        {
            this.DrawableID = drawableID;
            this.ParentDrawableID = parentDrawableID;
            this.Line = line;
        }

        /// <summary>
        /// Draws the line on each client.
        /// </summary>
        /// <exception cref="System.Exception">will be thrown, if the <see cref="DrawableID"/> or <see cref="LineName"/> don't exists.</exception>
        protected override void ExecuteOnClient()
        {
            if (!IsRequester())
            {
                GameObject drawable = GameFinder.FindDrawable(DrawableID, ParentDrawableID);
                if (drawable == null)
                {
                    throw new System.Exception($"There is no drawable with the ID {DrawableID}.");
                }

                if (Line != null && Line.id != "")
                {
                    GameDrawer.ReDrawLine(drawable, Line);
                } else
                {
                    throw new System.Exception($"There is no line to draw.");
                }
            }
        }

        /// <summary>
        /// Things to execute on the server (none for this class). Necessary because it is abstract
        /// in the superclass.
        /// </summary>
        protected override void ExecuteOnServer()
        {
        }
    }
}