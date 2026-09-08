// Namespace documentation is provided in EchoFace.cs.
namespace SEE.Tools.EchoFace
{
    /// <summary>
    /// Defines all supported ARKit/MediaPipe blendshapes in their exact transmission
    /// and model output order.
    /// </summary>
    /// <remarks>
    /// Values start at index 0 (<see cref="Neutral"/>) and auto-increment sequentially.
    /// The final member <see cref="Count"/> evaluates to the total number of defined
    /// blendshapes (52).
    /// </remarks>
    internal enum FaceBlendshape
    {
        /// <summary>
        /// Neutral facial expression (index 0).
        /// </summary>
        Neutral = 0,

        /// <summary>
        /// Pulls the left eyebrow down.
        /// </summary>
        BrowDownLeft,

        /// <summary>
        /// Pulls the right eyebrow down.
        /// </summary>
        BrowDownRight,

        /// <summary>
        /// Raises the inner parts of both eyebrows.
        /// </summary>
        BrowInnerUp,

        /// <summary>
        /// Raises the outer part of the left eyebrow.
        /// </summary>
        BrowOuterUpLeft,

        /// <summary>
        /// Raises the outer part of the right eyebrow.
        /// </summary>
        BrowOuterUpRight,

        /// <summary>
        /// Puffs both cheeks outward.
        /// </summary>
        CheekPuff,

        /// <summary>
        /// Squints and raises the left cheek.
        /// </summary>
        CheekSquintLeft,

        /// <summary>
        /// Squints and raises the right cheek.
        /// </summary>
        CheekSquintRight,

        /// <summary>
        /// Closes the left eyelid.
        /// </summary>
        EyeBlinkLeft,

        /// <summary>
        /// Closes the right eyelid.
        /// </summary>
        EyeBlinkRight,

        /// <summary>
        /// Rotates the gaze of the left eye downward.
        /// </summary>
        EyeLookDownLeft,

        /// <summary>
        /// Rotates the gaze of the right eye downward.
        /// </summary>
        EyeLookDownRight,

        /// <summary>
        /// Rotates the gaze of the left eye inward (toward the nose).
        /// </summary>
        EyeLookInLeft,

        /// <summary>
        /// Rotates the gaze of the right eye inward (toward the nose).
        /// </summary>
        EyeLookInRight,

        /// <summary>
        /// Rotates the gaze of the left eye outward (away from the nose).
        /// </summary>
        EyeLookOutLeft,

        /// <summary>
        /// Rotates the gaze of the right eye outward (away from the nose).
        /// </summary>
        EyeLookOutRight,

        /// <summary>
        /// Rotates the gaze of the left eye upward.
        /// </summary>
        EyeLookUpLeft,

        /// <summary>
        /// Rotates the gaze of the right eye upward.
        /// </summary>
        EyeLookUpRight,

        /// <summary>
        /// Tightens and squints the left eye margins.
        /// </summary>
        EyeSquintLeft,

        /// <summary>
        /// Tightens and squints the right eye margins.
        /// </summary>
        EyeSquintRight,

        /// <summary>
        /// Opens the left eye wider than the rest pose.
        /// </summary>
        EyeWideLeft,

        /// <summary>
        /// Opens the right eye wider than the rest pose.
        /// </summary>
        EyeWideRight,

        /// <summary>
        /// Pushes the lower jaw forward.
        /// </summary>
        JawForward,

        /// <summary>
        /// Moves the lower jaw to the left.
        /// </summary>
        JawLeft,

        /// <summary>
        /// Opens the lower jaw downward.
        /// </summary>
        JawOpen,

        /// <summary>
        /// Moves the lower jaw to the right.
        /// </summary>
        JawRight,

        /// <summary>
        /// Closes the mouth opening tightly.
        /// </summary>
        MouthClose,

        /// <summary>
        /// Pulls the left mouth corner backward into a dimple.
        /// </summary>
        MouthDimpleLeft,

