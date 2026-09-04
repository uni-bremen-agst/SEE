using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using SEE.Game.Avatars;

/// <summary>
/// Contains components and data structures for real-time facial animation driven
/// by externally provided MediaPipe/ARKit-style tracking data.
/// </summary>
namespace SEE.Tools.EchoFace
{
    /// <summary>
    /// Applies externally provided facial tracking data to a character.
    /// This component maps MediaPipe/ARKit-style blendshapes to custom
    /// blendshapes, synthesizes visemes for speech, and smoothly
    /// interpolates all values.
    /// </summary>
    /// <remarks>
    /// This component is intended to be attached to a character prefab
    /// containing a <see cref="SkinnedMeshRenderer"/> with the expected
    /// custom blendshape names, along with a head bone and two eye bones.
    /// Uses <see cref="FaceBlendshape"/> and <see cref="FaceLandmark"/> for
    /// zero-allocation, type-safe array indexing.
    /// </remarks>
    internal class EchoFace : MonoBehaviour
    {
        //-------------------------------------------------
        // Inspector Fields
        //-------------------------------------------------

        /// <summary>
        /// The skinned mesh renderer whose blendshapes are driven by this component.
        /// If not assigned in the Inspector, an attempt is made to auto-assign it
        /// from a child named "CC_Base_Body" during <see cref="Start"/>.
        /// </summary>
        [Header("Avatar")]
        [SerializeField]
        private SkinnedMeshRenderer skinnedMeshRenderer;

        /// <summary>
        /// Whether blendshape-driven face animation is applied at all.
        /// </summary>
        [Header("Face Animation")]
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
        /// The optimal value is around 3; higher values exaggerate the effect further.
        /// </remarks>
        [Tooltip("Power curve for eye squint expression, to make it more pronounced.")]
        [Range(0f, 12f)]
        [SerializeField]
        private float eyeSquintPower = 3f;

        /// <summary>
        /// Whether the head bone is rotated based on estimated landmark data.
        /// </summary>
        [Header("Head Rotation")]
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

        /// <summary>
        /// Whether the eye bones are rotated based on eye-look blendshapes.
        /// </summary>
        [Header("Eye Rotation")]
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
        /// Reusable buffer for calculating target blendshape weights each frame
        /// to avoid per-frame GC allocations.
        /// </summary>
        private readonly Dictionary<string, float> targetBlendshapeValues = new();

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
        private Quaternion leftEyeRestRotation = Quaternion.identity;

        /// <summary>
        /// The local rotation of <see cref="rightEyeTransform"/> at the time it
        /// was resolved, used as the rest pose that eye-look rotations are
        /// applied on top of.
        /// </summary>
        private Quaternion rightEyeRestRotation = Quaternion.identity;

        /// <summary>
        /// The local rotation of <see cref="headTransform"/> at the time it
        /// was resolved, used as the rest pose to restore on reset.
        /// </summary>
        private Quaternion headRestRotation = Quaternion.identity;

