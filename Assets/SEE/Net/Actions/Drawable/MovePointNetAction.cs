using Assets.SEE.Game.Drawable;
using Assets.SEE.Game;
using SEE.Net.Actions;
using System.Collections;
using UnityEngine;
using System.Collections.Generic;

namespace Assets.SEE.Net.Actions.Drawable
{
    public class MovePointNetAction : AbstractNetAction
    {
        public string DrawableID;
        public string ParentDrawableID;
        public string LineName;
        public Vector3 Position;
        public List<int> Indexes;

        /// <summary>
        /// Creates a new FastEraseNetAction.
        /// </summary>
        /// <param name="gameObjectID">the unique name of the gameObject of a line
        /// that has to be deleted</param>
        public MovePointNetAction(string drawableID, string parentDrawableID, string lineName, List<int> indexes, Vector3 position) : base()
        {
            DrawableID = drawableID;
            ParentDrawableID = parentDrawableID;
            LineName = lineName;
            this.Position = position;
            this.Indexes = indexes;
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
                        GameMoveRotator.MovePoint(GameDrawableFinder.FindChild(drawable, LineName), Indexes, Position);
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