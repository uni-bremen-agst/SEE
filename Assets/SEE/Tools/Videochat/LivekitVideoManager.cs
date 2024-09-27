// Code inspired by https://github.com/livekit-examples/unity-example/blob/main/LivekitUnitySampleApp/Assets/LivekitSamples.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LiveKit;
using LiveKit.Proto;
using RoomOptions = LiveKit.RoomOptions;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;
using SEE.Controls;

/// <summary>
/// Manages LiveKit video streams and plays them via the LivekitVideo object in the player objects.
/// Handles publishing/unpublishing local video, subscribing/unsubscribing to remote video,
/// and switching between available camera devices.
/// </summary>
public class LivekitVideoManager : NetworkBehaviour
{
    /// <summary>
    /// The URL of the LiveKit server to connect to.
    /// </summary>
    public string livekitUrl = "ws://localhost:7880";

    /// <summary>
    /// The URL used to fetch the access token required for authentication.
    /// </summary>
    public string tokenUrl = "http://localhost:3000";

    /// <summary>
    /// The room name to join in LiveKit.
    /// </summary>
    public string roomName = "development";

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
    /// The index of the currently selected camera.
    /// </summary>
    private int currentCameraIndex = 0;

    /// <summary>
    /// An array containing the available webcam devices.
    /// </summary>
    private WebCamDevice[] devices;

    /// <summary>
    /// The dropdown UI component used to select between different available cameras.
    /// </summary>
    public TMP_Dropdown cameraDropdown;

    /// <summary>
    /// A dictionary that maps participant identities to the GameObjects that represent their video streams.
    /// </summary>
    private Dictionary<string, GameObject> _videoObjects = new();

    /// <summary>
    /// A list of video sources created from the local webcam that are currently being published to the room.
    /// </summary>
    private List<RtcVideoSource> _rtcVideoSources = new();

    /// <summary>
    /// A list of video streams from remote participants in the LiveKit room.
    /// </summary>
    private List<VideoStream> _videoStreams = new();

    /// <summary>
    /// Initializes the video manager by obtaining a token and setting up the camera dropdown.
    /// </summary>
    private void Start()
    {
        SetupCameraDropdown();
        StartCoroutine(GetToken());
    }

