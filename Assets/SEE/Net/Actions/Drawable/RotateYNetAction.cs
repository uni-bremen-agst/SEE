using SEE.Controls.Actions.Drawable;
using SEE.Game.Drawable;

namespace SEE.Net.Actions.Drawable
{
    /// <summary>
    /// This class is responsible for changing the y rotation of an object on all clients.
    /// It is needed to mirror an image on the y axis about 180°.
    /// </summary>
    public class RotatorYNetAction : DrawableNetAction
    {
        /// <summary>
        /// The ID of the object that should be changed.
        /// </summary>
        public string ObjectName;
        /// <summary>
        /// The degree by which the object should be rotated.
        /// </summary>
        public float Degree;

        /// <summary>
        /// The constructor of this action. All it does is assign the value you pass it to a field.
        /// Used for undo / redo of <see cref="MoveRotateAction"/>
        /// </summary>
        /// <param name="drawableID">The ID of the drawable on which the object is located.</param>
        /// <param name="parentDrawableID">The ID of the drawable parent.</param>
        /// <param name="objectName">The ID of the object that should be changed.</param>
        /// <param name="localEulerAnlgeY">The value to which the object should be rotated.</param>
        public RotatorYNetAction(string drawableID, string parentDrawableID, string objectName, float localEulerAnlgeY)
            : base(drawableID, parentDrawableID)
        {
            ObjectName = objectName;
            Degree = localEulerAnlgeY;
        }

        /// <summary>
        /// Changes the rotation of the given object on each client.
        /// </summary>
        /// <exception cref="System.Exception">Will be thrown, if the <see cref="DrawableID"/> or <see cref="ObjectName"/> don't exists.</exception>
        public override void ExecuteOnClient()
        {
            base.ExecuteOnClient();
            GameMoveRotator.SetRotateY(FindChild(ObjectName), Degree);
        }
    }
}
