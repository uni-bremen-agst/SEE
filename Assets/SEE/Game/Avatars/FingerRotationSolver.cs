using Mediapipe;
using UnityEngine;


namespace SEE.Game.Avatars
{
    public class FingerRotationSolver
    {
        public float findTopDownRotation(Mediapipe.Tasks.Components.Containers.Landmark childLandmark, Mediapipe.Tasks.Components.Containers.Landmark parentLandmark, Vector3 startPos)
        {
            var coordinateDifferenceChildToParent = new Vector3(childLandmark.x - parentLandmark.x, childLandmark.y - parentLandmark.y, 0);
            var distance = Vector3.Distance(new Vector3(0, 0, 0), startPos);
            var squaredDifference = distance*distance - coordinateDifferenceChildToParent.y * coordinateDifferenceChildToParent.y - coordinateDifferenceChildToParent.x * coordinateDifferenceChildToParent.x;
            var newZCoordinate = Mathf.Sqrt(squaredDifference);
            var newChildPosition = new Vector3(coordinateDifferenceChildToParent.x, coordinateDifferenceChildToParent.y, newZCoordinate);
            Vector2 rotateFrom = new Vector2(startPos.y, startPos.z);

            Vector2 rotateTo = new Vector2(newChildPosition.y, newChildPosition.z);
            float newAngle = Vector2.Angle(rotateFrom, rotateTo);

            if (newAngle >= 130f)
            {
                newAngle = 130f;
            }
            if (childLandmark.y < parentLandmark.y && squaredDifference<0)
            {
                newAngle = 130f;
            }
            
            return newAngle;
        }

        public void setFingertipRotation(float angleToSet, Transform fingerBone3, Transform fingerBone2)
        {
            if (!float.IsNaN(angleToSet))
            {
                var newFinger2Rotation = fingerBone2.localRotation * Quaternion.Euler(0, 0, angleToSet - fingerBone2.localRotation.eulerAngles.z);
                fingerBone2.localRotation = newFinger2Rotation;
            }
            else
            {
                fingerBone2.localRotation = Quaternion.Euler(fingerBone2.localRotation.eulerAngles.x, fingerBone2.localRotation.eulerAngles.y, 0);
            }

            if (!float.IsNaN(angleToSet))
            {
                angleToSet = angleToSet / 2;
                if (angleToSet >= 60f)
                {
                    fingerBone3.localRotation = Quaternion.Euler(0, 0, 20f);
                }
                else if (angleToSet <= -60f)
                {
                    fingerBone3.localRotation = Quaternion.Euler(0, 0, -20f);
                }
                else
                {
                    var newMidFinger3Rotation = fingerBone3.localRotation * Quaternion.Euler(0, 0, angleToSet - fingerBone3.localRotation.eulerAngles.z);
                    fingerBone3.localRotation = newMidFinger3Rotation;
                }
            }
            else
            {
                fingerBone3.localRotation = Quaternion.Euler(fingerBone3.localRotation.eulerAngles.x, fingerBone3.localRotation.eulerAngles.y, 0);
            }

        }

        public void setBaseOfTheFingerRotation(float angleToSet, Transform fingerBone1)
        {
            if (!float.IsNaN(angleToSet))
            {
                var newFinger1Rotation = fingerBone1.localRotation * Quaternion.Euler(0, 0, angleToSet - fingerBone1.localRotation.eulerAngles.z);
                fingerBone1.localRotation = newFinger1Rotation;
            }
            else
            {
                fingerBone1.localRotation = Quaternion.Euler(fingerBone1.localRotation.eulerAngles.x, fingerBone1.localRotation.eulerAngles.y, 0);
            }
        }

        public float findXRotation(Mediapipe.Tasks.Components.Containers.Landmark childLandmark, Mediapipe.Tasks.Components.Containers.Landmark parentLandmark, Vector3 startPos)
        {
            var rotateFrom = new Vector2(startPos.x, startPos.y);
            var rotateTo = new Vector2(childLandmark.x - parentLandmark.x, childLandmark.y - parentLandmark.y);
            float newAngle = Vector2.SignedAngle(rotateFrom, rotateTo);

            return newAngle;
        }

        public float findWristYRotation(Mediapipe.Tasks.Components.Containers.Landmark childLandmark, Mediapipe.Tasks.Components.Containers.Landmark parentLandmark, Vector3 startPos)
        {
            var coordinateDifferenceChildToParent = new Vector3(childLandmark.x - parentLandmark.x, childLandmark.y - parentLandmark.y, 0);
            var distance = Vector3.Distance(Vector3.zero, startPos);
            var squaredDifference = distance * distance - coordinateDifferenceChildToParent.x * coordinateDifferenceChildToParent.x - coordinateDifferenceChildToParent.y* coordinateDifferenceChildToParent.y;
            
            var newZcoordinate = Mathf.Sqrt(squaredDifference);
            var newChildPosition = new Vector3(coordinateDifferenceChildToParent.x, coordinateDifferenceChildToParent.y, newZcoordinate);

            Vector2 rotateFrom1 = new Vector2(startPos.x, startPos.z);
            Vector2 rotateTo1 = new Vector2(newChildPosition.x, newChildPosition.z);
            float newAngle1 = Vector2.Angle(rotateFrom1, rotateTo1);

            return newAngle1;
        }

        public float findElbowRotation(Mediapipe.Tasks.Components.Containers.Landmark handLandmark, Mediapipe.Tasks.Components.Containers.Landmark elbowLandmark, Vector3 handStartPos)
        {
            var coordinateDifferenceChildToParent = new Vector3(handStartPos.x, handLandmark.y - elbowLandmark.y, 0);

            var distance1 = Vector3.Distance(new Vector3(0, 0, 0), handStartPos);

            var squaredDifference1 = distance1 * distance1 - coordinateDifferenceChildToParent.y * coordinateDifferenceChildToParent.y - coordinateDifferenceChildToParent.x * coordinateDifferenceChildToParent.x;
            var newZCoordinate1 = Mathf.Sqrt(squaredDifference1);
            var newChildPosition1 = new Vector3(coordinateDifferenceChildToParent.x, coordinateDifferenceChildToParent.y, newZCoordinate1);
            Vector2 rotateFrom1 = new Vector2(handStartPos.y, handStartPos.z);
            Vector2 rotateTo1 = new Vector2(newChildPosition1.y, newChildPosition1.z);
            float newAngle1 = Vector2.Angle(rotateFrom1, rotateTo1);

            return newAngle1;
        }
    }
}
