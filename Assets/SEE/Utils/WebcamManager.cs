using Cysharp.Threading.Tasks;
using SEE.UI;
using SEE.UI.Notification;
using SEE.User;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.Utils
{
    /// <summary>
    /// Manages multiple webcams with lazy initialization.
    /// Only the active webcam is played to save resources.
    ///
    /// Note: A WebCamTexture cannot be initialized more than once per device.
    /// Attempting to create multiple WebCamTextures for the same camera will fail,
    /// so this manager ensures that each device has at most one WebCamTexture instance.
    /// </summary>
    public static class WebcamManager
    {
        /// <summary>
        /// List of all available <see cref="ActiveWebcam"/> instances in the order
        /// they were detected on the system.
        /// </summary>
        private static readonly List<WebCamTexture> webcams = new();

        /// <summary>
        /// Index of the currently active webcam in the <see cref="webcams"/> list.
        /// </summary>
        private static int activeIndex = 0;

        /// <summary>
        /// Gets the index of the currently active webcam.
        /// Initialization is performed automatically if required.
        /// If the stored index is out of range, it is reset to <c>0</c>.
        /// </summary>
        public static int ActiveIndex
        {
            get
            {
                EnsureInitialized();
                return activeIndex;
            }
        }

        /// <summary>
        /// Tracks how many active systems are currently using the active webcam.
        ///
        /// This acts as a simple reference counter:
        /// - Each component or action that requires webcam access must call <see cref="Acquire"/>
        ///   before using the webcam, and <see cref="Release"/> when finished.
        /// - The webcam is automatically started when the first user acquires it
        ///   (usageCount transitions from 0 -> 1),
        ///   and automatically stopped when the last user releases it
        ///   (usageCount transitions from 1 -> 0).
        ///
        /// This ensures that the webcam remains active only while it is actually in use,
        /// preventing unnecessary resource usage or device locking issues.
        /// </summary>
        private static int usageCount = 0;

        /// <summary>
        /// Occurs when the active webcam is changed via <see cref="SwitchCamera(int)"/>.
        /// </summary>
        public static event Action<WebCamTexture> OnActiveWebcamChanged;

        /// <summary>
        /// Returns the <see cref="ActiveWebcam"/> of the currently active webcam.
        /// Initialization is performed automatically if necessary.
        /// Returns <c>null</c> if no webcam devices are available.
        /// </summary>
        public static WebCamTexture ActiveWebcam
        {
            get
            {
                EnsureInitialized();
                return webcams.Count > 0 ? webcams[activeIndex] : null;
            }
        }

        /// <summary>
        /// Ensures that the webcam system is initialized.
        /// If no devices have been enumerated yet, <see cref="Initialize"/> is invoked.
        /// </summary>
        private static void EnsureInitialized()
        {
            if (webcams.Count == 0)
            {
                Initialize();
            }
        }

        /// <summary>
        /// Initializes the webcam manager and registers all available webcams.
        /// - If no webcams are found on the system, an error is logged and no <see cref="ActiveWebcam"/> is created.
        /// - The first detected device is selected as the active webcam and started automatically.
        /// - All other webcams are registered in the list but not started, to save resources.
        /// - Each <see cref="ActiveWebcam"/> is created exactly once per device.
        /// </summary>
        private static void Initialize()
        {
            WebCamDevice[] devices = WebCamTexture.devices;

            if (devices.Length == 0)
            {
                Debug.LogError("No webcam found.\n");
                return;
            }

            // Try to load previous selected webcam device from PlayerPrefs.
            string savedCamera = UserSettings.Instance.Video.WebcamName;

            // Initialize the remaining webcams (not played yet)
            for (int i = 0; i < devices.Length; i++)
            {
                WebCamDevice device = devices[i];
                webcams.Add(new(device.name));

                if (device.name == savedCamera)
                {
                    activeIndex = i;
                }
            }

            Debug.Log($"[WebcamManager] Active webcam initialized: {devices[activeIndex].name}");
        }

        /// <summary>
        /// Increases the usage count and ensures that the active webcam is playing.
        /// Automatically initializes the webcam system if needed.
        /// If no webcams are available, the call has no effect.
        /// </summary>
        public static void Acquire()
        {
            EnsureInitialized();

            if (webcams.Count == 0)
            {
                return;
            }

            usageCount++;

            if (!webcams[activeIndex].isPlaying)
            {
                webcams[activeIndex].Play();
                UIOverlay.ActivateWebcam();
            }
        }

        /// <summary>
        /// Signals that a component or action has finished using the active webcam.
        ///
        /// This method decrements the internal <see cref="usageCount"/> and, if no other
        /// users remain (i.e., usageCount transitions from 1 -> 0),
        /// automatically stops the webcam via <see cref="WebCamTexture.Stop"/>.
        ///
        /// Always call this method when a system is done using the webcam to release resources properly.
        ///
        /// Calling this method more times than <see cref="Acquire"/> has been called
        /// has no additional effect other than clamping the usage count to zero.
        /// </summary>
        public static void Release()
        {
            if (usageCount > 0)
            {
                usageCount--;
            }

            if (usageCount == 0 && webcams[activeIndex].isPlaying)
            {
                webcams[activeIndex].Stop();
                UIOverlay.DeactivateWebcam();
            }
        }


        /// <summary>
        /// Switches the active webcam by index.
        ///
        /// This method stops the currently active webcam (if it is running)
        /// and activates the selected one. If there are active users
        /// (i.e., <see cref="usageCount"/> > 0), the newly selected webcam
        /// is automatically started to ensure continuous streaming.
        ///
        /// The <see cref="OnActiveWebcamChanged"/> event is invoked after the switch,
        /// allowing subscribers to update their references to the new webcam texture.
        /// </summary>
        /// <param name="index">
        /// The index of the webcam in the <see cref="webcams"/> list.
        /// Must be within the valid range of detected devices.
        /// </param>
        public static void SwitchCamera(int index)
        {
            if (index < 0 || index >= webcams.Count)
            {
                Debug.LogWarning($"[WebcamManager] Webcam index {index} is out of range.");
                return;
            }

            if (activeIndex != index)
            {
                if (webcams[activeIndex] != null
                    && webcams[activeIndex].isPlaying)
                {
                    StopWebcamAsync(activeIndex).Forget();
                    ShowNotification.Info("Video Systems Disabled",
                        "All video-based systems have been temporarily disabled due to a webcam change. " +
                        "If you still need them, please restart the corresponding systems.");
                }
                activeIndex = index;
                OnActiveWebcamChanged?.Invoke(webcams[activeIndex]);

                // Saves the selected camera.
                UserSettings.Instance.Video.WebcamName = webcams[activeIndex].deviceName;
                UserSettings.Instance.Save();
            }

            static async UniTask StopWebcamAsync(int index)
            {
                await UniTask.Yield();
                webcams[index].Stop();
                usageCount = 0;
            }
        }

#if UNITY_EDITOR
        /// <summary>
        /// Resets all static state of the <see cref="WebcamManager"/> when entering Play Mode
        /// inside the Unity Editor.
        ///
        /// This is required because Unity may keep static fields alive between Play Mode sessions
        /// when "Enter Play Mode Options" (without Domain Reload) are enabled.
        /// In such cases, previously created <see cref="WebCamTexture"/> instances
        /// become destroyed Unity objects, causing <see cref="MissingReferenceException"/>
        /// when the manager attempts to access them.
        ///
        /// By clearing all cached webcam objects before each Play Mode run, the manager
        /// is forced to perform a clean reinitialization and avoids referencing
        /// destroyed <see cref="WebCamTexture"/> instances.
        ///
        /// This logic runs only inside the Unity Editor and has no effect in builds.
        /// </summary>
        [UnityEditor.InitializeOnLoadMethod]
        private static void ResetEditorStatics()
        {
            UnityEditor.EditorApplication.playModeStateChanged += state =>
            {
                if (state == UnityEditor.PlayModeStateChange.EnteredPlayMode)
                {
                    webcams.Clear();
                    usageCount = 0;
                    activeIndex = 0;
                }
            };
        }
#endif
    }
}
