using UMA.PoseTools;
using Unity.Netcode;
using UnityEngine;
using System.Collections;

namespace SEE.Net.Actions
{
    /// <summary>
    /// Responsible to synchronize the facial expressions
    /// of the <see cref="UMAExpressionPlayer"/> on all clients.
    ///
    /// This component is intended to be attached to a gameobject that has a <see cref="UMAExpressionPlayer"/> as
    /// component.
    /// </summary>
    public class ExpressionPlayerSynchronizer : MonoBehaviour
    {
        /// <summary>
        /// Parts of this UMAExpressionPlayer should be synchronized on all clients.
        /// </summary>
        private UMAExpressionPlayer ExpressionPlayer;
        
        /// <summary>
        /// Time in between two update cycles for the synchronization in seconds.
        /// </summary>
        private const float RepeatCycle = 0.05f;
        
        /// <summary>
        /// The network object.
        /// </summary>
        private NetworkObject NetworkObject;
        
        /// <summary>
        /// Timer to count elapsed time.
        /// </summary>
        private float Timer;
        
        /// <summary>
        /// Waits until <see cref="UMAExpressionPlayer"/> is available and initializes <see cref="ExpressionPlayer"/> 
        /// and <see cref="NetworkObject"/>. 
        /// </summary>
        private IEnumerator WaitForExpressionPlayer()
        {
            // Waits for UMAExpressionPlayer to be created
            yield return new WaitUntil(() => gameObject.GetComponent<UMAExpressionPlayer>() != null);
            ExpressionPlayer = gameObject.GetComponent<UMAExpressionPlayer>();
            NetworkObject = gameObject.GetComponent<NetworkObject>();
            Timer = 0f;
        }

        /// <summary>
        /// Starts a coroutine that waits for components to be generated at runtime.
        /// </summary>
        private void Start()
        {
            StartCoroutine(WaitForExpressionPlayer());
        }
        
        /// <summary>
        /// Executes <see cref="Synchronize"/> every <see cref="RepeatCycle"/> seconds.
        /// </summary>
        private void Update()
        {
            Timer += Time.deltaTime;
            if (Timer >= RepeatCycle)
            {
                Synchronize();
                Timer = 0f;
            }
        }
        
        /// <summary>
        /// This method is invoked periodically while this component is active.
        /// </summary>
        private void Synchronize()
        {
            new ExpressionPlayerNetAction(ExpressionPlayer, NetworkObject.NetworkObjectId).Execute();
        }
    }
}