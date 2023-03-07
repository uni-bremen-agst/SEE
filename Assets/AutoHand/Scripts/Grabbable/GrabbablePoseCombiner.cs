using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Autohand{
    public class GrabbablePoseCombiner : MonoBehaviour{
        public List<GrabbablePose> poses = new List<GrabbablePose>();

        HandPoseData pose;

        public bool CanSetPose(Hand hand, Grabbable grab) {
            foreach(var pose in poses) {
                if(pose != null && pose.CanSetPose(hand, grab))
                    return true;
            }
            return false;
        }

        public void AddPose(GrabbablePose pose) {
            if(!poses.Contains(pose))
                poses.Add(pose);
        }

        private void OnDestroy()
        {
            for (int i = poses.Count - 1; i >= 0; i--)
            {
                Destroy(poses[i]);
            }
        }

        public GrabbablePose GetClosestPose(Hand hand, Grabbable grab) {
            List<GrabbablePose> possiblePoses = new List<GrabbablePose>();
            foreach(var handPose in this.poses)
                if(handPose != null && handPose.CanSetPose(hand, grab))
                    possiblePoses.Add(handPose);
            
            float closestValue = float.MaxValue;
            int closestIndex = 0;


            for (int i = 0; i < possiblePoses.Count; i++){
                var pregrabPos = hand.transform.position;
                var pregrabRot = hand.transform.rotation;
                var pregrabBodPos = hand.body.position;
                var pregrabBodRot = hand.body.rotation;

                var tempContainer = AutoHandExtensions.transformRuler;
                tempContainer.rotation = Quaternion.identity;
                tempContainer.position = possiblePoses[i].transform.position;
                tempContainer.localScale = possiblePoses[i].transform.lossyScale;

                var handMatch = AutoHandExtensions.transformRulerChild;
                handMatch.position = hand.transform.position;
                handMatch.rotation = hand.transform.rotation;

                pose = possiblePoses[i].GetHandPoseData(hand);

                handMatch.localPosition = pose.handOffset;
                handMatch.localRotation = pose.localQuaternionOffset;

                var distance = Vector3.Distance(handMatch.position, pregrabPos);
                var angleDistance = Quaternion.Angle(handMatch.rotation, pregrabRot) / 90f;

                var closenessValue = distance / possiblePoses[i].positionWeight + angleDistance / possiblePoses[i].rotationWeight;
                if(closenessValue < closestValue) {
                    closestIndex = i;
                    closestValue = closenessValue;
                }

                hand.transform.position = pregrabPos;
                hand.transform.rotation = pregrabRot;
                hand.body.position = pregrabBodPos;
                hand.body.rotation = pregrabBodRot;
            }

            return possiblePoses[closestIndex];
        }

        internal int PoseCount() {
            return poses.Count;
        }
    }
}
