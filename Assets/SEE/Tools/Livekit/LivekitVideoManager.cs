// Code inspired by https://github.com/livekit-examples/unity-example/blob/main/LivekitUnitySampleApp/Assets/LivekitSamples.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LiveKit;
using LiveKit.Proto;
using RoomOptions = LiveKit.RoomOptions;
using UnityEngine.UI;
using Unity.Netcode;
using SEE.Controls;
using SEE.GO;
using SEE.UI.Notification;
using SEE.Utils;
using SEE.UI;

namespace SEE.Tools.Livekit
{
    /// <summary>
    /// Manages LiveKit video streams and plays them via the LivekitVideo object in the player objects.
    /// Handles publishing/unpublishing local video, subscribing/unsubscribing to remote video,
    /// and switching between available camera devices.
    /// </summary>
    /// <remarks>This component is attached to UI Canvas/SettingsMenu/LivekitVideoManager.
    /// See the prefabs SettingsMenu.prefab.</remarks>
    public class LivekitVideoManager : MonoBehaviour
    {
        /// <summary>
        /// The URL of the LiveKit server to connect to. This is a websocket URL.
        /// </summary>
        [Tooltip("The URL of the LiveKit server to connect to. A websocket URL.")]
        public string LivekitUrl = "ws://localhost:7880";

        /// <summary>
        /// The URL used to fetch the access token required for authentication.
        /// </summary>
        [Tooltip("The URL used to fetch the access token required for authentication.")]
        public string TokenUrl = "http://localhost:3000";

        /// <summary>
        /// The room name to join in LiveKit.
        /// </summary>
        [Tooltip("The room name to join in LiveKit.")]
        public string RoomName = "development";

        /// <summary>
        /// The LiveKit room object that manages connection and tracks.
        /// </summary>
        private Room room = null;

        /// <summary>
        /// The local video track being published to the LiveKit server.
        /// </summary>
        private LocalVideoTrack publishedTrack = null;

        /// <summary>
        /// The WebCamTexture used to capture the video stream from the selected camera.
        /// </summary>
        private WebCamTexture webCamTexture = null;

        /// <summary>
        /// A dictionary that maps participant identities to the GameObjects that represent their video streams.
        /// </summary>
        private readonly Dictionary<string, GameObject> videoObjects = new();

        /// <summary>
        /// A list of video sources created from the local webcam that are currently being published to the room.
        /// </summary>
        private readonly List<RtcVideoSource> rtcVideoSources = new();

        /// <summary>
        /// A list of video streams from remote participants in the LiveKit room.
        /// </summary>
        private readonly List<VideoStream> videoStreams = new();

