using SEE.Extensions;
using System.Collections;
using UnityEngine;

namespace SEE.XR
{
#if ENABLE_VR
    /// <summary>
    /// Enables the two controllers of an XR camera rig when XR is enabled.
    ///
    /// This component is expected to be added to an XR camera rig which
    /// has two immediate inactive children named <see cref="LeftControllerName"/>
    /// and <see cref="RightControllerName"/>, respectively.
    /// </summary>
    internal class XRCameraRigManager : MonoBehaviour
    {
        /// <summary>
        /// Name of the child of the <see cref="gameObject"/> representing the left
        /// controller to be enabled.
        /// </summary>
        internal const string LeftControllerName = "Camera Offset/Left Controller";
        /// <summary>
        /// Name of the child of the <see cref="gameObject"/> representing the right
        /// controller to be enabled.
        /// </summary>
        internal const string RightControllerName = "Camera Offset/Right Controller";

        /// <summary>
        /// Enables the two controllers when XR is initialized.
        /// </summary>
        private void Start()
        {
            StartCoroutine(EnableControllersCoroutine());
        }

        /// <summary>
        /// Enables the two controllers when XR is initialized.
        /// </summary>
        /// <returns>null if XR is not yet initialized</returns>
        private IEnumerator EnableControllersCoroutine()
        {
            Debug.Log($"[{nameof(XRCameraRigManager)}] Waiting for XR to be initialized.\n");
            while (!ManualXRControl.IsInitialized())
            {
                yield return null;
            }

            Debug.Log($"[{nameof(XRCameraRigManager)}] Enabling controllers.\n");
            SetChildActive(gameObject, LeftControllerName, true);
            SetChildActive(gameObject, RightControllerName, true);
        }

        /// <summary>
        /// Enables/disables the child of <paramref name="gameObject"/> with <paramref name="childName"/>.
        /// </summary>
        /// <param name="gameObject">Object whose child is to be enabled/disabled.</param>
        /// <param name="childName">The name of the child; may be a composite name.</param>
        /// <param name="active">Whether to enable it.</param>
        private static void SetChildActive(GameObject gameObject, string childName, bool active)
        {
            Transform child = gameObject.transform.Find(childName);
            if (child)
            {
                child.gameObject.SetActive(active);
            }
            else
            {
                Debug.LogError($"Game object '{gameObject.FullName()}' does not have child with name '{childName}'.\n");
            }
        }
    }
#endif
}
