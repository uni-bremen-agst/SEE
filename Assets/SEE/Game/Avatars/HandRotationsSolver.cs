using UnityEngine;

namespace SEE.Game.Avatars
{
    /// <summary>
    /// Provides methods for calculating and assigning rotation values for various hand and finger joints based on
    /// landmarks detected by MediaPipe. This class is designed to facilitate the simulation of realistic hand
    /// movements, including flexion, extension, and rotation of fingers, thumb, wrist, and elbow.
    /// </summary>
    /// <remarks>The methods in this class rely on landmarks provided by MediaPipe to calculate rotation
    /// angles for different joints. These angles are then used to simulate realistic hand and finger movements. The
    /// calculations take into account the relative positions of bones and joints, as well as constraints to ensure
    /// natural movement (e.g., limiting maximum flexion angles).
    /// <para> This class assumes that the input landmarks and positions are consistent with the coordinate system
    /// used by MediaPipe. Inconsistencies in the input data may result in NaN values, which are handled
    /// appropriately in the rotation assignment methods. </para>
    /// <para> The class is intended for internal use and is not exposed as part of the public API. </para></remarks>
    internal class HandRotationsSolver
    {
        /// <summary>
        /// Finds values ​​for rotation in flexion and extension (especially of the fingers).
        /// </summary>
        /// <param name="childBoneLandmark">Landmark of the finger bone, which is the child of the second bone.</param>
        /// <param name="parentBoneLandmark">Landmark of the finger bone that is the parent of the first bone.</param>
        /// <param name="childBoneStartPos">The position of the child bone relative to the parent's position,
        /// recognized by MediaPipe at startup.</param>
        /// <returns>Rotation values ​​in degrees to be assigned.</returns>
        public float FindRotationForFlexionAndExtention
            (Mediapipe.Tasks.Components.Containers.Landmark childBoneLandmark,
             Mediapipe.Tasks.Components.Containers.Landmark parentBoneLandmark,
             Vector3 childBoneStartPos)
        {
            // New position of the child bone relative to the parent's position.
            Vector3 coordinateDifferenceChildToParent
                = new(childBoneLandmark.x - parentBoneLandmark.x, childBoneLandmark.y - parentBoneLandmark.y, 0);

            // Euclidean distance, which represents the length of the bone between the two keypoints (e.g. finger phalanges),
            // based on the first coordinates found by the mediapipe.
            float distance = Vector3.Distance(new Vector3(0, 0, 0), childBoneStartPos);
            float squaredDifference = distance * distance
                 - coordinateDifferenceChildToParent.y * coordinateDifferenceChildToParent.y
                 - coordinateDifferenceChildToParent.x * coordinateDifferenceChildToParent.x;

            float newZCoordinate = Mathf.Sqrt(squaredDifference);
            Vector3 newChildPosition = new(coordinateDifferenceChildToParent.x, coordinateDifferenceChildToParent.y, newZCoordinate);

            Vector2 rotateFrom = new(childBoneStartPos.y, childBoneStartPos.z);
            Vector2 rotateTo = new(newChildPosition.y, newChildPosition.z);
            float newAngle = Vector2.Angle(rotateFrom, rotateTo);

            // If the value found is too large and with it the fingers would pass through the palm, assign
            // such a value so that the palm is completely bent.
            if (newAngle >= 130f || (childBoneLandmark.y < parentBoneLandmark.y && squaredDifference < 0))
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
            if (float.IsNaN(angleToSet))
            {
                fingerMiddleBone.localRotation
                    = Quaternion.Euler(fingerMiddleBone.localRotation.eulerAngles.x, fingerMiddleBone.localRotation.eulerAngles.y, 0);
            }
            else
            {
                fingerMiddleBone.localRotation
                    *= Quaternion.Euler(0, 0, angleToSet - fingerMiddleBone.localRotation.eulerAngles.z);
            }

            if (float.IsNaN(angleToSet))
            {
                fingertipBone.localRotation
                    = Quaternion.Euler(fingertipBone.localRotation.eulerAngles.x, fingertipBone.localRotation.eulerAngles.y, 0);
            }
            else
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
                    fingertipBone.localRotation *= Quaternion.Euler(0, 0, angleToSet - fingertipBone.localRotation.eulerAngles.z);
                }
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
            if (float.IsNaN(angleToSet))
            {
                baseOfTheFingerBone.localRotation = Quaternion.Euler(baseOfTheFingerBone.localRotation.eulerAngles.x,
                                                                     baseOfTheFingerBone.localRotation.eulerAngles.y, 0);
            }
            else
            {
                baseOfTheFingerBone.localRotation *= Quaternion.Euler(0, 0, angleToSet - baseOfTheFingerBone.localRotation.eulerAngles.z);
            }
        }

