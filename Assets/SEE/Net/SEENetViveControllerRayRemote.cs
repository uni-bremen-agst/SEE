using UnityEngine;
using UnityEngine.Assertions;

namespace SEE.Net
{
    /// <summary>
    /// Responsible for the synchronization of the start- and end-position of a remote ray.
    /// 
    /// If a remote player instantiates the prefab <b>SEENetViveControllerRay</b> via
    /// <see cref="InstantiatePrefabAction"/>, that remote player synchonizes the start- and end-
    /// position of their local ray into <see cref="startTransform"/> and
    /// <see cref="endTransform"/>. The synchronization is done by <see cref="TransformView"/>s.
    /// </summary>
    public class SEENetViveControllerRayRemote : MonoBehaviour
    {
        /// <summary>
        /// The transform to copy the remote ray start position from.
        /// </summary>
        [SerializeField] private Transform startTransform;

        /// <summary>
        /// The transform to copy the remote ray end position from.
        /// </summary>
        [SerializeField] private Transform endTransform;

        /// <summary>
        /// The local line renderer to copy the remote start- and end-position to.
        /// </summary>
        [SerializeField] private LineRenderer lineRenderer;

        private void Start()
        {
            ViewContainer viewContainer = GetComponent<ViewContainer>();

            // We don't need this script, if this client did instantiate the prefab containing this
            // script.
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

        /// <summary>
        /// Updates the start- and end-position of the local ray. The transforms are synchronized
        /// by <see cref="TransformView"/>s from a remote client.
        /// </summary>
        private void Update()
        {
            lineRenderer.SetPosition(0, startTransform.position);
            lineRenderer.SetPosition(1, endTransform.position);
        }
    }
}