        /// <summary>
        /// Maps each synthesized viseme blendshape name (<c>V_*</c>) to a
        /// function that computes its weight, in the range [0, 1], from a
        /// <see cref="FaceData"/> instance via <see cref="FaceBlendshape"/> lookups.
        /// </summary>
        private readonly Dictionary<string, Func<FaceData, float>> visemeSynthesisMap = new()
        {
            {
                "V_Open",
                data =>
                Mathf.Clamp01(
                    data[FaceBlendshape.JawOpen] * 0.9f
                    + (data[FaceBlendshape.MouthUpperUpLeft] + data[FaceBlendshape.MouthUpperUpRight]) * 0.1f
                    + (data[FaceBlendshape.MouthLowerDownLeft] + data[FaceBlendshape.MouthLowerDownRight]) * 0.1f
                    + data[FaceBlendshape.MouthShrugLower] * 0.05f
                    - data[FaceBlendshape.MouthPucker] * 0.2f
                    - data[FaceBlendshape.MouthFunnel] * 0.1f
                )
            },
            {
                "V_Explosive",
                data =>
                Mathf.Clamp01(
                    Mathf.Max(data[FaceBlendshape.MouthPressLeft], data[FaceBlendshape.MouthPressRight]) * 0.7f
                    + data[FaceBlendshape.MouthPucker] * 0.4f
                    + data[FaceBlendshape.MouthClose] * 0.5f
                    + Mathf.Max(data[FaceBlendshape.MouthRollUpper], data[FaceBlendshape.MouthRollLower]) * 0.2f
                    + (1f - data[FaceBlendshape.JawOpen]) * 0.3f
                )
            },
            {
                "V_Dental_Lip",
                data =>
                Mathf.Clamp01(
                    (data[FaceBlendshape.MouthLowerDownLeft] + data[FaceBlendshape.MouthLowerDownRight]) * 0.4f
                    + data[FaceBlendshape.MouthRollLower] * 0.8f
                    + (data[FaceBlendshape.MouthUpperUpLeft] + data[FaceBlendshape.MouthUpperUpRight]) * 0.2f
                    + Mathf.Max(data[FaceBlendshape.NoseSneerLeft], data[FaceBlendshape.NoseSneerRight]) * 0.1f
                    + (1f - data[FaceBlendshape.JawOpen]) * 0.2f
                )
            },
            {
                "V_Tight_O",
                data =>
                Mathf.Clamp01(
                    data[FaceBlendshape.MouthPucker] * 0.7f
                    + data[FaceBlendshape.MouthFunnel] * 0.6f
                    + (1f - data[FaceBlendshape.JawOpen]) * 0.2f
                    + Mathf.Max(data[FaceBlendshape.MouthPressLeft], data[FaceBlendshape.MouthPressRight]) * 0.1f
                    - Mathf.Max(data[FaceBlendshape.MouthSmileLeft], data[FaceBlendshape.MouthSmileRight]) * 0.3f
                )
            },
            {
                "V_Tight",
                data =>
                Mathf.Clamp01(
                    Mathf.Max(data[FaceBlendshape.MouthPressLeft], data[FaceBlendshape.MouthPressRight]) * 0.7f
                    + data[FaceBlendshape.MouthClose] * 0.5f
                    + Mathf.Max(data[FaceBlendshape.MouthRollUpper], data[FaceBlendshape.MouthRollLower]) * 0.2f
                    + Mathf.Max(data[FaceBlendshape.MouthFrownLeft], data[FaceBlendshape.MouthFrownRight]) * 0.15f
                )
            },
            {
                "V_Wide",
                data =>
                Mathf.Clamp01(
                    (data[FaceBlendshape.MouthStretchLeft] + data[FaceBlendshape.MouthStretchRight]) * 0.3f
                    + (data[FaceBlendshape.MouthSmileLeft] + data[FaceBlendshape.MouthSmileRight]) * 0.3f
                    + data[FaceBlendshape.JawOpen] * 0.3f
                    + (data[FaceBlendshape.MouthDimpleLeft] + data[FaceBlendshape.MouthDimpleRight]) * 0.1f
                    - data[FaceBlendshape.MouthPucker] * 0.2f
                    - data[FaceBlendshape.MouthFunnel] * 0.2f
                )
            },
            {
                "V_Affricate",
                data =>
                Mathf.Clamp01(
                    data[FaceBlendshape.MouthFunnel] * 1.0f
                    + Mathf.Max(data[FaceBlendshape.MouthPressLeft], data[FaceBlendshape.MouthPressRight]) * 0.4f
                    + Mathf.Max(data[FaceBlendshape.MouthRollUpper], data[FaceBlendshape.MouthRollLower]) * 0.2f
                    + Mathf.Max(data[FaceBlendshape.MouthFrownLeft], data[FaceBlendshape.MouthFrownRight]) * 0.1f
                )
            },
            {
                "V_Lip_Open",
                data =>
                Mathf.Clamp01(
                    (data[FaceBlendshape.MouthUpperUpLeft] + data[FaceBlendshape.MouthUpperUpRight]) * 0.3f
                    + (data[FaceBlendshape.MouthLowerDownLeft] + data[FaceBlendshape.MouthLowerDownRight]) * 0.3f
                    + data[FaceBlendshape.MouthFunnel] * 0.6f
                    + data[FaceBlendshape.MouthPucker] * 0.4f
                    + data[FaceBlendshape.JawOpen] * 0.2f
                )
            }
        };

