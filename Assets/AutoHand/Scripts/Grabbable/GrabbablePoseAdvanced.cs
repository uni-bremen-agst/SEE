using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Autohand {
    [HelpURL("https://app.gitbook.com/s/5zKO0EvOjzUDeT2aiFk3/auto-hand/custom-poses#advanced-grabbable-pose")]
    public class GrabbablePoseAdvanced : GrabbablePose{
        [Tooltip("Usually this can be left empty, used to create a different center point if the objects transform isn't ceneterd for the prefered rotation/movement axis")]
        public Transform centerObject;
        [Space]
        [Tooltip("You want this set so the disc gizmo is around the axis you want the hand to rotate, or that the line is straight through the axis you want to move")]
        public Vector3 up = Vector3.up;
        [Space, Tooltip("Whether or not to automatically allow for the opposite direction pose to be automatically applied (I.E. Should I be able to grab my hammer only with the head facing up, or in both directions?)")]
        public bool useInvertPose = false;

        [Space]
        [Tooltip("The minimum angle rotation around the included directions")]
        public int minAngle = 0;
        [Tooltip("The maximum angle rotation around the included directions")]
        public int maxAngle = 360;
        [Space]
        [Tooltip("The minimum distance allowed from the saved posed along the included directions")]
        public float maxRange = 0;
        [Tooltip("The maximum distance allowed from the saved posed along the included directions")]
        public float minRange = 0;

        [Header("Requires Gizmos Enabled")]
        [Tooltip("Helps test pose by setting the angle of the editor hand, REQUIRES GIZMOS ENABLED")]
        public int testAngle = 0;
        [Tooltip("Helps test pose by setting the range position of the editor hand, REQUIRES GIZMOS ENABLED")]
        public float testRange = 0;


        int lastAngle = 0;
        float lastRange = 0;

        Vector3 pregrabPos;
        Quaternion pregrabRot;
        Transform tempContainer;
        Transform handMatch;
        Transform getTransform;


        protected override void Awake() {
            base.Awake();
            if (minAngle > maxAngle) {
                var tempAngle = minAngle;
                minAngle = maxAngle;
                maxAngle = tempAngle;
            }
            if (minRange > maxRange) {
                var temp = minRange;
                minRange = maxRange;
                maxRange = temp;
            }
        }

        public override HandPoseData GetHandPoseData(Hand hand) {
            pregrabPos = hand.transform.position;
            pregrabRot = hand.transform.rotation;

            var preGrabPose = GetNewPoseData(hand);
            base.GetHandPoseData(hand).SetPose(hand, transform);

            getTransform = GetTransform();

            tempContainer = AutoHandExtensions.transformRuler;
            tempContainer.rotation = Quaternion.identity;
            tempContainer.position = getTransform.position;
            tempContainer.localScale = getTransform.lossyScale;

            handMatch = AutoHandExtensions.transformRulerChild;
            handMatch.position = hand.transform.position;
            handMatch.rotation = hand.transform.rotation;

            tempContainer.rotation = getTransform.rotation;


            var closestRotation = GetClosestRotation(hand, up, useInvertPose);

            tempContainer.rotation = closestRotation;

            var closestPosition = GetClosestPosition(up);

            tempContainer.position = closestPosition;
            hand.transform.position = handMatch.position;
            hand.transform.rotation = handMatch.rotation;

            var pose = GetNewPoseData(hand);
            preGrabPose.SetPose(hand);

#if UNITY_EDITOR
            if(Application.isEditor && !Application.isPlaying)
                DestroyImmediate(tempContainer.gameObject);
#endif

            return pose;
        }

        public Quaternion GetClosestRotation(Hand hand, Vector3 up, bool addInverse) {
            tempContainer = AutoHandExtensions.transformRuler;
            tempContainer.rotation = Quaternion.identity;
            tempContainer.position = getTransform.position;
            tempContainer.localScale = getTransform.lossyScale;

            handMatch = AutoHandExtensions.transformRulerChild;
            handMatch.position = hand.transform.position;
            handMatch.rotation = hand.transform.rotation;

            tempContainer.rotation = getTransform.rotation;
            Quaternion closestRotation = tempContainer.rotation;

            //if((minAngle != 0 || maxAngle != 0 || addInverse) && !(minAngle == maxAngle))
           // {
                float closestDistance = float.MaxValue;
                float closestIndex = 0;

                var iteration = (Mathf.Abs(minAngle) + Mathf.Abs(maxAngle))/10f;
                if(iteration == 0)
                    iteration = 1;
                var additionalDirection = Vector3.zero;
                if(up.x != 0)
                    additionalDirection = new Vector3(0, 1, 0);
                else if(up.y != 0)
                    additionalDirection = new Vector3(1, 0, 0);
                else if(up.z != 0)
                    additionalDirection = new Vector3(0, 0, 1);

                for (float i = minAngle; i <= maxAngle; i += iteration) {
                    tempContainer.eulerAngles = getTransform.rotation * up;
                    tempContainer.RotateAround(getTransform.position, getTransform.rotation * up, i);


                    var distance = Vector3.Distance(handMatch.position, pregrabPos);
                    distance += Quaternion.Angle(handMatch.rotation, pregrabRot)/180f;
                    if (distance < closestDistance) {
                        closestDistance = distance;
                        closestRotation = tempContainer.rotation;
                        closestIndex = i;
                    }
                }

                for (float i = -iteration/2; i < iteration/2; i += iteration/10f) {
                    tempContainer.eulerAngles = getTransform.rotation * up;
                    tempContainer.RotateAround(getTransform.position, getTransform.rotation * up, closestIndex + i);

                    var distance = Vector3.Distance(handMatch.position, pregrabPos);
                    distance += Quaternion.Angle(handMatch.rotation, pregrabRot)/180f;
                    if (distance < closestDistance) {
                        closestDistance = distance;
                        closestRotation = tempContainer.rotation;
                        closestIndex = i;
                    }
                }

                if(addInverse) {
                    var closestInverseDistance = float.MaxValue;
                    float closestInverseIndex = 0;
                    for(float i = minAngle; i <= maxAngle; i += iteration) {
                        tempContainer.eulerAngles = getTransform.rotation * up;
                        tempContainer.RotateAround(getTransform.position, getTransform.rotation * up, i);
                        tempContainer.RotateAround(getTransform.position, getTransform.rotation * additionalDirection, 180);


                        var distance = Vector3.Distance(handMatch.position, pregrabPos);
                        distance += Quaternion.Angle(handMatch.rotation, pregrabRot)/180f;
                        if(distance < closestInverseDistance) {
                            closestInverseDistance = distance; 
                            if(closestInverseDistance < closestDistance)
                                closestRotation = tempContainer.rotation;
                            closestInverseIndex = i;
                        }
                    }

                    for(float i = -iteration / 2; i < iteration / 2; i += iteration / 10f) {
                        tempContainer.eulerAngles = getTransform.rotation * up;
                        tempContainer.RotateAround(getTransform.position, getTransform.rotation * up, closestInverseIndex + i);
                        tempContainer.RotateAround(getTransform.position, getTransform.rotation * additionalDirection, 180);

                        var distance = Vector3.Distance(handMatch.position, pregrabPos);
                        distance += Quaternion.Angle(handMatch.rotation, pregrabRot) / 180f;
                        if(distance < closestInverseDistance) {
                            closestInverseDistance = distance;
                            if(closestInverseDistance < closestDistance)
                                closestRotation = tempContainer.rotation;
                            closestInverseIndex = i;
                        }
                    }
                }

            //}

            return closestRotation;
        }


        public Vector3 GetClosestPosition(Vector3 up)
        {

            Vector3 closestPosition = tempContainer.position;

            if (minRange != 0 || maxRange != 0)
            {
                float closestDistance = float.MaxValue;
                float closestIndex = 0;

                var minRangeVec = getTransform.position + getTransform.rotation * up * minRange;
                var maxRangeVec = getTransform.position + getTransform.rotation * up * maxRange;

                for (int i = 0; i < 10; i++)
                {
                    tempContainer.position = Vector3.Lerp(minRangeVec, maxRangeVec, i / 10f);

                    var distance = Vector3.Distance(handMatch.position, pregrabPos);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestPosition = tempContainer.position;
                        closestIndex = i;
                    }
                }

                for (int i = -5; i < 5; i++)
                {
                    tempContainer.position = Vector3.Lerp(minRangeVec, maxRangeVec, closestIndex + i / 100f);

                    var distance = Vector3.Distance(handMatch.position, pregrabPos);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestPosition = tempContainer.position;
                    }
                }
            }

            return closestPosition;
        }

        public HandPoseData GetHandPoseData(Hand hand, int angle, float range) {
            base.GetHandPoseData(hand).SetPose(hand, transform);

            var getTransform = GetTransform();

            var tempContainer = AutoHandExtensions.transformRuler;
            tempContainer.rotation = Quaternion.identity;
            tempContainer.position = getTransform.position;
            tempContainer.localScale = getTransform.lossyScale;

            var handMatch = AutoHandExtensions.transformRulerChild;
            handMatch.position = hand.transform.position;
            handMatch.rotation = hand.transform.rotation;

            tempContainer.rotation = getTransform.rotation;

            tempContainer.eulerAngles = getTransform.rotation * up;
            tempContainer.RotateAround(tempContainer.transform.position, getTransform.rotation * up, angle);
            tempContainer.transform.position = getTransform.position +  getTransform.rotation * up * range;

            hand.transform.position = handMatch.position;
            hand.transform.rotation = handMatch.rotation;
            tempContainer.localScale = Vector3.one;

#if UNITY_EDITOR
            if(Application.isEditor)
                DestroyImmediate(tempContainer.gameObject);
#endif

            return base.GetNewPoseData(hand);
        }

        Transform GetTransform() {
            return centerObject != null ? centerObject : transform;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected() {
            if(Application.isPlaying)
                return;

            var usingTransform = GetTransform();
            var radius = 0.1f;
            
            var pose = HasPose(false) ? rightPose : leftPose;

            var handDir = Quaternion.AngleAxis(minAngle, usingTransform.rotation * up) * pose.handOffset.normalized;
            Handles.DrawWireArc(usingTransform.position, usingTransform.rotation * up, handDir, maxAngle-minAngle, radius);
            
            var minRangeVec = usingTransform.position + usingTransform.rotation * up * minRange;
            var maxRangeVec = usingTransform.position + usingTransform.rotation * up * maxRange;
            Gizmos.DrawLine(usingTransform.position, minRangeVec);
            Gizmos.DrawLine(usingTransform.position, maxRangeVec);

            if (editorHand != null && (testAngle != lastAngle || testRange != lastRange)) {
                testAngle = Mathf.Clamp(testAngle, minAngle, maxAngle);
                testRange = Mathf.Clamp(testRange, minRange, maxRange);

                if (minAngle > maxAngle) {
                    var temp = minAngle;
                    minAngle = maxAngle;
                    maxAngle = temp;
                }
                if (minRange > maxRange) {
                    var temp = minRange;
                    minRange = maxRange;
                    maxRange = temp;
                }
                
                lastAngle = testAngle;
                lastRange = testRange;
                if(CanSetPose(editorHand, transform.GetComponent<Grabbable>()))
                    GetHandPoseData(editorHand, testAngle, testRange).SetPose(editorHand, GetTransform());
            }
        }
#endif
    }
}
