using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using Newtonsoft.Json;

/// <summary>
/// A static class to hold named constants for landmark indices.
/// </summary>
public static class Landmarks
{
    public const string Chin = "152";
    public const string LeftUpperEyelid = "446";
    public const string RightUpperEyelid = "226";
}

/// <summary>
/// Represents the structure of the incoming JSON data.
/// </summary>
[Serializable]
public class FaceData
{
    [Serializable]
    public class LandmarkCoordinates
    {
        public float x;
        public float y;
        public float z;
    }

    public Dictionary<string, float> blendshapes;
    public Dictionary<string, LandmarkCoordinates> landmarks;

    public long ts;
}

/// <summary>
/// Receives blendshape and landmark data over UDP and applies it to a
/// SkinnedMeshRenderer. This script handles network communication on a
/// separate thread, maps MediaPipe ARKit blendshapes to custom
/// blendshapes, synthesizes visemes for speech, and smoothly
/// interpolates values.
/// </summary>
public class EchoFace : MonoBehaviour
{
    //-------------------------------------------------
    // Public Fields
    //-------------------------------------------------

    [Header("Network Settings")]
    [SerializeField]
    private int port = 12345;

    [Header("Avatar Settings")]
    [SerializeField]
    private SkinnedMeshRenderer skinnedMeshRenderer;

    [Header("Face Animation Settings")]
    [Tooltip("Enable all face animation based on blendshapes.")]
    public bool enableFaceAnimation = true;

    [Tooltip("Enables synthesized visemes (V_*). Also activates specific scaling for viseme-related blendshapes such as Mouth_Funnel* and Mouth_Pucker*.")]
    public bool enableVisemeSynthesis = true;

    [Tooltip("Smoothing rate for general blendshapes. Lower is smoother.")]
    [Range(0.01f, 1.0f)]
    [SerializeField]
    private float smoothingRate = 0.5f;

    [Tooltip("Smoothing rate specifically for viseme blendshapes.")]
    [Range(0.01f, 1.0f)]
    [SerializeField]
    private float visemeSmoothingRate = 0.9f;

    [Tooltip("Power curve for eye squint expression, to make it more pronounced.")]
    [Range(0f, 12f)]
    [SerializeField]
    private float eyeSquintPower = 12f;     // Note: Optimal value would be around 3

    [Header("Head Rotation Settings")]
    [Tooltip("Enable head rotation based on landmarks.")]
    public bool enableHeadRotation = true;

    [Tooltip("The Transform of the head bone to rotate.")]
    [SerializeField]
    private Transform headTransform;

    [Tooltip("The smoothing rate for head rotation.")]
    [Range(0.01f, 1.0f)]
    [SerializeField]
    private float rotationSmoothingRate = 0.5f;

    [Tooltip("Manual pitch correction to align the avatar with the webcam feed.")]
    [Range(-50f, 50f)]
    [SerializeField]
    private float tiltCorrection = -15.0f;

    [Header("Eye Rotation Settings")]
    [Tooltip("Enable eye rotation based on blendshapes.")]
    public bool enableEyeRotation = true;

    [Tooltip("The Transform of the left eye bone.")]
    [SerializeField]
    private Transform leftEyeTransform;

    [Tooltip("The Transform of the right eye bone.")]
    [SerializeField]
    private Transform rightEyeTransform;

    [Tooltip("The smoothing factor for eye rotation.")]
    [Range(0.01f, 1.0f)]
    [SerializeField]
    private float eyeRotationSmoothingRate = 0.5f;

    [Tooltip("The unified scaling factor for all eye movements (up, down, side-to-side).")]
    [Range(1f, 100f)]
    [SerializeField]
    private float eyeLookScale = 30f;

    [Header("Debug / Monitoring")]
    [SerializeField]
    private bool discardOutdatedPackets = true; // Ebene 1 (UDP)

    [SerializeField]
    private bool discardLatencyOutliers = true; // Ebene 2 (Statistik)


    //-------------------------------------------------
    // Private Fields
    //-------------------------------------------------

