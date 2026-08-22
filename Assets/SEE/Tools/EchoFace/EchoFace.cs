using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Contains components for real-time facial animation driven by externally
/// provided MediaPipe/ARKit-style tracking data.
/// </summary>
namespace SEE.Tools.EchoFace
{
    /// <summary>
    /// Provides named constants for MediaPipe facial landmark indices used
    /// to identify specific points on the face (e.g., chin, eyelids) for
    /// head-pose estimation.
    /// </summary>
    internal static class Landmarks
    {
        /// <summary>
        /// The landmark index for the chin.
        /// </summary>
        internal const string Chin = "152";

        /// <summary>
        /// The landmark index for the left upper eyelid.
        /// </summary>
        internal const string LeftUpperEyelid = "446";

        /// <summary>
        /// The landmark index for the right upper eyelid.
        /// </summary>
        internal const string RightUpperEyelid = "226";
    }

    /// <summary>
    /// Applies externally provided facial tracking data to a character.
    /// This component maps MediaPipe/ARKit-style blendshapes to custom
    /// blendshapes, synthesizes visemes for speech, and smoothly
    /// interpolates all values.
    /// </summary>
    /// <remarks>
    /// This component is intended to be attached to a character prefab
    /// containing a <see cref="SkinnedMeshRenderer"/> with the expected
    /// custom blendshape names, along with a head bone and two eye bones
    /// following the naming convention used by <see cref="FindDeepChild"/>.
    /// </remarks>
    internal class EchoFace : MonoBehaviour
    {
        //-------------------------------------------------
        // Public Fields
        //-------------------------------------------------

        [Header("Avatar Settings")]
        /// <summary>
        /// The skinned mesh renderer whose blendshapes are driven by this component.
        /// If not assigned in the Inspector, an attempt is made to auto-assign it
        /// from a child named "CC_Base_Body" during <see cref="Start"/>.
        /// </summary>
        [SerializeField]
        private SkinnedMeshRenderer skinnedMeshRenderer;

        [Header("Face Animation Settings")]
        /// <summary>
        /// Whether blendshape-driven face animation is applied at all.
        /// </summary>
        [Tooltip("Enable all face animation based on blendshapes.")]
        [SerializeField]
        private bool enableFaceAnimation = true;

        /// <summary>
        /// Whether synthesized viseme blendshapes (<c>V_*</c>) are computed and
        /// applied. Also enables scaling for related mouth blendshapes such as
        /// <c>Mouth_Funnel*</c> and <c>Mouth_Pucker*</c>.
        /// </summary>
        [Tooltip("Enables synthesized visemes (V_*). Also activates specific scaling for viseme-related blendshapes such as Mouth_Funnel* and Mouth_Pucker*.")]
        [SerializeField]
        private bool enableVisemeSynthesis = true;

        /// <summary>
        /// The exponential smoothing rate applied to non-viseme blendshapes.
        /// Lower values produce smoother, slower transitions.
        /// </summary>
        [Tooltip("Smoothing rate for general blendshapes. Lower is smoother.")]
        [Range(0.01f, 1.0f)]
        [SerializeField]
        private float smoothingRate = 0.5f;

        /// <summary>
        /// The exponential smoothing rate applied to viseme blendshapes.
        /// </summary>
        [Tooltip("Smoothing rate specifically for viseme blendshapes.")]
        [Range(0.01f, 1.0f)]
        [SerializeField]
        private float visemeSmoothingRate = 0.9f;

        /// <summary>
        /// The exponent applied to the eye-squint blendshape value to make
        /// stronger squints more pronounced.
        /// </summary>
        /// <remarks>
        /// The optimal value is around 3; higher values were found in practice
        /// to exaggerate the effect further.
        /// </remarks>
        [Tooltip("Power curve for eye squint expression, to make it more pronounced.")]
        [Range(0f, 12f)]
        [SerializeField]
        private float eyeSquintPower = 12f;

        [Header("Head Rotation Settings")]
        /// <summary>
        /// Whether the head bone is rotated based on estimated landmark data.
        /// </summary>
        [Tooltip("Enable head rotation based on landmarks.")]
        [SerializeField]
        private bool enableHeadRotation = true;

        /// <summary>
        /// The transform of the head bone to rotate. If not assigned in the
        /// Inspector, an attempt is made to auto-assign it from a descendant
        /// named "CC_Base_Head" during <see cref="Start"/>. If it cannot be
        /// found, head rotation is disabled.
        /// </summary>
        [Tooltip("The Transform of the head bone to rotate.")]
        [SerializeField]
        private Transform headTransform;

