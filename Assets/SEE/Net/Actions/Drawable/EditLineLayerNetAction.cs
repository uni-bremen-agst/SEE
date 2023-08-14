using Assets.SEE.Game.Drawable;
using Assets.SEE.Game;
using SEE.Net.Actions;
using System.Collections;
using UnityEngine;

namespace Assets.SEE.Net.Actions.Drawable
{
    public class EditLineLayerNetAction : AbstractNetAction
    {
        public string DrawableID;
        public string ParentDrawableID;
        public string LineName;
        public int layerOrder;

        /// <summary>
        /// Creates a new FastEraseNetAction.
        /// </summary>
        /// <param name="gameObjectID">the unique name of the gameObject of a line
        /// that has to be deleted</param>
        public EditLineLayerNetAction(string drawableID, string parentDrawableID, string lineName, int layerOrder) : base()
        {
            DrawableID = drawableID;
            ParentDrawableID = parentDrawableID;
            LineName = lineName;
            this.layerOrder = layerOrder;
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
                        GameEditLine.ChangeLayer(GameDrawableIDFinder.FindChild(drawable, LineName), layerOrder);
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