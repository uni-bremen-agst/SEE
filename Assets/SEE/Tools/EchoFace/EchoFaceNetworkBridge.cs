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

// Namespace documentation is provided in EchoFace.cs.
namespace SEE.Tools.EchoFace
{
    /// <summary>
    /// Defines the fixed, alphabetically sorted ordering of all supported
    /// blendshape names (with "_neutral" at index 0), used for compact
    /// network/UDP transmission where blendshapes are sent as a flat
    /// float array.
    /// </summary>
    /// <remarks>
    /// This list must match the <c>BLENDSHAPE_ORDER</c> used by the Python
    /// sender exactly, index by index; otherwise blendshape values will be
    /// mapped to the wrong names.
    /// </remarks>
    internal static class BlendshapeOrder
    {
        /// <summary>
        /// The blendshape names in transmission order, where the array
        /// index corresponds to the position of the value in the
        /// compact "bs" payload of <see cref="FaceDataUdpPayload"/>.
        /// </summary>
        internal static readonly string[] Names =
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
    /// </summary>
    /// <remarks>
    /// The lists are exposed as <see cref="IReadOnlyList{T}"/> to document
    /// that this data is treated as immutable by the receiver. The
    /// <see cref="JsonPropertyAttribute"/> on each field pins the mapping to
    /// the compact JSON keys used by the Python sender ("bs", "lm", "ts"),
    /// independent of the .NET field names.
    /// </remarks>
    [Serializable]
    internal class FaceDataUdpPayload
    {
        /// <summary>
        /// The blendshape weights, ordered according to
        /// <see cref="BlendshapeOrder.Names"/>. May be <c>null</c> if no
        /// blendshape data was included in the payload.
        /// </summary>
        [JsonProperty("bs")]
        internal IReadOnlyList<float> BlendshapeWeights;

        /// <summary>
        /// The landmark coordinates as three [x, y, z] triplets, in the
        /// fixed order Chin, RightUpperEyelid, LeftUpperEyelid. May be
        /// <c>null</c> if no landmark data was included in the payload.
        /// </summary>
        [JsonProperty("lm")]
        internal IReadOnlyList<IReadOnlyList<float>> LandmarkTriplets;

        /// <summary>
        /// The timestamp, in milliseconds, at which this frame was captured.
        /// </summary>
        [JsonProperty("ts")]
        internal long TimestampMs;
    }

    /// <summary>
    /// Receives raw face-tracking frames over UDP in a compact JSON format on
    /// the owning client, forwards each packet to the server via ServerRpc, and
    /// applies the data locally only after it has been broadcast back via
    /// ClientRpc. This class acts as a lightweight input adapter and
    /// performs no animation itself.
    /// </summary>
    internal class EchoFaceNetworkBridge : NetworkBehaviour
    {
        /// <summary>
        /// The local UDP port on which face-tracking packets are received.
        /// </summary>
        [Header("Local UDP Listener Settings")]
        [SerializeField]
        private int port = 12345;

        /// <summary>
        /// Whether only the latest received packet is processed, discarding
        /// any stale, already-queued packets to reduce latency.
        /// </summary>
        [SerializeField]
        [Tooltip("If enabled, only the latest packet will be processed, discarding stale packets to reduce latency.")]
        private bool discardStalePackets = true;

        /// <summary>
        /// The local <see cref="EchoFace"/> component that received face
        /// data is applied to. If not assigned in the Inspector, an
        /// attempt is made to auto-resolve it from the same
        /// <see cref="GameObject"/> in <see cref="Start"/> and, if
        /// necessary, again in <see cref="BroadcastFaceDataClientRpc"/>.
        /// </summary>
        [Header("Target")]
        [Tooltip("Reference to the local EchoFace component that should receive the incoming FaceData.")]
        [SerializeField]
        private EchoFace echoFace;

        /// <summary>
        /// The UDP client used to receive raw face-tracking packets.
        /// <c>null</c> until <see cref="StartUDPListener"/> succeeds, and
        /// reset to <c>null</c> after <see cref="Shutdown"/>.
        /// </summary>
        private UdpClient udpClient;

        /// <summary>
        /// The background thread running <see cref="ReceiveLoop"/>.
        /// <c>null</c> until <see cref="StartUDPListener"/> succeeds, and
        /// reset to <c>null</c> after <see cref="Shutdown"/>.
        /// </summary>
        private Thread receiveThread;

        /// <summary>
        /// Whether the UDP receive loop should keep running. Set to
        /// <c>true</c> in <see cref="StartUDPListener"/> and to <c>false</c>
        /// in <see cref="Shutdown"/> to signal <see cref="ReceiveLoop"/> to exit.
        /// </summary>
        private bool isRunning;

        /// <summary>
        /// Queues the original JSON packets received from UDP, to be
        /// dequeued and forwarded to the server in <see cref="Update"/>.
        /// </summary>
        private readonly ConcurrentQueue<string> jsonQueue = new();

