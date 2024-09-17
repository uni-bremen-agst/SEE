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

public class LivekitVideoManager : NetworkBehaviour
{
    public string livekitUrl = "ws://localhost:7880";
    public string tokenUrl = "http://localhost:3000";
    public string roomName = "development";
    private string token = "";

    private Room room = null;
    private LocalVideoTrack publishedTrack = null;

    private WebCamTexture webCamTexture = null;
    private int frameRate = 30;
    private int currentCameraIndex = 0;
    private WebCamDevice[] devices;
    public TMP_Dropdown cameraDropdown;

    Dictionary<string, GameObject> _videoObjects = new();
    List<RtcVideoSource> _rtcVideoSources = new();
    List<VideoStream> _videoStreams = new();

    void Start()
    {
        StartCoroutine(GetToken());
        SetupCameraDropdown();
    }

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

    // Token & Join Methods
    IEnumerator GetToken()
    {
        using (UnityEngine.Networking.UnityWebRequest www = UnityEngine.Networking.UnityWebRequest.Get($"{tokenUrl}/getToken?roomName={roomName}&participantName={NetworkManager.Singleton.LocalClientId.ToString()}"))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                token = www.downloadHandler.text;
                Debug.Log("[Livekit] Token received: " + token);
                StartCoroutine(JoinRoom());
            }
        }
    }

    IEnumerator JoinRoom()
    {
        if (room == null)
        {
            room = new Room();
            room.TrackSubscribed += TrackSubscribed;
            room.TrackUnsubscribed += UnTrackSubscribed;

            var options = new RoomOptions();
            var connect = room.Connect(livekitUrl, token, options);
            yield return connect;

            if (!connect.IsError)
            {
                Debug.Log("[Livekit] Connected to " + room.Name);
            }
        }
    }

    // Camera Methods
    void SetupCameraDropdown()
    {
        devices = WebCamTexture.devices;

        if (devices.Length > 0)
        {
            List<string> cameraNames = new List<string>();
            foreach (var device in devices)
            {
                cameraNames.Add(string.IsNullOrEmpty(device.name) ? "Unnamed Camera" : device.name);
            }

            cameraDropdown.ClearOptions();
            cameraDropdown.AddOptions(cameraNames);
            cameraDropdown.onValueChanged.AddListener(OpenSelectedCamera);

            OpenSelectedCamera(0);
        }
        else
        {
            Debug.LogError("[Livekit] No camera devices available");
        }
    }

    void OpenSelectedCamera(int index)
    {
        if (index >= 0 && index < devices.Length)
        {
            webCamTexture?.Stop();

            string selectedDeviceName = devices[index].name;
            webCamTexture = new WebCamTexture(selectedDeviceName, Screen.width, Screen.height, frameRate)
            {
                wrapMode = TextureWrapMode.Repeat
            };
            webCamTexture.Play();

            StartCoroutine(RepublishVideo());
            Debug.Log($"[Livekit] Switched to camera: {selectedDeviceName}");
        }
    }

    // Publish Methods
    IEnumerator PublishVideo()
    {
        if (room != null)
        {
            var source = new TextureVideoSource(webCamTexture);
            var track = LocalVideoTrack.CreateVideoTrack("my-video-track", source, room);

            var options = new TrackPublishOptions
            {
                VideoCodec = VideoCodec.H264,
                VideoEncoding = new VideoEncoding { MaxBitrate = 512000, MaxFramerate = frameRate },
                Simulcast = true,
                Source = TrackSource.SourceCamera
            };

            var publish = room.LocalParticipant.PublishTrack(track, options);
            yield return publish;

            if (!publish.IsError)
            {
                Debug.Log("[Livekit] Video track published!");
                publishedTrack = track;

                var localClientId = NetworkManager.Singleton.LocalClientId.ToString();
                var meshObject = GameObject.Find("LivekitVideo_" + localClientId);

                if (meshObject != null)
                {
                    MeshRenderer renderer = meshObject.GetComponent<MeshRenderer>();
                    if (renderer != null)
                    {
                        renderer.material.mainTexture = webCamTexture;
                        renderer.enabled = true;
                    }
                }

                _videoObjects[localClientId] = meshObject;
            }

            source.Start();
            StartCoroutine(source.Update());
            _rtcVideoSources.Add(source);
        }
    }

    IEnumerator UnpublishVideo()
    {
        if (room != null)
        {
            var unpublish = room.LocalParticipant.UnpublishTrack(publishedTrack, true);
            yield return unpublish;

            if (!unpublish.IsError)
            {
                Debug.Log("[Livekit] Video track unpublished.");
                publishedTrack = null;

                var localClientId = NetworkManager.Singleton.LocalClientId.ToString();
                if (_videoObjects.TryGetValue(localClientId, out GameObject meshObject) && meshObject != null)
                {
                    MeshRenderer renderer = meshObject.GetComponent<MeshRenderer>();
                    if (renderer != null)
                    {
                        renderer.enabled = false;
                        renderer.material.mainTexture = null; // Textur entfernen
                    }

                    _videoObjects.Remove(localClientId);
                }
            }
        }
    }

    IEnumerator RepublishVideo()
    {
        if (publishedTrack != null)
        {
            yield return StartCoroutine(UnpublishVideo());
            StartCoroutine(PublishVideo());
        }
    }

    // Track Methods
    void TrackSubscribed(IRemoteTrack track, RemoteTrackPublication publication, RemoteParticipant participant)
    {
        if (track is RemoteVideoTrack videoTrack)
        {
            Debug.Log("[Livekit] TrackSubscribed for " + participant.Identity);

            var meshObject = GameObject.Find("LivekitVideo_" + participant.Identity);
            if (meshObject != null)
            {
                MeshRenderer renderer = meshObject.GetComponent<MeshRenderer>();
                var stream = new VideoStream(videoTrack);
                stream.TextureReceived += tex =>
                {
                    if (renderer != null)
                    {
                        renderer.material.mainTexture = tex;
                        renderer.enabled = true;
                    }
                };

                _videoObjects[participant.Identity] = meshObject;
                stream.Start();
                StartCoroutine(stream.Update());
                _videoStreams.Add(stream);
            }
        }
    }


    void UnTrackSubscribed(IRemoteTrack track, RemoteTrackPublication publication, RemoteParticipant participant)
    {
        if (track is RemoteVideoTrack videoTrack)
        {
            if (_videoObjects.TryGetValue(participant.Identity, out GameObject meshObject) && meshObject != null)
            {
                MeshRenderer renderer = meshObject.GetComponent<MeshRenderer>();
                if (renderer != null)
                {
                    renderer.enabled = false;
                    renderer.material.mainTexture = null;
                }
            }
            _videoObjects.Remove(participant.Identity);
        }
    }

    // Cleanup Methods
    void CleanUp()
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