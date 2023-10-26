using Assets.SEE.Game.Drawable;
using SEE.Net.Actions;
using System.Collections;
using UnityEngine;
using SEE.Controls.Actions.Drawable;
using SEE.Game.Drawable;

namespace SEE.Net.Actions.Drawable
{
    /// <summary>
    /// This class is responsible for changing the scale (<see cref="ScaleAction"/>) of an object on all clients.
    /// </summary>
    public class ScaleNetAction : AbstractNetAction
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
        public ScaleNetAction(string drawableID, string parentDrawableID, string objectName, Vector3 scale) : base()
        {
            DrawableID = drawableID;
            ParentDrawableID = parentDrawableID;
            ObjectName = objectName;
            Scale = scale;
        }

        /// <summary>
        /// Things to execute on the server (none for this class). Necessary because it is abstract
        /// in the superclass.
        /// </summary>
        protected override void ExecuteOnServer()
        {

        }

        /// <summary>
        /// Changes the scale of the given object on each client.
        /// </summary>
        /// <exception cref="System.Exception">will be thrown, if the <see cref="DrawableID"/> or <see cref="ObjectName"/> don't exists.</exception>
        protected override void ExecuteOnClient()
        {
            if (!IsRequester())
            {
                GameObject drawable = GameFinder.Find(DrawableID, ParentDrawableID);
                if (drawable != null && GameFinder.FindChild(drawable, ObjectName) != null)
                {
                    GameScaler.SetScale(GameFinder.FindChild(drawable, ObjectName), Scale);
                }
                else
                {
                    throw new System.Exception($"There is no drawable with the ID {DrawableID} or line with the ID {ObjectName}.");
                }
            }
        }

        
    }
}