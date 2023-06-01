using UnityEngine;
using RTG;
using UnityEngine.SceneManagement;
using SEE.Utils;
using System;
using SEE.GO;

namespace SEE.Game
{
    /// <summary>
    /// Initializes the Runtime Gizmos (RTG), which are used for scaling, rotating, and moving
    /// nodes.
    ///
    /// This component is assumed to be attached to a game object which has a child
    /// named <see cref="RTGAppName"/> managing the RTG gizmos. That game object
    /// has a <see cref="RTGApp"/> component attachted to it and a child named
    /// <see cref="RTGFocusCameraName"/> with a <see cref="RTFocusCamera"/> component
    /// attached to it.
    /// </summary>
    public class RTGInitializer : MonoBehaviour
    {

        private const string RTGAppName = "RTGApp";

        private const string RTGFocusCameraName = "RTFocusCamera";

        private void Awake()
        {
            Camera camera = MainCamera.GetCameraNowOrLater(OnCameraAvailable);
            if (camera != null)
            {
                Initialize(camera);
            }
        }

        private void OnCameraAvailable(Camera camera)
        {
            Initialize(camera);
        }

        private void Initialize(Camera camera)
        {
            UnityEngine.Assertions.Assert.IsNotNull(camera);

            Transform rtgApp = gameObject.transform.Find(RTGAppName);
            if (rtgApp == null)
            {
                Debug.LogError($"Game object {gameObject.FullName()} is expected to have a child named {RTGAppName}.\n");
                return;
            }

            Transform rtFocusCamera = rtgApp.transform.Find(RTGFocusCameraName);
            if (rtFocusCamera == null)
            {
                Debug.LogError($"Game object {rtgApp.gameObject.FullName()} is expected to have a child named {RTGFocusCameraName}.\n");
                return;
            }

            if (rtFocusCamera.gameObject.TryGetComponentOrLog(out RTFocusCamera focusCamera))
            {
                focusCamera.Settings.CanProcessInput = false;
                focusCamera.SetTargetCamera(camera);
                rtgApp.gameObject.SetActive(true);
                //RTGApp.Initialize();
            }
        }
    }
}
