using System;
using Mediapipe.Tasks.Vision.HandLandmarker;
using Mediapipe.Tasks.Components.Containers;

namespace SEE.Game.Avatars
{
    /// <summary>
    /// Defines the list of hand landmarks from Mediapipe.
    /// </summary>
    public class MediaPipeHandLandmarks
    {
        /// <summary>
        /// Landmarks representing the bone segment of the left middle finger closest to the fingertip,
        /// the middle bone segment and the segment at the base of the palm.
        /// </summary>
        public Landmark leftMiddleFinger3Position = new();
        public Landmark leftMiddleFinger2Position = new();
        public Landmark leftMiddleFinger1Position = new();

        /// <summary>
        /// Landmarks representing the bone segment of the left index finger closest to the fingertip,
        /// the middle bone segment and the segment at the base of the palm.
        /// </summary>
        public Landmark leftIndexFinger3Position = new();
        public Landmark leftIndexFinger2Position = new();
        public Landmark leftIndexFinger1Position = new();

        /// <summary>
        /// Landmarks representing the bone segment of the left ring finger closest to the fingertip,
        /// the middle bone segment and the segment at the base of the palm.
        /// </summary>
        public Landmark leftRingFinger3Position = new();
        public Landmark leftRingFinger2Position = new();
        public Landmark leftRingFinger1Position = new();

        /// <summary>
        /// Landmarks representing the bone segment of the left pinky finger closest to the fingertip,
        /// the middle bone segment and the segment at the base of the palm.
        /// </summary>
        public Landmark leftPinkyFinger3Position = new();
        public Landmark leftPinkyFinger2Position = new();
        public Landmark leftPinkyFinger1Position = new();

        /// <summary>
        /// Landmarks representing the bone segment of the left thumb closest to the fingertip, and
        /// the middle bone segment.
        /// </summary>
        public Landmark leftThumb3Position = new();
        public Landmark leftThumb2Position = new();

        /// <summary>
        /// Landmark representing the position of the left hand.
        /// </summary>
        public Landmark leftHandPosition = new();

        /// <summary>
        /// Landmarks representing the bone segment of the right middle finger closest to the fingertip,
        /// the middle bone segment and the segment at the base of the palm.
        /// </summary>
        public Landmark rightMiddleFinger3Position = new();
        public Landmark rightMiddleFinger2Position = new();
        public Landmark rightMiddleFinger1Position = new();

        /// <summary>
        /// Landmarks representing the bone segment of the right index finger closest to the fingertip,
        /// the middle bone segment and the segment at the base of the palm.
        /// </summary>
        public Landmark rightIndexFinger3Position = new();
        public Landmark rightIndexFinger2Position = new();
        public Landmark rightIndexFinger1Position = new();

        /// <summary>
        /// Landmarks representing the bone segment of the right ring finger closest to the fingertip,
        /// the middle bone segment and the segment at the base of the palm.
        /// </summary>
        public Landmark rightRingFinger3Position = new();
        public Landmark rightRingFinger2Position = new();
        public Landmark rightRingFinger1Position = new();

        /// <summary>
        /// Landmarks representing the bone segment of the right pinky finger closest to the fingertip,
        /// the middle bone segment and the segment at the base of the palm.
        /// </summary>
        public Landmark rightPinkyFinger3Position = new();
        public Landmark rightPinkyFinger2Position = new();
        public Landmark rightPinkyFinger1Position = new();

        /// <summary>
        /// Landmarks representing the bone segment of the right thumb closest to the fingertip, and
        /// the middle bone segment.
        /// </summary>
        public Landmark rightThumb3Position = new();
        public Landmark rightThumb2Position = new();

        /// <summary>
        /// Landmark representing the position of the right hand.
        /// </summary>
        public Landmark rightHandPosition = new();
    }
}
