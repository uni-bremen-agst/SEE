using SEE.Controls.Actions.Drawable;
using SEE.Game.Drawable;
using UnityEngine;

namespace SEE.Net.Actions.Drawable
{
    /// <summary>
    /// This class is responsible for changing the y rotation of an object on all clients.
    /// It is needed to mirror an image on the y axis about 180°.
    /// </summary>
    public class RotatorYNetAction : AbstractNetAction
    {
        /// <summary>
        /// The id of the drawable on which the object is located.
        /// </summary>
        public string DrawableID;
        /// <summary>
        /// The id of the drawable parent.
        /// </summary>
        public string ParentDrawableID;
        /// <summary>
        /// The id of the object that should be changed.
        /// </summary>
        public string ObjectName;
        /// <summary>
        /// The degree by which the object should be rotated.
        /// </summary>
        public float Degree;

        /// <summary>
        /// The constructor of this action. All it does is assign the value you pass it to a field.
        /// Used for undo / redo of <see cref="MoveRotatorAction"/> 
        /// </summary>
        /// <param name="drawableID">The id of the drawable on which the object is located.</param>
        /// <param name="parentDrawableID">The id of the drawable parent.</param>
        /// <param name="objectName">The id of the object that should be changed.</param>
        /// <param name="localEulerAnlgeY">The value to which the object should be rotated</param>
        public RotatorYNetAction(string drawableID, string parentDrawableID, string objectName, float localEulerAnlgeY) : base()
        {
            DrawableID = drawableID;
            ParentDrawableID = parentDrawableID;
            ObjectName = objectName;
            Degree = localEulerAnlgeY;
        }

        /// <summary>
        /// Things to execute on the server (none for this class). Necessary because it is abstract
        /// in the superclass.
        /// </summary>
        protected override void ExecuteOnServer()
        {

        }

        /// <summary>
        /// Changes the rotation of the given object on each client.
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
                        GameObject child = GameFinder.FindChild(drawable, ObjectName);
                        GameMoveRotator.SetRotateY(child, Degree);

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