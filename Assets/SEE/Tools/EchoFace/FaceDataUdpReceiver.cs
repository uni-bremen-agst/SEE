using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using Newtonsoft.Json;
using Unity.Netcode;

/// <summary>
/// Defines the fixed, alphabetically sorted ordering of all supported
/// blendshape names (with "_neutral" at index 0).
///
/// This order is used for compact network/UDP transmission where
/// blendshapes are sent as a flat float array.
///
/// Important: This list must match the BLENDSHAPE_ORDER used by the
/// Python sender exactly, index by index, otherwise blendshape values
/// will map to the wrong names.
/// </summary>
public static class BlendshapeOrder
{
    public static readonly string[] Names =
    {
        "_neutral",
        "browDownLeft",
        "browDownRight",
        "browInnerUp",
        "browOuterUpLeft",
        "browOuterUpRight",
        "cheekPuff",
        "cheekSquintLeft",
        "cheekSquintRight",
        "eyeBlinkLeft",
        "eyeBlinkRight",
        "eyeLookDownLeft",
        "eyeLookDownRight",
        "eyeLookInLeft",
        "eyeLookInRight",
        "eyeLookOutLeft",
        "eyeLookOutRight",
        "eyeLookUpLeft",
        "eyeLookUpRight",
        "eyeSquintLeft",
        "eyeSquintRight",
        "eyeWideLeft",
        "eyeWideRight",
        "jawForward",
        "jawLeft",
        "jawOpen",
        "jawRight",
        "mouthClose",
        "mouthDimpleLeft",
        "mouthDimpleRight",
        "mouthFrownLeft",
        "mouthFrownRight",
        "mouthFunnel",
        "mouthLeft",
        "mouthLowerDownLeft",
        "mouthLowerDownRight",
        "mouthPressLeft",
        "mouthPressRight",
        "mouthPucker",
        "mouthRight",
        "mouthRollLower",
        "mouthRollUpper",
        "mouthShrugLower",
        "mouthShrugUpper",
        "mouthSmileLeft",
        "mouthSmileRight",
        "mouthStretchLeft",
        "mouthStretchRight",
        "mouthUpperUpLeft",
        "mouthUpperUpRight",
        "noseSneerLeft",
        "noseSneerRight",
    };
}

/// <summary>
/// Compact UDP / network payload for face tracking.
/// Uses an ordered list of blendshape values ("bs") and a compact landmark
/// list ("lm") containing three [x,y,z] triplets:
///   index 0 -> Chin              (ID 152)
///   index 1 -> RightUpperEyelid  (ID 226)
///   index 2 -> LeftUpperEyelid   (ID 446)
/// plus a timestamp ("ts").
///
/// The lists are exposed as IReadOnlyList to document that this data is
/// treated as immutable by the receiver.
/// </summary>
[Serializable]
public class FaceDataUdpPayload
{
    public IReadOnlyList<float> bs;
    public IReadOnlyList<IReadOnlyList<float>> lm;
    public long ts;
}

/// <summary>
/// Receives raw face-tracking frames over UDP in a compact JSON format and
/// forwards the latest decoded result to a target <see cref="EchoFace"/>
/// component. Optionally forwards each processed frame to the server for
/// synchronization with other clients. This class acts as a lightweight
/// input adapter and performs no animation itself.
/// </summary>
public class FaceDataUdpReceiver : NetworkBehaviour
{
    [Header("Network Settings")]
    [SerializeField]
    private int port = 12345;

    [SerializeField]
    [Tooltip("If enabled, only the latest packet will be processed, discarding stale packets to reduce latency.")]
    private bool discardStalePackets = true;

    [Header("Sync Settings")]
    [Tooltip("If enabled, every locally processed FaceData frame will be sent to the server for synchronization.")]
    [SerializeField]
    private bool enableNetworkSync = true;

    [Header("Target")]
    [Tooltip("Reference to the local EchoFace component that should receive the incoming FaceData.")]
    [SerializeField]
    private EchoFace echoFace;

    private UdpClient _udpClient;
    private Thread _receiveThread;
    private bool _isRunning;

    // We queue the original JSON packets received from UDP.
    private readonly ConcurrentQueue<string> _jsonQueue = new();

    // Used to discard out-of-order or duplicate packets based on timestamp (locally).
    private long _lastTimestampMs = -1;

