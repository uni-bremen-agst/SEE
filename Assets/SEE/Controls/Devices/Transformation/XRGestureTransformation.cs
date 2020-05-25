using System;
using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;

namespace SEE.Controls.Devices
{
    [Obsolete("This functionality is covered by a selection device.")]
    public class XRGestureTransformation : Transformation
    {
        [Tooltip("The left hand of the VR player")]
        public Hand LeftHand;
        [Tooltip("The right hand of the VR player")]
        public Hand RightHand;

        [Tooltip("The minimal lag time in seconds a guesture must have been seen to be considered activated. "
                 + "A larger value suppresses accidental detection. A smaller value is more reactive."),
                 Range(0, 2)]
        public float MinimalGestureTime = 0.1f;

        [Tooltip("The threshold for the curl degree of a finger to be considered stretched. "
                 + "Curl values range from 0 to 1, with 1 being fully curled and 0 means fully stretched. "
                 + "This value must be smaller than the Finger Closed Threshold"), 
                 Range(0, 0.49f)]
        public float FingerStretchedThreshold = 0.15f;
        [Tooltip("The threshold for the curl degree of a finger to be considered closed. "
                 + "Curl values range from 0 to 1, with 1 being fully curled and 0 means fully stretched. "
                 + "This value must be greater than the Finger Stretched Threshold"),
                 Range(0.5f, 1)]
        public float FingerClosedThreshold = 0.80f;

        /// <summary>
        /// The position of the LeftHand.
        /// </summary>
        private Vector3 LeftHandPosition
        {
            get => LeftHand.transform.position;
        }

        /// <summary>
        /// The position of the RightHand.
        /// </summary>
        private Vector3 RightHandPosition
        {
            get => RightHand.transform.position;
        }

        public override float ZoomFactor
        {
            get
            {
                if (previousGesture == Kind.Zoom)
                {
                    float currentDistanceBetweenHands = Vector3.Distance(LeftHandPosition, RightHandPosition);
                    return currentDistanceBetweenHands / initialDistanceBetweenHands;
                }
                else
                {
                    return 1.0f;
                }
            }
        }

        /// <summary>
        /// All fingers of one hand.
        /// </summary>
        private enum Finger
        {
            Thumb,
            Index,
            Middle,
            Ring,
            Pinky
        }

        /// <summary>
        /// The gesture that was detected at the last request.
        /// </summary>
        private Kind previousGesture = Kind.None;
        /// <summary>
        /// The absolute point in time in seconds when the last recognized gesture has been detected.
        /// </summary>
        private float gestureStartedTime;
        /// <summary>
        /// The distance in Unity units between the two hands when the Zoom gesture has been recognized first.
        /// </summary>
        private float initialDistanceBetweenHands = 0.0f;

        /// <summary>
        /// Initializes gestureStartedTime to None and gestureStartedTime to the present time.
        /// </summary>
        private void Start()
        {
            previousGesture = Kind.None;
            gestureStartedTime = Time.time;
        }

