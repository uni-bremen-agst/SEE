using SEE.Game.Drawable;
using SEE.Game.Drawable.Configurations;
using Unity.Netcode;
using UnityEngine;

namespace SEE.Net.Actions.Drawable
{
    /// <summary>
    /// Superclass for all surface network actions.
    /// It retains only the surface configuration and omits contained drawable type
    /// configurations to keep the network payload small.
    /// </summary>
    public class SurfaceNetAction : AbstractNetAction
    {
        /// <summary>
        /// Should not be sent to newly connecting clients
        /// </summary>
        public override bool ShouldBeSentToNewClient { get => false; }

        /// <summary>
        /// The configuration without drawable type objects
        /// </summary>
        public DrawableConfig DrawableConf;

        /// <summary>
        /// The drawable object that should be manipulated by this action.
        /// Will be set in the <see cref="ExecuteOnClient"/> method. Can be null.
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
        /// Creates a surface network action with a copy of the surface configuration
        /// that excludes contained drawable type configurations.
        /// </summary>
        /// <param name="config">The drawable configuration.</param>
        public SurfaceNetAction(DrawableConfig config)
        {
            DrawableConf = config.CloneWithoutDrawableTypes();
        }

        /// <summary>
        /// Unifies the search for the Surface object for the subclasses.
        /// </summary>
        public override void ExecuteOnClient()
        {
            Surface = GameFinder.FindDrawableSurface(DrawableConf.ID, DrawableConf.ParentID);
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