    private void Start()
    {
        // Never start the UDP listener on a dedicated server.
        if (!(IsClient && IsOwner))
        {
            enabled = false;
            return;
        }

        // Auto-resolve EchoFace if not assigned manually.
        if (echoFace == null)
        {
            echoFace = GetComponent<EchoFace>();

            if (echoFace == null)
            {
                Debug.LogWarning("[FaceDataUdpReceiver] EchoFace was not found on this GameObject.");
            }
        }

        StartUDPListener();
    }

    private void Update()
    {
        // Extra safety: do nothing if this is not the local owner.
        if (!(IsClient && IsOwner))
        {
            return;
        }

        // Dequeue the latest JSON packet (compact UDP payload).
        if (_jsonQueue.TryDequeue(out string json))
        {
            if (string.IsNullOrEmpty(json))
            {
                return;
            }

            // 1) Optionally synchronize this frame over Netcode first
            //    so that other clients get it as early as possible.
            bool shouldSync =
                enableNetworkSync &&   // globally enabled
                IsClient &&            // we are a client
                HasOtherClients();     // at least one other client connected

            // 1) First, forward this frame to the server so other clients receive it as early as possible
            if (shouldSync)
            {
                SubmitFaceDataServerRpc(json);
            }

            // 2) Deserialize once on the main thread, just before local use.
            FaceDataUdpPayload payload = null;
            try
            {
                payload = JsonConvert.DeserializeObject<FaceDataUdpPayload>(json);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[FaceDataUdpReceiver] Failed to deserialize FaceDataUdpPayload: {ex.Message}");
            }

            if (payload == null)
            {
                return;
            }

            // Discard outdated or identical packets based on timestamp (almost never happens, but just in case).
            if (payload.ts <= _lastTimestampMs)
            {
                return;
            }

            _lastTimestampMs = payload.ts;

            // 3) Expand into full FaceData and apply locally.
            if (echoFace != null)
            {
                var data = ConvertPayloadToFaceData(payload);
                if (data != null)
                {
                    echoFace.SetFaceData(data);
                }
            }
        }
    }

    private void OnApplicationQuit() => Shutdown();

    // Hide NetworkBehaviour.OnDestroy to plug in our shutdown logic.
    private new void OnDestroy() => Shutdown();

    /// <summary>
    /// Checks whether there is at least one other connected client
    /// besides this one. Used to decide if network sync is necessary.
    /// </summary>
    private bool HasOtherClients()
    {
        var nm = NetworkManager.Singleton;
        if (nm == null)
            return false;

        // ConnectedClientsList always includes the local client/host.
        // If the count is greater than 1, we know at least one other client is connected.
        return nm.ConnectedClientsList.Count > 1;
    }

    /// <summary>
    /// Initializes the UDP listener and spawns the receive thread.
    /// </summary>
    private void StartUDPListener()
    {
        try
        {
            _udpClient = new UdpClient(port);
            _isRunning = true;

            _receiveThread = new Thread(ReceiveLoop)
            {
                IsBackground = true,
                Name = "UDPFaceDataReceiver"
            };

            _receiveThread.Start();
            Debug.Log($"[FaceDataUdpReceiver] UDP listener started on port {port}.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[FaceDataUdpReceiver] UDP listener failed to start on port {port}: {ex.Message}");
        }
    }

