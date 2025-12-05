using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using Newtonsoft.Json;
using Unity.Netcode;

/// <summary>
/// Receives raw FaceData frames over UDP on a background thread and forwards
/// the latest decoded result to a target <see cref="EchoFace"/> component.
/// This class acts as a lightweight input adapter and performs no animation
/// itself.
/// </summary>
public class FaceDataUdpReceiver : NetworkBehaviour
{
    [Header("Network Settings")]
    [SerializeField]
    private int port = 12345;

    [SerializeField]
    [Tooltip("If enabled, only the latest packet will be processed, discarding stale packets to reduce latency.")]
    private bool discardStalePackets = true;

    [Header("Target")]
    [Tooltip("Reference to the local EchoFace component that should receive the incoming FaceData.")]
    [SerializeField]
    private EchoFace echoFace;

    private UdpClient _udpClient;
    private Thread _receiveThread;
    private bool _isRunning;

    private readonly ConcurrentQueue<FaceData> _faceDataQueue = new();
    private long _lastTimestampMs = -1;

    private void Start()
    {
        // Never start the UDP listener on a dedicated server
        if (!IsClient || !IsOwner)
        {
            enabled = false;
            return;
        }

        // Auto-resolve EchoFace if not assigned manually
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
        // Retrieve the most recent FaceData from the network thread
        if (_faceDataQueue.TryDequeue(out var data))
        {
            // Forward to EchoFace if available
            if (echoFace != null)
            {
                echoFace.SetFaceData(data);
            }
        }
    }

    private void OnApplicationQuit() => Shutdown();
    private void OnDestroy() => Shutdown();

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
    /// Background thread continuously receiving FaceData packets.
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

                // Deserialize the JSON into the FaceData class
                var receivedData = JsonConvert.DeserializeObject<FaceData>(json);
                if (receivedData != null)
                {
                    // Discard outdated or identical packets based on timestamp
                    if (receivedData.ts <= _lastTimestampMs)
                    {
                        continue;
                    }

                    _lastTimestampMs = receivedData.ts;

                    if (discardStalePackets)
                    {
                        // Clear older queued packets to keep only the latest
                        _faceDataQueue.Clear();
                    }

                    _faceDataQueue.Enqueue(receivedData);
                }
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
            _receiveThread.Join(500); // Wait up to 500ms for the thread to exit
            if (_receiveThread.IsAlive)
            {
                Debug.LogWarning("[FaceDataUdpReceiver] UDP receive thread did not terminate gracefully.");
            }
        }

        _udpClient = null;
        _receiveThread = null;
    }
}
