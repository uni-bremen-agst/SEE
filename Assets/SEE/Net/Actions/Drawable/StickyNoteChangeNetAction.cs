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
    /// This class is reponsible for change the sticky note values on all clients.
    /// </summary>
    public class StickyNoteChangeNetAction : AbstractNetAction
    {
        /// <summary>
        /// The configuration which holds all data.
        /// </summary>
        public DrawableConfig DrawableConf;

        /// <summary>
        /// The constructor of this action. All it does is assign the value you pass it to a field.
        /// </summary>
        public StickyNoteChangeNetAction(DrawableConfig config)
        {
            this.DrawableConf = config;
        }

        /// <summary>
        /// Things to execute on the server (none for this class). Necessary because it is abstract
        /// in the superclass.
        /// </summary>
        protected override void ExecuteOnServer()
        {
        }
        /// <summary>
        /// Changes the values of the sticky note on each client.
        /// </summary>
        protected override void ExecuteOnClient()
        {
            if (!IsRequester())
            {
                GameObject drawable = GameFinder.Find(DrawableConf.DrawableName, DrawableConf.DrawableParentName);
                GameObject stickyNote = drawable.transform.parent.gameObject;
                GameObject root = GameFinder.GetHighestParent(drawable);

                GameStickyNoteManager.ChangeLayer(root, DrawableConf.Order);
                GameStickyNoteManager.ChangeColor(stickyNote, DrawableConf.Color);
                GameStickyNoteManager.SetRotateX(root, DrawableConf.Rotation.x);
                GameStickyNoteManager.SetRotateY(root, DrawableConf.Rotation.y, DrawableConf.Position);
                GameScaler.SetScale(stickyNote, DrawableConf.Scale);
            }
        }
    }
}