using System;
using System.Collections.Generic;
using Mediapipe.Tasks.Components.Containers;
using Mediapipe.Tasks.Vision.FaceLandmarker;
using Unity.Netcode;
using UnityEngine;

// Namespace documentation is provided in EchoFace.cs.
namespace SEE.Tools.EchoFace
{
    /// <summary>
    /// Compact binary payload structure compatible with Netcode Unreliable
    /// delivery.
    /// </summary>
    internal struct NetworkFacePayload : INetworkSerializable
    {
        /// <summary>
        /// The total number of serialized blendshape weights, corresponding to
        /// <see cref="FaceBlendshape.Count"/>.
        /// </summary>
        internal const int BlendshapeCount = (int)FaceBlendshape.Count;

        /// <summary>
        /// The total number of serialized coordinate components across all landmarks
        /// (3 coordinates; 3 landmarks = 9 floats).
        /// </summary>
        internal const int LandmarkCount = (int)FaceLandmark.Count * 3;

        /// <summary>
        /// The timestamp, in milliseconds, at which this frame was captured.
        /// </summary>
        internal long TimestampMs;

        /// <summary>
        /// The blendshape weights, ordered according to <see cref="FaceBlendshape"/>.
        /// </summary>
        internal float[] Blendshapes;

        /// <summary>
        /// The landmark coordinates as a flat array of three [x, y, z] triplets,
        /// ordered according to <see cref="FaceLandmark"/>: Chin (indices 0-2),
        /// RightUpperEyelid (indices 3-5), LeftUpperEyelid (indices 6-8).
        /// </summary>
        internal float[] Landmarks;

        /// <summary>
        /// Serializes or deserializes this payload for Netcode, reading or
        /// writing <see cref="TimestampMs"/> followed by all
        /// <see cref="Blendshapes"/> and <see cref="Landmarks"/> values in
        /// order. When reading, allocates <see cref="Blendshapes"/> and
        /// <see cref="Landmarks"/> before populating them.
        /// </summary>
        /// <typeparam name="T">The concrete reader/writer type used by Netcode.</typeparam>
        /// <param name="serializer">The buffer serializer to read from or write to.</param>
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref TimestampMs);

            Blendshapes ??= new float[BlendshapeCount];
            Landmarks ??= new float[LandmarkCount];

            for (int i = 0; i < BlendshapeCount; i++)
            {
                serializer.SerializeValue(ref Blendshapes[i]);
            }

