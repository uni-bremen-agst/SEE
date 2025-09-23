using Mediapipe;
using UnityEngine;


namespace SEE.Game.Avatars
{
    internal class HandRotationsSolver
    {
        /// <summary>
        /// Finds values ​​for rotation in flexion and extension (especially of the fingers).
        /// </summary>
        /// <param name="childBoneLandmark">Landmark of the finger bone, which is the child of the second bone.</param>
        /// <param name="parentBoneLandmark">Landmark of the finger bone that is the parent of the first bone.</param>
        /// <param name="childBoneStartPos">The position of the child bone relative to the parent's position, recognized by MediaPipe at startup.</param>
        /// <returns>Rotation values ​​in degrees to be assigned</returns>
        public float FindRotationForFlexionAndExtention(Mediapipe.Tasks.Components.Containers.Landmark childBoneLandmark, Mediapipe.Tasks.Components.Containers.Landmark parentBoneLandmark, Vector3 childBoneStartPos)
        {
            // New position of the child bone relative to the parent's position.
            Vector3 coordinateDifferenceChildToParent = new Vector3(childBoneLandmark.x - parentBoneLandmark.x, childBoneLandmark.y - parentBoneLandmark.y, 0);

            // Euclidean distance, which represents the length of the bone between the two keypoints (e.g. finger phalanges),
            // based on the first coordinates found by the mediapipe.
            float distance = Vector3.Distance(new Vector3(0, 0, 0), childBoneStartPos);
            float squaredDifference = distance*distance - coordinateDifferenceChildToParent.y * coordinateDifferenceChildToParent.y - coordinateDifferenceChildToParent.x * coordinateDifferenceChildToParent.x;
            float newZCoordinate = Mathf.Sqrt(squaredDifference);
            Vector3 newChildPosition = new Vector3(coordinateDifferenceChildToParent.x, coordinateDifferenceChildToParent.y, newZCoordinate);
            Vector2 rotateFrom = new Vector2(childBoneStartPos.y, childBoneStartPos.z);

            Vector2 rotateTo = new Vector2(newChildPosition.y, newChildPosition.z);
            float newAngle = Vector2.Angle(rotateFrom, rotateTo);

            //If the value found is too large and with it the fingers would pass through the palm, assign such a value so that the palm is completely bent.
            if (newAngle >= 130f)
            {
                newAngle = 130f;
            }
            if (childBoneLandmark.y < parentBoneLandmark.y && squaredDifference<0)
            {
                newAngle = 130f;
            }
            
            return newAngle;
        }

        /// <summary>
        /// Assigns the found rotation value to the last two finger bones.
        /// </summary>
        /// <param name="angleToSet">Angle found for rotation.</param>
        /// <param name="fingertipBone">Transform component of the fingertip bone.</param>
        /// <param name="fingerMiddleBone">Transform component of the middle finger bone.</param>
        /// <remarks>The same calculated value is used to assign rotations to the middle finger bone and the last finger bone. 
        /// The idea was that bending only the very tip of the finger without bending the middle joint is practically impossible, 
        /// so there's no need to calculate the rotation angle for the tip of the finger separately. 
        /// It will receive a value that's half the rotation of the middle bone.</remarks>
        /// <remarks>IsNaN check is needed due to inconsistency in MediaPipe coordinates which can result in trying
        /// to take the square root of a negative number when using the formula for Euclidean distance.</remarks>
        public void SetFingertipRotation(float angleToSet, Transform fingertipBone, Transform fingerMiddleBone)
        {
            if (!float.IsNaN(angleToSet))
            {
                Quaternion newFinger2Rotation = fingerMiddleBone.localRotation * Quaternion.Euler(0, 0, angleToSet - fingerMiddleBone.localRotation.eulerAngles.z);
                fingerMiddleBone.localRotation = newFinger2Rotation;
            }
            else
            {
                fingerMiddleBone.localRotation = Quaternion.Euler(fingerMiddleBone.localRotation.eulerAngles.x, fingerMiddleBone.localRotation.eulerAngles.y, 0);
            }

            if (!float.IsNaN(angleToSet))
            {
                angleToSet = angleToSet / 2;
                if (angleToSet >= 60f)
                {
                    fingertipBone.localRotation = Quaternion.Euler(0, 0, 20f);
                }
                else if (angleToSet <= -60f)
                {
                    fingertipBone.localRotation = Quaternion.Euler(0, 0, -20f);
                }
                else
                {
                    Quaternion newMidFinger3Rotation = fingertipBone.localRotation * Quaternion.Euler(0, 0, angleToSet - fingertipBone.localRotation.eulerAngles.z);
                    fingertipBone.localRotation = newMidFinger3Rotation;
                }
            }
            else
            {
                fingertipBone.localRotation = Quaternion.Euler(fingertipBone.localRotation.eulerAngles.x, fingertipBone.localRotation.eulerAngles.y, 0);
            }

        }