        /// <summary>
        /// The timestamp, in milliseconds, of the last applied face data
        /// frame on this client instance (including the owner), used to
        /// discard out-of-order or duplicate packets. Initialized to
        /// <c>-1</c> so that the first received frame is always applied.
        /// </summary>
        private long lastTimestampMs = -1;

        /// <summary>
        /// Unity lifecycle method. Disables this component on any instance
        /// that is not the local owning client, since only the owner
        /// should read from the local UDP stream. Otherwise, auto-resolves
        /// <see cref="echoFace"/> if not assigned and starts the UDP
        /// listener via <see cref="StartUDPListener"/>.
        /// </summary>
        private void Start()
        {
            // Never start the UDP listener on a dedicated server.
            // Only the local owning client should read from the local UDP stream.
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
                    Debug.LogWarning("[EchoFaceNetworkBridge] EchoFace was not found on this GameObject.");
                }
            }

            StartUDPListener();
        }

        /// <summary>
        /// Unity lifecycle method. On the local owning client, dequeues the
        /// latest received JSON packet, if any, and forwards it to the
        /// server via <see cref="SubmitFaceDataServerRpc"/>. Does nothing
        /// on non-owning instances.
        /// </summary>
        private void Update()
        {
            // Extra safety: do nothing if this is not the local owner.
            if (!(IsClient && IsOwner))
            {
                return;
            }

            // Dequeue the latest JSON packet (compact UDP payload).
            if (jsonQueue.TryDequeue(out string json))
            {
                if (string.IsNullOrEmpty(json))
                {
                    return;
                }

                // Synchronize this frame over Netcode, so that
                // all clients (including the owner) receive it
                // through the same ClientRpc path.
                SubmitFaceDataServerRpc(json);
            }
        }

        /// <summary>
        /// Unity lifecycle method. Ensures the UDP listener and its
        /// receive thread are stopped when the application quits.
        /// </summary>
        private void OnApplicationQuit() => Shutdown();

        /// <summary>
        /// Unity lifecycle method. Ensures the UDP listener and its
        /// receive thread are stopped when this component is destroyed.
        /// </summary>
        public override void OnDestroy()
        {
            Shutdown();
            base.OnDestroy();
        }

        /// <summary>
        /// Initializes the UDP listener and spawns the receive thread.
        /// </summary>
        private void StartUDPListener()
        {
            try
            {
                udpClient = new UdpClient(new IPEndPoint(IPAddress.Loopback, port));
                isRunning = true;

                receiveThread = new(ReceiveLoop)
                {
                    IsBackground = true,
                    Name = "UDPFaceDataReceiver"
                };

                receiveThread.Start();
                Debug.Log($"[EchoFaceNetworkBridge] UDP listener started on port {port}.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[EchoFaceNetworkBridge] UDP listener failed to start on port {port}: {ex.Message}");
            }
        }

