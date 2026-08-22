using System;
using System.Collections.Generic;

// Namespace documentation is provided in EchoFace.cs.
namespace SEE.Tools.EchoFace
{
    /// <summary>
    /// Represents a single frame of face-tracking data, including blendshape
    /// weights, facial landmarks, and a timestamp.
    /// </summary>
    /// <remarks>
    /// Instances of this class are typically produced by converting a
    /// compact <see cref="FaceDataUdpPayload"/> (see
    /// <c>EchoFaceNetworkBridge.ConvertPayloadToFaceData</c>) and consumed
    /// by <see cref="EchoFace.SetFaceData"/>. Landmark keys are expected to
    /// match the constants defined in <see cref="Landmarks"/>.
    /// </remarks>
    [Serializable]
    internal class FaceData
    {
        /// <summary>
        /// Represents the x, y, and z coordinates of a single facial landmark.
        /// </summary>
        [Serializable]
        internal class LandmarkCoordinates
        {
            /// <summary>
            /// The x coordinate of the landmark.
            /// </summary>
            internal float X;

            /// <summary>
            /// The y coordinate of the landmark.
            /// </summary>
            internal float Y;

            /// <summary>
            /// The z coordinate of the landmark.
            /// </summary>
            internal float Z;
        }

        /// <summary>
        /// The blendshape weights for this frame, keyed by MediaPipe/ARKit
        /// blendshape name (e.g. "jawOpen", "mouthSmileLeft"). May be
        /// <c>null</c> if no blendshape data is available for this frame.
        /// </summary>
        internal Dictionary<string, float> Blendshapes;

        /// <summary>
        /// The facial landmark coordinates for this frame, keyed by
        /// landmark index (see <see cref="Landmarks"/>). May be <c>null</c>
        /// if no landmark data is available for this frame.
        /// </summary>
        /// <remarks>
        /// Named <c>LandmarkPositions</c> rather than <c>Landmarks</c> to
        /// avoid confusion with the unrelated <see cref="Landmarks"/> class,
        /// which defines the landmark index constants used as keys here.
        /// </remarks>
        internal Dictionary<string, LandmarkCoordinates> LandmarkPositions;

        /// <summary>
        /// The timestamp, in milliseconds, at which this frame was captured.
        /// </summary>
        internal long TimestampMs;
    }
}
