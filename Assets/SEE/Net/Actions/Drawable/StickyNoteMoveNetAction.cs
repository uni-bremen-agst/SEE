using SEE.Game.Drawable;
using UnityEngine;

namespace SEE.Net.Actions.Drawable
{
    /// <summary>
    /// This class is reponsible for change the position of a sticky note on all clients.
    /// </summary>
    public class StickyNoteMoveNetAction : DrawableNetAction
    {
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
        public StickyNoteMoveNetAction(string drawableID, string parentDrawableID, Vector3 position, Vector3 rotation)
            : base(drawableID, parentDrawableID)
        {
            Position = position;
            Rotation = rotation;
        }

        /// <summary>
        /// Changes the position of a sticky note on each client.
        /// </summary>
        public override void ExecuteOnClient()
        {
            base.ExecuteOnClient();
            GameStickyNoteManager.Move(GameFinder.GetHighestParent(Surface), Position, Rotation);
        }
    }
}