    // --- Monitoring ---
    // wir messen jetzt NUR maximale Latenz, keinen Durchschnitt mehr
    private double _maxLatencyObserved = 0.0;
    private double _lastLatencyMs = 0.0;

    private double _minLatencyObserved = double.MaxValue;
    private double _latencyAccumulator = 0.0;
    private int _latencyCount = 0;


    private int _processedThisSecond = 0;
    private float _fpsTimer = 0f;
    private int _processedFps = 0;

    // Zähler für verworfene Pakete
    private int _discardedByTimestampCount = 0; // im Receive-Thread verworfen (älter / identisch)
    private int _discardedByLatencyCount = 0;   // in der Latenz-Statistik verworfen (Outlier)
    private int _discardedByQueueTrimCount = 0;      // beim "nur aktuelles behalten" rausgeworfen

    private UdpClient _udpClient;
    private Thread _receiveThread;
    private bool _isRunning = false;

    // A thread-safe queue to pass data from the network thread to the main thread
    private readonly ConcurrentQueue<FaceData> _faceDataQueue = new();

    // Stores the latest face data to be used by LateUpdate
    private FaceData _latestFaceData;

    // Stores the timestamp of the last processed packet to discard outdated packets
    private long _lastTimestampMs = -1;

    // Stores the current blendshape values after smoothing, used for the next frame's smoothing calculation
    private readonly Dictionary<string, float> _currentBlendshapeValues = new();

    // A cached dictionary to store blendshape indices, preventing repeated lookups in the Update loop
    private readonly Dictionary<string, int> _blendshapeIndexCache = new();

    // Stores the current head and eye rotations, used for the next frame's smoothing calculation
    private Quaternion _currentHeadRotation = Quaternion.identity;
    private Quaternion _currentLeftEyeRotation = Quaternion.identity;
    private Quaternion _currentRightEyeRotation = Quaternion.identity;
    private Quaternion _leftEyeRestRotation;
    private Quaternion _rightEyeRestRotation;

