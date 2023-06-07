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
    /// This component is assumed to be attached to a game object which has a child
    /// named <see cref="RTGAppName"/> managing the RTG gizmos. That game object
    /// has a <see cref="RTGApp"/> component attachted to it and a child named
    /// <see cref="RTGFocusCameraName"/> with a <see cref="RTFocusCamera"/> component
    /// attached to it. The child <see cref="RTGApp"/> is assumed to be set inactive initially.
    /// This component will set it active and set the current camera RTG requires. Because
    /// the camera becomes available only later at runtime, we need to set it here.
    /// </summary>
    public class RTGInitializer : MonoBehaviour
    {
        /// <summary>
        /// Name of the game object representing RTG. This object will be added if
        /// a developer selects the menu entry
        /// "Tools/Runtime Configuration Gizmo/Initialize" in the Unity editor.
        /// </summary>
        private const string RTGAppName = "RTGApp";

        /// <summary>
        /// The name of the game object that is an immediate child of <see cref="RTGApp"/>
        /// holding a component <see cref="RTFocusCamera"/> requiring the camera
        /// to be set.
        /// </summary>
        private const string RTGFocusCameraName = "RTFocusCamera";

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
            }
        }
    }
}
