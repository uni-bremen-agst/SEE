using SEE.Game;
using SEE.Utils;
using UnityEngine;
using SEE.Controls.Actions.Drawable;
using SEE.Game.Drawable;
using Assets.SEE.Game.Drawable;

namespace SEE.Net.Actions.Drawable
{
    /// <summary>
    /// This class is responsible for destroying the rigid bodies and collision controllers of the objects of the drawable on all clients.
    /// </summary>
    public class RbAndCCDestroyerNetAction : AbstractNetAction
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
        /// The constructor of this action. All it does is assign the value you pass it to a field.
        /// </summary>
        /// <param name="drawableID">The id of the drawable.</param>
        /// <param name="parentDrawableID">The id of the drawable parent.</param>
        public RbAndCCDestroyerNetAction(string drawableID, string parentDrawableID) : base()
        {
            DrawableID = drawableID;
            ParentDrawableID = parentDrawableID;
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
        /// Destroys all rigid bodies and collision controller of all drawable types object of the given drawable on each client.
        /// </summary>
        /// <exception cref="System.Exception">will be thrown, if the <see cref="DrawableID"/> or <see cref="ObjectName"/> don't exists.</exception>
        protected override void ExecuteOnClient()
        {
            if (!IsRequester())
            {
                GameObject drawable = GameFinder.FindDrawable(DrawableID,ParentDrawableID);
                if (drawable != null)
                {
                    GameMoveRotator.DestroyRigidBodysAndCollisionControllersOfChildren(GameFinder.GetAttachedObjectsObject(drawable));
                }
                else
                {
                    throw new System.Exception($"There is no drawable with the ID {DrawableID}.");
                }
            }
        }
    }
}