        /// <summary>
        /// Finds values ​​for thumb and hand rotation using only x and y coordinates from MediaPipe.
        /// </summary>
        /// <param name="childBoneLandmark">Landmark of the bone, which is the child of the second bone.</param>
        /// <param name="parentBoneLandmark">Landmark of the finger bone that is the parent of the first bone.</param>
        /// <param name="childBoneStartPos">The position of the child bone relative to the parent's position, recognized by MediaPipe at startup.</param>
        /// <returns>Rotation values in degrees to be assigned.</returns>
        public float FindThumbAndWristXRotation
                        (Mediapipe.Tasks.Components.Containers.Landmark childBoneLandmark,
                         Mediapipe.Tasks.Components.Containers.Landmark parentBoneLandmark,
                         Vector3 childBoneStartPos)
        {
            Vector2 rotateFrom = new(childBoneStartPos.x, childBoneStartPos.y);
            Vector2 rotateTo = new(childBoneLandmark.x - parentBoneLandmark.x, childBoneLandmark.y - parentBoneLandmark.y);
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
        /// <returns>Rotation values ​​in degrees to be assigned.</returns>
        public float FindWristYRotation
            (Mediapipe.Tasks.Components.Containers.Landmark childBoneLandmark,
             Mediapipe.Tasks.Components.Containers.Landmark parentBoneLandmark,
             Vector3 childBoneStartPos)
        {
            // New position of the child bone relative to the parent's position.
            Vector3 coordinateDifferenceChildToParent = new(childBoneLandmark.x - parentBoneLandmark.x, childBoneLandmark.y - parentBoneLandmark.y, 0);

            // Euclidean distance, which represents the length of the bone between the two keypoints (e.g. hand and base of the index finger),
            // based on the first coordinates found by the mediapipe.
            float distance = Vector3.Distance(Vector3.zero, childBoneStartPos);
            float squaredDifference = distance * distance
                - coordinateDifferenceChildToParent.x * coordinateDifferenceChildToParent.x
                - coordinateDifferenceChildToParent.y* coordinateDifferenceChildToParent.y;

            float newZcoordinate = Mathf.Sqrt(squaredDifference);
            Vector3 newChildPosition = new(coordinateDifferenceChildToParent.x, coordinateDifferenceChildToParent.y, newZcoordinate);

            Vector2 rotateFrom = new(childBoneStartPos.x, childBoneStartPos.z);
            Vector2 rotateTo = new(newChildPosition.x, newChildPosition.z);
            float newAngle = Vector2.Angle(rotateFrom, rotateTo);

            return newAngle;
        }

        /// <summary>
        /// Calculates the angle at which the elbow bends when the user attempts to point downwards.
        /// </summary>
        /// <param name="handLandmark">Landmark of the hand, detected by MediaPipe.</param>
        /// <param name="elbowLandmark">Landmark of the elbow, detected by MediaPipe.</param>
        /// <param name="handStartPos">The position of the hand relative to the elbow's position, recognized by MediaPipe at startup.</param>
        /// <returns>Rotation values ​​in degrees to be assigned.</returns>
        public float FindElbowRotation
            (Mediapipe.Tasks.Components.Containers.Landmark handLandmark,
             Mediapipe.Tasks.Components.Containers.Landmark elbowLandmark,
             Vector3 handStartPos)
        {
            // New position of the child bone relative to the parent's position.
            Vector3 coordinateDifferenceChildToParent = new(handStartPos.x, handLandmark.y - elbowLandmark.y, 0);

            // Euclidean distance, which represents the length of the bone between the two keypoints (e.g. hand and elbow),
            // based on the first coordinates found by the mediapipe.
            float distance = Vector3.Distance(new Vector3(0, 0, 0), handStartPos);
            float squaredDifference = distance * distance
                - coordinateDifferenceChildToParent.y * coordinateDifferenceChildToParent.y
                - coordinateDifferenceChildToParent.x * coordinateDifferenceChildToParent.x;
            float newZCoordinate = Mathf.Sqrt(squaredDifference);
            Vector3 newChildPosition = new(coordinateDifferenceChildToParent.x, coordinateDifferenceChildToParent.y, newZCoordinate);
            Vector2 rotateFrom = new(handStartPos.y, handStartPos.z);
            Vector2 rotateTo = new(newChildPosition.y, newChildPosition.z);
            float newAngle = Vector2.Angle(rotateFrom, rotateTo);

            return newAngle;
        }
    }
}
