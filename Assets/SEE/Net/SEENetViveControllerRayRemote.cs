using UnityEngine;
using UnityEngine.Assertions;

namespace SEE.Net.Internal
{

    public class SEENetViveControllerRayRemote : MonoBehaviour
    {
        public Transform startTransform;
        public Transform endTransform;
        public LineRenderer lineRenderer;

        private void Start()
        {
            ViewContainer viewContainer = GetComponent<ViewContainer>();
            if (viewContainer == null || viewContainer.IsOwner())
            {
                Destroy(this);
                return;
            }

            Assert.IsNotNull(startTransform);
            Assert.IsNotNull(endTransform);
            Assert.IsNotNull(lineRenderer);
            lineRenderer.material.color = Color.green;
        }
        private void Update()
        {
            lineRenderer.SetPosition(0, startTransform.position);
            lineRenderer.SetPosition(1, endTransform.position);
        }
    }

}
