using SEE.Game.Drawable;
using SEE.GO;
using Unity.Netcode;
using UnityEngine;

namespace SEE.Net.Actions.Drawable
{
    /// <summary>
    /// Superclass for all drawable net actions. Provides the drawable surface object
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
        /// The ID of the drawable on which the object is located
        /// </summary>
        public string SurfaceID;

        /// <summary>
        /// The ID of the drawable surface parent
        /// </summary>
        public string SurfaceParentID;

        /// <summary>
        /// The constructor of this action. Sets <paramref name="surfaceID"/>, <paramref name="surfaceParentID"/>,
        /// and - based on these -<see cref="Surface"/>.
        /// </summary>
        /// <param name="surfaceID">The ID of the drawable surface on which the object should be placed.</param>
        /// <param name="surfaceParentID">The ID of the drawable surface parent.</param>
        public DrawableNetAction(string surfaceID, string surfaceParentID)
        {
            SurfaceID = surfaceID;
            SurfaceParentID = surfaceParentID;
        }

        /// <summary>
        /// Returns the drawable object with the ID <see cref="SurfaceID"/>
        /// and the parent ID <see cref="SurfaceParentID"/>.
        /// </summary>
        /// <param name="surfaceID">The ID of the drawable object.</param>
        /// <param name="surfaceParentID">The ID of the parent drawable object.</param>
        /// <returns>The found drawable object (never null).</returns>
        /// <exception cref="System.Exception">Thrown if a drawable object cannot be found.</exception>
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
        /// Returns the child of <see cref="Surface"/> with the ID <paramref name="childID"/>.
        /// </summary>
        /// <param name="childID">The ID of the requested child.</param>
        /// <returns>The child (will never be null).</returns>
        /// <exception cref="System.Exception">Thrown in case the child does not exist.</exception>
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
        /// Returns true if the child with the ID <paramref name="childId"/> exists and
        /// returns this child in output parameter <paramref name="child"/>. Otherwise,
        /// false is returned and <paramref name="child"/> is undefined.
        /// </summary>
        /// <param name="childId">The ID of the requested child.</param>
        /// <param name="child">The found child if it exists; otherwise undefined</param>
        /// <returns>True if the child exists.</returns>
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

        /// <summary>
        /// Ensures that the changes are also applied to the server, necessary for the <see cref="DrawableSynchronizer">.
        /// </summary>
        public override void ExecuteOnServer()
        {
            if (Requester != NetworkManager.Singleton.LocalClientId)
            {
                base.ExecuteOnServer();
                ExecuteOnClient();
            }
        }
    }
}