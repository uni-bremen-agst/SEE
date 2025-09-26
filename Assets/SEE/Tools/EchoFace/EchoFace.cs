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
    public const string LeftEyeInnerCorner = "362";
    public const string RightEyeInnerCorner = "133";
    public const string LeftEyeOuterCorner = "263";
    public const string RightEyeOuterCorner = "33";
    public const string LeftIris = "473";
    public const string RightIris = "468";
}

/// <summary>
/// Represents the structure of the incoming JSON data.
/// </summary>
[Serializable]
public class FaceData
{
    // A nested class to represent the 'x', 'y', 'z' landmark coordinates
    [Serializable]
    public class LandmarkCoordinates
    {
        public float x;
        public float y;
        public float z;
    }

    public Dictionary<string, float> blendshapes;
    public Dictionary<string, LandmarkCoordinates> landmarks;
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

    [Tooltip("Smoothing factor for general blendshapes. Lower is smoother.")]
    [Range(0.01f, 1.0f)]
    [SerializeField]
    private float smoothingFactor = 0.5f;

    [Tooltip("Smoothing factor specifically for viseme blendshapes.")]
    [Range(0.01f, 1.0f)]
    [SerializeField]
    private float visemeSmoothingFactor = 0.9f;

    [Tooltip("Power curve for eye squint expression, to make it more pronounced.")]
    [Range(0f, 5f)]
    [SerializeField]
    private float eyeSquintPower = 3f;

    [Tooltip("Adjust the default mouth-down offset for the avatar's neutral pose.")]
    [Range(0f, 1.0f)]
    [SerializeField]
    private float mouthDownValue = 0.7f;

    [Header("Head Rotation Settings")]
    [Tooltip("Enable head rotation based on landmarks.")]
    public bool enableHeadRotation = true;

    [Tooltip("The Transform of the head bone to rotate.")]
    [SerializeField]
    private Transform headTransform;

    [Tooltip("The smoothing factor for head rotation.")]
    [Range(0.01f, 1.0f)]
    [SerializeField]
    private float rotationSmoothingFactor = 0.5f;

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
    private float eyeRotationSmoothingFactor = 0.5f;

    [Tooltip("The unified scaling factor for all eye movements (up, down, side-to-side).")]
    [Range(1f, 100f)]
    [SerializeField]
    private float eyeLookScale = 30f;


    //-------------------------------------------------
    // Private Fields
    //-------------------------------------------------

    private UdpClient _udpClient;
    private Thread _receiveThread;
    private bool _isRunning = false;

    // A thread-safe queue to pass data from the network thread to the main thread
    private readonly ConcurrentQueue<FaceData> _faceDataQueue = new();

    // Stores the latest face data to be used by LateUpdate
    private FaceData _latestFaceData;

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
                + (1f - arkit["jawOpen"]) * 0.15f
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
                + (1f - arkit["jawOpen"]) * 0.4f
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
                + (1f - arkit["jawOpen"]) * 0.3f
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

    private readonly Dictionary<string, float> _residualBlendshapeFactors =
    new()
    {
        { "Mouth_Up_Upper_L", 0.3f },
        { "Mouth_Up_Upper_R", 0.3f },
        { "Mouth_Down_Lower_L", 0.2f },
        { "Mouth_Down_Lower_R", 0.2f },
        { "Mouth_Funnel_Up_L", 0.1f },
        { "Mouth_Funnel_Up_R", 0.1f },
        { "Mouth_Funnel_Down_L", 0.1f },
        { "Mouth_Funnel_Down_R", 0.1f },
        { "Mouth_Pucker_Up_L", 0.8f },
        { "Mouth_Pucker_Up_R", 0.8f },
        { "Mouth_Pucker_Down_L", 0.8f },
        { "Mouth_Pucker_Down_R", 0.8f },
    };

    //-------------------------------------------------
    // Unity Lifecycle Methods
    //-------------------------------------------------

