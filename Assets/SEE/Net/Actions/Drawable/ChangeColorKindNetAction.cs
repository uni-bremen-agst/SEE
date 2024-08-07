using SEE.Controls.Actions.Drawable;
using SEE.Game.Drawable;
using SEE.Game.Drawable.Configurations;
using UnityEngine;

namespace SEE.Net.Actions.Drawable
{
    /// <summary>
    /// This class is responsible for changing the color kind (<see cref="EditAction"/>)
    /// of a line on all clients.
    /// </summary>
    public class ChangeColorKindNetAction : DrawableNetAction
    {
        /// <summary>
        /// The new color kind for the line.
        /// </summary>
        public GameDrawer.ColorKind ColorKind;

        /// <summary>
        /// The line configuration.
        /// </summary>
        public LineConf Line;

        /// <summary>
        /// The constructor of this action. All it does is assign the value you pass it to a field.
        /// </summary>
        /// <param name="drawableID">The id of the drawable on which the object is located.</param>
        /// <param name="parentDrawableID">The id of the drawable parent.</param>
        /// <param name="line">The configuration of the line that should be changed</param>
        /// <param name="colorKind">The new color kind for the line.</param>
        public ChangeColorKindNetAction(string drawableID, string parentDrawableID, LineConf line,
            GameDrawer.ColorKind colorKind)
            : base(drawableID, parentDrawableID)
        {
            Line = line;
            ColorKind = colorKind;
        }

        /// <summary>
        /// Changes the color kind of the given line on each client.
        /// </summary>
        /// <exception cref="System.Exception">will be thrown, if the <see cref="DrawableID"/> or <see cref="Line.id"/> don't exists.</exception>
        public override void ExecuteOnClient()
        {
            base.ExecuteOnClient();
            GameDrawer.ChangeColorKind(FindChild(Line.Id), ColorKind, Line);
        }
    }
}