        /// <summary>
        /// Assigns the found rotation value to the bone that lies at the base of the finger.
        /// </summary>
        /// <param name="angleToSet">Angle found for rotation.</param>
        /// <param name="baseOfTheFingerBone">Transform component of the bone that lies at the base of the finger.</param>
        /// <remarks>IsNaN check is needed due to inconsistency in MediaPipe coordinates which can result in trying
        /// to take the square root of a negative number when using the formula for Euclidean distance.</remarks>
        public void SetBaseOfTheFingerRotation(float angleToSet, Transform baseOfTheFingerBone)
        {
            if (!float.IsNaN(angleToSet))
            {
                Quaternion newFinger1Rotation = baseOfTheFingerBone.localRotation * Quaternion.Euler(0, 0, angleToSet - baseOfTheFingerBone.localRotation.eulerAngles.z);
                baseOfTheFingerBone.localRotation = newFinger1Rotation;
            }
            else
            {
                baseOfTheFingerBone.localRotation = Quaternion.Euler(baseOfTheFingerBone.localRotation.eulerAngles.x, baseOfTheFingerBone.localRotation.eulerAngles.y, 0);
            }
        }

        /// <summary>
        /// Finds values ​​for thumb and hand rotation using only x and y coordinates from MediaPipe.
        /// </summary>
        /// <param name="childBoneLandmark">Landmark of the bone, which is the child of the second bone.</param>
        /// <param name="parentBoneLandmark">Landmark of the finger bone that is the parent of the first bone.</param>
        /// <param name="childBoneStartPos">The position of the child bone relative to the parent's position, recognized by MediaPipe at startup.</param>
        /// <returns></returns>
        public float FindThumbAndWristXRotation(Mediapipe.Tasks.Components.Containers.Landmark childBoneLandmark, Mediapipe.Tasks.Components.Containers.Landmark parentBoneLandmark, Vector3 childBoneStartPos)
        {
            Vector2 rotateFrom = new Vector2(childBoneStartPos.x, childBoneStartPos.y);
            Vector2 rotateTo = new Vector2(childBoneLandmark.x - parentBoneLandmark.x, childBoneLandmark.y - parentBoneLandmark.y);
            float newAngle = Vector2.SignedAngle(rotateFrom, rotateTo);

            return newAngle;
        }

        /// <summary>
        /// Calculates the rotation for the palm rotation from facing forward to the back of the palm facing forward.
        /// </summary>
        /// <param name="childBoneLandmark">Landmark of the finger bone, that was used as a refference to detect whether the palm of the user is rotating
        /// (base of the index finger in this case).
        /// from facing forward to the back of the palm facing forward.</param>
        /// <param name="parentBoneLandmark">Landmark of the hand.</param>
        /// <param name="childBoneStartPos">The position of the finger bone relative to the parent's position, recognized by MediaPipe at startup.</param>
        /// <returns></returns>
        public float FindWristYRotation(Mediapipe.Tasks.Components.Containers.Landmark childBoneLandmark, Mediapipe.Tasks.Components.Containers.Landmark parentBoneLandmark, Vector3 childBoneStartPos)
        {
            Vector3 coordinateDifferenceChildToParent = new Vector3(childBoneLandmark.x - parentBoneLandmark.x, childBoneLandmark.y - parentBoneLandmark.y, 0);
            float distance = Vector3.Distance(Vector3.zero, childBoneStartPos);
            float squaredDifference = distance * distance - coordinateDifferenceChildToParent.x * coordinateDifferenceChildToParent.x - coordinateDifferenceChildToParent.y* coordinateDifferenceChildToParent.y;

            float newZcoordinate = Mathf.Sqrt(squaredDifference);
            Vector3 newChildPosition = new Vector3(coordinateDifferenceChildToParent.x, coordinateDifferenceChildToParent.y, newZcoordinate);

            Vector2 rotateFrom1 = new Vector2(childBoneStartPos.x, childBoneStartPos.z);
            Vector2 rotateTo1 = new Vector2(newChildPosition.x, newChildPosition.z);
            float newAngle1 = Vector2.Angle(rotateFrom1, rotateTo1);

            return newAngle1;
        }

        /// <summary>
        /// Calculates the angle at which the elbow bends when the user attempts to point downwards.
        /// </summary>
        /// <param name="handLandmark">Landmark of the hand, detected by MediaPipe.</param>
        /// <param name="elbowLandmark">Landmark of the elbow, detected by MediaPipe.</param>
        /// <param name="handStartPos">The position of the hand relative to the elbow's position, recognized by MediaPipe at startup.</param>
        /// <returns></returns>
        public float FindElbowRotation(Mediapipe.Tasks.Components.Containers.Landmark handLandmark, Mediapipe.Tasks.Components.Containers.Landmark elbowLandmark, Vector3 handStartPos)
        {
            Vector3 coordinateDifferenceChildToParent = new Vector3(handStartPos.x, handLandmark.y - elbowLandmark.y, 0);

            float distance1 = Vector3.Distance(new Vector3(0, 0, 0), handStartPos);

            float squaredDifference1 = distance1 * distance1 - coordinateDifferenceChildToParent.y * coordinateDifferenceChildToParent.y - coordinateDifferenceChildToParent.x * coordinateDifferenceChildToParent.x;
            float newZCoordinate1 = Mathf.Sqrt(squaredDifference1);
            Vector3 newChildPosition1 = new Vector3(coordinateDifferenceChildToParent.x, coordinateDifferenceChildToParent.y, newZCoordinate1);
            Vector2 rotateFrom1 = new Vector2(handStartPos.y, handStartPos.z);
            Vector2 rotateTo1 = new Vector2(newChildPosition1.y, newChildPosition1.z);
            float newAngle1 = Vector2.Angle(rotateFrom1, rotateTo1);

            return newAngle1;
        }
    }
}
