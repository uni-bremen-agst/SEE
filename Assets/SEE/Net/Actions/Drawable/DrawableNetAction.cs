using SEE.Game.Drawable;
using SEE.GO;
using Unity.Netcode;
using UnityEngine;

namespace SEE.Net.Actions.Drawable
{
    /// <summary>
    /// Superclass for all drawable new actions. Provides the drawable surface object
    /// and an implelentation of <see cref="ExecuteOnServer"/> that does nothing.
    /// </summary>
    public abstract class DrawableNetAction : AbstractNetAction
    {
        /// <summary>
        /// Should not be sent to newly connecting clients
        /// </summary>
        public override bool ShouldBeSentToNewClient { get => false; }

        /// <summary>
        /// The drawable object that should be manipulated by this action.
        /// Will be set in the constructor.
        /// </summary>
        protected GameObject Surface { get; private set; }

        /// <summary>
        /// The id of the drawable on which the object is located
        /// </summary>
        public string SurfaceID;

        /// <summary>
        /// The id of the drawable surface parent
        /// </summary>
        public string SurfaceParentID;

        /// <summary>
        /// The constructor of this action. Sets <paramref name="surfaceID"/>, <paramref name="surfaceParentID"/>,
        /// and - based on these -<see cref="Surface"/>.
        /// </summary>
        /// <param name="surfaceID">The id of the drawable surface on which the object should be placed.</param>
        /// <param name="surfaceParentID">The id of the drawable surface parent.</param>
        public DrawableNetAction(string surfaceID, string surfaceParentID)
        {
            SurfaceID = surfaceID;
            SurfaceParentID = surfaceParentID;
        }

        /// <summary>
        /// Returns the drawable object with the id <see cref="SurfaceID"/>
        /// and the parent id <see cref="SurfaceParentID"/>.
        /// </summary>
        /// <param name="surfaceID">The id of the drawable object.</param>
        /// <param name="surfaceParentID">The id of the parent drawable object.</param>
        /// <returns>the found drawable object (never null)</returns>
        /// <exception cref="System.Exception">thrown if a drawable object cannot be found</exception>
        protected static GameObject Find(string surfaceID, string surfaceParentID)
        {
            GameObject surface = GameFinder.FindDrawableSurface(surfaceID, surfaceParentID);
            if (surface == null)
            {
                throw new System.Exception($"There is no drawable with the ID {surfaceID} and parent ID {surfaceParentID}.");
            }
            return surface;
        }

        /// <summary>
        /// Returns the child of <see cref="Surface"/> with the id <paramref name="childID"/>.
        /// </summary>
        /// <param name="childID">the ID of the requested child</param>
        /// <returns>the child (will never be null)</returns>
        /// <exception cref="System.Exception">thrown in case the child does not exist</exception>
        protected GameObject FindChild(string childID)
        {
            GameObject child = GameFinder.FindChild(Surface, childID);
            if (child == null)
            {
                throw new System.Exception($"The drawable {Surface.FullName()} has no child with the ID {childID}.");
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
            child = GameFinder.FindChild(Surface, childId);
            return child != null;
        }

        /// <summary>
        /// Unifies the search for the Surface object for the subclasses.
        /// </summary>
        public override void ExecuteOnClient()
        {
            Surface = Find(SurfaceID, SurfaceParentID);
        }
    }
}
