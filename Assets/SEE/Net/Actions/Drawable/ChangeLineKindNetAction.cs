using SEE.Controls.Actions.Drawable;
using SEE.Game.Drawable;
using UnityEngine;

namespace SEE.Net.Actions.Drawable
{
    /// <summary>
    /// This class is responsible for changing the line kind (<see cref="EditAction"/>) of a line on all clients.
    /// </summary>
    public class ChangeLineKindNetAction : AbstractNetAction
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
        /// The id of the line that should be changed
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
        /// <param name="drawableID">The id of the drawable on which the object is located.</param>
        /// <param name="parentDrawableID">The id of the drawable parent.</param>
        /// <param name="lineName">The id of the line that should be changed</param>
        /// <param name="lineKind">The line kind to which the line kind holder value of the line should be set.</param>
        /// <param name="tiling">The tiling to which the line renderer texture scale of the line should be set.</param>
        public ChangeLineKindNetAction(string drawableID, string parentDrawableID, string lineName, 
            GameDrawer.LineKind lineKind, float tiling) : base()
        {
            DrawableID = drawableID;
            ParentDrawableID = parentDrawableID;
            LineName = lineName;
            this.LineKind = lineKind;
            this.Tiling = tiling;
        }

        /// <summary>
        /// Things to execute on the server (none for this class). Necessary because it is abstract
        /// in the superclass.
        /// </summary>
        protected override void ExecuteOnServer()
        {

        }

        /// <summary>
        /// Changes the line kind of the given line on each client.
        /// </summary>
        /// <exception cref="System.Exception">will be thrown, if the <see cref="DrawableID"/> or <see cref="LineName"/> don't exists.</exception>
        protected override void ExecuteOnClient()
        {
            if (!IsRequester())
            {
                if (!IsRequester())
                {
                    GameObject drawable = GameFinder.FindDrawable(DrawableID, ParentDrawableID);
                    if (drawable != null && GameFinder.FindChild(drawable, LineName) != null)
                    {
                        GameDrawer.ChangeLineKind(GameFinder.FindChild(drawable, LineName), LineKind, Tiling);
                    }
                    else
                    {
                        throw new System.Exception($"There is no drawable with the ID {DrawableID} or line with the ID {LineName}.");
                    }
                }
            }
        }
    }
}