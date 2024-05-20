﻿using SEE.GO;
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
            gameObject.SetChildActive(LeftControllerName, true);
            gameObject.SetChildActive(RightControllerName, true);
        }
    }
#endif
}