    private readonly Dictionary<
        string,
        Func<Dictionary<string, float>, float>
    > _visemeSynthesisMap =
        new()
        {
        {
            "V_Open",
            arkit =>
            Mathf.Clamp01(
                arkit["jawOpen"] * 0.9f
                + (arkit["mouthUpperUpLeft"] + arkit["mouthUpperUpRight"]) * 0.1f
                + (arkit["mouthLowerDownLeft"] + arkit["mouthLowerDownRight"]) * 0.1f
                + arkit["mouthShrugLower"] * 0.05f
                - arkit["mouthPucker"] * 0.2f
                - arkit["mouthFunnel"] * 0.1f
            )
        },
        {
            "V_Explosive",
            arkit =>
            Mathf.Clamp01(
                Mathf.Max(arkit["mouthPressLeft"], arkit["mouthPressRight"]) * 0.7f
                + arkit["mouthPucker"] * 0.4f
                + arkit["mouthClose"] * 0.5f
                + Mathf.Max(arkit["mouthRollUpper"], arkit["mouthRollLower"]) * 0.2f
                + (1f - arkit["jawOpen"]) * 0.3f
            )
        },
        {
            "V_Dental_Lip",
            arkit =>
            Mathf.Clamp01(
                (arkit["mouthLowerDownLeft"] + arkit["mouthLowerDownRight"]) * 0.4f
                + arkit["mouthRollLower"] * 0.8f
                + (arkit["mouthUpperUpLeft"] + arkit["mouthUpperUpRight"]) * 0.2f
                + Mathf.Max(arkit["noseSneerLeft"], arkit["noseSneerRight"]) * 0.1f
                + (1f - arkit["jawOpen"]) * 0.2f
            )
        },
        {
            "V_Tight_O",
            arkit =>
            Mathf.Clamp01(
                arkit["mouthPucker"] * 0.7f
                + arkit["mouthFunnel"] * 0.6f
                + (1f - arkit["jawOpen"]) * 0.2f
                + Mathf.Max(arkit["mouthPressLeft"], arkit["mouthPressRight"]) * 0.1f
                - Mathf.Max(arkit["mouthSmileLeft"], arkit["mouthSmileRight"]) * 0.3f
            )
        },
        {
            "V_Tight",
            arkit =>
            Mathf.Clamp01(
                Mathf.Max(arkit["mouthPressLeft"], arkit["mouthPressRight"]) * 0.7f
                + arkit["mouthClose"] * 0.5f
                + Mathf.Max(arkit["mouthRollUpper"], arkit["mouthRollLower"]) * 0.2f
                + Mathf.Max(arkit["mouthFrownLeft"], arkit["mouthFrownRight"]) * 0.15f
            )
        },
        {
            "V_Wide",
            arkit =>
            Mathf.Clamp01(
                (arkit["mouthStretchLeft"] + arkit["mouthStretchRight"]) * 0.3f
                + (arkit["mouthSmileLeft"] + arkit["mouthSmileRight"]) * 0.3f
                + arkit["jawOpen"] * 0.3f
                + (arkit["mouthDimpleLeft"] + arkit["mouthDimpleRight"]) * 0.1f
                - arkit["mouthPucker"] * 0.2f
                - arkit["mouthFunnel"] * 0.2f
            )
        },
        {
            "V_Affricate",
            arkit =>
            Mathf.Clamp01(
                arkit["mouthFunnel"] * 1.0f
                + Mathf.Max(arkit["mouthPressLeft"], arkit["mouthPressRight"]) * 0.4f
                + Mathf.Max(arkit["mouthRollUpper"], arkit["mouthRollLower"]) * 0.2f
                + Mathf.Max(arkit["mouthFrownLeft"], arkit["mouthFrownRight"]) * 0.1f
            )
        },
        {
            "V_Lip_Open",
            arkit =>
            Mathf.Clamp01(
                (arkit["mouthUpperUpLeft"] + arkit["mouthUpperUpRight"]) * 0.3f
                + (arkit["mouthLowerDownLeft"] + arkit["mouthLowerDownRight"]) * 0.3f
                + arkit["mouthFunnel"] * 0.6f
                + arkit["mouthPucker"] * 0.4f
                + arkit["jawOpen"] * 0.2f
            )
        }
    };