    private void Start()
    {
        // Attempt to auto-assign skinnedMeshRenderer if not set in Inspector
        if (skinnedMeshRenderer == null)
        {
            skinnedMeshRenderer = transform.Find("CC_Base_Body")?.GetComponent<SkinnedMeshRenderer>();
            if (skinnedMeshRenderer == null)
            {
                Debug.LogWarning("SkinnedMeshRenderer not found. Please assign it manually.");
            }
        }

        // Attempt to auto-assign headTransform
        if (headTransform == null)
        {
            headTransform = FindDeepChild(transform, "CC_Base_Head");
            if (headTransform == null)
            {
                Debug.LogWarning("Head bone transform not found. Head rotation will be disabled.");
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

    private void Update()
    {
        // Dequeue data in Update. Store the latest data for LateUpdate.
        if (_faceDataQueue.TryDequeue(out var receivedData))
        {
            _latestFaceData = receivedData;
        }
    }

    private void LateUpdate()
    {
        // Apply blendshapes and head/eye rotation in LateUpdate after all animations have been processed.
        if (_latestFaceData == null)
        {
            return;
        }

        // Apply blendshapes
        if (enableFaceAnimation && skinnedMeshRenderer != null && _latestFaceData.blendshapes != null)
        {
            ApplyBlendshapes(_latestFaceData.blendshapes);
        }

        // Estimate and apply head pose
        if (enableHeadRotation && headTransform != null && _latestFaceData.landmarks != null && _latestFaceData.landmarks.Count >= 3)
        {
            Quaternion targetRotation = EstimateHeadPose(_latestFaceData.landmarks);
            ApplyHeadPose(targetRotation);
        }

        // Apply eye rotation
        if (enableEyeRotation)
        {
            ApplyEyeRotation();
        }
    }

    private void OnApplicationQuit() => Shutdown();
    private void OnDestroy() => Shutdown();

    //-------------------------------------------------
    // Private Methods
    //-------------------------------------------------

    /// <summary>
    /// Converts MediaPipe landmark coordinates to a Unity Vector3.
    /// MediaPipe's coordinate system is different from Unity's, so the axes are flipped.
    /// </summary>
    /// <param name="coords">The landmark coordinates from the JSON data.</param>
    /// <returns>A new Vector3 suitable for use in Unity's world space.</returns>
    private Vector3 ToUnityVector3(FaceData.LandmarkCoordinates coords)
    {
        return new Vector3(-coords.x, -coords.y, -coords.z);
    }

    /// <summary>
    /// Applies blendshape weights with smoothing.
    /// </summary>
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

                // Version 1 (more subtle):
                // - Apply the eyeSquintPower to exaggerate stronger squints
                // - Scale the browDown contribution linearly with the powered squint
                value = Mathf.Pow(value, eyeSquintPower);
                value = Mathf.Clamp01(value + value * browDownValue);

                // Alternative Version (more expressive):
                // float brow = Mathf.Pow(browDownValue * value, eyeSquintPower);
                // value = Mathf.Clamp01(value + brow);
            }

            foreach (var name in customNames)
                targetBlendshapeValues[name] = Mathf.Max(
                    targetBlendshapeValues.GetValueOrDefault(name, 0f),
                    value
                );
        }

        // 2. Synthesize and Apply Visemes
        foreach (var visemeKvp in _visemeSynthesisMap)
        {
            targetBlendshapeValues[visemeKvp.Key] = visemeKvp.Value(blendshapes);
        }

        // Add slight mouth down since the model's neutral is a bit too up
        targetBlendshapeValues["Mouth_Down"] = mouthDownValue;

        // 3. Smooth and Set Blendshape Weights
        foreach (var kvp in targetBlendshapeValues)
        {
            // Get the index from the cache. If it doesn't exist, we can't set the weight.
            if (!_blendshapeIndexCache.TryGetValue(kvp.Key, out int index))
            {
                continue;
            }

            float targetValue = kvp.Value;

            // Get the current value to smooth from.
            _currentBlendshapeValues.TryGetValue(kvp.Key, out float currentValue);

            // Check if the blendshape is in the new residual factors dictionary.
            if (_residualBlendshapeFactors.TryGetValue(kvp.Key, out float individualScale))
            {
                targetValue *= individualScale;
            }

            float smoothingFactorToUse = _visemeSynthesisMap.ContainsKey(kvp.Key)
                ? visemeSmoothingFactor
                : smoothingFactor;
            float alpha = 1f - Mathf.Exp(-smoothingFactorToUse * Time.deltaTime * 60f);
            float smoothedValue = Mathf.Lerp(currentValue, targetValue, alpha);

            skinnedMeshRenderer.SetBlendShapeWeight(index, smoothedValue * 100f);
            _currentBlendshapeValues[kvp.Key] = smoothedValue;
        }
    }