        /// <summary>
        /// The exponential smoothing rate applied to head rotation.
        /// </summary>
        [Tooltip("The smoothing rate for head rotation.")]
        [Range(0.01f, 1.0f)]
        [SerializeField]
        private float rotationSmoothingRate = 0.5f;

        /// <summary>
        /// A manual pitch correction, in degrees, applied to the estimated head
        /// rotation to compensate for the webcam's viewing angle. Also used to
        /// offset the vertical eye-look rotation.
        /// </summary>
        [Tooltip("Manual pitch correction to align the avatar with the webcam feed.")]
        [Range(-50f, 50f)]
        [SerializeField]
        private float tiltCorrection = -15.0f;

        [Header("Eye Rotation Settings")]
        /// <summary>
        /// Whether the eye bones are rotated based on eye-look blendshapes.
        /// </summary>
        [Tooltip("Enable eye rotation based on blendshapes.")]
        [SerializeField]
        private bool enableEyeRotation = true;

        /// <summary>
        /// The transform of the left eye bone. If not assigned in the
        /// Inspector, it is auto-assigned via <see cref="FindEyeBones"/>
        /// once <see cref="headTransform"/> has been resolved.
        /// </summary>
        [Tooltip("The Transform of the left eye bone.")]
        [SerializeField]
        private Transform leftEyeTransform;

        /// <summary>
        /// The transform of the right eye bone. If not assigned in the
        /// Inspector, it is auto-assigned via <see cref="FindEyeBones"/>
        /// once <see cref="headTransform"/> has been resolved.
        /// </summary>
        [Tooltip("The Transform of the right eye bone.")]
        [SerializeField]
        private Transform rightEyeTransform;

        /// <summary>
        /// The exponential smoothing rate applied to eye rotation.
        /// </summary>
        [Tooltip("The smoothing factor for eye rotation.")]
        [Range(0.01f, 1.0f)]
        [SerializeField]
        private float eyeRotationSmoothingRate = 0.5f;

        /// <summary>
        /// The unified scaling factor, in degrees, applied to all eye-look
        /// blendshape values (up, down, left, right) to derive eye rotation angles.
        /// </summary>
        [Tooltip("The unified scaling factor for all eye movements (up, down, side-to-side).")]
        [Range(1f, 100f)]
        [SerializeField]
        private float eyeLookScale = 30f;

        //-------------------------------------------------
        // Private Fields
        //-------------------------------------------------

        /// <summary>
        /// Stores the most recently received face data, to be applied during
        /// the next <see cref="LateUpdate"/> call. May be <c>null</c> before
        /// the first call to <see cref="SetFaceData"/>.
        /// </summary>
        private FaceData latestFaceData;

        /// <summary>
        /// Stores the current, already-smoothed blendshape values, keyed by
        /// custom blendshape name, used as the starting point for the next
        /// frame's smoothing calculation.
        /// </summary>
        private readonly Dictionary<string, float> currentBlendshapeValues = new();

        /// <summary>
        /// Caches the mesh blendshape index for each custom blendshape name,
        /// to avoid repeated lookups on <see cref="skinnedMeshRenderer"/>'s
        /// mesh every frame. Populated once in <see cref="CacheBlendshapeIndices"/>.
        /// </summary>
        private readonly Dictionary<string, int> blendshapeIndexCache = new();

        /// <summary>
        /// The current, already-smoothed head rotation, used as the starting
        /// point for the next frame's smoothing calculation.
        /// </summary>
        private Quaternion currentHeadRotation = Quaternion.identity;

        /// <summary>
        /// The current, already-smoothed left eye rotation (relative to
        /// <see cref="leftEyeRestRotation"/>), used as the starting point for
        /// the next frame's smoothing calculation.
        /// </summary>
        private Quaternion currentLeftEyeRotation = Quaternion.identity;

        /// <summary>
        /// The current, already-smoothed right eye rotation (relative to
        /// <see cref="rightEyeRestRotation"/>), used as the starting point for
        /// the next frame's smoothing calculation.
        /// </summary>
        private Quaternion currentRightEyeRotation = Quaternion.identity;

