using UnityEngine;
using RTG;
using SEE.Utils;
using SEE.GO;

namespace SEE.Game
{
    /// <summary>
    /// Initializes the Runtime Gizmos (RTG), which are used for scaling, rotating, and moving
    /// nodes.
    ///
    /// This component assumes that there is a game object in the scene with a
    /// <see cref="RTGApp"/> component attached to it. That game object
    /// is assumed to have a child named <see cref="rtgFocusCameraName"/> with a
    /// <see cref="RTFocusCamera"/> component attached to it. The game object holding the
    /// <see cref="RTGApp"/> is assumed to be set inactive initially.
    /// This component will set it active on request. This capability is intended to
    /// activate <see cref="RTGApp"/> only when truly needed, because <see cref="RTGApp"/>
    /// has a significant performance cost.
    ///
    /// In addition, this component sets the camera RTG requires
    /// as soon as the <see cref="MainCamera.Camera"/> becomes available. Because
    /// the camera becomes available only later at runtime, we cannot set it earlier
    /// or in the Unity editor.
    /// </summary>
    public class RTGInitializer : MonoBehaviour
    {
        /// <summary>
        /// The name of the game object that is an immediate child of <see cref="RTGApp"/>
        /// holding a component <see cref="RTFocusCamera"/> requiring the camera
        /// to be set.
        /// </summary>
        private const string rtgFocusCameraName = "RTFocusCamera";

        /// <summary>
        /// Sets the <see cref="RTFocusCamera"/> to the <see cref="MainCamera"/>.
        /// If the main camera is available, RTG will be inialized.
        /// The setting and initialization may be postponed until the main camera is available.
        /// </summary>
        private void Awake()
        {
            Camera camera = MainCamera.GetCameraNowOrLater(Initialize);
            if (camera != null)
            {
                Initialize(camera);
            }
        }

        /// <summary>
        /// Sets the <see cref="RTFocusCamera"/> to the <paramref name="camera"/> and
        /// activates <see cref="RTGApp"/>.
        /// </summary>
        /// <param name="camera">the main camera</param>
        private static void Initialize(Camera camera)
        {
            UnityEngine.Assertions.Assert.IsNotNull(camera);

            GameObject rtgApp = GetRTGApp();
            if (rtgApp != null)
            {
                Transform rtFocusCamera = rtgApp.transform.Find(rtgFocusCameraName);
                if (rtFocusCamera == null)
                {
                    Debug.LogError($"Game object {rtgApp.FullName()} is expected to have a child named {rtgFocusCameraName}.\n");
                    return;
                }

                if (rtFocusCamera.gameObject.TryGetComponentOrLog(out RTFocusCamera focusCamera))
                {
                    focusCamera.Settings.CanProcessInput = false;
                    focusCamera.SetTargetCamera(camera);
                }
                // The RTG App should be inactive until it is really needed. Will be enabled later via Enable().
                rtgApp.SetActive(false);
            }
        }

        /// <summary>
        /// The cached game object holding the <see cref="RTGApp"/> component.
        /// </summary>
        private static GameObject rtgApp;

        /// <summary>
        /// Returns the game object holding the <see cref="RTGApp"/> component.
        /// </summary>
        /// <returns>the game object holding the <see cref="RTGApp"/> component or null if none exists</returns>
        private static GameObject GetRTGApp()
        {
            if (rtgApp != null)
            {
                return rtgApp;
            }
            RTGApp result = GameObject.FindObjectOfType<RTGApp>(true);
            if (result == null)
            {
                Debug.LogError($"There is no game object having a {nameof(RTGApp)} component in the scene.\n");
                return null;
            }
            else
            {
                rtgApp = result.gameObject;
                return rtgApp;
            }
        }

        /// <summary>
        /// Enables the <see cref="RTGApp"/>.
        /// </summary>
        public static void Enable()
        {
            GetRTGApp()?.SetActive(true);
        }

        /// <summary>
        /// Disables the <see cref="RTGApp"/>.
        /// </summary>
        public static void Disable()
        {
            GetRTGApp()?.SetActive(false);
        }
    }
}
