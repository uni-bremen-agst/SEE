using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Autohand
{
    [CreateAssetMenu(fileName = "AutoHand Pose", menuName = "Auto Hand/Custom Pose", order = 1)]
    public class HandPoseScriptable : ScriptableObject{
        [HideInInspector]
        public bool rightSaved;
        [HideInInspector]
        public bool leftSaved;
        public HandPoseData rightPose;
        public HandPoseData leftPose;

        public void SavePoses(HandPoseData right, HandPoseData left)
        {
            SaveRightPose(right);
            SaveLeftPose(left);
        }

        public void SaveRightPose(HandPoseData right)
        {
            rightPose = right;
            rightSaved = true;
        }

        public void SaveLeftPose(HandPoseData left)
        {
            leftPose = left;
            leftSaved = true;
        }
    }
}
