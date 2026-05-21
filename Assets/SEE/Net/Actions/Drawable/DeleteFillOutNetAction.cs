using SEE.Game.Drawable;
using SEE.Utils;

namespace SEE.Net.Actions.Drawable
{
    /// <summary>
    /// This class is responsible for deleting a fill out of a line on all clients.
    /// </summary>
    public class DeleteFillOutNetAction : DrawableNetAction
    {
        /// <summary>
        /// The ID of the line which fill out should be deleted.
        /// </summary>
        public string LineID;

        /// <summary>
        /// The constructor of this action. All it does is to assign the values you pass it to a field.
        /// </summary>
        /// <param name="drawableID">The ID of the drawable on which the object is located.</param>
        /// <param name="parentDrawableID">The ID of the drawable parent.</param>
        /// <param name="lineName">The line ID of the object that should be deleted.</param>
        public DeleteFillOutNetAction(string drawableID, string parentDrawableID, string lineName)
            : base(drawableID, parentDrawableID)
        {
            LineID = lineName;
        }

        /// <summary>
        /// Deletes the object on each client.
        /// </summary>
        /// <exception cref="System.Exception">Will be thrown if the <see cref="LineID"/> does not exists.</exception>
        public override void ExecuteOnClient()
        {
            base.ExecuteOnClient();
            Destroyer.Destroy(GameFinder.FindChild(FindChild(LineID), ValueHolder.FillOut));
        }
    }
}
