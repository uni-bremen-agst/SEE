using MathNet.Numerics.LinearAlgebra;
using SEE.Game.Drawable;
using SEE.GO;
using UnityEngine;

namespace SEE.Net.Actions.Drawable
{
    /// <summary>
    /// Superclass for all drawable new actions. Provides the drawable object
    /// and an implelentation of <see cref="ExecuteOnServer"/> that does nothing.
    /// </summary>
    public abstract class DrawableNetAction : AbstractNetAction
    {
        /// <summary>
        /// The drawable object that should be manipulated by this action.
        /// Will be set in the constructor.
        /// </summary>
        protected GameObject Drawable { get; private set; }

        /// <summary>
        /// The id of the drawable on which the object is located
        /// </summary>
        public string DrawableID;
        /// <summary>
        /// The id of the drawable parent
        /// </summary>
        public string ParentDrawableID;

        /// <summary>
        /// The constructor of this action. Sets <paramref name="drawableID"/>, <paramref name="parentDrawableID"/>,
        /// and - based on these -<see cref="Drawable"/>.
        /// </summary>
        /// <param name="drawableID">The id of the drawable on which the object should be placed.</param>
        /// <param name="parentDrawableID">The id of the drawable parent.</param>
        public DrawableNetAction(string drawableID, string parentDrawableID)
        {
            DrawableID = drawableID;
            ParentDrawableID = parentDrawableID;
        }

        /// <summary>
        /// Returns the drawable object with the id <see cref="DrawableID"/>
        /// and the parent id <see cref="ParentDrawableID"/>.
        /// </summary>
        /// <param name="drawableID">The id of the drawable object.</param>
        /// <param name="parentDrawableID">The id of the parent drawable object.</param>
        /// <returns>the found drawable object (never null)</returns>
        /// <exception cref="System.Exception">thrown if a drawable object cannot be found</exception>
        protected static GameObject Find(string drawableID, string parentDrawableID)
        {
            GameObject drawable = GameFinder.FindDrawable(drawableID, parentDrawableID);
            if (drawable == null)
            {
                throw new System.Exception($"There is no drawable with the ID {drawableID} and parent ID {parentDrawableID}.");
            }
            return drawable;
        }

        /// <summary>
        /// Returns the child of <see cref="Drawable"/> with the id <paramref name="childID"/>.
        /// </summary>
        /// <param name="childID">the ID of the requested child</param>
        /// <returns>the child (will never be null)</returns>
        /// <exception cref="System.Exception">thrown in case the child does not exist</exception>
        protected GameObject FindChild(string childID)
        {
            GameObject child = GameFinder.FindChild(Drawable, childID);
            if (child == null)
            {
                throw new System.Exception($"The drawable {Drawable.FullName()} has no child with the ID {childID}.");
            }
            return child;
        }

        /// <summary>
        /// Returns true if the child with the id <paramref name="childId"/> exists and
        /// returns this child in output parameter <paramref name="child"/>. Otherwise,
        /// false is returned and <paramref name="child"/> is undefined.
        /// </summary>
        /// <param name="childId"><the ID of the requested child/param>
        /// <param name="child">the found child if it exists; otherwise undefined</param>
        /// <returns>true if the child exists</returns>
        protected bool TryFindChild(string childId, out GameObject child)
        {
            child = GameFinder.FindChild(Drawable, childId);
            return child != null;
        }

        /// <summary>
        /// Things to execute on the server (none for this class and its subclasses).
        /// </summary>
        protected override void ExecuteOnServer()
        {
            // Nothing to do here.
        }

        protected override void ExecuteOnClient()
        {
            Drawable = Find(DrawableID, ParentDrawableID);
        }
    }
}