        /// <summary>
        /// The local rotation of <see cref="leftEyeTransform"/> at the time it
        /// was resolved, used as the rest pose that eye-look rotations are
        /// applied on top of.
        /// </summary>
        private Quaternion leftEyeRestRotation;

        /// <summary>
        /// The local rotation of <see cref="rightEyeTransform"/> at the time it
        /// was resolved, used as the rest pose that eye-look rotations are
        /// applied on top of.
        /// </summary>
        private Quaternion rightEyeRestRotation;

        /// <summary>
        /// Maps each synthesized viseme blendshape name (<c>V_*</c>) to a
        /// function that computes its weight, in the range [0, 1], from a set
        /// of ARKit-style blendshape values.
        /// </summary>
        private readonly Dictionary<string, Func<Dictionary<string, float>, float>> visemeSynthesisMap = new()
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
                    //+ (1f - arkit["jawOpen"]) * 0.2f
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
                    //+ (1f - arkit["jawOpen"]) * 0.3f
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

        /// <summary>
        /// Maps each MediaPipe/ARKit blendshape name to the list of custom
        /// blendshape names on <see cref="skinnedMeshRenderer"/>'s mesh that it
        /// should drive.
        /// </summary>
        private readonly Dictionary<string, List<string>> mediapipeToCustomMap = new()
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

        /// <summary>
        /// Base scaling factors, keyed by custom blendshape name, that are
        /// always applied when setting the corresponding blendshape weight.
        /// Used for subtle shaping or expression tuning.
        /// </summary>
        private readonly Dictionary<string, float> baseBlendshapeScales = new()
        {
            { "Mouth_Up_Upper_L", 0.6f },
            { "Mouth_Up_Upper_R", 0.6f },
        };

        /// <summary>
        /// Scaling factors, keyed by custom blendshape name, that are applied
        /// only when <see cref="enableVisemeSynthesis"/> is <c>true</c>. These
        /// primarily target phoneme-related blendshapes to avoid excessive
        /// deformation when the model is driven by procedural viseme data.
        /// </summary>
        private readonly Dictionary<string, float> visemeBlendshapeScales = new()
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

        /// <summary>
        /// Unity lifecycle method. Auto-assigns <see cref="skinnedMeshRenderer"/>
        /// and <see cref="headTransform"/> if they were not set in the
        /// Inspector, resolves the eye bones via <see cref="FindEyeBones"/>,
        /// and caches blendshape indices via <see cref="CacheBlendshapeIndices"/>.
        /// </summary>
        private void Start()
        {
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
        }

        /// <summary>
        /// Unity lifecycle method. Applies the latest received face data
        /// (blendshapes, head rotation, and eye rotation) after all other
        /// animation has been processed for the frame. Does nothing if no
        /// face data has been received yet.
        /// </summary>
        private void LateUpdate()
        {
            // Apply blendshapes and head/eye rotation in LateUpdate after all animations have been processed.
            if (latestFaceData == null)
            {
                return;
            }

            // Apply blendshapes
            if (enableFaceAnimation && skinnedMeshRenderer != null)
            {
                ApplyBlendshapes(latestFaceData.Blendshapes);
            }

            // Estimate and apply head pose
            if (enableHeadRotation && headTransform != null)
            {
                Quaternion targetRotation = EstimateHeadRotation(latestFaceData.LandmarkPositions);
                ApplyHeadRotation(targetRotation);
            }

            // Apply eye rotation
            if (enableEyeRotation && leftEyeTransform != null && rightEyeTransform != null)
            {
                ApplyEyeRotation();
            }

            // latestFaceData = null; // IMPORTANT: Resetting the data will enable other components to manipulate the face causing jitter!
        }

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
            return new Vector3(-coords.X, -coords.Y, -coords.Z);
        }

