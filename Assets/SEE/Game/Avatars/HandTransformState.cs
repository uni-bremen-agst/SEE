using UnityEngine;


namespace SEE.Game.Avatars
{
    /// <summary>
    /// Stores values ​​to display the current position and rotation of the hand and fingers,
    /// as well as other information needed for animation and synchronization of animations.
    /// </summary>
    public class HandTransformState
    {
        /// <summary>
        /// Position of the hand.
        /// </summary>
        public Vector3 HandPosition = Vector3.one;

        /// <summary>
        /// Rotation of the hand.
        /// </summary>
        public Quaternion HandRotation = Quaternion.Euler(0, 0, 0);

        /// <summary>
        /// Local position of the bend goal.
        /// </summary>
        public Vector3 BendGoalLocalPosition = Vector3.one;

        /// <summary>
        /// The rotation that should be assigned to the hand when moving in front of the avatar.
        /// </summary>
        public Quaternion HandRotationForMovementInFrontOfTheAvatar = Quaternion.Euler(0, 0, 0);

        /// <summary>
        /// The rotation that should be assigned to the hand when moving away from the avatar.
        /// </summary>
        public Quaternion HandRotationForMovementToTheSide = Quaternion.Euler(0, 0, 0);

        /// <summary>
        /// The rotation that should be assigned to the hand when moving down.
        /// </summary>
        public Quaternion HandRotationForMovementDown = Quaternion.Euler(0, 0, 0);

        /// <summary>
        /// Previous coordinates of the landmarks, detedted by MediaPipe.
        /// </summary>
        public Vector3 PreviousMediapipeCoordinates = Vector3.one;

        /// <summary>
        /// New coordinates of the landmarks, detedted by MediaPipe.
        /// </summary>
        public Vector3 NewMediapipeCoordinates = Vector3.one;

        /// <summary>
        /// If true, no landmarks for this hand have been detected yet.
        /// </summary>
        public bool IsFirstHandLandmark = true;

        /// <summary>
        /// The position of the fingertip bone of the middle finger relative to it's parent transform's position, recognized by MediaPipe at startup.
        /// </summary>
        public Vector3 MidFinger3StartPos = Vector3.zero;

        /// <summary>
        /// The position of the middle bone of the middle finger relative to it's parent transform's position, recognized by MediaPipe at startup.
        /// </summary>
        public Vector3 MidFinger2StartPos = Vector3.zero;

        /// <summary>
        /// The position of the fingertip bone of the index finger relative to it's parent transform's position, recognized by MediaPipe at startup.
        /// </summary>
        public Vector3 IndexFinger3StartPos = Vector3.zero;

        /// <summary>
        /// The position of the middle bone of the index finger relative to it's parent transform's position, recognized by MediaPipe at startup.
        /// </summary>
        public Vector3 IndexFinger2StartPos = Vector3.zero;

        /// <summary>
        /// The position of the bone that lies at the base of the index finger relative to it's parent transform's position, recognized by MediaPipe at startup.
        /// </summary>
        public Vector3 IndexFinger1StartPos = Vector3.zero;

        /// <summary>
        /// The position of the fingertip bone of the ring finger relative to it's parent transform's position, recognized by MediaPipe at startup.
        /// </summary>
        public Vector3 RingFinger3StartPos = Vector3.zero;

        /// <summary>
        /// The position of the middle bone of the ring finger relative to it's parent transform's position, recognized by MediaPipe at startup.
        /// </summary>
        public Vector3 RingFinger2StartPos = Vector3.zero;

        /// <summary>
        /// The position of the fingertip bone of the little finger relative to it's parent transform's position, recognized by MediaPipe at startup.
        /// </summary>
        public Vector3 PinkyFinger3StartPos = Vector3.zero;

        /// <summary>
        /// The position of the middle bone of the little finger relative to it's parent transform's position, recognized by MediaPipe at startup.
        /// </summary>
        public Vector3 PinkyFinger2StartPos = Vector3.zero;

        /// <summary>
        /// The position of the fingertip bone of the thumb relative to it's parent transform's position, recognized by MediaPipe at startup.
        /// </summary>
        public Vector3 Thumb3StartPos = Vector3.zero;

        /// <summary>
        /// Coordinates of the hand position relative to the head position.
        /// </summary>
        public Vector3 HandToHeadCoordinateDifference = Vector3.zero;

        // Parameters needed to animate a player's Avatar
        // for all other players.

        /// <summary>
        /// The value for the hand position assigned to the IK effector from FullBodyBipedIK.
        /// </summary>
        public Vector3 HandIKEffectorPosition = Vector3.zero;

        /// <summary>
        /// The value for the hand rotation assigned to the IR effector from FullBodyBiped
        /// </summary>
        public Quaternion HandIKEffectorRotation = Quaternion.Euler(0, 0, 0);

        /// <summary>
        /// The value for the weight that determines the level of influence of changes in the IK effectors of the hands on other bones in the chain.
        /// </summary>
        public float HandIKRotationWeight = 0f;

        // Since finger rotations only vary in one dimension (flexion-extension), a Vector3 was used
        // to store finger rotation information, where x represents the rotation for the bone at the base of the finger,
        // y for the middle bone in the finger, and z for the bone at the tip of the finger.
        // The values ​​for the thumb rotations were stored using Quaternion,
        // since for the thumbs-up and thumbs-down gestures the thumbs are rotated in multiple dimensions at once.

        /// <summary>
        /// Rotations of the joints of the index finger.
        /// </summary>
        public Vector3 IndexFingerRotations = Vector3.zero;

        /// <summary>
        /// Rotations of the joints of the middle finger.
        /// </summary>
        public Vector3 MiddleFingerRotations = Vector3.zero;

        /// <summary>
        /// Rotations of the joints of the ring finger.
        /// </summary>
        public Vector3 RingFingerRotations = Vector3.zero;

        /// <summary>
        /// Rotations of the joints of the little finger.
        /// </summary>
        public Vector3 PinkyFingerRotations = Vector3.zero;

        /// <summary>
        /// Rotations of the bone that lies at the base of the thumb.
        /// </summary>
        public Quaternion Thumb1Rotations = Quaternion.Euler(0, 0, 0);

        /// <summary>
        /// Rotations of the middle bone of the thumb.
        /// </summary>
        public Quaternion Thumb2Rotations = Quaternion.Euler(0, 0, 0);

        /// <summary>
        /// Rotations of the bone that lies at fingertip of the thumb.
        /// </summary>
        public Quaternion Thumb3Rotations = Quaternion.Euler(0, 0, 0);
    }
}
