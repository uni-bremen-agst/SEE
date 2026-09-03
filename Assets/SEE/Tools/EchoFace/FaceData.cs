using System;

// Namespace documentation is provided in EchoFace.cs.
namespace SEE.Tools.EchoFace
{
    /// <summary>
    /// Represents a single frame of face-tracking data, including blendshape
    /// weights, facial landmarks, and a timestamp.
    /// </summary>
    /// <remarks>
    /// Instances of this class are typically produced by converting a
    /// compact <see cref="NetworkFacePayload"/> (see
    /// <c>EchoFaceNetworkBridge.ConvertPayloadToFaceData</c>) and consumed
    /// by <see cref="EchoFace.SetFaceData"/>. Blendshapes and landmarks are
    /// organized as flat, enum-indexed arrays for zero-allocation performance.
    /// </remarks>
    [Serializable]
    internal class FaceData
    {
        /// <summary>
        /// Represents the x, y, and z coordinates of a single facial landmark.
        /// Defined as a struct to avoid heap allocations.
        /// </summary>
        [Serializable]
        internal struct LandmarkCoordinates
        {
            /// <summary>
            /// The x coordinate of the landmark in MediaPipe's coordinate space.
            /// </summary>
            internal readonly float X;

            /// <summary>
            /// The y coordinate of the landmark in MediaPipe's coordinate space.
            /// </summary>
            internal readonly float Y;

            /// <summary>
            /// The z coordinate of the landmark in MediaPipe's coordinate space.
            /// </summary>
            internal readonly float Z;

            /// <summary>
            /// Initializes a new instance of the <see cref="LandmarkCoordinates"/> struct
            /// with explicit coordinate components.
            /// </summary>
            /// <param name="x">The x coordinate.</param>
            /// <param name="y">The y coordinate.</param>
            /// <param name="z">The z coordinate.</param>
            internal LandmarkCoordinates(float x, float y, float z)
            {
                X = x;
                Y = y;
                Z = z;
            }
        }

        /// <summary>
        /// The raw blendshape weights for this frame, ordered according to
        /// <see cref="FaceBlendshape"/>. May be <c>null</c> if no blendshape
        /// data is available for this frame.
        /// </summary>
        internal float[] Blendshapes;

        /// <summary>
        /// The facial landmark coordinates for this frame, ordered according to
        /// <see cref="FaceLandmark"/>. May be <c>null</c> if no landmark data
        /// is available for this frame.
        /// </summary>
        internal LandmarkCoordinates[] Landmarks;

        /// <summary>
        /// The timestamp, in milliseconds, at which this frame was captured.
        /// </summary>
        internal long TimestampMs;

        /// <summary>
        /// Safely retrieves the weight of the specified blendshape.
        /// Returns <c>0f</c> if the blendshape array is unassigned or the index is out of range.
        /// </summary>
        /// <param name="shape">The target blendshape.</param>
        /// <returns>The blendshape weight in the range [0, 1], or <c>0f</c> if unavailable.</returns>
        internal float this[FaceBlendshape shape]
        {
            get
            {
                int index = (int)shape;
                return (Blendshapes != null && index >= 0 && index < Blendshapes.Length) ? Blendshapes[index] : 0f;
            }
        }

        /// <summary>
        /// Safely retrieves the coordinates of the specified facial landmark.
        /// Returns <c>default</c> if the landmark array is unassigned or the index is out of range.
        /// </summary>
        /// <param name="landmark">The target landmark identifier.</param>
        /// <returns>The landmark coordinates, or <c>default</c> if unavailable.</returns>
        internal LandmarkCoordinates this[FaceLandmark landmark]
        {
            get
            {
                int index = (int)landmark;
                return (Landmarks != null && index >= 0 && index < Landmarks.Length) ? Landmarks[index] : default;
            }
        }

        /// <summary>
        /// Indicates whether all required facial landmarks for head-pose estimation
        /// are present and assigned in this frame.
        /// </summary>
        /// <returns><c>true</c> if all required landmarks are present; otherwise, <c>false</c>.</returns>
        internal bool HasLandmarks => Landmarks != null && Landmarks.Length >= (int)FaceLandmark.Count;
    }
}
