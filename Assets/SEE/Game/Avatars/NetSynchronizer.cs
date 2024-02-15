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
        private const float repeatCycle = 0.05f;

        /// <summary>
        /// The network object.
        /// </summary>
        protected NetworkObject NetworkObject;

        /// <summary>
        /// Timer to count the elapsed time in seconds for one cycle.
        /// </summary>
        private float timer;

        /// <summary>
        /// Executes <see cref="Synchronize"/> every <see cref="repeatCycle"/> seconds.
        /// </summary>
        private void Update()
        {
            timer += Time.deltaTime;
            if (timer >= repeatCycle)
            {
                Synchronize();
                timer = 0f;
            }
        }

        /// <summary>
        /// Method to be called periodically every <see cref="repeatCycle"/>.
        /// </summary>
        protected abstract void Synchronize();

        /// <summary>
        /// Resets <see cref="timer"/> and sets <see cref="NetworkObject"/>.
        /// </summary>
        protected virtual void Start()
        {
            NetworkObject = gameObject.AddOrGetComponent<NetworkObject>();
            timer = 0;
        }
    }
}