        /// <summary>
        /// Applies blendshape weights with smoothing.
        /// </summary>
        /// <param name="blendshapes">
        /// The MediaPipe/ARKit blendshape values to apply, keyed by blendshape
        /// name. If <c>null</c>, the method returns without applying anything.
        /// </param>
        private void ApplyBlendshapes(Dictionary<string, float> blendshapes)
        {
            if (blendshapes == null)
            {
                return;
            }

            Dictionary<string, float> targetBlendshapeValues = new();

            // 1. Map MediaPipe to Custom Blendshapes and apply enhancements
            foreach (var kvp in blendshapes)
            {
                if (!mediapipeToCustomMap.TryGetValue(kvp.Key, out List<string> customNames))
                {
                    continue;
                }

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

                foreach (string name in customNames)
                {
                    targetBlendshapeValues[name] = Mathf.Max(
                        targetBlendshapeValues.GetValueOrDefault(name, 0f),
                        value
                    );
                }
            }

            // 2. Apply custom logic for specific blendshapes

            // Multiply upper lip lift by the smile intensity to drive Mouth_Down,
            // creating a counter-pull to hide the upper gums while smiling.
            targetBlendshapeValues["Mouth_Down"] = Mathf.Max(
                blendshapes.GetValueOrDefault("mouthUpperUpLeft", 0f),
                blendshapes.GetValueOrDefault("mouthUpperUpRight", 0f)
            ) * Mathf.Max(
                blendshapes.GetValueOrDefault("mouthSmileLeft", 0f),
                blendshapes.GetValueOrDefault("mouthSmileRight", 0f)
            );

            // Damp the lower lip's downward movement proportionally to 'jawOpen' to prevent the lip
            // from drooping and exposing the lower gums when the mouth is wide open.
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
                foreach (var visemeKvp in visemeSynthesisMap)
                {
                    targetBlendshapeValues[visemeKvp.Key] = visemeKvp.Value(blendshapes);
                }
            }
            else
            {
                // Zero out any viseme blendshapes if synthesis is disabled
                foreach (string visemeKey in visemeSynthesisMap.Keys)
                {
                    if (currentBlendshapeValues.TryGetValue(visemeKey, out float currentValue)
                        && currentValue > 0.0001f)
                    {
                        targetBlendshapeValues[visemeKey] = 0f;
                    }
                }
            }

