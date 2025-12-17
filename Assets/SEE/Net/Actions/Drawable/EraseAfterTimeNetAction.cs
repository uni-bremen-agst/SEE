using UnityEngine;

namespace SEE.Net.Actions.Drawable
{
    /// <summary>
    /// This class is responsible for deleting an object after a chosen time on a drawable on all clients.
    /// </summary>
    public class EraseAfterTimeNetAction : DrawableNetAction
    {
        /// <summary>
        /// The id of the object that should be deleted
        /// </summary>
        public string ObjectName;
        /// <summary>
        /// The time that should be waited until the object is deleted.
        /// </summary>
        public float Time;

        /// <summary>
        /// The constructor of this action. All it does is assign the value you pass it to a field.
        /// </summary>
        /// <param name="drawableID">The id of the drawable on which the object is located.</param>
        /// <param name="parentDrawableID">The id of the drawable parent.</param>
        /// <param name="objectName">The id of the object that should be deleted.</param>
        /// <param name="time">The time that should be waited until the object is deleted.</param>
        public EraseAfterTimeNetAction(string drawableID, string parentDrawableID, string objectName, float time)
            : base(drawableID, parentDrawableID)
        {
            ObjectName = objectName;
            Time = time;
        }

        /// <summary>
        /// Deletes the object on each client.
        /// </summary>
        /// <exception cref="System.Exception">Will be thrown, if the <see cref="DrawableID"/> or <see cref="ObjectName"/> don't exists.</exception>
        public override void ExecuteOnClient()
        {
            base.ExecuteOnClient();
            Object.Destroy(FindChild(ObjectName), Time);
        }
    }
}