    private readonly Dictionary<string, List<string>> _mediapipeToCustomMap =
        new()
        {
        { "browDownLeft", new() { "Brow_Drop_L" } },
        { "browDownRight", new() { "Brow_Drop_R" } },
        { "browInnerUp", new() { "Brow_Raise_Inner_L", "Brow_Raise_Inner_R" } },
        { "browOuterUpLeft", new() { "Brow_Raise_Outer_L" } },
        { "browOuterUpRight", new() { "Brow_Raise_Outer_R" } },
        { "cheekPuff", new() { "Cheek_Puff_L", "Cheek_Puff_R" } },
        { "cheekSquintLeft", new() { "Cheek_Raise_L" } },
        { "cheekSquintRight", new() { "Cheek_Raise_R" } },
        { "eyeBlinkLeft", new() { "Eye_Blink_L" } },
        { "eyeBlinkRight", new() { "Eye_Blink_R" } },
        { "eyeSquintLeft", new() { "Eye_Squint_L" } },
        { "eyeSquintRight", new() { "Eye_Squint_R" } },
        { "eyeWideLeft", new() { "Eye_Wide_L" } },
        { "eyeWideRight", new() { "Eye_Wide_R" } },
        { "eyeLookDownLeft", new() { "Eye_L_Look_Down" } },
        { "eyeLookDownRight", new() { "Eye_R_Look_Down" } },
        { "eyeLookUpLeft", new() { "Eye_L_Look_Up" } },
        { "eyeLookUpRight", new() { "Eye_R_Look_Up" } },
        { "eyeLookInLeft", new() { "Eye_L_Look_R" } },
        { "eyeLookInRight", new() { "Eye_R_Look_L" } },
        { "eyeLookOutLeft", new() { "Eye_L_Look_L" } },
        { "eyeLookOutRight", new() { "Eye_R_Look_R" } },
        { "jawForward", new() { "Jaw_Forward" } },
        { "jawLeft", new() { "Jaw_L" } },
        { "jawRight", new() { "Jaw_R" } },
        { "jawOpen", new() { "Merged_Open_Mouth" } },
        { "mouthClose", new() { "Mouth_Close" } },
        { "mouthDimpleLeft", new() { "Mouth_Dimple_L" } },
        { "mouthDimpleRight", new() { "Mouth_Dimple_R" } },
        { "mouthFrownLeft", new() { "Mouth_Frown_L" } },
        { "mouthFrownRight", new() { "Mouth_Frown_R" } },
        {
            "mouthFunnel",
            new()
            {
                "Mouth_Funnel_Up_L",
                "Mouth_Funnel_Up_R",
                "Mouth_Funnel_Down_L",
                "Mouth_Funnel_Down_R"
            }
        },
        { "mouthLeft", new() { "Mouth_L" } },
        { "mouthRight", new() { "Mouth_R" } },
        { "mouthLowerDownLeft", new() { "Mouth_Down_Lower_L" } },
        { "mouthLowerDownRight", new() { "Mouth_Down_Lower_R" } },
        { "mouthPressLeft", new() { "Mouth_Press_L" } },
        { "mouthPressRight", new() { "Mouth_Press_R" } },
        {
            "mouthPucker",
            new()
            {
                "Mouth_Pucker_Up_L",
                "Mouth_Pucker_Up_R",
                "Mouth_Pucker_Down_L",
                "Mouth_Pucker_Down_R"
            }
        },
        { "mouthRollLower", new() { "Mouth_Roll_In_Lower_L", "Mouth_Roll_In_Lower_R" } },
        { "mouthRollUpper", new() { "Mouth_Roll_In_Upper_L", "Mouth_Roll_In_Upper_R" } },
        { "mouthShrugLower", new() { "Mouth_Shrug_Lower" } },
        { "mouthShrugUpper", new() { "Mouth_Shrug_Upper" } },
        { "mouthSmileLeft", new() { "Mouth_Smile_L" } },
        { "mouthSmileRight", new() { "Mouth_Smile_R" } },
        { "mouthStretchLeft", new() { "Mouth_Stretch_L" } },
        { "mouthStretchRight", new() { "Mouth_Stretch_R" } },
        { "mouthUpperUpLeft", new() { "Mouth_Up_Upper_L" } },
        { "mouthUpperUpRight", new() { "Mouth_Up_Upper_R" } },
        { "noseSneerLeft", new() { "Nose_Sneer_L" } },
        { "noseSneerRight", new() { "Nose_Sneer_R" } }
        };

    // Base scaling factors that are always applied.
    // Used for subtle shaping or expression tuning.
    private readonly Dictionary<string, float> _baseBlendshapeScales = new()
    {
        { "Mouth_Up_Upper_L", 0.6f },
        { "Mouth_Up_Upper_R", 0.6f },
    };

    // Scaling factors that should only be applied when viseme synthesis is enabled.
    private readonly Dictionary<string, float> _visemeBlendshapeScales = new()
    {
        { "Mouth_Funnel_Up_L",   0.3f },
        { "Mouth_Funnel_Up_R",   0.3f },
        { "Mouth_Funnel_Down_L", 0.3f },
        { "Mouth_Funnel_Down_R", 0.3f },
        { "Mouth_Pucker_Up_L",   0.8f },
        { "Mouth_Pucker_Up_R",   0.8f },
        { "Mouth_Pucker_Down_L", 0.8f },
        { "Mouth_Pucker_Down_R", 0.8f },
    };

    //-------------------------------------------------
    // Unity Lifecycle Methods
    //-------------------------------------------------

