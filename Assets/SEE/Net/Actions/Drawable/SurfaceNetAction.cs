using SEE.Game.Drawable;
using SEE.Game.Drawable.Configurations;
using Unity.Netcode;
using UnityEngine;

namespace SEE.Net.Actions.Drawable
{
    /// <summary>
    /// Superclass for all surface net actions.
    /// It clears the list of drawable types of the surface to make
    /// the configuration smaller.
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
        /// The id of the drawable on which the object is located
        /// </summary>
        public string SurfaceID;
        /// <summary>
        /// The id of the drawable surface parent
        /// </summary>
        public string SurfaceParentID;

        /// <summary>
        /// The constructor of this action.
        /// It clears the drawable type object lists.
        /// </summary>
        /// <param name="config">The drawable configuration.</param>
        public SurfaceNetAction(DrawableConfig config)
        {
            DrawableConf = (DrawableConfig)config.Clone();
            DrawableConf.LineConfigs.Clear();
            DrawableConf.TextConfigs.Clear();
            DrawableConf.ImageConfigs.Clear();
            DrawableConf.MindMapNodeConfigs.Clear();

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