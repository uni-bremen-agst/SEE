using UnityEngine;
using UnityEngine.Assertions;

namespace SEE.Net
{
    /// <summary>
    /// Responsible for the synchronization of the start- and end-position of the local ray.
    /// 
    /// This script searches for the local ray game object and copies its start- and end-position
    /// into synchonized transforms. These are synchronized via <see cref="TransformView"/>s. This
    /// script is part of the prefab <b>SEENetViveControllerRay</b>, which can be instantiated by
    /// any player via <see cref="InstantiatePrefabAction"/>.
    /// </summary>
    public class SEENetViveControllerRayLocal : MonoBehaviour
    {
        /// <summary>
        /// The name of the local ray <see cref="GameObject"/>.
        /// </summary>
        private const string Name = "/Player Rig/Interaction Manager/VR Vive-style Controller (Right)/Controller Head/GameObject/RaySelect";

        /// <summary>
        /// The transform synchronizing the local ray start position.
        /// </summary>
        [SerializeField] private Transform startTransform;

        /// <summary>
        /// The transform synchronizing the local ray end position.
        /// </summary>
        [SerializeField] private Transform endTransform;

        /// <summary>
        /// The local line renderer responsible for rendering the ray, which is supposed to be
        /// synchronized with other players.
        /// </summary>
        private LineRenderer lineRenderer;

        /// <summary>
        /// Determines the start- and end-transform of the ray to be synchronized.
        /// </summary>
        private void Start()
        {
            ViewContainer viewContainer = GetComponent<ViewContainer>();

            // We don't need this script, if this client did not instantiate the prefab containing
            // this script.
            if (viewContainer == null || !viewContainer.IsOwner())
            {
                Destroy(this);
                return;
            }

            Assert.IsNotNull(startTransform);
            Assert.IsNotNull(endTransform);
            GameObject lineRendererGameObject = GameObject.Find(Name);
            Assert.IsNotNull(lineRendererGameObject);
            lineRenderer = lineRendererGameObject.GetComponent<LineRenderer>();
            Assert.IsNotNull(lineRenderer);
        }

        /// <summary>
        /// Copies the position of the local ray into the synchronized transforms.
        /// </summary>
        private void Update()
        {
            startTransform.position = lineRenderer.GetPosition(0);
            endTransform.position = lineRenderer.GetPosition(1);
        }
    }
}
