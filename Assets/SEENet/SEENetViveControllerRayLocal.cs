using UnityEngine;
using UnityEngine.Assertions;

namespace SEE.Net.Internal
{

    public class SEENetViveControllerRayLocal : MonoBehaviour
    {
        public const string NAME = "/Player Rig/Interaction Manager/VR Vive-style Controller (Right)/Controller Head/GameObject/RaySelect";

        public Transform startTransform;
        public Transform endTransform;
        private LineRenderer lineRenderer;

        private void Start()
        {
            ViewContainer viewContainer = GetComponent<ViewContainer>();
            if (viewContainer == null || !viewContainer.IsOwner())
            {
                Destroy(this);
                return;
            }

            Assert.IsNotNull(startTransform);
            Assert.IsNotNull(endTransform);
            GameObject lineRendererGameObject = GameObject.Find(NAME);
            Assert.IsNotNull(lineRendererGameObject);
            lineRenderer = lineRendererGameObject.GetComponent<LineRenderer>();
            Assert.IsNotNull(lineRenderer);
        }
        private void Update()
        {
            startTransform.position = lineRenderer.GetPosition(0);
            endTransform.position = lineRenderer.GetPosition(1);
        }
    }

}