        /// <summary>
        /// Maps each tracked <see cref="FaceBlendshape"/> to the list of custom
        /// blendshape names on <see cref="skinnedMeshRenderer"/>'s mesh that it
        /// should drive.
        /// </summary>
        private readonly Dictionary<FaceBlendshape, List<string>> mediapipeToCustomMap = new()
        {
            { FaceBlendshape.BrowDownLeft, new() { "Brow_Drop_L" } },
            { FaceBlendshape.BrowDownRight, new() { "Brow_Drop_R" } },
            { FaceBlendshape.BrowInnerUp, new() { "Brow_Raise_Inner_L", "Brow_Raise_Inner_R" } },
            { FaceBlendshape.BrowOuterUpLeft, new() { "Brow_Raise_Outer_L" } },
            { FaceBlendshape.BrowOuterUpRight, new() { "Brow_Raise_Outer_R" } },
            { FaceBlendshape.CheekPuff, new() { "Cheek_Puff_L", "Cheek_Puff_R" } },
            { FaceBlendshape.CheekSquintLeft, new() { "Cheek_Raise_L" } },
            { FaceBlendshape.CheekSquintRight, new() { "Cheek_Raise_R" } },
            { FaceBlendshape.EyeBlinkLeft, new() { "Eye_Blink_L" } },
            { FaceBlendshape.EyeBlinkRight, new() { "Eye_Blink_R" } },
            { FaceBlendshape.EyeSquintLeft, new() { "Eye_Squint_L" } },
            { FaceBlendshape.EyeSquintRight, new() { "Eye_Squint_R" } },
            { FaceBlendshape.EyeWideLeft, new() { "Eye_Wide_L" } },
            { FaceBlendshape.EyeWideRight, new() { "Eye_Wide_R" } },
            { FaceBlendshape.EyeLookDownLeft, new() { "Eye_L_Look_Down" } },
            { FaceBlendshape.EyeLookDownRight, new() { "Eye_R_Look_Down" } },
            { FaceBlendshape.EyeLookUpLeft, new() { "Eye_L_Look_Up" } },
            { FaceBlendshape.EyeLookUpRight, new() { "Eye_R_Look_Up" } },
            { FaceBlendshape.EyeLookInLeft, new() { "Eye_L_Look_R" } },
            { FaceBlendshape.EyeLookInRight, new() { "Eye_R_Look_L" } },
            { FaceBlendshape.EyeLookOutLeft, new() { "Eye_L_Look_L" } },
            { FaceBlendshape.EyeLookOutRight, new() { "Eye_R_Look_R" } },
            { FaceBlendshape.JawForward, new() { "Jaw_Forward" } },
            { FaceBlendshape.JawLeft, new() { "Jaw_L" } },
            { FaceBlendshape.JawRight, new() { "Jaw_R" } },
            { FaceBlendshape.JawOpen, new() { "Merged_Open_Mouth" } },
            { FaceBlendshape.MouthClose, new() { "Mouth_Close" } },
            { FaceBlendshape.MouthDimpleLeft, new() { "Mouth_Dimple_L" } },
            { FaceBlendshape.MouthDimpleRight, new() { "Mouth_Dimple_R" } },
            { FaceBlendshape.MouthFrownLeft, new() { "Mouth_Frown_L" } },
            { FaceBlendshape.MouthFrownRight, new() { "Mouth_Frown_R" } },
            {
                FaceBlendshape.MouthFunnel,
                new()
                {
                    "Mouth_Funnel_Up_L",
                    "Mouth_Funnel_Up_R",
                    "Mouth_Funnel_Down_L",
                    "Mouth_Funnel_Down_R"
                }
            },
            { FaceBlendshape.MouthLeft, new() { "Mouth_L" } },
            { FaceBlendshape.MouthRight, new() { "Mouth_R" } },
            { FaceBlendshape.MouthLowerDownLeft, new() { "Mouth_Down_Lower_L" } },
            { FaceBlendshape.MouthLowerDownRight, new() { "Mouth_Down_Lower_R" } },
            { FaceBlendshape.MouthPressLeft, new() { "Mouth_Press_L" } },
            { FaceBlendshape.MouthPressRight, new() { "Mouth_Press_R" } },
            {
                FaceBlendshape.MouthPucker,
                new()
                {
                    "Mouth_Pucker_Up_L",
                    "Mouth_Pucker_Up_R",
                    "Mouth_Pucker_Down_L",
                    "Mouth_Pucker_Down_R"
                }
            },
            { FaceBlendshape.MouthRollLower, new() { "Mouth_Roll_In_Lower_L", "Mouth_Roll_In_Lower_R" } },
            { FaceBlendshape.MouthRollUpper, new() { "Mouth_Roll_In_Upper_L", "Mouth_Roll_In_Upper_R" } },
            { FaceBlendshape.MouthShrugLower, new() { "Mouth_Shrug_Lower" } },
            { FaceBlendshape.MouthShrugUpper, new() { "Mouth_Shrug_Upper" } },
            { FaceBlendshape.MouthSmileLeft, new() { "Mouth_Smile_L" } },
            { FaceBlendshape.MouthSmileRight, new() { "Mouth_Smile_R" } },
            { FaceBlendshape.MouthStretchLeft, new() { "Mouth_Stretch_L" } },
            { FaceBlendshape.MouthStretchRight, new() { "Mouth_Stretch_R" } },
            { FaceBlendshape.MouthUpperUpLeft, new() { "Mouth_Up_Upper_L" } },
            { FaceBlendshape.MouthUpperUpRight, new() { "Mouth_Up_Upper_R" } },
            { FaceBlendshape.NoseSneerLeft, new() { "Nose_Sneer_L" } },
            { FaceBlendshape.NoseSneerRight, new() { "Nose_Sneer_R" } }
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
                skinnedMeshRenderer = transform.Find(AvatarSceleton.BaseBody)?.GetComponent<SkinnedMeshRenderer>();
                if (skinnedMeshRenderer == null)
                {
                    Debug.LogWarning("[EchoFace] SkinnedMeshRenderer not found. Please assign it manually.\n");
                    return;
                }
            }