    /// <summary>
    /// Background thread continuously receiving compact JSON payloads.
    /// </summary>
    private void ReceiveLoop()
    {
        var remoteEP = new IPEndPoint(IPAddress.Any, port);

        while (_isRunning)
        {
            try
            {
                byte[] data = _udpClient.Receive(ref remoteEP);
                string json = Encoding.UTF8.GetString(data);

                if (string.IsNullOrEmpty(json))
                {
                    continue;
                }

                if (discardStalePackets)
                {
                    // Clear older queued packets to keep only the latest.
                    _jsonQueue.Clear();
                }

                _jsonQueue.Enqueue(json);
            }
            catch (SocketException ex) when (ex.ErrorCode == 10004)
            {
                if (_isRunning)
                    Debug.LogWarning("[FaceDataUdpReceiver] UDP socket was interrupted (normal shutdown).");
            }
            catch (ObjectDisposedException)
            {
                if (_isRunning)
                    Debug.LogWarning("[FaceDataUdpReceiver] UDP client was disposed unexpectedly.");
            }
            catch (Exception ex)
            {
                if (_isRunning)
                    Debug.LogError($"[FaceDataUdpReceiver] UDP receive error: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Converts a compact FaceDataUdpPayload (with ordered blendshape values
    /// and a small landmark list) back into a full FaceData object used by EchoFace.
    /// </summary>
    private FaceData ConvertPayloadToFaceData(FaceDataUdpPayload payload)
    {
        if (payload == null)
            return null;

        // 1) Rebuild blendshape dictionary.
        Dictionary<string, float> blendshapeDict = null;

        if (payload.bs != null)
        {
            blendshapeDict = new Dictionary<string, float>(payload.bs.Count);
            int count = Mathf.Min(payload.bs.Count, BlendshapeOrder.Names.Length);

            for (int i = 0; i < count; i++)
            {
                blendshapeDict[BlendshapeOrder.Names[i]] = payload.bs[i];
            }
        }

        // 2) Rebuild landmarks dictionary with keys matching Landmarks constants.
        var landmarks = new Dictionary<string, FaceData.LandmarkCoordinates>();

        if (payload.lm != null)
        {
            void SetLm(int index, string key)
            {
                if (index < 0 || index >= payload.lm.Count)
                {
                    landmarks[key] = new FaceData.LandmarkCoordinates { x = 0f, y = 0f, z = 0f };
                    return;
                }

                var list = payload.lm[index];
                float x = list.Count > 0 ? list[0] : 0f;
                float y = list.Count > 1 ? list[1] : 0f;
                float z = list.Count > 2 ? list[2] : 0f;

                landmarks[key] = new FaceData.LandmarkCoordinates { x = x, y = y, z = z };
            }

            // Sorted by numeric size: 152, 226, 446.
            SetLm(0, Landmarks.Chin);              // "152"
            SetLm(1, Landmarks.RightUpperEyelid);  // "226"
            SetLm(2, Landmarks.LeftUpperEyelid);   // "446"
        }

        return new FaceData
        {
            ts = payload.ts,
            blendshapes = blendshapeDict,
            landmarks = landmarks
        };
    }

    /// <summary>
    /// Stops the UDP client and terminates the receive thread.
    /// </summary>
    private void Shutdown()
    {
        if (!_isRunning)
            return;

        _isRunning = false;
        _udpClient?.Close();

        if (_receiveThread != null && _receiveThread.IsAlive)
        {
            _receiveThread.Join(500); // Wait up to 500ms for the thread to exit.
            if (_receiveThread.IsAlive)
            {
                Debug.LogWarning("[FaceDataUdpReceiver] UDP receive thread did not terminate gracefully.");
            }
        }

        _udpClient = null;
        _receiveThread = null;
    }

    // -------------------------------------------------
    // RPCs for synchronizing FaceData
    // -------------------------------------------------

    /// <summary>
    /// Called by the owning client to send the latest compact FaceData JSON
    /// snapshot to the server. Uses unreliable delivery because only
    /// the most recent face pose is relevant.
    /// </summary>
    [ServerRpc(Delivery = RpcDelivery.Unreliable)]
    private void SubmitFaceDataServerRpc(string json)
    {
        // Just broadcast to all clients. The owner has already applied
        // the frame locally in Update(), so we do not update EchoFace here.
        BroadcastFaceDataClientRpc(json);
    }

    /// <summary>
    /// Broadcasts the latest compact FaceData JSON snapshot to all clients.
    /// Non-owning clients apply it to their local EchoFace instance.
    /// </summary>
    [ClientRpc(Delivery = RpcDelivery.Unreliable)]
    private void BroadcastFaceDataClientRpc(string json)
    {
        // Only non-owning clients should consume the networked data.
        // The owner already applied the data locally from UDP.
        if (!IsClient || IsOwner)
        {
            return;
        }

        if (echoFace == null)
        {
            echoFace = GetComponent<EchoFace>();
            if (echoFace == null)
            {
                Debug.LogWarning("[FaceDataUdpReceiver] EchoFace not found on this client when applying networked FaceData.");
                return;
            }
        }

        FaceDataUdpPayload payload = null;
        try
        {
            payload = JsonConvert.DeserializeObject<FaceDataUdpPayload>(json);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[FaceDataUdpReceiver] Failed to deserialize FaceDataUdpPayload on client: {ex.Message}");
        }

        if (payload == null)
        {
            return;
        }

        var data = ConvertPayloadToFaceData(payload);

        if (data != null)
        {
            echoFace.SetFaceData(data);
        }
    }
}
