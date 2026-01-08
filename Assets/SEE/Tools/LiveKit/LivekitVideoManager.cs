// Code inspired by https://github.com/livekit-examples/unity-example/blob/main/LivekitUnitySampleApp/Assets/LivekitSamples.cs
using LiveKit;
using LiveKit.Proto;
using SEE.Controls;
using SEE.GO;
using SEE.UI;
using SEE.UI.Notification;
using SEE.User;
using SEE.Utils;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using RoomOptions = LiveKit.RoomOptions;

namespace SEE.Tools.LiveKit
{
    /// <summary>
    /// Manages LiveKit video streams and plays them via the LivekitVideo object in the player objects.
    /// Handles publishing/unpublishing local video, subscribing/unsubscribing to remote video,
    /// and switching between available camera devices.
    /// </summary>
    /// <remarks>
    /// This component is attached to the DesktopPlayer.
    ///
    /// The initial LiveKit settings (LiveKit URL, Token URL, Room Name)
    /// can be edited in the <see cref="UserSettings"/> component in the SEEStart scene,
    /// where the UserSettings component is attached to the NetworkManager.
    /// /remarks>
    public class LiveKitVideoManager : MonoBehaviour
    {
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
        /// A list of video sources created from the local webcam that are currently being published to the room.
        /// </summary>
        private readonly List<RtcVideoSource> rtcVideoSources = new();

        /// <summary>
        /// A list of video streams from remote participants in the LiveKit room.
        /// </summary>
        private readonly List<VideoStream> videoStreams = new();

        /// <summary>
        /// Gets the current connection status to the LiveKit room.
        /// </summary>
        public ConnectionStatus ConnectionState { get; private set; }

        /// <summary>
        /// Represents the connection status to the LiveKit room.
        /// </summary>
        public enum ConnectionStatus
        {
            Disconnected,
            TokenFailed,
            RoomConnectionFailed,
            Connected
        }

