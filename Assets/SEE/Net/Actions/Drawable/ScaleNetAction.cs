using Assets.SEE.Game;
using Assets.SEE.Game.Drawable;
using SEE.Net.Actions;
using System.Collections;
using UnityEngine;

namespace Assets.SEE.Net.Actions.Drawable
{
    public class ScaleNetAction : AbstractNetAction
    {
        public string DrawableID;
        public string ParentDrawableID;
        public string ObjectName;
        public Vector3 Scale;

        /// <summary>
        /// Creates a new FastEraseNetAction.
        /// </summary>
        /// <param name="gameObjectID">the unique name of the gameObject of a line
        /// that has to be deleted</param>
        public ScaleNetAction(string drawableID, string parentDrawableID, string objectName, Vector3 scale) : base()
        {
            DrawableID = drawableID;
            ParentDrawableID = parentDrawableID;
            ObjectName = objectName;
            Scale = scale;
        }
        protected override void ExecuteOnServer()
        {

        }
        protected override void ExecuteOnClient()
        {
            if (!IsRequester())
            {
                GameObject drawable = GameDrawableFinder.Find(DrawableID, ParentDrawableID);
                if (drawable != null && GameDrawableFinder.FindChild(drawable, ObjectName) != null)
                {
                    GameScaler.SetScale(GameDrawableFinder.FindChild(drawable, ObjectName), Scale);
                }
                else
                {
                    throw new System.Exception($"There is no drawable with the ID {DrawableID} or line with the ID {ObjectName}.");
                }
            }
        }

        
    }
}