using Cysharp.Threading.Tasks;
using SEE.UI;
using SEE.UI.Notification;
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
        /// List of all available <see cref="WebCamTexture"/> instances in the order
        /// they were detected on the system.
        /// </summary>
        private static readonly List<WebCamTexture> webcams = new();

        /// <summary>
        /// Index of the currently active webcam in the <see cref="webcams"/> list.
        /// </summary>
        private static int activeIndex = 0;

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
        /// Gets the currently active <see cref="WebCamTexture"/>.
        /// If no webcam has been initialized yet, this property automatically calls
        /// <see cref="Initialize"/> to create and start the first available webcam device.
        /// If initialization fails (e.g., because no camera is available), this property will return null.
        /// </summary>
        public static WebCamTexture WebCamTexture
        {
            get
            {
                if (webcams.Count == 0)
                {
                    Initialize();
                }
                return webcams.Count > 0 ? webcams[activeIndex] : null;
            }
        }

        /// <summary>
        /// Initializes the webcam manager and registers all available webcams.
        /// - If no webcams are found on the system, an error is logged and no <see cref="WebCamTexture"/> is created.
        /// - The first detected device is selected as the active webcam and started automatically.
        /// - All other webcams are registered in the list but not started, to save resources.
        /// - Each <see cref="WebCamTexture"/> is created exactly once per device.
        /// </summary>
        private static void Initialize()
        {
            WebCamDevice[] devices = WebCamTexture.devices;

            if (devices.Length == 0)
            {
                Debug.LogError("No webcam found.\n");
                return;
            }

            // Initialize the remaining webcams (not played yet)
            for (int i = 0; i < devices.Length; i++)
            {
                WebCamDevice device = devices[i];
                webcams.Add(new(device.name));
                if (i == 0)
                {
                    activeIndex = i;
                    Debug.Log($"[WebcamManager] Active webcam initialized: {device.name}\n");
                }
            }
        }

        /// <summary>
        /// Indicates that a component or action intends to use the active webcam.
        ///
        /// This method increments the internal <see cref="usageCount"/> and ensures the webcam is running:
        /// - If this is the first user (i.e., usageCount transitions from 0 -> 1),
        ///   the webcam is automatically started via <see cref="WebCamTexture.Play"/>.
        /// - If the webcam is already active, the method simply increases the counter without restarting it.
        ///
        /// Always call this method before accessing the webcam to guarantee that
        /// the <see cref="WebCamTexture"/> is initialized and streaming frames.
        /// </summary>
        public static void Acquire()
        {
            if (webcams.Count == 0)
            {
                Initialize();
            }

            if (webcams.Count == 0)
            {
                return;
            }

            usageCount++;

            if (!webcams[activeIndex].isPlaying)
            {
                webcams[activeIndex].Play();
                SettingsMenu.ActivateWebcam();
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
                SettingsMenu.DeactivateWebcam();
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
                if (webcams[activeIndex].isPlaying)
                {
                    StopWebcamAsync(activeIndex).Forget();
                    ShowNotification.Info("Video Systems Disabled",
                        "All video-based systems have been temporarily disabled due to a webcam change. " +
                        "If you still need them, please restart the corresponding systems.");
                }
                activeIndex = index;
                OnActiveWebcamChanged?.Invoke(webcams[activeIndex]);

                // Saves the selected camera.
                PlayerPrefs.SetString("selectedCamera", webcams[activeIndex].deviceName);
            }

            static async UniTask StopWebcamAsync(int index)
            {
                await UniTask.Yield();
                webcams[index].Stop();
                usageCount = 0;
            }
        }
    }
}
