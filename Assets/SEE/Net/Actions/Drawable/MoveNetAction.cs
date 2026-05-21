using SEE.Controls.Actions.Drawable;
using SEE.Game.Drawable;
using UnityEngine;

namespace SEE.Net.Actions.Drawable
{
    /// <summary>
    /// This class is responsible for changing the position (<see cref="MoveRotateAction"/>) of an object on all clients.
    /// </summary>
    public class MoveNetAction : DrawableNetAction
    {
        /// <summary>
        /// The ID of the object that should be changed
        /// </summary>
        public string ObjectName;
        /// <summary>
        /// The new position of the object
        /// </summary>
        public Vector3 Position;
        /// <summary>
        /// The state if the children should moved also.
        /// Only necressary for mind map nodes.
        /// </summary>
        public bool IncludeChildren;

        /// <summary>
        /// The constructor of this action. All it does is assign the value you pass it to a field.
        /// </summary>
        /// <param name="drawableID">The ID of the drawable on which the object is located.</param>
        /// <param name="parentDrawableID">The ID of the drawable parent.</param>
        /// <param name="objectName">The ID of the object that should be changed.</param>
        /// <param name="position">The new position to which the object should be set.</param>
        public MoveNetAction(string drawableID, string parentDrawableID, string objectName, Vector3 position, bool includeChildren)
            : base(drawableID, parentDrawableID)
        {
            ObjectName = objectName;
            Position = position;
            IncludeChildren = includeChildren;
        }

        /// <summary>
        /// Changes the position of the given object on each client.
        /// </summary>
        /// <exception cref="System.Exception">Will be thrown, if the <see cref="DrawableID"/> or <see cref="ObjectName"/> don't exists.</exception>
        public override void ExecuteOnClient()
        {
            base.ExecuteOnClient();
            GameMoveRotator.SetPosition(FindChild(ObjectName), Position, IncludeChildren);
        }
    }
}