        /// <summary>
        /// If this code is executed in an environment different from <see cref="PlayerInputType.DesktopPlayer"/>,
        /// the object will be disabled.
        /// </summary>
        private void Start()
        {
            if (!UserSettings.IsDesktop)
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
            Disconnect();
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
            if (WebcamManager.ActiveWebcam != null)
            {
                HandleWebcamChanged(WebcamManager.ActiveWebcam);
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
        /// Can be null if no webcam is available.
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
        public IEnumerator FetchTokenAndJoinRoom()
        {
            ConnectionState = ConnectionStatus.Disconnected;
            // Send a GET request to the token server to retrieve the token for this client.
            string uri = $"{UserSettings.Instance.Video.TokenUrl}" +
                $"/getToken?roomName={UserSettings.Instance.Video.RoomName}" +
                $"&participantName={NetworkManager.Singleton.LocalClientId}";
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
                ShowNotification.Error("LiveKit", $"Failed to get token from {uri}: {www.error}.");
                ConnectionState = ConnectionStatus.TokenFailed;
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
            if (IsConnected())
            {
                Disconnect();
            }
            // Initialize a new room instance.
            room = new();

            // Subscribe to events related to track management.
            room.TrackSubscribed += TrackSubscribed;
            room.TrackUnsubscribed += UnTrackSubscribed;

            RoomOptions options = new();

            // Attempt to connect to the room using the LiveKit server URL and the provided token.;
            ConnectInstruction connect = room.Connect(UserSettings.Instance.Video.LiveKitUrl, token, options);
            float elapsed = 0f;
            float timeoutSeconds = 10f;
            while (!connect.IsDone)
            {
                if (elapsed >= timeoutSeconds)
                {
                    ShowNotification.Error("LiveKit",
                        $"Connection to room \"{UserSettings.Instance.Video.RoomName}\" timed out after {timeoutSeconds} seconds.");
                    ConnectionState = ConnectionStatus.RoomConnectionFailed;
                    room.Disconnect();
                    room = null;
                    yield break;
                }
                elapsed += Time.deltaTime;
                yield return null;
            }

            // Check if the connection was successful.
            if (connect.IsError)
            {
                ShowNotification.Error("LiveKit", $"Failed to connect to room: \"{UserSettings.Instance.Video.RoomName}\" {connect}.");
                ConnectionState = ConnectionStatus.RoomConnectionFailed;
                room.Disconnect();
                room = null;
            }
            else
            {
                Debug.Log($"[LiveKit] Connected to \"{room.Name}\" \n");
                ConnectionState = ConnectionStatus.Connected;
            }
        }

        /// <summary>
        /// Indicates whether the current LiveKit room is connected.
        /// </summary>
        /// <returns>
        /// True if the <see cref="room"/> is not null and
        /// <see cref="room.IsConnected"/> evaluates to true;
        /// otherwise false.
        /// </returns>
        private bool IsConnected() => room != null && room.IsConnected;

        /// <summary>
        /// Disconnects from the LiveKit room, stops publishing, cleans up resources,
        /// and updates the connection state.
        /// </summary>
        public void Disconnect()
        {
            ConnectionState = ConnectionStatus.Disconnected;

            if (publishedTrack != null)
            {
                StartCoroutine(UnpublishVideo());
            }

            CleanUp();

            if (room != null)
            {
                room.TrackSubscribed -= TrackSubscribed;
                room.TrackUnsubscribed -= UnTrackSubscribed;
                room.Disconnect();
                room = null;
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
                ShowNotification.Error("LiveKit", "Not connected.");
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

                // Get the LiveKitVideo instance from the registry.
                if (!LiveKitVideoRegistry.TryGet(NetworkManager.Singleton.LocalClientId, out LiveKitVideo liveKitVideo))
                {
                    Debug.LogError("LiveKitVideo object not found for local client!");
                    yield break;
                }

                if (liveKitVideo.TryGetComponent(out MeshRenderer renderer))
                {
                    // Enable the renderer and set the texture.
                    renderer.material.mainTexture = webCamTexture;
                    renderer.enabled = true;
                }

                // Start capturing and updating the video source.
                source.Start();
                StartCoroutine(source.Update());
                rtcVideoSources.Add(source);

                UIOverlay.ToggleLiveKit();
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
            using (LoadingSpinner.ShowIndeterminate("Establishing LiveKit connection..."))
            {
                if (room == null || !room.IsConnected)
                {
                    yield return StartCoroutine(FetchTokenAndJoinRoom());
                    // wait one frame to allow room state update.
                    yield return null;
                }

                if (IsConnected())
                {
                    yield return StartCoroutine(PublishVideo());
                }
            }
        }

        /// <summary>
        /// Unpublishes the local video track from the LiveKit room.
        /// Stops the video, disables the mesh renderer, and removes the video track.
        /// </summary>
        /// <returns>Coroutine to handle the asynchronous unpublishing process.</returns>
        private IEnumerator UnpublishVideo()
        {
            foreach (RtcVideoSource source in rtcVideoSources)
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

                // Get the LiveKitVideo instance from the registry.
                if (LiveKitVideoRegistry.TryGet(NetworkManager.Singleton.LocalClientId, out LiveKitVideo liveKitVideo)
                    && liveKitVideo.TryGetComponent(out MeshRenderer renderer))
                {
                    renderer.enabled = false;
                    renderer.material.mainTexture = null;
                }

                UIOverlay.ToggleLiveKit();
            }
        }
        #endregion

        #region Track Methods
        /// <summary>
        /// Callback method that is invoked when a remote track is subscribed.
        /// Handles the display of the remote video stream on a mesh object.
        /// The Mesh object is provided by <see cref="LiveKitVideo"/>,
        /// which is instantiated as an immediate child of the Player object.
        /// </summary>
        /// <param name="track">The remote track being subscribed to.</param>
        /// <param name="publication">The publication details of the track.</param>
        /// <param name="participant">The remote participant owning the track.</param>
        private void TrackSubscribed(IRemoteTrack track, RemoteTrackPublication publication, RemoteParticipant participant)
        {
            if (track is RemoteVideoTrack videoTrack)
            {
                Debug.Log("[LiveKit] TrackSubscribed for " + participant.Identity);

                // Retrieve the LiveKitVideo instance from the registry
                ulong clientId = ParseIdentity(participant);
                if (!LiveKitVideoRegistry.TryGet(clientId, out LiveKitVideo liveKitVideo))
                {
                    Debug.LogError($"No LiveKitVideo registered for participant {participant.Identity}.\n");
                    return;
                }

                // Create a new VideoStream instance for the subscribed track.
                VideoStream stream = new(videoTrack);

                // Assign texture to the LiveKitVideo's renderer when received.
                stream.TextureReceived += texture =>
                {
                    if (liveKitVideo.TryGetComponent(out MeshRenderer renderer))
                    {
                        // Enable the renderer and set the texture.
                        renderer.material.mainTexture = texture;
                        renderer.enabled = true;
                    }
                };

                stream.Start(); // Start the video stream.
                StartCoroutine(stream.Update()); // Continuously update the video stream.
                videoStreams.Add(stream); // Add the stream to the list of active video streams.
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
            if (track is RemoteVideoTrack
                && LiveKitVideoRegistry.TryGet(ParseIdentity(participant), out LiveKitVideo liveKitVideo)
                && liveKitVideo.TryGetComponent(out MeshRenderer renderer))
            {
                renderer.enabled = false;
                renderer.material.mainTexture = null;
            }
        }

        /// <summary>
        /// Converts a <see cref="RemoteParticipant"/>.Identity string to a ulong client ID.
        /// Throws an exception if the identity is not a valid ulong.
        /// </summary>
        /// <param name="participant">The LiveKit remote participant.</param>
        /// <returns>The client ID as ulong.</returns>
        /// <exception cref="System.FormatException">
        /// Thrown if the Identity cannot be parsed to ulong.
        /// </exception>
        private ulong ParseIdentity(RemoteParticipant participant)
        {
            if (ulong.TryParse(participant.Identity, out ulong clientId))
            {
                return clientId;
            }
            else
            {
                throw new System.FormatException(
                    $"Invalid RemoteParticipant.Identity: '{participant.Identity}' cannot be converted to ulong.");
            }
        }
        #endregion

        #region Cleanup Methods
        /// <summary>
        /// Cleans up all video-related objects and stops all video streams and RTC sources.
        /// </summary>
        private void CleanUp()
        {
            // Stop all local RTC video sources
            foreach (RtcVideoSource rtcVideoSource in rtcVideoSources)
            {
                rtcVideoSource.Stop();
            }
            rtcVideoSources.Clear();

            // Stop all remote video streams
            foreach (VideoStream videoStream in videoStreams)
            {
                videoStream.Stop();
            }
            videoStreams.Clear();

            // Disable all LiveKitVideo renderers (local + remote) from the registry
            foreach (LiveKitVideo liveKitVideo in LiveKitVideoRegistry.GetAll())
            {
                if (liveKitVideo != null && liveKitVideo.TryGetComponent(out MeshRenderer renderer))
                {
                    renderer.enabled = false;
                    renderer.material.mainTexture = null;
                }
            }
        }
        #endregion
    }
}
