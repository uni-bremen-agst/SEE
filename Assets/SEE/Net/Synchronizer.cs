using SEE.Controls;
using UnityEngine;

namespace SEE.Net
{
    [RequireComponent(typeof(InteractableObject))]
    public class Synchronizer : MonoBehaviour
    {
        private bool sendUpdate;
        private InteractableObject interactable;
        public int updateTimeout;

        private void Start()
        {
            sendUpdate = false;
            interactable = GetComponent<InteractableObject>();
            updateTimeout = 0;

            if (Network.UseInOfflineMode)
            {
                Destroy(this);
            }
            else
            {
                InvokeRepeating(nameof(Synchronize), 0.1f, 0.1f);
            }
        }

        private void Update()
        {
            sendUpdate |= transform.hasChanged;
        }

        public void NotifyJustReceivedUpdate()
        {
            updateTimeout = 3;
        }

        private void Synchronize()
        {
            if (Network.UseInOfflineMode)
            {
                Destroy(this);
            }
            else if (sendUpdate)
            {
                if (updateTimeout > 0)
                {
                    updateTimeout--;
                }
                else
                {
                    sendUpdate = false;
                    new SynchronizeInteractableAction(interactable, false).Execute();
                }
            }
        }
    }
}
