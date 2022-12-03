using System.Collections;
using UnityEngine;
using UnityEngine.XR.Management;

namespace SEE.XR
{
    /// <summary>
    /// Allows to manually initialize XR.
    ///
    /// This component is assumed to be attached to an object that comes
    /// into existence only if XR is to be started locally (i.e, within
    /// this instance of the game on the computer running the VR),
    /// for instance, a SteamVR camera rig.
    /// </summary>
    internal class ManualXRControl : MonoBehaviour
    {
        /// <summary>
        /// Inializes XR.
        ///
        /// From the Unity documenation:
        /// Manual initialization of XR cannot be done before Start completes as it depends
        /// on graphics initialization within Unity completing.
        /// Initialization of XR must be complete either before the Unity graphics system is
        /// setup and initialized (as in Automatic life cycle management) or must be put off
        /// till after graphics is completely initialized. The easiest way to check this is
        /// to just make sure you do not try to start XR until Start is called on your
        /// MonoBehaviour instance.
        /// </summary>
        private void Start()
        {
            Status();
            StartCoroutine(nameof(StartXRCoroutine));
            Status();
        }

        /// <summary>
        /// Emits the current status of XR.
        /// </summary>
        private static void Status()
        {
            Debug.Log($"[XR] status: automaticRunning={XRGeneralSettings.Instance.Manager.automaticRunning} "
                      + $"automaticLoading={XRGeneralSettings.Instance.Manager.automaticLoading} "
                      + $"isInitializationComplete={XRGeneralSettings.Instance.Manager.isInitializationComplete}"
                      + ".\n");
        }

        /// <summary>
        /// Initializes XR. Upon the completion of this initialization all registered
        /// XR plug-ins are started (as set in the project settings).
        /// </summary>
        /// <returns>null if initialization is not yet completed</returns>
        public IEnumerator StartXRCoroutine()
        {
            Debug.Log("[XR] Initializing XR...\n");

            yield return XRGeneralSettings.Instance.Manager.InitializeLoader();

            if (XRGeneralSettings.Instance.Manager.activeLoader == null)
            {
                Debug.LogError("[XR] Initializing XR Failed. Check Editor or Player log for details.");
            }
            else
            {
                Debug.Log("[XR] Starting XR...");
                XRGeneralSettings.Instance.Manager.StartSubsystems();
            }
        }

        /// <summary>
        /// If XR was initialized, it will be stopped.
        /// </summary>
        private void OnDestroy()
        {
            if (XRGeneralSettings.Instance.Manager.isInitializationComplete)
            {
                StopXR();
            }
        }

        /// <summary>
        /// Stops XR.
        /// </summary>
        public static void StopXR()
        {
            Debug.Log("[XR] Stopping XR...");
            XRGeneralSettings.Instance.Manager.StopSubsystems();
            XRGeneralSettings.Instance.Manager.DeinitializeLoader();
            Debug.Log("[XR] XR stopped completely.");
            Status();
        }

        /// <summary>
        /// Returns true if XR is initialized.
        /// </summary>
        /// <returns>true if XR is initialized</returns>
        public static bool IsInitialized()
        {
            return XRGeneralSettings.Instance.Manager.isInitializationComplete;
        }
    }
}