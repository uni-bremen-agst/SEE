using UnityEngine;

namespace SEE.Net
{
    public class Synchronizer : MonoBehaviour
    {
        private bool sendUpdate;
        public int updateTimeout;

        void Start()
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

        void Update()
        {
            sendUpdate |= Input.GetMouseButton(2) && transform.hasChanged;
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
                    new SynchronizeAction(gameObject).Execute();
                }
            }
        }
    }
}
