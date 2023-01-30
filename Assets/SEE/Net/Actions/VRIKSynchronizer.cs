using RootMotion.FinalIK;
using Unity.Netcode;
using UnityEngine;

namespace SEE.Net.Actions
{
    /// <summary>
    /// Responsible to synchronize the vrik model on all clients.
    ///
    /// </summary>
    public class VRIKSynchronizer : MonoBehaviour
    {
        /// <summary>
        /// Time in between two update cycles for the synchronization in seconds.
        /// </summary>
        private const float RepeatCycle = 0.05f;

        /// <summary>
        /// The network object.
        /// </summary>
        private NetworkObject NetworkObject;

        private VRIK Vrik;

        /// <summary>
        /// Initializes the network object and vrik model.
        /// </summary>
        private void Start()
        {
            NetworkObject = gameObject.GetComponent<NetworkObject>();
            Vrik = gameObject.GetComponent<VRIK>();
            InvokeRepeating(nameof(Synchronize), RepeatCycle, RepeatCycle);
        }

        /// <summary>
        /// If an change should be sent the update is sent to all clients with
        /// the <see cref="NetworkObjectId"/> and <see cref="Vrik"/> as parameters.
        ///
        /// This method is invoked periodically while this component is active.
        /// It is registered in <see cref="Start"/>.
        /// </summary>
        private void Synchronize()
        {
            new VRIKNetAction(NetworkObject.NetworkObjectId, Vrik).Execute();
        }
    }
}