            // Attempt to auto-assign headTransform
            if (headTransform == null)
            {
                headTransform = transform.Find(AvatarSceleton.Head);
                if (headTransform == null)
                {
                    Debug.LogWarning("[EchoFace] Head bone transform not found. Head rotation will be disabled.\n");
                }
            }

            // Find eye bones if head is found
            if (headTransform != null)
            {
                headRestRotation = headTransform.localRotation;
                FindEyeBones(headTransform);
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
            if (latestFaceData == null)
            {
                return;
            }

            // Apply blendshapes
            if (enableFaceAnimation && skinnedMeshRenderer != null)
            {
                ApplyBlendshapes(latestFaceData);
            }

            // Estimate and apply head pose
            if (enableHeadRotation && headTransform != null)
            {
                Quaternion targetRotation = EstimateHeadRotation(latestFaceData);
                ApplyHeadRotation(targetRotation);
            }

            // Apply eye rotation
            if (enableEyeRotation && leftEyeTransform != null && rightEyeTransform != null)
            {
                ApplyEyeRotation(latestFaceData);
            }
        }

        /// <summary>
        /// Unity lifecycle method. Ensures all facial animations and tracking
        /// states are reset to their default rest pose when this component is disabled.
        /// </summary>
        private void OnDisable()
        {
            ResetToRestPose();
        }

        //-------------------------------------------------
        // Private Methods
        //-------------------------------------------------

