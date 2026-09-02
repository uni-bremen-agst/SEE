using System;
using System.Collections;
using Mediapipe;
using Mediapipe.Tasks.Vision.Core;
using Mediapipe.Tasks.Vision.FaceLandmarker;
using Mediapipe.Unity.Experimental;
using UnityEngine;
using Stopwatch = System.Diagnostics.Stopwatch;

using SEE.Utils;

// Namespace documentation is provided in EchoFace.cs.
namespace SEE.Tools.EchoFace
{
    /// <summary>
    /// Drives MediaPipe's Face Landmarker task against the currently active
    /// webcam (as managed by <see cref="WebcamManager"/>) and raises
    /// <see cref="OnFaceTracked"/> once per frame with the latest available
    /// detection result.
    /// </summary>
    /// <remarks>
    /// Acquires the active webcam via <see cref="WebcamManager"/> in
    /// <see cref="OnEnable"/> and releases it in <see cref="OnDisable"/>.
    /// Re-initializes the underlying <see cref="FaceLandmarker"/> whenever
    /// the active webcam changes (see <see cref="HandleWebcamChanged"/>),
    /// since the landmarker is bound to a fixed image resolution.
    /// </remarks>
    internal class MediaPipeFaceTracker : MonoBehaviour
    {
        //-------------------------------------------------
        // Public Fields
        //-------------------------------------------------

        /// <summary>
        /// The Face Landmarker task model asset (.bytes) used to create the
        /// underlying <see cref="FaceLandmarker"/>. Must be assigned in the
        /// Inspector; initialization fails with a logged error if it is
        /// <c>null</c> or empty.
        /// </summary>
        [Header("Model Configuration")]
        [Tooltip("The face landmarker task model asset (.bytes).")]
        [SerializeField]
        private TextAsset faceLandmarkerModelAsset;

        /// <summary>
        /// The maximum number of faces the underlying <see cref="FaceLandmarker"/>
        /// should detect per frame.
        /// </summary>
        [SerializeField]
        private int numFaces = 1;

        /// <summary>
        /// Raised once per <see cref="LateUpdate"/> frame in which a face
        /// was detected, with the latest available detection result and
        /// its associated timestamp in milliseconds.
        /// </summary>
        internal event Action<FaceLandmarkerResult, long> OnFaceTracked;

        //-------------------------------------------------
        // Private Fields
        //-------------------------------------------------

        /// <summary>
        /// The currently active webcam texture, as reported by
        /// <see cref="WebcamManager"/>. <c>null</c> until a webcam becomes
        /// active.
        /// </summary>
        private WebCamTexture webCamTexture;

        /// <summary>
        /// The CPU-readable texture frame used to copy pixel data from
        /// <see cref="webCamTexture"/> into a <see cref="Mediapipe.Image"/>
        /// each frame. Re-created whenever the webcam changes; the previous
        /// instance is disposed in <see cref="ShutdownMediaPipe"/>.
        /// </summary>
        private TextureFrame textureFrame;

        /// <summary>
        /// The underlying MediaPipe Face Landmarker instance. <c>null</c>
        /// until <see cref="CoInitializeMediaPipe"/> succeeds, and reset to
        /// <c>null</c> in <see cref="ShutdownMediaPipe"/>.
        /// </summary>
        private FaceLandmarker faceLandmarker;

        /// <summary>
        /// Measures elapsed time since initialization, used to generate
        /// timestamps for <see cref="FaceLandmarker.DetectAsync"/>.
        /// </summary>
        private readonly Stopwatch stopwatch = new();

        /// <summary>
        /// Guards <see cref="latestResult"/>, <see cref="latestTimestamp"/>,
        /// and <see cref="hasNewResult"/> against concurrent access, since
        /// they are written from MediaPipe's asynchronous result callback
        /// and read from <see cref="LateUpdate"/> on the main thread.
        /// </summary>
        private readonly object resultLock = new();

        /// <summary>
        /// The most recently received detection result from MediaPipe's
        /// asynchronous callback. Only valid while holding <see cref="resultLock"/>.
        /// </summary>
        private FaceLandmarkerResult latestResult;

        /// <summary>
        /// The timestamp, in milliseconds, associated with
        /// <see cref="latestResult"/>. Only valid while holding
        /// <see cref="resultLock"/>.
        /// </summary>
        private long latestTimestamp;

