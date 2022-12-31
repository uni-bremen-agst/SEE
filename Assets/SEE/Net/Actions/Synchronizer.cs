using SEE.Controls;
using UnityEngine;

namespace SEE.Net.Actions
{
    /// <summary>
    /// Responsible to synchronize position, rotation and potentially local scale
    /// of an interactable object on all clients.
    ///
    /// This component is intended to be attached to an interactable object while
    /// it is being manipulated, that is, while its position, rotation, or scale
    /// is changed.
    /// </summary>
    [RequireComponent(typeof(InteractableObject))]
    public class Synchronizer : MonoBehaviour
    {
        /// <summary>
        /// Whether a sychronization is needed, i.e., whether any of the
        /// synchronized attributes of the interactable object's transform
        /// have changed.
        /// </summary>
        private bool sendUpdate;
        /// <summary>
        /// The interactable object this component is attached to and whose
        /// counter parts on all clients should be synchronized with.
        /// </summary>
        private InteractableObject interactable;

        /// <summary>
        /// The number of update cycles this component waits until it assumes
        /// that the <see cref="SynchronizeInteractableNetAction"/> message sent
        /// to all clients has not been received. If no <see cref="NotifyJustReceivedUpdate"/>
        /// has been received, the data is sent again to all clients.
        /// </summary>
        public int updateTimeout;

        /// <summary>
        /// Time in between two update cycles for the synchronization in seconds.
        /// </summary>
        private const float RepeatCycle = 0.1f;

        /// <summary>
        /// Intializes <see cref="sendUpdate"/>, <see cref="interactable"/>, and
        /// <see cref="updateTimeout"/>. The period call of <see cref="Synchronize"/> is
        /// triggered.
        /// </summary>
        private void Start()
        {
            sendUpdate = false;
            interactable = GetComponent<InteractableObject>();
            updateTimeout = 0;
            InvokeRepeating(nameof(Synchronize), RepeatCycle, RepeatCycle);
        }

        /// <summary>
        /// Updates <see cref="sendUpdate"/> reflecting whether the
        /// Transform of the game object this component is attached to
        /// has changed and, hence, those changes need to synchronized
        /// on all clients.
        /// </summary>
        private void Update()
        {
            sendUpdate |= transform.hasChanged;
        }

        /// <summary>
        /// Let's this component know that the <see cref="SynchronizeInteractableNetAction"/>
        /// originally sent by this component has been received by a client.
        ///
        /// Resets <see cref="updateTimeout"/>.
        /// </summary>
        public void NotifyJustReceivedUpdate()
        {
            updateTimeout = 3;
        }

        /// <summary>
        /// If an update should be sent (indicated by <see cref="sendUpdate"/>
        /// and <see cref="updateTimeout"/> is greater than zero), a
        /// <see cref="SynchronizeInteractableNetAction"/> is sent to all clients with
        /// the <see cref="interactable"/> object as parameter.
        ///
        /// This method is invoked periodically while this component is active.
        /// It is registered in <see cref="Start"/>.
        /// </summary>
        private void Synchronize()
        {
            if (sendUpdate)
            {
                if (updateTimeout > 0)
                {
                    updateTimeout--;
                }
                else
                {
                    sendUpdate = false;
                    new SynchronizeInteractableNetAction(interactable, false).Execute();
                }
            }
        }
    }
}
