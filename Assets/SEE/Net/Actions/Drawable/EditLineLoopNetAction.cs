using Assets.SEE.Game;
using Assets.SEE.Game.Drawable;
using SEE.Net.Actions;
using SEE.Utils;
using System.Collections;
using UnityEngine;

namespace Assets.SEE.Net.Actions.Drawable
{
    public class EditLineLoopNetAction : AbstractNetAction
    {
        public string DrawableID;
        public string ParentDrawableID;
        public string LineName;
        public bool Loop;

        /// <summary>
        /// Creates a new FastEraseNetAction.
        /// </summary>
        /// <param name="gameObjectID">the unique name of the gameObject of a line
        /// that has to be deleted</param>
        public EditLineLoopNetAction(string drawableID, string parentDrawableID, string lineName, bool loop) : base()
        {
            DrawableID = drawableID;
            ParentDrawableID = parentDrawableID;
            LineName = lineName;
            Loop = loop;
        }
        protected override void ExecuteOnServer()
        {

        }

        protected override void ExecuteOnClient()
        {
            if (!IsRequester())
            {
                GameObject drawable = GameDrawableFinder.Find(DrawableID, ParentDrawableID);
                if (drawable != null && GameDrawableFinder.FindChild(drawable, LineName) != null)
                {
                    GameEditLine.ChangeLoop(GameDrawableFinder.FindChild(drawable, LineName), Loop);
                }
                else
                {
                    throw new System.Exception($"There is no drawable with the ID {DrawableID} or line with the ID {LineName}.");
                }
            }
        }
    }
}