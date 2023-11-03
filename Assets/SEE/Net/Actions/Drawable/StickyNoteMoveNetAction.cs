using SEE.Net.Actions;
using System.Collections;
using UnityEngine;
using SEE.Controls.Actions.Drawable;
using Assets.SEE.Game.Drawable;
using SEE.Game.Drawable.Configurations;
using SEE.Game.Drawable;
using UnityEditor;

namespace SEE.Net.Actions.Drawable
{
    /// <summary>
    /// This class is reponsible for change the position of a sticky note on all clients.
    /// </summary>
    public class StickyNoteMoveNetAction : AbstractNetAction
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
        /// The new positon for the sticky note.
        /// </summary>
        public Vector3 Position;
        /// <summary>
        /// The rotation for the sticky note.
        /// </summary>
        public Vector3 Rotation;

        /// <summary>
        /// The constructor of this action. All it does is assign the value you pass it to a field.
        /// </summary>
        public StickyNoteMoveNetAction(string drawableID, string drawableParentID, Vector3 position, Vector3 rotation)
        {
            this.DrawableID = drawableID;
            this.ParentDrawableID = drawableParentID;
            this.Position = position;
            this.Rotation = rotation;
        }

        /// <summary>
        /// Things to execute on the server (none for this class). Necessary because it is abstract
        /// in the superclass.
        /// </summary>
        protected override void ExecuteOnServer()
        {
        }
        /// <summary>
        /// Change the position of a sticky note on each client.
        /// </summary>
        protected override void ExecuteOnClient()
        {
            if (!IsRequester())
            {
                GameObject drawable = GameFinder.Find(DrawableID, ParentDrawableID);
                if (drawable != null)
                {
                    GameStickyNoteManager.Move(GameFinder.GetHighestParent(drawable), Position, Rotation);
                }
                else
                {
                    throw new System.Exception($"There is no drawable with the ID {DrawableID}.");
                }
            }
        }
    }
}