            for (int i = 0; i < LandmarkCount; i++)
            {
                serializer.SerializeValue(ref Landmarks[i]);
            }
        }
    }

    /// <summary>
    /// Bridges <see cref="MediaPipeFaceTracker"/>'s locally detected face
    /// data to <see cref="EchoFace"/> over the network: on the owning
    /// client, converts each tracked frame into a compact
    /// <see cref="NetworkFacePayload"/> and forwards it to the server via
    /// ServerRpc; the server then broadcasts it to all clients via
    /// ClientRpc, and every client (including the owner) applies it to its
    /// local <see cref="EchoFace"/> instance.
    /// </summary>
    internal class EchoFaceNetworkBridge : NetworkBehaviour
    {
        /// <summary>
        /// The local <see cref="EchoFace"/> component that received face
        /// data is applied to. If not assigned in the Inspector, an
        /// attempt is made to auto-resolve it from the same
        /// <see cref="GameObject"/> in <see cref="Start"/>.
        /// </summary>
        [Header("Target & Sources")]
        [SerializeField]
        private EchoFace echoFace;

        /// <summary>
        /// The local <see cref="MediaPipeFaceTracker"/> whose
        /// <see cref="MediaPipeFaceTracker.OnFaceTracked"/> event is
        /// subscribed to on the owning client. If not assigned in the
        /// Inspector, an attempt is made to auto-resolve it from the same
        /// <see cref="GameObject"/> via <see cref="Component.GetComponent{T}()"/> in
        /// <see cref="OnNetworkSpawn"/>.
        /// </summary>
        [SerializeField]
        private MediaPipeFaceTracker tracker;

        /// <summary>
        /// The timestamp, in milliseconds, of the last applied face data
        /// frame on this client instance (including the owner), used to
        /// discard out-of-order or duplicate packets. Initialized to
        /// <c>-1</c> so that the first received frame is always applied.
        /// </summary>
        private long lastTimestampMs = -1;

        /// <summary>
        /// Reusable buffer holding the blendshape weights for the current
        /// frame, ordered according to <see cref="FaceBlendshape"/>,
        /// to avoid re-allocating an array on every tracked frame.
        /// </summary>
        private readonly float[] blendshapeSendBuffer = new float[NetworkFacePayload.BlendshapeCount];

        /// <summary>
        /// Reusable buffer holding the flat landmark coordinates for the
        /// current frame (see <see cref="NetworkFacePayload.Landmarks"/>),
        /// to avoid re-allocating an array on every tracked frame.
        /// </summary>
        private readonly float[] landmarkSendBuffer = new float[NetworkFacePayload.LandmarkCount];

        /// <summary>
        /// Unity lifecycle method. Auto-resolves <see cref="echoFace"/> if
        /// not assigned. Owner- and network-dependent setup happens in
        /// <see cref="OnNetworkSpawn"/> instead, since ownership is not yet
        /// reliably available at this point.
        /// </summary>
        private void Start()
        {
            if (echoFace == null)
            {
                echoFace = GetComponent<EchoFace>();
            }
        }

        /// <summary>
        /// Netcode lifecycle method, called once this <see cref="NetworkObject"/>
        /// has spawned and ownership is reliably available. On the owning
        /// client, auto-resolves <see cref="tracker"/> if not assigned and
        /// subscribes to its <see cref="MediaPipeFaceTracker.OnFaceTracked"/> event.
        /// </summary>
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (!IsOwner)
            {
                return;
            }

            if (tracker == null)
            {
                tracker = GetComponent<MediaPipeFaceTracker>();
            }

            if (tracker != null)
            {
                tracker.OnFaceTracked += HandleRawFaceTracked;
            }
        }

        /// <summary>
        /// Unity lifecycle method, overriding <see cref="NetworkBehaviour.OnDestroy"/>.
        /// Unsubscribes from <see cref="tracker"/>'s
        /// <see cref="MediaPipeFaceTracker.OnFaceTracked"/> event, if subscribed.
        /// </summary>
        public override void OnDestroy()
        {
            if (tracker != null)
            {
                tracker.OnFaceTracked -= HandleRawFaceTracked;
            }

            base.OnDestroy();
        }

        /// <summary>
        /// Handles a newly tracked face frame from <see cref="tracker"/> on
        /// the owning client: packs the blendshape and landmark data into
        /// <see cref="blendshapeSendBuffer"/> and <see cref="landmarkSendBuffer"/>,
        /// and forwards them to the server via <see cref="SubmitFaceDataServerRpc"/>.
        /// </summary>
        /// <param name="result">The raw MediaPipe Face Landmarker detection result.</param>
        /// <param name="timestampMs">The timestamp, in milliseconds, at which the frame was captured.</param>
        private void HandleRawFaceTracked(FaceLandmarkerResult result, long timestampMs)
        {
            if (!IsOwner || !IsSpawned)
            {
                return;
            }

            // 1. Capture blendshapes directly via Category.index (matches FaceBlendshape ordering).
            Array.Clear(blendshapeSendBuffer, 0, blendshapeSendBuffer.Length);
            if (result.faceBlendshapes?.Count > 0 && result.faceBlendshapes[0].categories != null)
            {
                List<Category> categories = result.faceBlendshapes[0].categories;
                for (int i = 0; i < categories.Count; i++)
                {
                    Category cat = categories[i];
                    if (cat.index >= 0 && cat.index < NetworkFacePayload.BlendshapeCount)
                    {
                        blendshapeSendBuffer[cat.index] = cat.score;
                    }
                }
            }

            // 2. Pack landmarks in order Chin (152), RightUpperEyelid (226), LeftUpperEyelid (446).
            if (result.faceLandmarks?.Count > 0 && result.faceLandmarks[0].landmarks?.Count > 446)
            {
                List<NormalizedLandmark> lms = result.faceLandmarks[0].landmarks;

                NormalizedLandmark chin = lms[152];
                NormalizedLandmark rightEye = lms[226];
                NormalizedLandmark leftEye = lms[446];

                // 152 - Chin
                landmarkSendBuffer[0] = chin.x;
                landmarkSendBuffer[1] = chin.y;
                landmarkSendBuffer[2] = chin.z;

                // 226 - RightUpperEyelid
                landmarkSendBuffer[3] = rightEye.x;
                landmarkSendBuffer[4] = rightEye.y;
                landmarkSendBuffer[5] = rightEye.z;

                // 446 - LeftUpperEyelid
                landmarkSendBuffer[6] = leftEye.x;
                landmarkSendBuffer[7] = leftEye.y;
                landmarkSendBuffer[8] = leftEye.z;
            }
            else
            {
                Array.Clear(landmarkSendBuffer, 0, landmarkSendBuffer.Length);
            }

            SubmitFaceDataServerRpc(new()
            {
                TimestampMs = timestampMs,
                Blendshapes = blendshapeSendBuffer,
                Landmarks = landmarkSendBuffer
            });
        }

        /// <summary>
        /// Called by the owning client to send the latest
        /// <see cref="NetworkFacePayload"/> to the server. Uses unreliable
        /// delivery because only the most recent face pose is relevant.
        /// </summary>
        /// <param name="payload">The compact face data payload to broadcast.</param>
        [ServerRpc(Delivery = RpcDelivery.Unreliable)]
        private void SubmitFaceDataServerRpc(NetworkFacePayload payload)
        {
            BroadcastFaceDataClientRpc(payload);
        }

        /// <summary>
        /// Broadcasts the latest <see cref="NetworkFacePayload"/> to all
        /// clients. Every client (including the owner) applies it to its
        /// local <see cref="EchoFace"/> instance so that the animation path
        /// is identical everywhere.
        /// </summary>
        /// <param name="payload">The compact face data payload received from the server.</param>
        [ClientRpc(Delivery = RpcDelivery.Unreliable)]
        private void BroadcastFaceDataClientRpc(NetworkFacePayload payload)
        {
            if (echoFace == null || !echoFace.enabled || payload.TimestampMs <= lastTimestampMs)
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

        /// <summary>
        /// Converts a compact <see cref="NetworkFacePayload"/> into a
        /// <see cref="FaceData"/> instance used by <see cref="EchoFace"/>.
        /// Passes the raw blendshape array directly through to avoid GC allocations.
        /// </summary>
        /// <param name="payload">The compact payload to convert.</param>
        /// <returns>The reconstructed <see cref="FaceData"/>.</returns>
        private FaceData ConvertPayloadToFaceData(NetworkFacePayload payload)
        {
            FaceData.LandmarkCoordinates[] landmarks = null;
            if (payload.Landmarks != null && payload.Landmarks.Length >= NetworkFacePayload.LandmarkCount)
            {
                landmarks = new FaceData.LandmarkCoordinates[(int)FaceLandmark.Count];
                landmarks[(int)FaceLandmark.Chin] = new(payload.Landmarks[0], payload.Landmarks[1], payload.Landmarks[2]);
                landmarks[(int)FaceLandmark.RightUpperEyelid] = new(payload.Landmarks[3], payload.Landmarks[4], payload.Landmarks[5]);
                landmarks[(int)FaceLandmark.LeftUpperEyelid] = new(payload.Landmarks[6], payload.Landmarks[7], payload.Landmarks[8]);
            }

            return new()
            {
                Blendshapes = payload.Blendshapes,
                Landmarks = landmarks,
                TimestampMs = payload.TimestampMs
            };
        }
    }
}
