using RootMotion.FinalIK;
using SEE.Net.Actions;
using Unity.Netcode;
using UnityEngine;

namespace SEE.Game.Avatars
{
    /// <summary>
    /// Responsible for synchronizing the VRIK model on all clients.
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
        private NetworkObject networkObject;

        /// <summary>
        /// VRIK Component.
        /// </summary>
        private VRIK vrik;

        /// <summary>
        /// Timer to count elapsed time.
        /// </summary>
        private float timer;

        /// <summary>
        /// Initializes the network object and VRIK model. The periodic call of
        /// <see cref="Synchronize"/> is triggered.
        /// </summary>
        private void Start()
        {
            networkObject = gameObject.GetComponent<NetworkObject>();
            vrik = gameObject.GetComponent<VRIK>();
            timer = 0f;
        }

        /// <summary>
        /// Executes <see cref="Synchronize"/> every <see cref="RepeatCycle"/> seconds.
        /// </summary>
        private void Update()
        {
            timer += Time.deltaTime;
            if (timer >= RepeatCycle)
            {
                Synchronize();
                timer = 0f;
            }
        }

        /// <summary>
        /// If a change should be sent, the update is sent to all clients with
        /// the <see cref="NetworkObjectId"/> and <see cref="vrik"/> as parameters.
        /// </summary>
        private void Synchronize()
        {
            new VRIKNetAction(networkObject.NetworkObjectId, vrik).Execute();
        }
    }
}