using SEE.Controls.Actions.Drawable;
using SEE.Utils;

namespace SEE.Net.Actions.Drawable
{
    /// <summary>
    /// This class is responsible for deleting (<see cref="EraseAction"/>) an object on a drawable on all clients.
    /// </summary>
    public class EraseNetAction : DrawableNetAction
    {
        /// <summary>
        /// The id of the object that should be deleted
        /// </summary>
        public string ObjectName;

        /// <summary>
        /// The constructor of this action. All it does is assign the value you pass it to a field.
        /// </summary>
        /// <param name="drawableID">The id of the drawable on which the object is located.</param>
        /// <param name="parentDrawableID">The id of the drawable parent.</param>
        /// <param name="objectName">The id of the object that should be deleted.</param>
        public EraseNetAction(string drawableID, string parentDrawableID, string objectName)
            : base(drawableID, parentDrawableID)
        {
            ObjectName = objectName;
        }

        /// <summary>
        /// Deletes the object on each client.
        /// </summary>
        /// <exception cref="System.Exception">Will be thrown, if the <see cref="DrawableID"/> or <see cref="ObjectName"/> don't exists.</exception>
        public override void ExecuteOnClient()
        {
            base.ExecuteOnClient();
            Destroyer.Destroy(FindChild(ObjectName));
        }
    }
}