        /// <summary>
        /// Background thread continuously receiving compact JSON payloads.
        /// </summary>
        /// <remarks>
        /// Runs on <see cref="receiveThread"/>, not the Unity main thread.
        /// Must therefore not call into the Unity API directly; received
        /// packets are only queued in <see cref="jsonQueue"/> for
        /// processing on the main thread in <see cref="Update"/>.
        /// </remarks>
        private void ReceiveLoop()
        {
            IPEndPoint remoteEP = new(IPAddress.Any, port);

            while (isRunning)
            {
                try
                {
                    byte[] data = udpClient.Receive(ref remoteEP);
                    string json = Encoding.UTF8.GetString(data);

                    if (string.IsNullOrEmpty(json))
                    {
                        continue;
                    }

                    if (discardStalePackets)
                    {
                        // Clear older queued packets to keep only the latest.
                        jsonQueue.Clear();
                    }

                    jsonQueue.Enqueue(json);
                }
                catch (SocketException ex) when (ex.ErrorCode == 10004)
                {
                    if (isRunning)
                    {
                        Debug.LogWarning("[EchoFaceNetworkBridge] UDP socket was interrupted (normal shutdown).");
                    }
                }
                catch (ObjectDisposedException)
                {
                    if (isRunning)
                    {
                        Debug.LogWarning("[EchoFaceNetworkBridge] UDP client was disposed unexpectedly.");
                    }
                }
                catch (Exception ex)
                {
                    if (isRunning)
                    {
                        Debug.LogError($"[EchoFaceNetworkBridge] UDP receive error: {ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// Converts a compact FaceDataUdpPayload (with ordered blendshape values
        /// and a small landmark list) back into a full FaceData object used by EchoFace.
        /// </summary>
        /// <param name="payload">
        /// The compact payload to convert. May be <c>null</c>.
        /// </param>
        /// <returns>
        /// The reconstructed <see cref="FaceData"/>, or <c>null</c> if
        /// <paramref name="payload"/> is <c>null</c>.
        /// </returns>
        private FaceData ConvertPayloadToFaceData(FaceDataUdpPayload payload)
        {
            if (payload == null)
            {
                return null;
            }

            if (payload.BlendshapeWeights != null && payload.BlendshapeWeights.Count != BlendshapeOrder.Names.Length)
            {
               Debug.LogWarning(
                $"[EchoFaceNetworkBridge] Unexpected blendshape count: {
                    payload.BlendshapeWeights.Count} (expected {
                    BlendshapeOrder.Names.Length}).");
            }

            // 1) Rebuild blendshape dictionary.
            Dictionary<string, float> blendshapeDict = null;

            if (payload.BlendshapeWeights != null)
            {
                blendshapeDict = new(payload.BlendshapeWeights.Count);
                int count = Mathf.Min(payload.BlendshapeWeights.Count, BlendshapeOrder.Names.Length);

                for (int i = 0; i < count; i++)
                {
                    blendshapeDict[BlendshapeOrder.Names[i]] = payload.BlendshapeWeights[i];
                }
            }

            // 2) Rebuild landmarks dictionary with keys matching Landmarks constants.
            Dictionary<string, FaceData.LandmarkCoordinates> landmarks = new();

            if (payload.LandmarkTriplets != null)
            {
                void SetLm(int index, string key)
                {
                    if (index < 0 || index >= payload.LandmarkTriplets.Count)
                    {
                        landmarks[key] = new() { X = 0f, Y = 0f, Z = 0f };
                        return;
                    }

                    IReadOnlyList<float> list = payload.LandmarkTriplets[index];
                    float x = list.Count > 0 ? list[0] : 0f;
                    float y = list.Count > 1 ? list[1] : 0f;
                    float z = list.Count > 2 ? list[2] : 0f;

                    landmarks[key] = new() { X = x, Y = y, Z = z };
                }

                // Sorted by numeric size: 152, 226, 446.
                SetLm(0, Landmarks.Chin);
                SetLm(1, Landmarks.RightUpperEyelid);
                SetLm(2, Landmarks.LeftUpperEyelid);
            }

            return new()
            {
                TimestampMs = payload.TimestampMs,
                Blendshapes = blendshapeDict,
                LandmarkPositions = landmarks
            };
        }

        /// <summary>
        /// Stops the UDP client and terminates the receive thread.
        /// </summary>
        private void Shutdown()
        {
            if (!isRunning)
            {
                return;
            }

            isRunning = false;
            udpClient?.Close();

            if (receiveThread != null && receiveThread.IsAlive)
            {
                receiveThread.Join(500); // Wait up to 500ms for the thread to exit.
                if (receiveThread.IsAlive)
                {
                    Debug.LogWarning("[EchoFaceNetworkBridge] UDP receive thread did not terminate gracefully.");
                }
            }

            udpClient = null;
            receiveThread = null;
        }

        // -------------------------------------------------
        // RPCs for synchronizing FaceData
        // -------------------------------------------------

        /// <summary>
        /// Called by the owning client to send the latest compact FaceData JSON
        /// snapshot to the server. Uses unreliable delivery because only
        /// the most recent face pose is relevant.
        /// </summary>
        /// <param name="json">
        /// The compact <see cref="FaceDataUdpPayload"/>, serialized as JSON.
        /// </param>
        [ServerRpc(Delivery = RpcDelivery.Unreliable)]
        private void SubmitFaceDataServerRpc(string json)
        {
            // Broadcast the received JSON to all clients (including the owner).
            BroadcastFaceDataClientRpc(json);
        }

        /// <summary>
        /// Broadcasts the latest compact FaceData JSON snapshot to all clients.
        /// Every client (including the owner) applies it to their local EchoFace
        /// instance so that the animation path is identical everywhere.
        /// </summary>
        /// <param name="json">
        /// The compact <see cref="FaceDataUdpPayload"/>, serialized as JSON.
        /// </param>
        [ClientRpc(Delivery = RpcDelivery.Unreliable)]
        private void BroadcastFaceDataClientRpc(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                return;
            }

            if (echoFace == null)
            {
                echoFace = GetComponent<EchoFace>();
                if (echoFace == null)
                {
                    Debug.LogWarning("[EchoFaceNetworkBridge] EchoFace not found on this client when applying networked FaceData.");
                    return;
                }
            }

            if (echoFace.enabled == false)
            {
                // Do not apply data if EchoFace is disabled.
                return;
            }

            FaceDataUdpPayload payload = null;
            try
            {
                payload = JsonConvert.DeserializeObject<FaceDataUdpPayload>(json);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[EchoFaceNetworkBridge] Failed to deserialize FaceDataUdpPayload on client: {ex.Message}");
            }

            if (payload == null)
            {
                return;
            }

            // Timestamp filter per client instance to avoid applying out-of-order frames.
            if (payload.TimestampMs <= lastTimestampMs)
            {
                return;
            }

            lastTimestampMs = payload.TimestampMs;

            FaceData data = ConvertPayloadToFaceData(payload);

            if (data != null)
            {
                echoFace.SetFaceData(data);
            }
        }
    }
}
