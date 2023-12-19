using SEE.Game.Drawable;
using SEE.Game.Drawable.Configurations;
using UnityEngine;

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
        /// /// <exception cref="System.Exception">will be thrown, if the <see cref="DrawableConf"/> don't exists.</exception>
        protected override void ExecuteOnClient()
        {
            if (!IsRequester())
            {
                if (DrawableConf != null && GameFinder.FindDrawable(DrawableConf.ID, DrawableConf.ParentID) != null)
                {
                    GameObject drawable = GameFinder.FindDrawable(DrawableConf.ID, DrawableConf.ParentID);
                    GameObject stickyNote = drawable.transform.parent.gameObject;
                    GameObject root = GameFinder.GetHighestParent(drawable);

                    GameStickyNoteManager.ChangeLayer(root, DrawableConf.Order);
                    GameStickyNoteManager.ChangeColor(stickyNote, DrawableConf.Color);
                    GameStickyNoteManager.SetRotateX(root, DrawableConf.Rotation.x);
                    GameStickyNoteManager.SetRotateY(root, DrawableConf.Rotation.y, DrawableConf.Position);
                    GameScaler.SetScale(stickyNote, DrawableConf.Scale);
                } else
                {
                    throw new System.Exception($"There is no drawable with the ID {DrawableConf.ID}.");
                }
            }
        }
    }
}