            // 4. Smooth and Set Blendshape Weights
            foreach (var kvp in targetBlendshapeValues)
            {
                // Get the index from the cache. If it doesn't exist, we can't set the weight.
                if (!blendshapeIndexCache.TryGetValue(kvp.Key, out int index))
                {
                    continue;
                }

                float targetValue = kvp.Value;
                // Get the current value to smooth from.
                currentBlendshapeValues.TryGetValue(kvp.Key, out float currentValue);

                // Apply base scaling
                if (baseBlendshapeScales.TryGetValue(kvp.Key, out float baseScale))
                {
                    targetValue *= baseScale;
                }

                // Apply viseme-specific scaling only if viseme synthesis is enabled
                if (enableVisemeSynthesis &&
                    visemeBlendshapeScales.TryGetValue(kvp.Key, out float visemeScale))
                {
                    targetValue *= visemeScale;
                }

                float smoothingRateToUse =
                    (enableVisemeSynthesis && visemeSynthesisMap.ContainsKey(kvp.Key))
                        ? visemeSmoothingRate
                        : smoothingRate;

                // Smooth transition using exponential smoothing
                float alpha = 1f - Mathf.Exp(-smoothingRateToUse * Time.deltaTime * 60f);
                float smoothedValue = Mathf.Lerp(currentValue, targetValue, alpha);

                skinnedMeshRenderer.SetBlendShapeWeight(index, smoothedValue * 100f);
                currentBlendshapeValues[kvp.Key] = smoothedValue;
            }
        }

        /// <summary>
        /// Estimates the head's rotation from a set of facial landmarks by
        /// constructing an orthonormal basis from the chin and eyelid
        /// positions, then applying a manual pitch correction.
        /// </summary>
        /// <param name="landmarks">
        /// The landmark positions, keyed by landmark index (see
        /// <see cref="Landmarks"/>). Must contain at least the chin and both
        /// upper eyelid landmarks; otherwise the previously computed rotation
        /// is returned unchanged.
        /// </param>
        /// <returns>
        /// The estimated head rotation, or the current head rotation if the
        /// required landmarks are missing.
        /// </returns>
        private Quaternion EstimateHeadRotation(Dictionary<string, FaceData.LandmarkCoordinates> landmarks)
        {
            // Ensure the required landmarks exist using named constants.
            if (landmarks == null || landmarks.Count < 3)
            {
                Debug.LogWarning("[EchoFace] Required landmarks for head pose not found in the received data.");
                return currentHeadRotation;
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
            if (latestFaceData?.Blendshapes == null)
            {
                return;
            }

            Dictionary<string, float> blendshapes = latestFaceData.Blendshapes;
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
            float alpha = 1f - Mathf.Exp(-eyeRotationSmoothingRate * Time.deltaTime * 60f);
            currentLeftEyeRotation = Quaternion.Slerp(currentLeftEyeRotation, targetLeftRotation, alpha);
            currentRightEyeRotation = Quaternion.Slerp(currentRightEyeRotation, targetRightRotation, alpha);

            // Apply relative to rest rotations
            leftEyeTransform.localRotation = leftEyeRestRotation * currentLeftEyeRotation;
            rightEyeTransform.localRotation = rightEyeRestRotation * currentRightEyeRotation;
        }

        /// <summary>
        /// Applies the calculated head pose to the head bone with smoothing.
        /// </summary>
        /// <param name="targetRotation">The target rotation calculated by EstimateHeadRotation.</param>
        private void ApplyHeadRotation(Quaternion targetRotation)
        {
            if (headTransform == null)
            {
                return;
            }

            float alpha = 1f - Mathf.Exp(-rotationSmoothingRate * Time.deltaTime * 60f);
            currentHeadRotation = Quaternion.Slerp(currentHeadRotation, targetRotation, alpha);

            // Apply the smoothed rotation to the head transform.
            headTransform.localRotation = currentHeadRotation;
        }

        /// <summary>
        /// Finds the left and right eye bones by recursively searching under the head transform.
        /// </summary>
        /// <param name="head">
        /// The head transform to search under. If <c>null</c>, the method
        /// returns without doing anything.
        /// </param>
        private void FindEyeBones(Transform head)
        {
            if (head == null)
            {
                return;
            }

            // Use the recursive search method to find the eyes
            leftEyeTransform = FindDeepChild(head, "CC_Base_L_Eye");
            rightEyeTransform = FindDeepChild(head, "CC_Base_R_Eye");

            if (leftEyeTransform == null || rightEyeTransform == null)
            {
                Debug.LogWarning("[EchoFace] Eye bone transforms not found. Eye rotation will be disabled.");
            }
            else
            {
                // Cache the initial local rotation of each eye bone
                leftEyeRestRotation = leftEyeTransform.localRotation;
                rightEyeRestRotation = rightEyeTransform.localRotation;
            }
        }

        /// <summary>
        /// Recursively finds a child transform by name.
        /// </summary>
        /// <param name="parent">The transform whose descendants are searched.</param>
        /// <param name="name">The name of the child transform to find.</param>
        /// <returns>
        /// The first matching descendant transform, searched breadth-first
        /// among direct children before recursing; or <c>null</c> if no
        /// matching descendant exists.
        /// </returns>
        private Transform FindDeepChild(Transform parent, string name)
        {
            // First, check direct children
            Transform directChild = parent.Find(name);
            if (directChild != null)
            {
                return directChild;
            }

            // If not found, recursively search grand-children and beyond
            foreach (Transform child in parent)
            {
                Transform found = FindDeepChild(child, name);
                if (found != null)
                {
                    return found;
                }
            }

            // Not found in this branch
            return null;
        }

        /// <summary>
        /// Caches blendshape name-to-index mappings for faster lookup.
        /// </summary>
        private void CacheBlendshapeIndices()
        {
            blendshapeIndexCache.Clear();
            Mesh mesh = skinnedMeshRenderer.sharedMesh;
            if (mesh == null)
            {
                return;
            }

            List<string> allBlendshapeNames = mediapipeToCustomMap.Values
                    .SelectMany(x => x)
                    .Concat(visemeSynthesisMap.Keys)
                    .ToList();

            allBlendshapeNames.Add("Mouth_Down");
            allBlendshapeNames.Add("Mouth_Up");

            foreach (string name in allBlendshapeNames.Distinct())
            {
                int index = mesh.GetBlendShapeIndex(name);
                if (index >= 0)
                {
                    blendshapeIndexCache[name] = index;
                }
                else
                {
                    Debug.LogWarning($"[EchoFace] Blendshape '{name}' not found on the mesh.");
                }
            }
        }

        /// <summary>
        /// Receives externally provided face-tracking data (e.g., from UDP or other sources)
        /// and stores it as the latest frame to be applied during <c>LateUpdate</c>.
        /// </summary>
        /// <remarks>
        /// This method must be called from the Unity main thread only.
        /// </remarks>
        /// <param name="data">A complete FaceData frame containing blendshapes,
        /// landmarks, and timestamp information.</param>
        internal void SetFaceData(FaceData data)
        {
            latestFaceData = data;
        }
    }
}