    private Quaternion EstimateHeadPose(Dictionary<string, FaceData.LandmarkCoordinates> landmarks)
    {
        // Ensure the required landmarks exist using named constants.
        if (!landmarks.ContainsKey(Landmarks.Chin) ||
            !landmarks.ContainsKey(Landmarks.LeftUpperEyelid) ||
            !landmarks.ContainsKey(Landmarks.RightUpperEyelid))
        {
            Debug.LogWarning("Required landmarks for head pose not found in the received data.");
            return _currentHeadRotation;
        }

        Vector3 chin = ToUnityVector3(landmarks[Landmarks.Chin]);
        Vector3 leftEyeInner = ToUnityVector3(landmarks[Landmarks.LeftUpperEyelid]);
        Vector3 rightEyeInner = ToUnityVector3(landmarks[Landmarks.RightUpperEyelid]);

        // Calculate a vector representing the direction of the face's "up."
        Vector3 eyeMidpoint = (leftEyeInner + rightEyeInner) * 0.5f;
        Vector3 upVector = (eyeMidpoint - chin).normalized;

        // Calculate a vector representing the direction of the face's "right."
        // This vector points from the right eye to the left eye.
        Vector3 rightVector = (leftEyeInner - rightEyeInner).normalized;

        // The "forward" vector is perpendicular to both "up" and "right".
        // The cross product is ordered (up, right) for a right-handed coordinate system.
        Vector3 forwardVector = Vector3.Cross(upVector, rightVector).normalized;

        // Create the final rotation from the calculated vectors.
        Quaternion targetRotation = Quaternion.LookRotation(forwardVector, upVector);

        // Apply a manual pitch correction for the camera's tilt.
        Quaternion correction = Quaternion.Euler(tiltCorrection, 0, 0);
        targetRotation = targetRotation * correction;

        return targetRotation;
    }

    /// <summary>
    /// Rotates the eye bones based on blendshape-driven look directions.
    /// </summary>
    private void ApplyEyeRotation()
    {
        if (leftEyeTransform == null || rightEyeTransform == null || _latestFaceData?.blendshapes == null)
            return;

        var blendshapes = _latestFaceData.blendshapes;
        float pitchLeft = 0f;
        float yawLeft = 0f;
        float pitchRight = 0f;
        float yawRight = 0f;

        // Pitch (up/down)
        pitchLeft -= blendshapes.GetValueOrDefault("eyeLookUpLeft") * eyeLookScale;
        pitchLeft += blendshapes.GetValueOrDefault("eyeLookDownLeft") * eyeLookScale;
        pitchLeft -= tiltCorrection * 0.5f;

        pitchRight -= blendshapes.GetValueOrDefault("eyeLookUpRight") * eyeLookScale;
        pitchRight += blendshapes.GetValueOrDefault("eyeLookDownRight") * eyeLookScale;
        pitchRight -= tiltCorrection * 0.5f;

        // Yaw (left/right)
        yawLeft -= blendshapes.GetValueOrDefault("eyeLookOutLeft") * eyeLookScale;
        yawLeft += blendshapes.GetValueOrDefault("eyeLookInLeft") * eyeLookScale;

        yawRight += blendshapes.GetValueOrDefault("eyeLookOutRight") * eyeLookScale;
        yawRight -= blendshapes.GetValueOrDefault("eyeLookInRight") * eyeLookScale;

        // Target rotations, with Z-axis fixed at 0
        Quaternion targetLeftRotation = Quaternion.Euler(pitchLeft, 0, yawLeft);
        Quaternion targetRightRotation = Quaternion.Euler(pitchRight, 0, yawRight);

        // Smooth interpolation
        float alpha = 1f - Mathf.Exp(-eyeRotationSmoothingFactor * Time.deltaTime * 60f);
        _currentLeftEyeRotation = Quaternion.Slerp(_currentLeftEyeRotation, targetLeftRotation, alpha);
        _currentRightEyeRotation = Quaternion.Slerp(_currentRightEyeRotation, targetRightRotation, alpha);

        // Apply relative to rest rotations
        leftEyeTransform.localRotation = _leftEyeRestRotation * _currentLeftEyeRotation;
        rightEyeTransform.localRotation = _rightEyeRestRotation * _currentRightEyeRotation;
    }

