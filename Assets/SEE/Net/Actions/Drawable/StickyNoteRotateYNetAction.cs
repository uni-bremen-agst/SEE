using SEE.Game.Drawable;
using UnityEngine;

namespace SEE.Net.Actions.Drawable
{
    /// <summary>
    /// This class is reponsible for change the y rotation of a sticky note on all clients.
    /// </summary>
    public class StickyNoteRoateYNetAction : DrawableNetAction
    {
        /// <summary>
        /// The degree by which the object should be rotated.
        /// </summary>
        public float Degree;
        /// <summary>
        /// The position of the object.
        /// </summary>
        public Vector3 ObjectPosition;

        /// <summary>
        /// The constructor of this action. All it does is assign the value you pass it to a field.
        /// </summary>
        public StickyNoteRoateYNetAction(string drawableID, string parentDrawableID, float degree, Vector3 oldPosition)
            : base(drawableID, parentDrawableID)
        {
            Degree = degree;
            ObjectPosition = oldPosition;
        }

        /// <summary>
        /// Change the y rotation of a sticky note on each client.
        /// </summary>
        public override void ExecuteOnClient()
        {
            base.ExecuteOnClient();
            GameStickyNoteManager.SetRotateY(GameFinder.GetHighestParent(Surface), Degree);
        }
    }
}