        public override Kind Recognize()
        {
            bool leftThumbStretched = SteamVR_Actions.default_SkeletonLeftHand.thumbCurl <= FingerStretchedThreshold;
            bool leftIndexStretched = SteamVR_Actions.default_SkeletonLeftHand.indexCurl <= FingerStretchedThreshold;
            bool leftMiddleStretched = SteamVR_Actions.default_SkeletonLeftHand.middleCurl <= FingerStretchedThreshold;
            bool leftRingStretched = SteamVR_Actions.default_SkeletonLeftHand.ringCurl <= FingerStretchedThreshold;
            bool leftPinkyStretched = SteamVR_Actions.default_SkeletonLeftHand.pinkyCurl <= FingerStretchedThreshold;

            bool leftThumbClosed = SteamVR_Actions.default_SkeletonLeftHand.thumbCurl >= FingerClosedThreshold;
            bool leftIndexClosed = SteamVR_Actions.default_SkeletonLeftHand.indexCurl >= FingerClosedThreshold;
            bool leftMiddleClosed = SteamVR_Actions.default_SkeletonLeftHand.middleCurl >= FingerClosedThreshold;
            bool leftRingClosed = SteamVR_Actions.default_SkeletonLeftHand.ringCurl >= FingerClosedThreshold;
            bool leftPinkyClosed = SteamVR_Actions.default_SkeletonLeftHand.pinkyCurl >= FingerClosedThreshold;

            bool leftFist = leftThumbClosed && leftIndexClosed && leftMiddleClosed && leftRingClosed && leftPinkyClosed;
            bool leftGimmeFive = leftThumbStretched && leftIndexStretched && leftMiddleStretched && leftRingStretched && leftPinkyStretched;

            bool rightThumbStretched = SteamVR_Actions.default_SkeletonRightHand.thumbCurl <= FingerStretchedThreshold;
            bool rightIndexStretched = SteamVR_Actions.default_SkeletonRightHand.indexCurl <= FingerStretchedThreshold;
            bool rightMiddleStretched = SteamVR_Actions.default_SkeletonRightHand.middleCurl <= FingerStretchedThreshold;
            bool rightRingStretched = SteamVR_Actions.default_SkeletonRightHand.ringCurl <= FingerStretchedThreshold;
            bool rightPinkyStretched = SteamVR_Actions.default_SkeletonRightHand.pinkyCurl <= FingerStretchedThreshold;

            bool rightThumbClosed = SteamVR_Actions.default_SkeletonRightHand.thumbCurl >= FingerClosedThreshold;
            bool rightIndexClosed = SteamVR_Actions.default_SkeletonRightHand.indexCurl >= FingerClosedThreshold;
            bool rightMiddleClosed = SteamVR_Actions.default_SkeletonRightHand.middleCurl >= FingerClosedThreshold;
            bool rightRingClosed = SteamVR_Actions.default_SkeletonRightHand.ringCurl >= FingerClosedThreshold;
            bool rightPinkyClosed = SteamVR_Actions.default_SkeletonRightHand.pinkyCurl >= FingerClosedThreshold;

            bool rightFist = rightThumbClosed && rightIndexClosed && rightMiddleClosed && rightRingClosed && rightPinkyClosed;
            bool rightGimmeFive = rightThumbStretched && rightIndexStretched && rightMiddleStretched && rightRingStretched && rightPinkyStretched;

            //text.text += "\nleftThumb:  " + FingerState(SteamVR_Actions.default_SkeletonLeftHand.thumbCurl);
            //text.text += "\nleftIndex:  " + FingerState(SteamVR_Actions.default_SkeletonLeftHand.indexCurl);
            //text.text += "\nleftMiddle: " + FingerState(SteamVR_Actions.default_SkeletonLeftHand.middleCurl);
            //text.text += "\nleftRing:   " + FingerState(SteamVR_Actions.default_SkeletonLeftHand.ringCurl);
            //text.text += "\nleftPinky:  " + FingerState(SteamVR_Actions.default_SkeletonLeftHand.pinkyCurl);

            //text.text += "\nrightThumb:  " + FingerState(SteamVR_Actions.default_SkeletonRightHand.thumbCurl);
            //text.text += "\nrightIndex:  " + FingerState(SteamVR_Actions.default_SkeletonRightHand.indexCurl);
            //text.text += "\nrightMiddle: " + FingerState(SteamVR_Actions.default_SkeletonRightHand.middleCurl);
            //text.text += "\nrightRing:   " + FingerState(SteamVR_Actions.default_SkeletonRightHand.ringCurl);
            //text.text += "\nrightPinky:  " + FingerState(SteamVR_Actions.default_SkeletonRightHand.pinkyCurl);

            // Recognize the current gesture.
            // Only left hand fully open => Move Object Left
            // Only right hand fully open => Move Object Right

            // Left and right index finger fully stretched, all other fingers not fully stretched => Move Object to the back
            // Left and right thumb fully stretched, all other fingers not fully stretched => Move Object to the front
            Kind gesture = Kind.None;

            if (leftGimmeFive && rightGimmeFive)
            {
                // Left and right hand fully open => zooming
                // Distance between both hand is scale delta for object.
                gesture = Kind.Zoom;
            }
            else if (leftGimmeFive && rightFist)
            {
                gesture = Kind.MoveRight;
            }
            else if (leftFist && rightGimmeFive)
            {
                gesture = Kind.MoveLeft;
            }
            else if (leftIndexStretched && rightIndexStretched
                     && leftThumbClosed && leftMiddleClosed && leftRingClosed && leftPinkyClosed
                     && rightThumbClosed && rightMiddleClosed && rightRingClosed && rightPinkyClosed)
            {
                gesture = Kind.MoveForward;
            }
            else if (leftThumbStretched && rightThumbStretched
                     && leftIndexClosed && leftMiddleClosed && leftRingClosed && leftPinkyClosed
                     && rightIndexClosed && rightMiddleClosed && rightRingClosed && rightPinkyClosed)
            {
                gesture = Kind.MoveBackward;
            }

            //text.text = gesture.ToString() + "\n";

            if (gesture == Kind.None || gesture != previousGesture)
            {
                // no or a different gesture made
                // Reset
                previousGesture = gesture;
                gestureStartedTime = Time.time;
                return Kind.None;
            }
            // assert: gesture != Gesture.None and gesture = previousGesture
            // A gesture must have been seen for at least MinimalGestureTime to be considered.
            else if (Time.time - gestureStartedTime >= MinimalGestureTime)
            {
                // Same guesture seen long enough.
                //text.text = previousGesture.ToString() + " activated\n";
                return previousGesture;
            }
            else
            {
                // Gesture detected but not seen long enough.
                return Kind.None;
            }
        }
    }
}