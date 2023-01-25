using System;
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
        private const float RepeatCycle = 0.1f;
        
        /// <summary>
        /// The network object.
        /// </summary>
        private NetworkObject NetworkObject;
        
        
        private void Start()
        {
            NetworkObject = gameObject.GetComponent<NetworkObject>();
            InvokeRepeating(nameof(Synchronize), RepeatCycle, RepeatCycle);
        }
        
        private void Synchronize()
        {
            new VRIkNetAction(NetworkObject.NetworkObjectId).Execute();
        }
        
    }
}