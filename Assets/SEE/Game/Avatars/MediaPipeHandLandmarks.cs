using System;
using Mediapipe.Tasks.Vision.HandLandmarker;
using Mediapipe.Tasks.Components.Containers;
using UnityEngine;

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
        public Vector3 LeftMiddleFinger3Position = new();
        public Vector3 LeftMiddleFinger2Position = new();
        public Vector3 LeftMiddleFinger1Position = new();

        /// <summary>
        /// Landmarks representing the bone segment of the left index finger closest to the fingertip,
        /// the middle bone segment and the segment at the base of the palm.
        /// </summary>
        public Vector3 LeftIndexFinger3Position = new();
        public Vector3 LeftIndexFinger2Position = new();
        public Vector3 LeftIndexFinger1Position = new();

        /// <summary>
        /// Landmarks representing the bone segment of the left ring finger closest to the fingertip,
        /// the middle bone segment and the segment at the base of the palm.
        /// </summary>
        public Vector3 LeftRingFinger3Position = new();
        public Vector3 LeftRingFinger2Position = new();
        public Vector3 LeftRingFinger1Position = new();

        /// <summary>
        /// Landmarks representing the bone segment of the left pinky finger closest to the fingertip,
        /// the middle bone segment and the segment at the base of the palm.
        /// </summary>
        public Vector3 LeftPinkyFinger3Position = new();
        public Vector3 LeftPinkyFinger2Position = new();
        public Vector3 LeftPinkyFinger1Position = new();

        /// <summary>
        /// Landmarks representing the bone segment of the left thumb closest to the fingertip, and
        /// the middle bone segment.
        /// </summary>
        public Vector3 LeftThumb3Position = new();
        public Vector3 LeftThumb2Position = new();

        /// <summary>
        /// Landmark representing the position of the left hand.
        /// </summary>
        public Vector3 LeftHandPosition = new();

        /// <summary>
        /// Landmarks representing the bone segment of the right middle finger closest to the fingertip,
        /// the middle bone segment and the segment at the base of the palm.
        /// </summary>
        public Vector3 RightMiddleFinger3Position = new();
        public Vector3 RightMiddleFinger2Position = new();
        public Vector3 RightMiddleFinger1Position = new();

        /// <summary>
        /// Landmarks representing the bone segment of the right index finger closest to the fingertip,
        /// the middle bone segment and the segment at the base of the palm.
        /// </summary>
        public Vector3 RightIndexFinger3Position = new();
        public Vector3 RightIndexFinger2Position = new();
        public Vector3 RightIndexFinger1Position = new();

        /// <summary>
        /// Landmarks representing the bone segment of the right ring finger closest to the fingertip,
        /// the middle bone segment and the segment at the base of the palm.
        /// </summary>
        public Vector3 RightRingFinger3Position = new();
        public Vector3 RightRingFinger2Position = new();
        public Vector3 RightRingFinger1Position = new();

        /// <summary>
        /// Landmarks representing the bone segment of the right pinky finger closest to the fingertip,
        /// the middle bone segment and the segment at the base of the palm.
        /// </summary>
        public Vector3 RightPinkyFinger3Position = new();
        public Vector3 RightPinkyFinger2Position = new();
        public Vector3 RightPinkyFinger1Position = new();

        /// <summary>
        /// Landmarks representing the bone segment of the right thumb closest to the fingertip, and
        /// the middle bone segment.
        /// </summary>
        public Vector3 RightThumb3Position = new();
        public Vector3 RightThumb2Position = new();

        /// <summary>
        /// Landmark representing the position of the right hand.
        /// </summary>
        public Vector3 RightHandPosition = new();
    }
}