        /// <summary>
        /// Whether <see cref="latestResult"/> has been updated since the
        /// last time it was published via <see cref="OnFaceTracked"/>.
        /// Only valid while holding <see cref="resultLock"/>.
        /// </summary>
        private bool hasNewResult;

        /// <summary>
        /// Whether <see cref="faceLandmarker"/> and <see cref="textureFrame"/>
        /// have been successfully initialized for the current
        /// <see cref="webCamTexture"/> and are ready for use in
        /// <see cref="LateUpdate"/>.
        /// </summary>
        private bool isInitialized;

        /// <summary>
        /// Whether this component currently holds an acquisition on
        /// <see cref="WebcamManager"/>, used to ensure
        /// <see cref="WebcamManager.Release"/> is called at most once per acquisition.
        /// </summary>
        private bool isAcquired;

        /// <summary>
        /// The currently running <see cref="CoInitializeMediaPipe"/>
        /// coroutine, or <c>null</c> if none is running. Stopped in
        /// <see cref="ShutdownMediaPipe"/> if still active.
        /// </summary>
        private Coroutine initCoroutine;

        //-------------------------------------------------
        // Unity Lifecycle Methods
        //-------------------------------------------------

        /// <summary>
        /// Unity lifecycle method. Validates that the required model asset has
        /// been assigned in the Inspector before any initialization starts.
        /// </summary>
        private void Awake()
        {
            if (faceLandmarkerModelAsset == null)
            {
                Debug.LogError("[MediaPipeFaceTracker] Face Landmarker Model Asset is NULL! Please assign the .bytes asset in the Inspector.\n");
            }
        }

        /// <summary>
        /// Unity lifecycle method. Subscribes to <see cref="WebcamManager.OnActiveWebcamChanged"/>,
        /// acquires the webcam via <see cref="WebcamManager"/>, and begins initialization
        /// if an active webcam is already present. Runs every time this component is enabled.
        /// </summary>
        private void OnEnable()
        {
            WebcamManager.OnActiveWebcamChanged += HandleWebcamChanged;

            if (!isAcquired)
            {
                WebcamManager.Acquire();
                isAcquired = true;
            }

            WebCamTexture activeCam = WebcamManager.ActiveWebcam;
            if (activeCam != null)
            {
                HandleWebcamChanged(activeCam);
            }
        }

        /// <summary>
        /// Unity lifecycle method. Unsubscribes from
        /// <see cref="WebcamManager.OnActiveWebcamChanged"/>, shuts down
        /// MediaPipe to prevent native camera driver graph locks, and releases
        /// the webcam acquisition if held.
        /// </summary>
        private void OnDisable()
        {
            WebcamManager.OnActiveWebcamChanged -= HandleWebcamChanged;

            ShutdownMediaPipe();

            if (isAcquired)
            {
                WebcamManager.Release();
                isAcquired = false;
            }
        }

        /// <summary>
        /// Unity lifecycle method. Ensures MediaPipe is shut down when the object is destroyed.
        /// </summary>
        private void OnDestroy()
        {
            ShutdownMediaPipe();
        }

        /// <summary>
        /// Unity lifecycle method. Copies the current webcam frame into a
        /// <see cref="Mediapipe.Image"/>, submits it for asynchronous face
        /// detection, and raises <see cref="OnFaceTracked"/> with the
        /// latest available result under a short synchronization lock. Does
        /// nothing until initialization has completed and the webcam is
        /// playing and has produced a new frame.
        /// </summary>
        private void LateUpdate()
        {
            if (!isInitialized || webCamTexture == null || !webCamTexture.isPlaying || !webCamTexture.didUpdateThisFrame)
            {
                return;
            }

            // Flip horizontally and vertically to match the orientation
            // expected by the Face Landmarker model.
            textureFrame.ReadTextureOnCPU(webCamTexture, flipHorizontally: true, flipVertically: true);
            using Mediapipe.Image image = textureFrame.BuildCPUImage();

            long currentTimestamp = stopwatch.ElapsedMilliseconds;
            faceLandmarker.DetectAsync(image, currentTimestamp);

            // Directly invoke the event under lock to avoid a redundant second deep copy (CloneTo).
            lock (resultLock)
            {
                if (!hasNewResult)
                {
                    return;
                }

                hasNewResult = false;

                if (latestResult.faceLandmarks != null && latestResult.faceLandmarks.Count > 0)
                {
                    OnFaceTracked?.Invoke(latestResult, latestTimestamp);
                }
            }
        }

