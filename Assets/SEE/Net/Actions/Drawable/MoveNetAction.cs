using SEE.Net.Actions;
using SEE.Controls.Actions.Drawable;
using UnityEngine;
using SEE.Game.Drawable;

namespace SEE.Net.Actions.Drawable
{
    /// <summary>
    /// This class is responsible for changing the position (<see cref="MoveRotatorAction"/>) of an object on all clients.
    /// </summary>
    public class MoveNetAction : AbstractNetAction
    {
        /// <summary>
        /// The id of the drawable on which the object is located
        /// </summary>
        public string DrawableID;
        /// <summary>
        /// The id of the drawable parent
        /// </summary>
        public string ParentDrawableID;
        /// <summary>
        /// The id of the object that should be changed
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
        /// <param name="drawableID">The id of the drawable on which the object is located.</param>
        /// <param name="parentDrawableID">The id of the drawable parent.</param>
        /// <param name="objectName">The id of the object that should be changed.</param>
        /// <param name="position">The new position to which the object should be set.</param>
        public MoveNetAction(string drawableID, string parentDrawableID, string objectName, Vector3 position, bool includeChildren) : base()
        {
            DrawableID = drawableID;
            ParentDrawableID = parentDrawableID;
            ObjectName = objectName;
            this.Position = position;
            this.IncludeChildren = includeChildren;
        }

        /// <summary>
        /// Things to execute on the server (none for this class). Necessary because it is abstract
        /// in the superclass.
        /// </summary>
        protected override void ExecuteOnServer()
        {

        }

        /// <summary>
        /// Changes the position of the given object on each client.
        /// </summary>
        /// <exception cref="System.Exception">will be thrown, if the <see cref="DrawableID"/> or <see cref="ObjectName"/> don't exists.</exception>
        protected override void ExecuteOnClient()
        {
            if (!IsRequester())
            {
                if (!IsRequester())
                {
                    GameObject drawable = GameFinder.FindDrawable(DrawableID, ParentDrawableID);
                    if (drawable != null && GameFinder.FindChild(drawable, ObjectName) != null)
                    {
                        GameMoveRotator.SetPosition(GameFinder.FindChild(drawable, ObjectName), Position, IncludeChildren);
                    }
                    else
                    {
                        throw new System.Exception($"There is no drawable with the ID {DrawableID} or line with the ID {ObjectName}.");
                    }
                }
            }
        }
    }
}