    private void Start()
    {
        Application.targetFrameRate = 800;

        // Attempt to auto-assign skinnedMeshRenderer if not set in Inspector
        if (skinnedMeshRenderer == null)
        {
            skinnedMeshRenderer = transform.Find("CC_Base_Body")?.GetComponent<SkinnedMeshRenderer>();
            if (skinnedMeshRenderer == null)
            {
                Debug.LogWarning("[EchoFace] SkinnedMeshRenderer not found. Please assign it manually.");
            }
        }

        // Attempt to auto-assign headTransform
        if (headTransform == null)
        {
            headTransform = FindDeepChild(transform, "CC_Base_Head");
            if (headTransform == null)
            {
                Debug.LogWarning("[EchoFace] Head bone transform not found. Head rotation will be disabled.");
            }
            else
            {
                // Find eye bones if head is found
                FindEyeBones(headTransform);
            }
        }

        CacheBlendshapeIndices();
        StartUDPListener();
    }

    double NowUnixSeconds()
    {
        return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000.0;
    }

    private void Update()
    {
        // Dequeue data in Update. Store the latest data for LateUpdate.
        if (_faceDataQueue.TryDequeue(out var receivedData))
        {
            _latestFaceData = receivedData;
            _processedThisSecond++; // zählt nur neue Pakete, nicht jede Frame
        }
    }

    private void LateUpdate()
    {
        if (_latestFaceData == null)
            goto LOG_ONLY; // nichts zu tun, aber trotzdem jede Sekunde loggen

        // 1) Latenz messen (jetzt – gesendet)
        if (_latestFaceData.ts > 0)
        {
            double nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            double latencyMs = nowMs - _latestFaceData.ts;
            _lastLatencyMs = latencyMs;

            if (latencyMs < _minLatencyObserved)
                _minLatencyObserved = latencyMs;

            if (latencyMs > _maxLatencyObserved)
                _maxLatencyObserved = latencyMs;

            _latencyAccumulator += latencyMs;
            _latencyCount++;

        }

        // 2) Face-Animation anwenden
        if (enableFaceAnimation && skinnedMeshRenderer != null && _latestFaceData.blendshapes != null)
        {
            ApplyBlendshapes(_latestFaceData.blendshapes);
        }

        // 3) Head-Rotation
        if (enableHeadRotation && headTransform != null && _latestFaceData.landmarks != null && _latestFaceData.landmarks.Count >= 3)
        {
            var targetRotation = EstimateHeadRotation(_latestFaceData.landmarks);
            ApplyHeadRotation(targetRotation);
        }

        // 4) Eye-Rotation
        if (enableEyeRotation)
        {
            ApplyEyeRotation();
        }

        // damit wir im nächsten Frame nicht versehentlich dasselbe Paket nochmal benutzen
        _latestFaceData = null;

    LOG_ONLY:

        _fpsTimer += Time.deltaTime;
        // 5) FPS + Monitoring jede Sekunde ausgeben
        if (_fpsTimer >= 1f)
        {
            _processedFps = _processedThisSecond;
            _processedThisSecond = 0;
            _fpsTimer = 0f;
            double avgLatency = _latencyCount > 0 ? _latencyAccumulator / _latencyCount : 0;
            Debug.Log(
                $"[EchoFace] FPS={_processedFps}, " +
                $"latency min={_minLatencyObserved:F2} ms, " +
                $"avg={avgLatency:F2} ms, " +
                $"max={_maxLatencyObserved:F2} ms, " +
                $"discarded (timestamp)={_discardedByTimestampCount}, " +
                $"discarded (queue-trim)={_discardedByQueueTrimCount}"
            );

            // Reset Durchschnittszähler, damit du pro Sekunde Mittelwerte bekommst
            _latencyAccumulator = 0.0;
            _latencyCount = 0;
        }
    }

    private void OnApplicationQuit() => Shutdown();
    private void OnDestroy() => Shutdown();

    //-------------------------------------------------
    // Private Methods
    //-------------------------------------------------

    private Vector3 ToUnityVector3(FaceData.LandmarkCoordinates coords)
    {
        return new Vector3(-coords.x, -coords.y, -coords.z);
    }

