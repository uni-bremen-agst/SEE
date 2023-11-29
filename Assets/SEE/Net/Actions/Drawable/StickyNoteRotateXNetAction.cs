using SEE.Net.Actions;
using System.Collections;
using UnityEngine;
using SEE.Controls.Actions.Drawable;
using Assets.SEE.Game.Drawable;
using SEE.Game.Drawable.Configurations;
using SEE.Game.Drawable;

namespace SEE.Net.Actions.Drawable
{
    /// <summary>
    /// This class is reponsible for change the x rotation of a sticky note on all clients.
    /// </summary>
    public class StickyNoteRotateXNetAction : AbstractNetAction
    {
        /// <summary>
        /// The id of the drawable on which the object is located.
        /// </summary>
        public string DrawableID;
        /// <summary>
        /// The id of the drawable parent.
        /// </summary>
        public string ParentDrawableID;
        /// <summary>
        /// The degree by which the object should be rotated.
        /// </summary>
        public float Degree;

        /// <summary>
        /// The constructor of this action. All it does is assign the value you pass it to a field.
        /// </summary>
        public StickyNoteRotateXNetAction(string drawableID, string drawableParentID, float degree)
        {
            this.DrawableID = drawableID;
            this.ParentDrawableID = drawableParentID;
            this.Degree = degree;
        }

        /// <summary>
        /// Things to execute on the server (none for this class). Necessary because it is abstract
        /// in the superclass.
        /// </summary>
        protected override void ExecuteOnServer()
        {
        }
        /// <summary>
        /// Change the x rotation of a sticky note on each client.
        /// </summary>
        /// <exception cref="System.Exception">will be thrown, if the <see cref="DrawableID"/> don't exists.</exception>
        protected override void ExecuteOnClient()
        {
            if (!IsRequester())
            {
                GameObject drawable = GameFinder.FindDrawable(DrawableID, ParentDrawableID);
                if (drawable != null)
                {
                    GameStickyNoteManager.SetRotateX(GameFinder.GetHighestParent(drawable), Degree);
                }
                else
                {
                    throw new System.Exception($"There is no drawable with the ID {DrawableID} and {ParentDrawableID}.");
                }
            }
        }
    }
}