        //-------------------------------------------------
        // Private Methods
        //-------------------------------------------------

        /// <summary>
        /// Handles a change of the active webcam by shutting down any
        /// existing MediaPipe instance and, if the new webcam is not
        /// <c>null</c> and this <see cref="GameObject"/> is active,
        /// starting re-initialization via <see cref="CoInitializeMediaPipe"/>.
        /// </summary>
        /// <param name="newWebcam">
        /// The webcam texture that is now active, or <c>null</c> if no
        /// webcam is currently active.
        /// </param>
        private void HandleWebcamChanged(WebCamTexture newWebcam)
        {
            ShutdownMediaPipe();
            webCamTexture = newWebcam;

            if (webCamTexture != null && gameObject.activeInHierarchy)
            {
                initCoroutine = StartCoroutine(CoInitializeMediaPipe());
            }
        }

        /// <summary>
        /// Waits for <see cref="webCamTexture"/> to start producing frames,
        /// then creates the underlying <see cref="FaceLandmarker"/> and
        /// <see cref="TextureFrame"/> for it.
        /// </summary>
        /// <returns>An enumerator for use with Unity's coroutine system.</returns>
        private IEnumerator CoInitializeMediaPipe()
        {
            if (faceLandmarkerModelAsset == null || faceLandmarkerModelAsset.bytes == null || faceLandmarkerModelAsset.bytes.Length == 0)
            {
                Debug.LogError("[MediaPipeFaceTracker] Valid Face Landmarker Model Asset (.bytes) is missing!\n");
                yield break;
            }

            byte[] modelBytes = faceLandmarkerModelAsset.bytes;

            const float timeout = 10f;
            float elapsed = 0f;

            while (webCamTexture != null && (!webCamTexture.isPlaying || webCamTexture.width <= 16))
            {
                elapsed += Time.deltaTime;
                if (elapsed >= timeout)
                {
                    Debug.LogError($"[MediaPipeFaceTracker] Timeout ({timeout}s) waiting for webcam to start!\n");
                    yield break;
                }
                yield return null;
            }

            if (webCamTexture == null)
            {
                yield break;
            }

            FaceLandmarkerOptions options = new(
                baseOptions: new Mediapipe.Tasks.Core.BaseOptions(
                    Mediapipe.Tasks.Core.BaseOptions.Delegate.CPU,
                    modelAssetBuffer: modelBytes
                ),
                runningMode: RunningMode.LIVE_STREAM,
                numFaces: numFaces,
                outputFaceBlendshapes: true,
                resultCallback: (FaceLandmarkerResult result, Mediapipe.Image image, long timestamp) =>
                {
                    lock (resultLock)
                    {
                        result.CloneTo(ref latestResult);
                        latestTimestamp = timestamp;
                        hasNewResult = true;
                    }
                }
            );

            try
            {
                faceLandmarker = FaceLandmarker.CreateFromOptions(options);
                textureFrame = new(webCamTexture.width, webCamTexture.height, TextureFormat.RGBA32);
                stopwatch.Restart();
                isInitialized = true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MediaPipeFaceTracker] Failed to create FaceLandmarker: {ex.Message}\n{ex.StackTrace}\n");
            }
        }

        /// <summary>
        /// Stops any running initialization coroutine, stops the stopwatch,
        /// and disposes and clears <see cref="faceLandmarker"/> and
        /// <see cref="textureFrame"/>.
        /// </summary>
        private void ShutdownMediaPipe()
        {
            isInitialized = false;

            if (initCoroutine != null)
            {
                StopCoroutine(initCoroutine);
                initCoroutine = null;
            }

            if (stopwatch.IsRunning)
            {
                stopwatch.Stop();
            }

            faceLandmarker?.Close();
            if (faceLandmarker != null)
            {
                ((IDisposable)faceLandmarker).Dispose();
                faceLandmarker = null;
            }

            // Dispose the previous texture frame to avoid leaking native resources.
            textureFrame?.Dispose();
            textureFrame = null;
        }
    }
}
