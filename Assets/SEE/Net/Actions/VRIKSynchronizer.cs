using System;
using RootMotion.FinalIK;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SEE.Net.Actions
{
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
        
        private void Start()
        {
            NetworkObject = gameObject.GetComponent<NetworkObject>();
            Vrik = gameObject.GetComponent<VRIK>();
            InvokeRepeating(nameof(Synchronize), RepeatCycle, RepeatCycle);
        }
        
        private void Synchronize()
        {
            new VRIKNetAction(NetworkObject.NetworkObjectId, Vrik).Execute();
        }
    }
}