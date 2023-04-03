using SEE.GO;
using Unity.Netcode;
using UnityEngine;

namespace SEE.Game.Avatars
{
    /// <summary>
    /// A framework calling a sychronization method periodically.
    /// </summary>
    internal abstract class NetSynchronizer : MonoBehaviour
    {
        /// <summary>
        /// Time in between two update cycles for the synchronization in seconds.
        /// </summary>
        private const float RepeatCycle = 0.05f;

        /// <summary>
        /// The network object.
        /// </summary>
        protected NetworkObject networkObject;

        /// <summary>
        /// Timer to count the elapsed time in seconds for one cycle.
        /// </summary>
        private float timer;

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
        /// Method to be called periodically every <see cref="RepeatCycle"/>.
        /// </summary>
        protected abstract void Synchronize();

        /// <summary>
        /// Resets <see cref="timer"/> and sets <see cref="networkObject"/>.
        /// </summary>
        protected virtual void Start()
        {
            networkObject = gameObject.AddOrGetComponent<NetworkObject>();
            timer = 0;
        }
    }
}
