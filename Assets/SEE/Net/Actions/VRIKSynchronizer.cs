using RootMotion.FinalIK;
using Unity.Netcode;
using UnityEngine;

namespace SEE.Net.Actions
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
        private NetworkObject NetworkObject;

        /// <summary>
        /// VRIK Component.
        /// </summary>
        private VRIK Vrik;

        /// <summary>
        /// Timer to count elapsed time.
        /// </summary>
        private float Timer;

        /// <summary>
        /// Initializes the network object and VRIK model. The periodic call of
        /// <see cref="Synchronize"/> is triggered.
        /// </summary>
        private void Start()
        {
            NetworkObject = gameObject.GetComponent<NetworkObject>();
            Vrik = gameObject.GetComponent<VRIK>();
            Timer = 0f;
        }

        /// <summary>
        /// Executes <see cref="Synchronize"/> every 0.5 seconds.
        /// </summary>
        private void Update()
        {
            Debug.Log(Timer);
            Timer += Time.deltaTime;
            while (Timer >= RepeatCycle)
            {
                Synchronize();
                Timer -= RepeatCycle;
                Debug.Log(Timer);
            }
        }
        
        /// <summary>
        /// If a change should be sent, the update is sent to all clients with
        /// the <see cref="NetworkObjectId"/> and <see cref="Vrik"/> as parameters.
        /// </summary>
        private void Synchronize()
        {
            new VRIKNetAction(NetworkObject.NetworkObjectId, Vrik).Execute();
        }
    }
}