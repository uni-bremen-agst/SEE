using UnityEngine;

namespace SEE.Net
{
    public class Synchronizer : MonoBehaviour
    {
        private bool sendUpdate;
        public int updateTimeout;

        private void Start()
        {
            sendUpdate = false;
            updateTimeout = 0;

            if (Network.UseInOfflineMode)
            {
                Destroy(this);
            }
            else
            {
                InvokeRepeating("Synchronize", 0.1f, 0.1f);
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
            if (sendUpdate)
            {
                if (updateTimeout > 0)
                {
                    updateTimeout--;
                }
                else
                {
                    sendUpdate = false;
                    new SynchronizeBuildingTransformAction(gameObject, false).Execute();
                }
            }
        }
    }
}
