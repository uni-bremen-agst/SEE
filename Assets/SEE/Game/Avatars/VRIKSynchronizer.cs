using RootMotion.FinalIK;
using SEE.Net.Actions;

namespace SEE.Game.Avatars
{
    /// <summary>
    /// Responsible for synchronizing the VRIK model on all clients.
    /// </summary>
    internal class VRIKSynchronizer : NetSynchronizer
    {
        /// <summary>
        /// VRIK Component.
        /// </summary>
        private VRIK vrik;

        /// <summary>
        /// Initializes the network object and VRIK model. The periodic call of
        /// <see cref="Synchronize"/> is triggered.
        /// </summary>
        protected override void Start()
        {
            base.Start();
            vrik = gameObject.GetComponent<VRIK>();
        }

        /// <summary>
        /// If a change should be sent, the update is sent to all clients with
        /// the <see cref="NetworkObjectId"/> and <see cref="vrik"/> as parameters.
        /// </summary>
        protected override void Synchronize()
        {
            new VRIKNetAction(networkObject.NetworkObjectId, vrik).Execute();
        }
    }
}