    /// <summary>
    /// Cleans up resources when the object is destroyed, including stopping the webcam
    /// and disconnecting from the LiveKit room.
    /// </summary>
    private void OnDestroy()
    {
        if (webCamTexture != null)
        {
            webCamTexture.Stop();
        }
        room.Disconnect();
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
                StartCoroutine(PublishVideo());
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

    // Camera Methods
    /// <summary>
    /// Initializes the camera dropdown menu with the list of available camera devices.
    /// This method populates the dropdown with the names of all connected camera devices
    /// and sets up a listener to handle camera selection changes.
    /// </summary>
    private void SetupCameraDropdown()
    {
        // Retrieve the list of available camera devices
        devices = WebCamTexture.devices;

        if (devices.Length > 0)
        {
            List<string> cameraNames = new List<string>();

            // Iterate through each device and add its name to the list
            foreach (var device in devices)
            {
                cameraNames.Add(string.IsNullOrEmpty(device.name) ? "Unnamed Camera" : device.name);
            }

            cameraDropdown.ClearOptions();
            cameraDropdown.AddOptions(cameraNames);
            cameraDropdown.onValueChanged.AddListener(OpenSelectedCamera);

            // Open the first camera by default
            OpenSelectedCamera(0);
        }
        else
        {
            Debug.LogError("[Livekit] No camera devices available");
        }
    }

    /// <summary>
    /// Opens and starts the selected camera device based on the provided index.
    /// This method stops any currently active camera, initializes a new WebCamTexture with the selected
    /// camera device, and starts capturing video from it. It also republishes the video stream.
    /// </summary>
    /// <param name="index">The index of the selected camera device in the dropdown list.</param>
    private void OpenSelectedCamera(int index)
    {
        if (index >= 0 && index < devices.Length)
        {
            webCamTexture?.Stop();

            string selectedDeviceName = devices[index].name;

            // Initialize a new WebCamTexture with the selected camera device
            webCamTexture = new WebCamTexture(selectedDeviceName);
            webCamTexture.Play();

            StartCoroutine(RepublishVideo());
            Debug.Log($"[Livekit] Switched to camera: {selectedDeviceName}");
        }
    }

    // Connection Methods
    /// <summary>
    /// Fetches an authentication token from the token server and connects to the LiveKit room.
    /// Makes a GET request to the token server with the room name and participant name.
    /// The name of the participant is the local client ID.
    /// </summary>
    /// <returns>Coroutine to handle the asynchronous token request process.</returns>
    private IEnumerator GetToken()
    {
        // Send a GET request to the token server to retrieve the token for this client
        using (UnityEngine.Networking.UnityWebRequest www = UnityEngine.Networking.UnityWebRequest.Get(
            $"{tokenUrl}/getToken?roomName={roomName}&participantName={NetworkManager.Singleton.LocalClientId.ToString()}"))
        {
            // Wait for the request to complete.
            yield return www.SendWebRequest();

            // Check if the request was successful.
            if (www.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                // Token received, proceed to join the room using the received token.
                StartCoroutine(JoinRoom(www.downloadHandler.text));
            }
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
        room = new Room();

        // Subscribe to events related to track management.
        room.TrackSubscribed += TrackSubscribed;
        room.TrackUnsubscribed += UnTrackSubscribed;

        var options = new RoomOptions();

        // Attempt to connect to the room using the LiveKit server URL and the provided token.
        var connect = room.Connect(livekitUrl, token, options);
        yield return connect;

        // Check if the connection was successful.
        if (!connect.IsError)
        {
            Debug.Log("[Livekit] Connected to " + room.Name);
        }
    }

    // Publish Methods
    /// <summary>
    /// Publishes the local video track to the LiveKit room.
    /// Creates a video track from the webcamtexture and publishes it to the room.
    /// Updates the mesh object for the local client with the video.
    /// The Mesh object is provided by LivekitVideo.prefab,
    /// which is instantiated as a immediate child of the Player object.
    /// </summary>
    /// <returns>Coroutine to handle the asynchronous publishing process.</returns>
    private IEnumerator PublishVideo()
    {
        // Check if the room is initialized.
        if (room != null)
        {
            // Create a video source from the current webcam texture.
            var source = new TextureVideoSource(webCamTexture);

            // Create a local video track with the video source.
            var track = LocalVideoTrack.CreateVideoTrack("my-video-track", source, room);

            // Define options for publishing the video track.
            var options = new TrackPublishOptions
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
            var publish = room.LocalParticipant.PublishTrack(track, options);
            yield return publish;

            // Check if the publishing was successful.
            if (!publish.IsError)
            {
                Debug.Log("[Livekit] Video track published!");
                publishedTrack = track;

                // Find and update the mesh object for the local client with the video.
                var localClientId = NetworkManager.Singleton.LocalClientId.ToString();
                var meshObject = GameObject.Find("LivekitVideo_" + localClientId);

                if (meshObject != null)
                {
                    MeshRenderer renderer = meshObject.GetComponent<MeshRenderer>();
                    if (renderer != null)
                    {
                        // Enable the renderer and set the texture.
                        renderer.material.mainTexture = webCamTexture;
                        renderer.enabled = true;
                    }
                }

                // Store the mesh object in the dictionary.
                _videoObjects[localClientId] = meshObject;
            }

            // Start capturing and updating the video source.
            source.Start();
            StartCoroutine(source.Update());
            _rtcVideoSources.Add(source);
        }
    }

    /// <summary>
    /// Unpublishes the local video track from the LiveKit room.
    /// Stops the video, disables the mesh renderer, and removes the video track.
    /// </summary>
    /// <returns>Coroutine to handle the asynchronous unpublishing process.</returns>
    private IEnumerator UnpublishVideo()
    {
        // Check if the room is initialized.
        if (room != null)
        {
            // Unpublish the video track from the room.
            var unpublish = room.LocalParticipant.UnpublishTrack(publishedTrack, true);
            yield return unpublish;

            // Check if the unpublishing was successful.
            if (!unpublish.IsError)
            {
                Debug.Log("[Livekit] Video track unpublished.");
                publishedTrack = null;

                // Find and update the mesh object for the local client.
                var localClientId = NetworkManager.Singleton.LocalClientId.ToString();
                if (_videoObjects.TryGetValue(localClientId, out GameObject meshObject) && meshObject != null)
                {
                    MeshRenderer renderer = meshObject.GetComponent<MeshRenderer>();
                    if (renderer != null)
                    {
                        // Disable the renderer and clear the texture.
                        renderer.enabled = false;
                        renderer.material.mainTexture = null;
                    }

                    // Remove the mesh object from the dictionary.
                    _videoObjects.Remove(localClientId);
                }
            }
        }
    }

    /// <summary>
    /// Republishes the video track, typically used after switching the camera.
    /// </summary>
    /// <returns>Coroutine that handles the republishing of the video.</returns>
    private IEnumerator RepublishVideo()
    {
        if (publishedTrack != null)
        {
            yield return StartCoroutine(UnpublishVideo());
            StartCoroutine(PublishVideo());
        }
    }

    // Track Methods
    /// <summary>
    /// Callback method that is invoked when a remote track is subscribed.
    /// Handles the display of the remote video stream on a mesh object.
    /// The Mesh object is provided by LivekitVideo.prefab,
    /// which is instantiated as a immediate child of the Player object.
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
            var meshObject = GameObject.Find("LivekitVideo_" + participant.Identity);
            if (meshObject != null)
            {
                MeshRenderer renderer = meshObject.GetComponent<MeshRenderer>();

                // Create a new VideoStream instance for the subscribed track.
                var stream = new VideoStream(videoTrack);
                stream.TextureReceived += texture =>
                {
                    if (renderer != null)
                    {
                        // Enable the renderer and set the texture.
                        renderer.material.mainTexture = texture;
                        renderer.enabled = true;
                    }
                };

                _videoObjects[participant.Identity] = meshObject; // Add the VideoStream to the list of video streams.
                stream.Start(); // Start the video stream.
                StartCoroutine(stream.Update()); // Continuously update the video stream.
                _videoStreams.Add(stream); // Add the stream to the list of active video streams.
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
            if (_videoObjects.TryGetValue(participant.Identity, out GameObject meshObject) && meshObject != null)
            {
                MeshRenderer renderer = meshObject.GetComponent<MeshRenderer>();
                if (renderer != null)
                {
                    // Disable the renderer and clear the texture.
                    renderer.enabled = false;
                    renderer.material.mainTexture = null;
                }
            }

            // Remove the stream from the list of active video streams.
            _videoObjects.Remove(participant.Identity);
        }
    }

    // Cleanup Methods
    /// <summary>
    /// Cleans up all video-related objects and stops all video streams and RTC sources.
    /// </summary>
    private void CleanUp()
    {
        foreach (var item in _videoObjects)
        {
            foreach (var rtcVideoSource in _rtcVideoSources)
        {
            rtcVideoSource.Stop();
        }

        foreach (var videoStream in _videoStreams)
        {
            videoStream.Stop();
        }

        _rtcVideoSources.Clear();
        _videoStreams.Clear();
        }
    }
}
