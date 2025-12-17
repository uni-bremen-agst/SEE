using SEE.Controls.Actions.Drawable;
using SEE.Game.Drawable;
using UnityEngine;

namespace SEE.Net.Actions.Drawable
{
    /// <summary>
    /// This class is responsible for changing the scale (<see cref="ScaleAction"/>) of an object on all clients.
    /// </summary>
    public class ScaleNetAction : DrawableNetAction
    {
        /// <summary>
        /// The id of the object that should be changed
        /// </summary>
        public string ObjectName;
        /// <summary>
        /// The scale that should be set
        /// </summary>
        public Vector3 Scale;

        /// <summary>
        /// The constructor of this action. All it does is assign the value you pass it to a field.
        /// </summary>
        /// <param name="drawableID">The id of the drawable on which the object is located.</param>
        /// <param name="parentDrawableID">The id of the drawable parent.</param>
        /// <param name="objectName">The id of the object that should be changed.</param>
        /// <param name="scale">The scale that should be set.</param>
        public ScaleNetAction(string drawableID, string parentDrawableID, string objectName, Vector3 scale)
            : base(drawableID, parentDrawableID)
        {
            ObjectName = objectName;
            Scale = scale;
        }

        /// <summary>
        /// Changes the scale of the given object on each client.
        /// </summary>
        /// <exception cref="System.Exception">Will be thrown, if the <see cref="DrawableID"/> or <see cref="ObjectName"/> don't exists.</exception>
        public override void ExecuteOnClient()
        {
            base.ExecuteOnClient();
            if (TryFindChild(ObjectName, out GameObject child))
            {
                GameScaler.SetScale(GameFinder.FindChild(Surface, ObjectName), Scale);
            }
            else
            {
                GameScaler.SetScale(Surface.transform.parent.gameObject, Scale);
            }
        }
    }
}