        /// <summary>
        /// Converts MediaPipe landmark coordinates to a Unity Vector3.
        /// MediaPipe's coordinate system is different from Unity's, so the axes are flipped.
        /// </summary>
        /// <param name="coords">The landmark coordinates from the tracking data.</param>
        /// <returns>A new Vector3 suitable for use in Unity's local space.</returns>
        private Vector3 ToUnityVector3(FaceData.LandmarkCoordinates coords)
        {
            return new(-coords.X, -coords.Y, -coords.Z);
        }

        /// <summary>
        /// Applies blendshape weights with exponential smoothing.
        /// </summary>
        /// <param name="data">
        /// The face data containing raw blendshape weights.
        /// If <c>null</c> or unassigned, the method returns without applying anything.
        /// </param>
        private void ApplyBlendshapes(FaceData data)
        {
            if (data == null || data.Blendshapes == null)
            {
                return;
            }

            targetBlendshapeValues.Clear();

            // 1. Map MediaPipe to Custom Blendshapes and apply enhancements
            for (int i = 0; i < (int)FaceBlendshape.Count; i++)
            {
                FaceBlendshape shape = (FaceBlendshape)i;
                if (!mediapipeToCustomMap.TryGetValue(shape, out List<string> customNames))
                {
                    continue;
                }

                float value = data[shape];

                // Apply power curve to eyeSquint AND add the influence of browDown
                if (shape == FaceBlendshape.EyeSquintLeft || shape == FaceBlendshape.EyeSquintRight)
                {
                    float browDown = shape == FaceBlendshape.EyeSquintLeft
                        ? data[FaceBlendshape.BrowDownLeft]
                        : data[FaceBlendshape.BrowDownRight];

                    value = Mathf.Pow(value, eyeSquintPower);
                    value = Mathf.Clamp01(value + value * browDown);
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
                data[FaceBlendshape.MouthUpperUpLeft],
                data[FaceBlendshape.MouthUpperUpRight]
            ) * Mathf.Max(
                data[FaceBlendshape.MouthSmileLeft],
                data[FaceBlendshape.MouthSmileRight]
            );

            // Damp the lower lip's downward movement proportionally to 'jawOpen' to prevent the lip
            // from drooping and exposing the lower gums when the mouth is wide open.
            float jawOpen = data[FaceBlendshape.JawOpen];
            targetBlendshapeValues["Mouth_Down_Lower_L"] = Mathf.Clamp01(
                data[FaceBlendshape.MouthLowerDownLeft] * (1f - jawOpen)
            );

            targetBlendshapeValues["Mouth_Down_Lower_R"] = Mathf.Clamp01(
                data[FaceBlendshape.MouthLowerDownRight] * (1f - jawOpen)
            );

            // 3. Synthesize and Apply Visemes
            if (enableVisemeSynthesis)
            {
                foreach (var visemeKvp in visemeSynthesisMap)
                {
                    targetBlendshapeValues[visemeKvp.Key] = visemeKvp.Value(data);
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
        /// Estimates the head's rotation from facial landmarks in <see cref="FaceData"/>
        /// by constructing an orthonormal basis from the chin and eyelid positions.
        /// </summary>
        /// <param name="faceData">The latest frame's face data.</param>
        /// <returns>
        /// The estimated head rotation, or the current head rotation if landmarks are missing.
        /// </returns>
        private Quaternion EstimateHeadRotation(FaceData faceData)
        {
            if (faceData == null || !faceData.HasLandmarks)
            {
                return currentHeadRotation;
            }

            Vector3 chin = ToUnityVector3(faceData[FaceLandmark.Chin]);
            Vector3 leftEyeInner = ToUnityVector3(faceData[FaceLandmark.LeftUpperEyelid]);
            Vector3 rightEyeInner = ToUnityVector3(faceData[FaceLandmark.RightUpperEyelid]);

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
            targetRotation *= correction;

            return targetRotation;
        }

        /// <summary>
        /// Rotates the eye bones based on blendshape-driven look directions.
        /// Yaw rotations are aligned so that both eyes rotate parallel in the gaze direction.
        /// </summary>
        /// <param name="data">The face data containing raw blendshape weights.</param>
        private void ApplyEyeRotation(FaceData data)
        {
            if (data == null || data.Blendshapes == null)
            {
                return;
            }

            float pitchLeft = 0f;
            float yawLeft = 0f;
            float pitchRight = 0f;
            float yawRight = 0f;

            // Pitch (up/down)
            pitchLeft -= data[FaceBlendshape.EyeLookUpLeft] * eyeLookScale;
            pitchLeft += data[FaceBlendshape.EyeLookDownLeft] * eyeLookScale;
            pitchLeft -= tiltCorrection * 0.5f;

            pitchRight -= data[FaceBlendshape.EyeLookUpRight] * eyeLookScale;
            pitchRight += data[FaceBlendshape.EyeLookDownRight] * eyeLookScale;
            pitchRight -= tiltCorrection * 0.5f;

            // Yaw (left/right: looking left drives left eye OUT and right eye IN; both must rotate in parallel)
            yawLeft -= data[FaceBlendshape.EyeLookOutLeft] * eyeLookScale;
            yawLeft += data[FaceBlendshape.EyeLookInLeft] * eyeLookScale;

            yawRight -= data[FaceBlendshape.EyeLookInRight] * eyeLookScale;
            yawRight += data[FaceBlendshape.EyeLookOutRight] * eyeLookScale;

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
        /// Finds the left and right eye bones by looking up the avatar skeleton hierarchy.
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

            leftEyeTransform = transform.Find(AvatarSceleton.LeftEye);
            rightEyeTransform = transform.Find(AvatarSceleton.RightEye);

            if (leftEyeTransform == null || rightEyeTransform == null)
            {
                Debug.LogWarning("[EchoFace] Eye bone transforms not found. Eye rotation will be disabled.\n");
            }
            else
            {
                // Cache the initial local rotation of each eye bone
                leftEyeRestRotation = leftEyeTransform.localRotation;
                rightEyeRestRotation = rightEyeTransform.localRotation;
            }
        }

        /// <summary>
        /// Caches blendshape name-to-index mappings for faster lookup.
        /// </summary>
        private void CacheBlendshapeIndices()
        {
            if (skinnedMeshRenderer == null)
            {
                return;
            }

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

            foreach (string name in allBlendshapeNames.Distinct())
            {
                int index = mesh.GetBlendShapeIndex(name);
                if (index >= 0)
                {
                    blendshapeIndexCache[name] = index;
                }
                else
                {
                    Debug.LogWarning($"[EchoFace] Blendshape '{name}' not found on the mesh.\n");
                }
            }
        }

        /// <summary>
        /// Receives externally provided face-tracking data (e.g., from network replication)
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

        /// <summary>
        /// Resets the facial animation, bone transforms, and internal tracking state
        /// back to their default rest pose.
        /// </summary>
        internal void ResetToRestPose()
        {
            // Reset blendshapes to zero
            if (skinnedMeshRenderer != null && skinnedMeshRenderer.sharedMesh != null)
            {
                int count = skinnedMeshRenderer.sharedMesh.blendShapeCount;
                for (int i = 0; i < count; i++)
                {
                    skinnedMeshRenderer.SetBlendShapeWeight(i, 0f);
                }
            }

            // Reset head and eye rotations to their rest poses
            if (headTransform != null)
            {
                headTransform.localRotation = headRestRotation;
            }

            if (leftEyeTransform != null)
            {
                leftEyeTransform.localRotation = leftEyeRestRotation;
            }

            if (rightEyeTransform != null)
            {
                rightEyeTransform.localRotation = rightEyeRestRotation;
            }

            // Clear internal smoothing states and buffered frames
            currentBlendshapeValues.Clear();
            targetBlendshapeValues.Clear();
            currentHeadRotation = Quaternion.identity;
            currentLeftEyeRotation = Quaternion.identity;
            currentRightEyeRotation = Quaternion.identity;
            latestFaceData = null;
        }
    }
}
