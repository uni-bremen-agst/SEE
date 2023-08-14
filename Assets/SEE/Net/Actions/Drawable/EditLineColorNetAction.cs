using Assets.SEE.Game.Drawable;
using Assets.SEE.Game;
using SEE.Net.Actions;
using System.Collections;
using UnityEngine;

namespace Assets.SEE.Net.Actions.Drawable
{
    public class EditLineColorNetAction : AbstractNetAction
    {
        public string DrawableID;
        public string ParentDrawableID;
        public string LineName;
        public Color color;

        /// <summary>
        /// Creates a new FastEraseNetAction.
        /// </summary>
        /// <param name="gameObjectID">the unique name of the gameObject of a line
        /// that has to be deleted</param>
        public EditLineColorNetAction(string drawableID, string parentDrawableID, string lineName, Color color) : base()
        {
            DrawableID = drawableID;
            ParentDrawableID = parentDrawableID;
            LineName = lineName;
            this.color = color;
        }
        protected override void ExecuteOnServer()
        {

        }

        protected override void ExecuteOnClient()
        {
            if (!IsRequester())
            {
                if (!IsRequester())
                {
                    GameObject drawable = GameDrawableIDFinder.Find(DrawableID, ParentDrawableID);
                    if (drawable != null && GameDrawableIDFinder.FindChild(drawable, LineName) != null)
                    {
                        GameEditLine.ChangeColor(GameDrawableIDFinder.FindChild(drawable, LineName), color);
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