    /// <summary>
    /// Applies the calculated head pose to the head bone with smoothing.
    /// </summary>
    /// <param name="targetRotation">The target rotation calculated by EstimateHeadPose.</param>
    private void ApplyHeadPose(Quaternion targetRotation)
    {
        if (headTransform == null) return;

        float alpha = 1f - Mathf.Exp(-rotationSmoothingFactor * Time.deltaTime * 60f);
        _currentHeadRotation = Quaternion.Slerp(_currentHeadRotation, targetRotation, alpha);

        // Apply the smoothed rotation to the head transform.
        headTransform.localRotation = _currentHeadRotation;
    }

    /// <summary>
    /// Finds the left and right eye bones by recursively searching under the head transform.
    /// </summary>
    private void FindEyeBones(Transform head)
    {
        if (head == null) return;

        // Use the recursive search method to find the eyes
        leftEyeTransform = FindDeepChild(head, "CC_Base_L_Eye");
        rightEyeTransform = FindDeepChild(head, "CC_Base_R_Eye");

        if (leftEyeTransform == null || rightEyeTransform == null)
        {
            Debug.LogWarning("Eye bone transforms not found. Eye rotation will be disabled.");
        }
        else
        {
            // Cache the initial local rotation of each eye bone
            _leftEyeRestRotation = leftEyeTransform.localRotation;
            _rightEyeRestRotation = rightEyeTransform.localRotation;
        }
    }

    /// <summary>
    /// Recursively finds a child transform by name.
    /// </summary>
    private Transform FindDeepChild(Transform parent, string name)
    {
        // First, check direct children
        var directChild = parent.Find(name);
        if (directChild != null)
        {
            return directChild;
        }

        // If not found, recursively search grand-children and beyond
        foreach (Transform child in parent)
        {
            var found = FindDeepChild(child, name);
            if (found != null)
            {
                return found;
        }
        }

        // Not found in this branch
        return null;
    }

    /// <summary>
    /// Initializes the UDP client and starts the receive thread.
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
                Name = "UDPEchoFace"
            };
            _receiveThread.Start();
            Debug.Log($"UDP listener started on port {port}.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"UDP listener failed to start on port {port}: {ex.Message}");
        }
    }

    /// <summary>
    /// Threaded loop to receive data from the UDP client.
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

                // Deserialize the JSON into the new FaceData class
                var receivedData = JsonConvert.DeserializeObject<FaceData>(json);
                if (receivedData != null)
                {
                    while (_faceDataQueue.TryDequeue(out _)) { }
                    _faceDataQueue.Enqueue(receivedData);
                }
            }
            catch (SocketException ex) when (ex.ErrorCode == 10004)
            {
                if (_isRunning)
                    Debug.LogWarning("UDP socket interrupted (normal shutdown).");
            }
            catch (ObjectDisposedException)
            {
                if (_isRunning)
                    Debug.LogWarning("UDP client disposed (normal shutdown).");
            }
            catch (Exception ex)
            {
                if (_isRunning)
                    Debug.LogError($"UDP receive error: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Caches blendshape name-to-index mappings for faster lookup.
    /// </summary>
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
                Debug.LogWarning($"Blendshape '{name}' not found on the mesh.");
            }
        }
    }

    /// <summary>
    /// Shuts down the UDP client and the receiving thread.
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
                Debug.LogWarning("UDP receive thread did not terminate gracefully.");
            }
        }

        _udpClient = null;
        _receiveThread = null;
    }
}
