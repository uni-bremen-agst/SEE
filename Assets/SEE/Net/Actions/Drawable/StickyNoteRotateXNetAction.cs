using SEE.Game.Drawable;
using SEE.GO;

namespace SEE.Net.Actions.Drawable
{
    /// <summary>
    /// This class is reponsible for change the x rotation of a sticky note on all clients.
    /// </summary>
    public class StickyNoteRotateXNetAction : DrawableNetAction
    {
        /// <summary>
        /// The degree by which the object should be rotated.
        /// </summary>
        public float Degree;

        /// <summary>
        /// The constructor of this action. All it does is assign the value you pass it to a field.
        /// </summary>
        public StickyNoteRotateXNetAction(string drawableID, string parentDrawableID, float degree)
            : base(drawableID, parentDrawableID)
        {
            Degree = degree;
        }

        /// <summary>
        /// Changes the x rotation of a sticky note on each client.
        /// </summary>
        /// <exception cref="System.Exception">Will be thrown, if the <see cref="DrawableID"/> don't exists.</exception>
        public override void ExecuteOnClient()
        {
            base.ExecuteOnClient();
            GameStickyNoteManager.SetRotateX(Surface.GetRootParent(), Degree);
        }
    }
}
