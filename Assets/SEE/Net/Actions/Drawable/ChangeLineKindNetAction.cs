using SEE.Controls.Actions.Drawable;
using SEE.Game.Drawable;

namespace SEE.Net.Actions.Drawable
{
    /// <summary>
    /// This class is responsible for changing the line kind (<see cref="EditAction"/>) of a line on all clients.
    /// </summary>
    public class ChangeLineKindNetAction : DrawableNetAction
    {
        /// <summary>
        /// The ID of the line that should be changed
        /// </summary>
        public string LineName;
        /// <summary>
        /// The new line kind.
        /// </summary>
        public GameDrawer.LineKind LineKind;
        /// <summary>
        /// The tiling to which the line renderer texture scale of the line should be set.
        /// Only necessary if the <see cref="GameDrawer.LineKind"/> is <see cref="GameDrawer.LineKind.Dashed"/>.
        /// </summary>
        public float Tiling;

        /// <summary>
        /// The constructor of this action. All it does is assign the value you pass it to a field.
        /// </summary>
        /// <param name="drawableID">The ID of the drawable on which the object is located.</param>
        /// <param name="parentDrawableID">The ID of the drawable parent.</param>
        /// <param name="lineName">The ID of the line that should be changed.</param>
        /// <param name="lineKind">The line kind to which the line kind holder value of the line should be set.</param>
        /// <param name="tiling">The tiling to which the line renderer texture scale of the line should be set.</param>
        public ChangeLineKindNetAction(string drawableID, string parentDrawableID, string lineName,
            GameDrawer.LineKind lineKind, float tiling)
            : base(drawableID, parentDrawableID)
        {
            LineName = lineName;
            LineKind = lineKind;
            Tiling = tiling;
        }

        /// <summary>
        /// Changes the line kind of the given line on each client.
        /// </summary>
        /// <exception cref="System.Exception">Will be thrown, if the <see cref="DrawableID"/>
        /// or <see cref="LineName"/> don't exists.</exception>
        public override void ExecuteOnClient()
        {
            base.ExecuteOnClient();
            GameDrawer.ChangeLineKind(FindChild(LineName), LineKind, Tiling);
        }
    }
}