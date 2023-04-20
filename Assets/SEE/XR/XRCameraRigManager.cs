using SEE.GO;
using System.Collections;
using UnityEngine;

namespace SEE.XR
{
    /// <summary>
    /// Enables the two controllers of an XR camera rig when XR is enabled.
    ///
    /// This component is expected to be added to a SteamVR camera rig which
    /// has two immediate inactive children named <see cref="LeftHand Controller"/>
    /// and <see cref="RightHand Controller"/>, respectively.
    /// </summary>
    internal class XRCameraRigManager : MonoBehaviour
    {
        /// <summary>
        /// Name of the child of the <see cref="gameObject"/> representing the left
        /// controller to be enabled.
        /// </summary>
        internal const string LeftControllerName ="Camera Offset/LeftHand Controller"; //"TrackerOffsets/Controller (left)"; 
        /// <summary>
        /// Name of the child of the <see cref="gameObject"/> representing the right
        /// controller to be enabled.
        /// </summary>
        internal const string RightControllerName ="Camera Offset/RightHand Controller"; // "TrackerOffsets/Controller (right)"; 

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
            gameObject.SetChildActive(LeftControllerName, true);
            gameObject.SetChildActive(RightControllerName, true);

            // An XRHandOffset component is assumed to be attached and disabled initially.
            // It will query the XR devices, however, it needs to wait until we have started XR.
            // That is why it should be disabled initially. Now we know XR is running, hence,
            // we can start it.
            if (gameObject.TryGetComponent(out XRHandOffset xrHandOffset))
            {
                if (xrHandOffset.enabled)
                {
                    Debug.LogWarning($"{gameObject.FullName()} has an enabled {nameof(XRHandOffset)} component. It should disabled initially.\n");
                }
                else
                {
                    xrHandOffset.enabled = true;
                }
            }
        }
    }
}
