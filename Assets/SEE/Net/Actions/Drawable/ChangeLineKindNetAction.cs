using Assets.SEE.Game.Drawable;
using Assets.SEE.Game;
using SEE.Net.Actions;
using System.Collections;
using UnityEngine;
using SEE.Game;

namespace Assets.SEE.Net.Actions.Drawable
{
    public class ChangeLineKindNetAction : AbstractNetAction
    {
        public string DrawableID;
        public string ParentDrawableID;
        public string LineName;
        public GameDrawer.LineKind LineKind;
        public float Tiling;

        /// <summary>
        /// Creates a new FastEraseNetAction.
        /// </summary>
        /// <param name="gameObjectID">the unique name of the gameObject of a line
        /// that has to be deleted</param>
        public ChangeLineKindNetAction(string drawableID, string parentDrawableID, string lineName, GameDrawer.LineKind lineKind, float tiling) : base()
        {
            DrawableID = drawableID;
            ParentDrawableID = parentDrawableID;
            LineName = lineName;
            this.LineKind = lineKind;
            this.Tiling = tiling;
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
                    GameObject drawable = GameDrawableFinder.Find(DrawableID, ParentDrawableID);
                    if (drawable != null && GameDrawableFinder.FindChild(drawable, LineName) != null)
                    {
                        GameDrawer.ChangeLineKind(GameDrawableFinder.FindChild(drawable, LineName), LineKind, Tiling);
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