    private void ApplyBlendshapes(Dictionary<string, float> blendshapes)
    {
        var targetBlendshapeValues = new Dictionary<string, float>();

        // 1. Map MediaPipe to Custom Blendshapes and apply enhancements
        foreach (var kvp in blendshapes)
        {
            if (!_mediapipeToCustomMap.TryGetValue(kvp.Key, out var customNames))
                continue;

            float value = kvp.Value;

            // Apply power curve to eyeSquint AND add the influence of browDown
            if (kvp.Key.Contains("eyeSquint"))
            {
                float browDownValue = 0f;
                if (kvp.Key == "eyeSquintLeft" && blendshapes.ContainsKey("browDownLeft"))
                {
                    browDownValue = blendshapes["browDownLeft"];
                }
                else if (kvp.Key == "eyeSquintRight" && blendshapes.ContainsKey("browDownRight"))
                {
                    browDownValue = blendshapes["browDownRight"];
                }

                value = Mathf.Pow(value, eyeSquintPower);
                value = Mathf.Clamp01(value + value * browDownValue);
            }

            foreach (var name in customNames)
                targetBlendshapeValues[name] = Mathf.Max(
                    targetBlendshapeValues.GetValueOrDefault(name, 0f),
                    value
                );
        }

        // 2. Apply custom logic for specific blendshapes
        targetBlendshapeValues["Mouth_Down"] = Mathf.Max(
            blendshapes.GetValueOrDefault("mouthUpperUpLeft", 0f),
            blendshapes.GetValueOrDefault("mouthUpperUpRight", 0f)
        ) * Mathf.Max(
            blendshapes.GetValueOrDefault("mouthSmileLeft", 0f),
            blendshapes.GetValueOrDefault("mouthSmileRight", 0f)
        );

        targetBlendshapeValues["Mouth_Down_Lower_L"] = Mathf.Clamp01(
            blendshapes.GetValueOrDefault("mouthLowerDownLeft", 0f)
            * (1 - blendshapes.GetValueOrDefault("jawOpen", 0f))
        );

        targetBlendshapeValues["Mouth_Down_Lower_R"] = Mathf.Clamp01(
            blendshapes.GetValueOrDefault("mouthLowerDownRight", 0f)
            * (1 - blendshapes.GetValueOrDefault("jawOpen", 0f))
        );

        // 3. Synthesize and Apply Visemes
        if (enableVisemeSynthesis)
        {
            foreach (var visemeKvp in _visemeSynthesisMap)
            {
                targetBlendshapeValues[visemeKvp.Key] = visemeKvp.Value(blendshapes);
            }
        }
        else
        {
            foreach (var visemeKey in _visemeSynthesisMap.Keys)
            {
                if (_currentBlendshapeValues.TryGetValue(visemeKey, out float currentValue)
                    && currentValue > 0.0001f)
                {
                    targetBlendshapeValues[visemeKey] = 0f;
                }
            }
        }

        // 4. Smooth and Set Blendshape Weights
        foreach (var kvp in targetBlendshapeValues)
        {
            if (!_blendshapeIndexCache.TryGetValue(kvp.Key, out int index))
            {
                continue;
            }

            float targetValue = kvp.Value;
            _currentBlendshapeValues.TryGetValue(kvp.Key, out float currentValue);

            if (_baseBlendshapeScales.TryGetValue(kvp.Key, out float baseScale))
            {
                targetValue *= baseScale;
            }

            if (enableVisemeSynthesis &&
                _visemeBlendshapeScales.TryGetValue(kvp.Key, out float visemeScale))
            {
                targetValue *= visemeScale;
            }

            float smoothingRateToUse =
                (enableVisemeSynthesis && _visemeSynthesisMap.ContainsKey(kvp.Key))
                    ? visemeSmoothingRate
                    : smoothingRate;

            float alpha = 1f - Mathf.Exp(-smoothingRateToUse * Time.deltaTime * 60f);
            float smoothedValue = Mathf.Lerp(currentValue, targetValue, alpha);

            skinnedMeshRenderer.SetBlendShapeWeight(index, smoothedValue * 100f);
            _currentBlendshapeValues[kvp.Key] = smoothedValue;
        }
    }

