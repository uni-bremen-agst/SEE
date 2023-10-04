using Assets.SEE.Game;
using SEE.Game;
using SEE.Utils;
using UnityEngine;
using SEE.Controls.Actions.Drawable;

namespace SEE.Net.Actions.Drawable
{
    /// <summary>
    /// This class is responsible for deleting (<see cref="EraseAction"/>) an object on a drawable on all clients.
    /// </summary>
    public class EraseNetAction : AbstractNetAction
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
        /// The id of the object that should be deleted
        /// </summary>
        public string ObjectName;

        /// <summary>
        /// The constructor of this action. All it does is assign the value you pass it to a field.
        /// </summary>
        /// <param name="drawableID">The id of the drawable on which the object is located.</param>
        /// <param name="parentDrawableID">The id of the drawable parent.</param>
        /// <param name="objectName">The id of the object that should be deleted.</param>
        public EraseNetAction(string drawableID, string parentDrawableID, string objectName) : base()
        {
            DrawableID = drawableID;
            ParentDrawableID = parentDrawableID;
            ObjectName = objectName;
        }

        /// <summary>
        /// Things to execute on the server (none for this class). Necessary because it is abstract
        /// in the superclass.
        /// </summary>
        protected override void ExecuteOnServer()
        {
            // Intentionally left blank.
        }

        /// <summary>
        /// Deletes the object on each client.
        /// </summary>
        /// <exception cref="System.Exception">will be thrown, if the <see cref="DrawableID"/> or <see cref="ObjectName"/> don't exists.</exception>
        protected override void ExecuteOnClient()
        {
            if (!IsRequester())
            {
                GameObject drawable = GameDrawableFinder.Find(DrawableID,ParentDrawableID);
                if (drawable != null && GameDrawableFinder.FindChild(drawable, ObjectName) != null)
                {
                    Destroyer.Destroy(GameDrawableFinder.FindChild(drawable, ObjectName));
                }
                else
                {
                    throw new System.Exception($"There is no drawable with the ID {DrawableID} or line with the ID {ObjectName}.");
                }
            }
        }
    }
}