        /// <summary>
        /// If this code is executed in an environment different from <see cref="PlayerInputType.DesktopPlayer"/>,
        /// the object will be disabled.
        /// </summary>
        private void Start()
        {
            if (SceneSettings.InputType != PlayerInputType.DesktopPlayer)
            {
                gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Cleans up resources when the object is destroyed, including stopping the webcam
        /// and disconnecting from the LiveKit room.
        /// </summary>
        private void OnDestroy()
        {
            room?.Disconnect();
            CleanUp();
            room = null;
        }

        /// <summary>
        /// Toggles video publishing on and off.
        /// It can be toggled with <see cref="SEEInput.ToggleFaceCam"/>.
        /// </summary>
        private void Update()
        {
            if (SEEInput.ToggleFaceCam())
            {
                if (publishedTrack == null)
                {
                    if (room == null || !room.IsConnected)
                    {
                        StartCoroutine(ConnectAndPublish());
                    }
                    else
                    {
                        StartCoroutine(PublishVideo());
                    }
                }
                else
                {
                    StartCoroutine(UnpublishVideo());
                }
            }
        }

        /// <summary>
        /// Pauses or resumes the webcam video when the application is paused or resumed.
        /// </summary>
        /// <param name="pause">Whether the application is paused.</param>
        private void OnApplicationPause(bool pause)
        {
            if (webCamTexture != null)
            {
                if (pause)
                {
                    webCamTexture.Pause();
                }
                else
                {
                    webCamTexture.Play();
                }
            }
        }

        #region Camera Methods
        /// <summary>
        /// Subscribes to the <see cref="WebcamManager.OnActiveWebcamChanged"/> event.
        /// This ensures that the component reacts whenever the active webcam changes.
        /// Additionally, if a webcam is already active when this component is enabled,
        /// <see cref="HandleWebcamChanged"/> is called immediately to synchronize state.
        /// </summary>
        private void OnEnable()
        {
            WebcamManager.OnActiveWebcamChanged += HandleWebcamChanged;
            // Request current state once when enabling
            if (WebcamManager.WebCamTexture != null)
            {
                HandleWebcamChanged(WebcamManager.WebCamTexture);
            }
        }

        /// <summary>
        /// Unsubscribes from the <see cref="WebcamManager.OnActiveWebcamChanged"/> event
        /// to prevent memory leaks or invalid callbacks when the component is disabled.
        /// </summary>
        private void OnDisable()
        {
            WebcamManager.OnActiveWebcamChanged -= HandleWebcamChanged;
        }

        /// <summary>
        /// Handles updates when the active webcam changes.
        /// Updates the currently published video stream, saves the selected camera,
        /// and switches the <see cref="WebCamTexture"/> used by this component.
        /// </summary>
        /// <param name="newWebcam">
        /// The newly active <see cref="WebCamTexture"/> provided by the <see cref="WebcamManager"/>.
        /// Can be <c>null</c> if no webcam is available.
        /// </param>
        private void HandleWebcamChanged(WebCamTexture newWebcam)
        {
            if (newWebcam == null || webCamTexture == newWebcam)
            {
                return;
            }

            if (publishedTrack != null)
            {
                StartCoroutine(UnpublishVideo());
            }

            // Initialize a new WebCamTexture with the selected camera device.
            webCamTexture = newWebcam;
        }
        #endregion

        #region Connection Methods
        /// <summary>
        /// Fetches an authentication token from the token server and connects to the LiveKit room.
        /// Makes a GET request to the token server with the room name and participant name.
        /// The name of the participant is the local client ID.
        /// </summary>
        /// <returns>Coroutine to handle the asynchronous token request process.</returns>
        private IEnumerator GetToken()
        {
            // Send a GET request to the token server to retrieve the token for this client.
            string uri = $"{TokenUrl}/getToken?roomName={RoomName}&participantName={NetworkManager.Singleton.LocalClientId}";
            using UnityEngine.Networking.UnityWebRequest www = UnityEngine.Networking.UnityWebRequest.Get(uri);
            // Wait for the request to complete.
            yield return www.SendWebRequest();

            // Check if the request was successful.
            if (www.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                // Token received, proceed to join the room using the received token.
                yield return StartCoroutine(JoinRoom(www.downloadHandler.text));
            }
            else
            {
                ShowNotification.Error("Livekit", $"Failed to get token from {uri}: {www.error}.");
            }
        }

        /// <summary>
        /// Connects to the LiveKit room using the previously fetched token.
        /// Initializes the room, subscribes to events, and connects with provided room options.
        /// </summary>
        /// <param name="token">The authentication token received from the token server.</param>
        /// <returns>Coroutine that handles the connection to the room.</returns>
        private IEnumerator JoinRoom(string token)
        {
            // Initialize a new room instance.
            room = new();

            // Subscribe to events related to track management.
            room.TrackSubscribed += TrackSubscribed;
            room.TrackUnsubscribed += UnTrackSubscribed;

            RoomOptions options = new();

            // Attempt to connect to the room using the LiveKit server URL and the provided token.
            Debug.Log($"[Livekit] Connecting to room: \"{room.Name}\" at URL {LivekitUrl}...\n");
            ConnectInstruction connect = room.Connect(LivekitUrl, token, options);
            yield return connect;

            // Check if the connection was successful.
            if (connect.IsError)
            {
                ShowNotification.Error("Livekit", $"Failed to connect to room: \"{room.Name}\" {connect}.");
            }
            else
            {
                Debug.Log($"[Livekit] Connected to \"{room.Name}\" \n");
            }
        }
        #endregion

        #region Publish Methods
        /// <summary>
        /// Publishes the local video track to the LiveKit room.
        /// Creates a video track from the webcamtexture and publishes it to the room.
        /// Updates the mesh object for the local client with the video.
        /// The Mesh object is provided by LivekitVideo.prefab,
        /// which is instantiated as an immediate child of the Player object.
        /// </summary>
        /// <returns>Coroutine to handle the asynchronous publishing process.</returns>
        private IEnumerator PublishVideo()
        {
            if (room == null || !room.IsConnected)
            {
                ShowNotification.Error("Livekit", "Not connected.");
                yield break;
            }
            // Acquire camera device.
            WebcamManager.Acquire();

            // Create a video source from the current webcam texture.
            WebCameraSource source = new(webCamTexture);

            // Create a local video track with the video source.
            LocalVideoTrack track = LocalVideoTrack.CreateVideoTrack("my-video-track", source, room);

            // Define options for publishing the video track.
            TrackPublishOptions options = new()
            {
                VideoCodec = VideoCodec.H264, // Codec to be used for video.
                VideoEncoding = new VideoEncoding
                {
                    MaxBitrate = 512000, // Maximum bitrate in bits per second.
                                         // Higher values improve the quality, but require more bandwidth.
                    MaxFramerate = 30 // Maximum frames per second.
                },
                Simulcast = true, // Enable simulcast for better scalability.
                                  // Allows participants different quality levels, but increases the server load.
                Source = TrackSource.SourceCamera // Specify the source as the camera.
            };

            // Publish the video track to the room.
            UnityEngine.Assertions.Assert.IsNotNull(room.LocalParticipant, "Local participant is null");
            PublishTrackInstruction publish = room.LocalParticipant.PublishTrack(track, options);
            yield return publish;

            // Check if the publishing was successful.
            if (!publish.IsError)
            {
                publishedTrack = track;

                // Find and update the mesh object for the local client with the video.
                string localClientId = NetworkManager.Singleton.LocalClientId.ToString();
                GameObject meshObject = GameObject.Find("LivekitVideo_" + localClientId);

                if (meshObject != null && meshObject.TryGetComponent(out MeshRenderer renderer))
                {
                    // Enable the renderer and set the texture.
                    renderer.material.mainTexture = webCamTexture;
                    renderer.enabled = true;
                }

                // Store the mesh object in the dictionary.
                videoObjects[localClientId] = meshObject;

                // Start capturing and updating the video source.
                source.Start();
                StartCoroutine(source.Update());
                rtcVideoSources.Add(source);

                UIOverlay.ActivateLivekit();
            }
        }

        /// <summary>
        /// Establishes a connection to the LiveKit room if not already connected
        /// and publishes the local video track.
        /// </summary>
        /// <returns>
        /// A coroutine that yields while the connection and publishing process is ongoing.
        /// </returns>
        private IEnumerator ConnectAndPublish()
        {
            if (room == null || !room.IsConnected)
            {
                yield return StartCoroutine(GetToken());
                // wait one frame to allow room state update.
                yield return null;
            }

            if (room != null && room.IsConnected)
            {
                yield return StartCoroutine(PublishVideo());
            }
        }

        /// <summary>
        /// Unpublishes the local video track from the LiveKit room.
        /// Stops the video, disables the mesh renderer, and removes the video track.
        /// </summary>
        /// <returns>Coroutine to handle the asynchronous unpublishing process.</returns>
        private IEnumerator UnpublishVideo()
        {
            foreach (var source in rtcVideoSources)
            {
                source.Stop();
            }
            rtcVideoSources.Clear();
            yield return null;
            // Release camera device.
            if (webCamTexture.isPlaying)
            {
                WebcamManager.Release();
            }

            // Unpublish the video track from the room.
            UnpublishTrackInstruction unpublish = room.LocalParticipant.UnpublishTrack(publishedTrack, true);
            yield return unpublish;

            // Check if the unpublishing was successful.
            if (!unpublish.IsError)
            {
                publishedTrack = null;

                // Find and update the mesh object for the local client.
                string localClientId = NetworkManager.Singleton.LocalClientId.ToString();
                if (videoObjects.TryGetValue(localClientId, out GameObject meshObject) && meshObject != null)
                {
                    if (meshObject.TryGetComponent(out MeshRenderer renderer))
                    {
                        // Disable the renderer and clear the texture.
                        renderer.enabled = false;
                        renderer.material.mainTexture = null;
                    }

                    // Remove the mesh object from the dictionary.
                    videoObjects.Remove(localClientId);

                    UIOverlay.DeactivateLivekit();
                }
            }
        }
        #endregion

        #region Track Methods
        /// <summary>
        /// Callback method that is invoked when a remote track is subscribed.
        /// Handles the display of the remote video stream on a mesh object.
        /// The Mesh object is provided by LivekitVideo.prefab,
        /// which is instantiated as an immediate child of the Player object.
        /// </summary>
        /// <param name="track">The remote track being subscribed to.</param>
        /// <param name="publication">The publication details of the track.</param>
        /// <param name="participant">The remote participant owning the track.</param>
        private void TrackSubscribed(IRemoteTrack track, RemoteTrackPublication publication, RemoteParticipant participant)
        {
            if (track is RemoteVideoTrack videoTrack)
            {
                Debug.Log("[Livekit] TrackSubscribed for " + participant.Identity);

                // Find the LivekitVideo object to display the video stream.
                GameObject meshObject = GameObject.Find("LivekitVideo_" + participant.Identity);
                if (meshObject != null)
                {
                    // Create a new VideoStream instance for the subscribed track.
                    VideoStream stream = new(videoTrack);
                    stream.TextureReceived += texture =>
                    {
                        if (meshObject.TryGetComponent(out MeshRenderer renderer))
                        {
                            // Enable the renderer and set the texture.
                            renderer.material.mainTexture = texture;
                            renderer.enabled = true;
                        }
                    };

                    videoObjects[participant.Identity] = meshObject; // Add the VideoStream to the list of video streams.
                    stream.Start(); // Start the video stream.
                    StartCoroutine(stream.Update()); // Continuously update the video stream.
                    videoStreams.Add(stream); // Add the stream to the list of active video streams.
                }
            }
        }

        /// <summary>
        /// Callback method that is invoked when a remote track is unsubscribed.
        /// Cleans up the mesh object associated with the remote video stream.
        /// </summary>
        /// <param name="track">The remote track being unsubscribed.</param>
        /// <param name="publication">The publication details of the track.</param>
        /// <param name="participant">The remote participant owning the track.</param>
        private void UnTrackSubscribed(IRemoteTrack track, RemoteTrackPublication publication, RemoteParticipant participant)
        {
            if (track is RemoteVideoTrack videoTrack)
            {
                if (videoObjects.TryGetValue(participant.Identity, out GameObject meshObject)
                    && meshObject != null
                    && meshObject.TryGetComponent(out MeshRenderer renderer))
                {
                    // Disable the renderer and clear the texture.
                    renderer.enabled = false;
                    renderer.material.mainTexture = null;
                }

                // Remove the stream from the list of active video streams.
                videoObjects.Remove(participant.Identity);
            }
        }
        #endregion

        #region Cleanup Methods
        /// <summary>
        /// Cleans up all video-related objects and stops all video streams and RTC sources.
        /// </summary>
        private void CleanUp()
        {
            foreach (KeyValuePair<string, GameObject> videoObject in videoObjects)
            {
                foreach (RtcVideoSource rtcVideoSource in rtcVideoSources)
                {
                    rtcVideoSource.Stop();
                }

                foreach (VideoStream videoStream in videoStreams)
                {
                    videoStream.Stop();
                }

                rtcVideoSources.Clear();
                videoStreams.Clear();
            }
        }
    }
}
#endregion