    private Quaternion EstimateHeadRotation(Dictionary<string, FaceData.LandmarkCoordinates> landmarks)
    {
        if (!landmarks.ContainsKey(Landmarks.Chin) ||
            !landmarks.ContainsKey(Landmarks.LeftUpperEyelid) ||
            !landmarks.ContainsKey(Landmarks.RightUpperEyelid))
        {
            Debug.LogWarning("[EchoFace] Required landmarks for head pose not found in the received data.");
            return _currentHeadRotation;
        }

        Vector3 chin = ToUnityVector3(landmarks[Landmarks.Chin]);
        Vector3 leftEyeInner = ToUnityVector3(landmarks[Landmarks.LeftUpperEyelid]);
        Vector3 rightEyeInner = ToUnityVector3(landmarks[Landmarks.RightUpperEyelid]);

        Vector3 eyeMidpoint = (leftEyeInner + rightEyeInner) * 0.5f;
        Vector3 upVector = (eyeMidpoint - chin).normalized;
        Vector3 rightVector = (leftEyeInner - rightEyeInner).normalized;
        Vector3 forwardVector = Vector3.Cross(upVector, rightVector).normalized;

        Quaternion targetRotation = Quaternion.LookRotation(forwardVector, upVector);
        Quaternion correction = Quaternion.Euler(tiltCorrection, 0, 0);
        targetRotation = targetRotation * correction;

        return targetRotation;
    }

    private void ApplyEyeRotation()
    {
        if (leftEyeTransform == null || rightEyeTransform == null || _latestFaceData?.blendshapes == null)
            return;

        var blendshapes = _latestFaceData.blendshapes;
        float pitchLeft = 0f;
        float yawLeft = 0f;
        float pitchRight = 0f;
        float yawRight = 0f;

        pitchLeft -= blendshapes.GetValueOrDefault("eyeLookUpLeft") * eyeLookScale;
        pitchLeft += blendshapes.GetValueOrDefault("eyeLookDownLeft") * eyeLookScale;
        pitchLeft -= tiltCorrection * 0.5f;

        pitchRight -= blendshapes.GetValueOrDefault("eyeLookUpRight") * eyeLookScale;
        pitchRight += blendshapes.GetValueOrDefault("eyeLookDownRight") * eyeLookScale;
        pitchRight -= tiltCorrection * 0.5f;

        yawLeft -= blendshapes.GetValueOrDefault("eyeLookOutLeft") * eyeLookScale;
        yawLeft += blendshapes.GetValueOrDefault("eyeLookInLeft") * eyeLookScale;

        yawRight += blendshapes.GetValueOrDefault("eyeLookOutRight") * eyeLookScale;
        yawRight -= blendshapes.GetValueOrDefault("eyeLookInRight") * eyeLookScale;

        Quaternion targetLeftRotation = Quaternion.Euler(pitchLeft, 0, yawLeft);
        Quaternion targetRightRotation = Quaternion.Euler(pitchRight, 0, yawRight);

        float alpha = 1f - Mathf.Exp(-eyeRotationSmoothingRate * Time.deltaTime * 60f);
        _currentLeftEyeRotation = Quaternion.Slerp(_currentLeftEyeRotation, targetLeftRotation, alpha);
        _currentRightEyeRotation = Quaternion.Slerp(_currentRightEyeRotation, targetRightRotation, alpha);

        leftEyeTransform.localRotation = _leftEyeRestRotation * _currentLeftEyeRotation;
        rightEyeTransform.localRotation = _rightEyeRestRotation * _currentRightEyeRotation;
    }

    private void ApplyHeadRotation(Quaternion targetRotation)
    {
        if (headTransform == null) return;

        float alpha = 1f - Mathf.Exp(-rotationSmoothingRate * Time.deltaTime * 60f);
        _currentHeadRotation = Quaternion.Slerp(_currentHeadRotation, targetRotation, alpha);
        headTransform.localRotation = _currentHeadRotation;
    }

