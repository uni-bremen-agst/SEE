using SEE.Controls.Actions.Drawable;
using SEE.Game.Drawable;
using SEE.Game.Drawable.Configurations;
using UnityEngine;

namespace SEE.Net.Actions.Drawable
{
    /// <summary>
    /// This class is responsible for changing the color kind (<see cref="EditAction"/>) of a line on all clients.
    /// </summary>
    public class ChangeColorKindNetAction : AbstractNetAction
    {
        /// <summary>
        /// The id of the drawable on which the line is located
        /// </summary>
        public string DrawableID;

        /// <summary>
        /// The id of the drawable parent
        /// </summary>
        public string ParentDrawableID;

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
            GameDrawer.ColorKind colorKind) : base()
        {
            DrawableID = drawableID;
            ParentDrawableID = parentDrawableID;
            Line = line;
            this.ColorKind = colorKind;
        }

        /// <summary>
        /// Things to execute on the server (none for this class). Necessary because it is abstract
        /// in the superclass.
        /// </summary>
        protected override void ExecuteOnServer()
        {

        }

        /// <summary>
        /// Changes the color kind of the given line on each client.
        /// </summary>
        /// <exception cref="System.Exception">will be thrown, if the <see cref="DrawableID"/> or <see cref="Line.id"/> don't exists.</exception>
        protected override void ExecuteOnClient()
        {
            if (!IsRequester())
            {
                if (!IsRequester())
                {
                    GameObject drawable = GameFinder.FindDrawable(DrawableID, ParentDrawableID);
                    if (drawable != null && GameFinder.FindChild(drawable, Line.id) != null)
                    {
                        GameDrawer.ChangeColorKind(GameFinder.FindChild(drawable, Line.id), ColorKind, Line);
                    }
                    else
                    {
                        throw new System.Exception($"There is no drawable with the ID {DrawableID} or line with the ID {Line.id}.");
                    }
                }
            }
        }
    }
}