        /// <summary>
        /// Pulls the right mouth corner backward into a dimple.
        /// </summary>
        MouthDimpleRight,

        /// <summary>
        /// Pulls the left mouth corner downward into a frown.
        /// </summary>
        MouthFrownLeft,

        /// <summary>
        /// Pulls the right mouth corner downward into a frown.
        /// </summary>
        MouthFrownRight,

        /// <summary>
        /// Shapes the mouth into an open funnel (e.g. for "O" phonemes).
        /// </summary>
        MouthFunnel,

        /// <summary>
        /// Displaces the entire mouth toward the left.
        /// </summary>
        MouthLeft,

        /// <summary>
        /// Pulls the left lower lip downward.
        /// </summary>
        MouthLowerDownLeft,

        /// <summary>
        /// Pulls the right lower lip downward.
        /// </summary>
        MouthLowerDownRight,

        /// <summary>
        /// Compresses the left corners of the lips together.
        /// </summary>
        MouthPressLeft,

        /// <summary>
        /// Compresses the right corners of the lips together.
        /// </summary>
        MouthPressRight,

        /// <summary>
        /// Puckers the lips forward (e.g. for kissing or "U" phonemes).
        /// </summary>
        MouthPucker,

        /// <summary>
        /// Displaces the entire mouth toward the right.
        /// </summary>
        MouthRight,

        /// <summary>
        /// Rolls the lower lip inward over the teeth.
        /// </summary>
        MouthRollLower,

        /// <summary>
        /// Rolls the upper lip inward over the teeth.
        /// </summary>
        MouthRollUpper,

        /// <summary>
        /// Shrugs the lower lip upward.
        /// </summary>
        MouthShrugLower,

        /// <summary>
        /// Shrugs the upper lip upward.
        /// </summary>
        MouthShrugUpper,

        /// <summary>
        /// Pulls the left mouth corner upward into a smile.
        /// </summary>
        MouthSmileLeft,

        /// <summary>
        /// Pulls the right mouth corner upward into a smile.
        /// </summary>
        MouthSmileRight,

        /// <summary>
        /// Stretches the left mouth corner laterally.
        /// </summary>
        MouthStretchLeft,

        /// <summary>
        /// Stretches the right mouth corner laterally.
        /// </summary>
        MouthStretchRight,

        /// <summary>
        /// Raises the left upper lip upward.
        /// </summary>
        MouthUpperUpLeft,

        /// <summary>
        /// Raises the right upper lip upward.
        /// </summary>
        MouthUpperUpRight,

        /// <summary>
        /// Wrinkles the left side of the nose in a sneer.
        /// </summary>
        NoseSneerLeft,

        /// <summary>
        /// Wrinkles the right side of the nose in a sneer.
        /// </summary>
        NoseSneerRight,

        /// <summary>
        /// The total number of supported blendshapes (52).
        /// </summary>
        Count
    }

    /// <summary>
    /// Identifies the specific facial landmarks required for head-pose estimation.
    /// </summary>
    /// <remarks>
    /// Values start at index 0 (<see cref="Chin"/>) and auto-increment sequentially.
    /// The final member <see cref="Count"/> evaluates to the total number of tracked
    /// landmarks (3).
    /// </remarks>
    internal enum FaceLandmark
    {
        /// <summary>
        /// The landmark index for the chin (corresponds to MediaPipe landmark 152).
        /// </summary>
        Chin = 0,

        /// <summary>
        /// The landmark index for the right upper eyelid (corresponds to MediaPipe landmark 226).
        /// </summary>
        RightUpperEyelid,

        /// <summary>
        /// The landmark index for the left upper eyelid (corresponds to MediaPipe landmark 446).
        /// </summary>
        LeftUpperEyelid,

        /// <summary>
        /// The total number of tracked facial landmarks (3).
        /// </summary>
        Count
    }
}