    private void FindEyeBones(Transform head)
    {
        if (head == null) return;

        leftEyeTransform = FindDeepChild(head, "CC_Base_L_Eye");
        rightEyeTransform = FindDeepChild(head, "CC_Base_R_Eye");

        if (leftEyeTransform == null || rightEyeTransform == null)
        {
            Debug.LogWarning("[EchoFace] Eye bone transforms not found. Eye rotation will be disabled.");
        }
        else
        {
            _leftEyeRestRotation = leftEyeTransform.localRotation;
            _rightEyeRestRotation = rightEyeTransform.localRotation;
        }
    }

    private Transform FindDeepChild(Transform parent, string name)
    {
        var directChild = parent.Find(name);
        if (directChild != null)
        {
            return directChild;
        }

        foreach (Transform child in parent)
        {
            var found = FindDeepChild(child, name);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private void StartUDPListener()
    {
        try
        {
            _udpClient = new UdpClient(port);
            _isRunning = true;
            _receiveThread = new Thread(ReceiveLoop)
            {
                IsBackground = true,
                Name = "UDPEchoFace"
            };
            _receiveThread.Start();
            Debug.Log($"[EchoFace] UDP listener started on port {port}.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[EchoFace] UDP listener failed to start on port {port}: {ex.Message}");
        }
    }

    private void ReceiveLoop()
    {
        var remoteEP = new IPEndPoint(IPAddress.Any, port);
        while (_isRunning)
        {
            try
            {
                byte[] data = _udpClient.Receive(ref remoteEP);
                string json = Encoding.UTF8.GetString(data);

                var receivedData = JsonConvert.DeserializeObject<FaceData>(json);
                if (receivedData != null)
                {
                    // hier werden "alten" Pakete wirklich verworfen -> zählen!
                    if (receivedData.ts <= _lastTimestampMs)
                    {
                        _discardedByTimestampCount++;
                        continue;
                    }
                    _lastTimestampMs = receivedData.ts;
                    if (discardOutdatedPackets)
                    {

                        // nur aktuelles behalten
                        while (_faceDataQueue.TryDequeue(out _))
                        {
                            _discardedByQueueTrimCount++;
                        }
                    }
                    _faceDataQueue.Enqueue(receivedData);
                }
            }
            catch (SocketException ex) when (ex.ErrorCode == 10004)
            {
                if (_isRunning)
                    Debug.LogWarning("[EchoFace] UDP socket interrupted (normal shutdown).");
            }
            catch (ObjectDisposedException)
            {
                if (_isRunning)
                    Debug.LogWarning("[EchoFace] UDP client disposed (normal shutdown).");
            }
            catch (Exception ex)
            {
                if (_isRunning)
                    Debug.LogError($"[EchoFace] UDP receive error: {ex.Message}");
            }
        }
    }

    private void CacheBlendshapeIndices()
    {
        _blendshapeIndexCache.Clear();
        var mesh = skinnedMeshRenderer.sharedMesh;
        if (mesh == null)
            return;

        var allBlendshapeNames = _mediapipeToCustomMap.Values
                .SelectMany(x => x)
                .Concat(_visemeSynthesisMap.Keys)
                .ToList();

        allBlendshapeNames.Add("Mouth_Down");
        allBlendshapeNames.Add("Mouth_Up");

        foreach (var name in allBlendshapeNames.Distinct())
        {
            int index = mesh.GetBlendShapeIndex(name);
            if (index >= 0)
            {
                _blendshapeIndexCache[name] = index;
            }
            else
            {
                Debug.LogWarning($"[EchoFace] Blendshape '{name}' not found on the mesh.");
            }
        }
    }

    private void Shutdown()
    {
        if (!_isRunning)
            return;

        _isRunning = false;
        _udpClient?.Close();

        if (_receiveThread != null && _receiveThread.IsAlive)
        {
            _receiveThread.Join(500);
            if (_receiveThread.IsAlive)
            {
                Debug.LogWarning("[EchoFace] UDP receive thread did not terminate gracefully.");
            }
        }

        _udpClient = null;
        _receiveThread = null;
    }
}
