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
        public Landmark LeftMiddleFinger3Position = new();
        public Landmark LeftMiddleFinger2Position = new();
        public Landmark LeftMiddleFinger1Position = new();

        /// <summary>
        /// Landmarks representing the bone segment of the left index finger closest to the fingertip,
        /// the middle bone segment and the segment at the base of the palm.
        /// </summary>
        public Landmark LeftIndexFinger3Position = new();
        public Landmark LeftIndexFinger2Position = new();
        public Landmark LeftIndexFinger1Position = new();

        /// <summary>
        /// Landmarks representing the bone segment of the left ring finger closest to the fingertip,
        /// the middle bone segment and the segment at the base of the palm.
        /// </summary>
        public Landmark LeftRingFinger3Position = new();
        public Landmark LeftRingFinger2Position = new();
        public Landmark LeftRingFinger1Position = new();

        /// <summary>
        /// Landmarks representing the bone segment of the left pinky finger closest to the fingertip,
        /// the middle bone segment and the segment at the base of the palm.
        /// </summary>
        public Landmark LeftPinkyFinger3Position = new();
        public Landmark LeftPinkyFinger2Position = new();
        public Landmark LeftPinkyFinger1Position = new();

        /// <summary>
        /// Landmarks representing the bone segment of the left thumb closest to the fingertip, and
        /// the middle bone segment.
        /// </summary>
        public Landmark LeftThumb3Position = new();
        public Landmark LeftThumb2Position = new();

        /// <summary>
        /// Landmark representing the position of the left hand.
        /// </summary>
        public Landmark LeftHandPosition = new();

        /// <summary>
        /// Landmarks representing the bone segment of the right middle finger closest to the fingertip,
        /// the middle bone segment and the segment at the base of the palm.
        /// </summary>
        public Landmark RightMiddleFinger3Position = new();
        public Landmark RightMiddleFinger2Position = new();
        public Landmark RightMiddleFinger1Position = new();

        /// <summary>
        /// Landmarks representing the bone segment of the right index finger closest to the fingertip,
        /// the middle bone segment and the segment at the base of the palm.
        /// </summary>
        public Landmark RightIndexFinger3Position = new();
        public Landmark RightIndexFinger2Position = new();
        public Landmark RightIndexFinger1Position = new();

        /// <summary>
        /// Landmarks representing the bone segment of the right ring finger closest to the fingertip,
        /// the middle bone segment and the segment at the base of the palm.
        /// </summary>
        public Landmark RightRingFinger3Position = new();
        public Landmark RightRingFinger2Position = new();
        public Landmark RightRingFinger1Position = new();

        /// <summary>
        /// Landmarks representing the bone segment of the right pinky finger closest to the fingertip,
        /// the middle bone segment and the segment at the base of the palm.
        /// </summary>
        public Landmark RightPinkyFinger3Position = new();
        public Landmark RightPinkyFinger2Position = new();
        public Landmark RightPinkyFinger1Position = new();

        /// <summary>
        /// Landmarks representing the bone segment of the right thumb closest to the fingertip, and
        /// the middle bone segment.
        /// </summary>
        public Landmark RightThumb3Position = new();
        public Landmark RightThumb2Position = new();

        /// <summary>
        /// Landmark representing the position of the right hand.
        /// </summary>
        public Landmark RightHandPosition = new();
    }
}
