using Assets.SEE.Game;
using Assets.SEE.Game.Drawable;
using SEE.Net.Actions;
using SEE.Utils;
using System.Collections;
using UnityEngine;

namespace Assets.SEE.Net.Actions.Drawable
{
    public class EditLineThicknessNetAction : AbstractNetAction
    {
        public string DrawableID;
        public string ParentDrawableID;
        public string LineName;
        public float thickness;

        /// <summary>
        /// Creates a new FastEraseNetAction.
        /// </summary>
        /// <param name="gameObjectID">the unique name of the gameObject of a line
        /// that has to be deleted</param>
        public EditLineThicknessNetAction(string drawableID, string parentDrawableID, string lineName, float thickness) : base()
        {
            DrawableID = drawableID;
            ParentDrawableID = parentDrawableID;
            LineName = lineName;
            this.thickness = thickness;
        }
        protected override void ExecuteOnServer()
        {

        }

        protected override void ExecuteOnClient()
        {
            if (!IsRequester())
            {
                GameObject drawable = GameDrawableIDFinder.Find(DrawableID, ParentDrawableID);
                if (drawable != null && GameDrawableIDFinder.FindChild(drawable, LineName) != null)
                {
                    GameEditLine.ChangeThickness(GameDrawableIDFinder.FindChild(drawable, LineName), thickness);
                }
                else
                {
                    throw new System.Exception($"There is no drawable with the ID {DrawableID} or line with the ID {LineName}.");
                }
            }
        }
    }
}