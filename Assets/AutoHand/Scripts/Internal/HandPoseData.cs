using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Autohand {
    [System.Serializable]
    public struct HandPoseData{
        public Vector3 handOffset;

        /// <summary>DEPRECATED -> USE LOCAL_QUATERNION_OFFSET INSTEAD</summary>
        public Vector3 rotationOffset;
        public Quaternion localQuaternionOffset;
        public Vector3[] posePositions;
        public Quaternion[] poseRotations;
        
        /// <summary>Creates a new pose using the current hand relative to a given grabbable</summary>
        public HandPoseData(Hand hand, Grabbable grabbable) {
            posePositions = new Vector3[0];
            poseRotations = new Quaternion[0];
            handOffset = new Vector3();
            rotationOffset = Vector3.zero;
            localQuaternionOffset = Quaternion.identity;
            SavePose(hand, grabbable.transform);
        }

        /// <summary>Creates a new pose using the current hand relative to a given grabbable</summary>
        public HandPoseData(Hand hand, Transform point) {
            posePositions = new Vector3[0];
            poseRotations = new Quaternion[0];
            handOffset = new Vector3();
            rotationOffset = Vector3.zero;
            localQuaternionOffset = Quaternion.identity;
            SavePose(hand, point);
        }
        
        /// <summary>Creates a new pose using the current hand shape</summary>
        public HandPoseData(Hand hand) {
            posePositions = new Vector3[0];
            poseRotations = new Quaternion[0];
            handOffset = new Vector3();
            rotationOffset = Vector3.zero;
            localQuaternionOffset = Quaternion.identity;
            SavePose(hand, null);
        }

        /// <summary>Creates a new pose using the current hand shape</summary>
        public HandPoseData(HandPoseData data) {
            posePositions = new Vector3[data.posePositions.Length];
            data.posePositions.CopyTo(posePositions, 0);

            poseRotations = new Quaternion[data.poseRotations.Length];
            data.poseRotations.CopyTo(poseRotations, 0);

            handOffset = data.handOffset;
            rotationOffset = data.rotationOffset;
            localQuaternionOffset = data.localQuaternionOffset;
        }




        public void SavePose(Hand hand, Transform relativeTo) {
            var posePositionsList = new List<Vector3>();
            var poseRotationsList = new List<Quaternion>();
            
            if(relativeTo != null){
                var tempContainer = AutoHandExtensions.transformRuler;
                tempContainer.localScale = relativeTo.lossyScale;
                tempContainer.transform.position = relativeTo.position;
                tempContainer.transform.rotation = relativeTo.rotation;
                var handMatch = AutoHandExtensions.transformRulerChild;
                handMatch.transform.position = hand.transform.position;
                handMatch.transform.rotation = hand.transform.rotation;

                handOffset = handMatch.localPosition;
                localQuaternionOffset = handMatch.localRotation;

#if UNITY_EDITOR
                if(Application.isEditor && !Application.isPlaying)
                    GameObject.DestroyImmediate(tempContainer.gameObject);
#endif
            }
            else {
                handOffset = hand.transform.localPosition;
                localQuaternionOffset = hand.transform.localRotation;
            }

            rotationOffset = Vector3.zero;

            foreach(var finger in hand.fingers) {
                AssignChildrenPose(finger.transform);
            }

            void AssignChildrenPose(Transform obj) {
                AddPoint(obj.localPosition, obj.localRotation);
                for(int j = 0; j < obj.childCount; j++) {
                    AssignChildrenPose(obj.GetChild(j));
                }
            }

            void AddPoint(Vector3 pos, Quaternion rot) {
                posePositionsList.Add(pos);
                poseRotationsList.Add(rot);
            }
            
            posePositions = new Vector3[posePositionsList.Count];
            poseRotations = new Quaternion[posePositionsList.Count];
            for(int i = 0; i < posePositionsList.Count; i++) {
                posePositions[i] = posePositionsList[i];
                poseRotations[i] = poseRotationsList[i];
            }
        }


        public Quaternion GetRotationOffset(){
            if (rotationOffset != Vector3.zero)
                localQuaternionOffset = Quaternion.Euler(rotationOffset);
            return localQuaternionOffset;
        }


        public void SetPose(Hand hand, Transform relativeTo = null) {
            //This might prevent static poses from breaking from the update
            if(rotationOffset != Vector3.zero)
                localQuaternionOffset = Quaternion.Euler(rotationOffset);

            if(relativeTo != null && relativeTo != hand.transform) {
                var tempContainer = AutoHandExtensions.transformRuler;
                tempContainer.localScale = relativeTo.lossyScale;
                tempContainer.position = relativeTo.position;
                tempContainer.rotation = relativeTo.rotation;
                var handMatch = AutoHandExtensions.transformRulerChild;
                handMatch.localPosition = handOffset;
                handMatch.localRotation = localQuaternionOffset;

                hand.transform.position = handMatch.position;
                hand.transform.rotation = handMatch.rotation;
                hand.body.position = hand.transform.position;
                hand.body.rotation = hand.transform.rotation;

#if UNITY_EDITOR
                if(Application.isEditor && !Application.isPlaying)
                    GameObject.DestroyImmediate(tempContainer.gameObject);
#endif
            }

            int i = -1;
            void AssignChildrenPose(Transform obj, HandPoseData pose) {
                i++;
                obj.localPosition = pose.posePositions[i];
                obj.localRotation = pose.poseRotations[i];
                for(int j = 0; j < obj.childCount; j++) {
                    AssignChildrenPose(obj.GetChild(j), pose);
                }
            }

            if(posePositions != null)
                foreach(var finger in hand.fingers)
                    AssignChildrenPose(finger.transform, this);
        }

        /// <summary>Sets the finger pose without changing the hands position</summary>
        public void SetFingerPose(Hand hand, Transform relativeTo = null) {
            
            int i = -1;
            void AssignChildrenPose(Transform obj, HandPoseData pose) {
                i++;
                obj.localPosition = pose.posePositions[i];
                obj.localRotation = pose.poseRotations[i];
                for(int j = 0; j < obj.childCount; j++) {
                    AssignChildrenPose(obj.GetChild(j), pose);
                }
            }

            if(posePositions != null)
                foreach(var finger in hand.fingers)
                    AssignChildrenPose(finger.transform, this);
        }

        /// <summary>Sets the position without setting the finger pose</summary>
        public void SetPosition(Hand hand, Transform relativeTo = null) {
            //This might prevent static poses from breaking from the update
            if(rotationOffset != Vector3.zero)
                localQuaternionOffset = Quaternion.Euler(rotationOffset);

            if(relativeTo != null && relativeTo != hand.transform) {
                var tempContainer = AutoHandExtensions.transformRuler;
                tempContainer.localScale = relativeTo.lossyScale;
                tempContainer.position = relativeTo.position;
                tempContainer.rotation = relativeTo.rotation;
                var handMatch = AutoHandExtensions.transformRulerChild;
                handMatch.localPosition = handOffset;
                handMatch.localRotation = localQuaternionOffset;

                hand.transform.position = handMatch.position;
                hand.transform.rotation = handMatch.rotation;
                hand.body.position = hand.transform.position;
                hand.body.rotation = hand.transform.rotation;

#if UNITY_EDITOR
                if(Application.isEditor && !Application.isPlaying)
                    GameObject.DestroyImmediate(tempContainer.gameObject);
#endif
            }
        }

        public static HandPoseData LerpPose(HandPoseData from, HandPoseData to, float point) {
            var lerpPose = new HandPoseData();
            lerpPose.handOffset = Vector3.Lerp(from.handOffset, to.handOffset, point);
            lerpPose.localQuaternionOffset = Quaternion.Lerp(from.localQuaternionOffset, to.localQuaternionOffset, point);
            lerpPose.posePositions = new Vector3[from.posePositions.Length];
            lerpPose.poseRotations = new Quaternion[from.poseRotations.Length];

            for(int i = 0; i < from.posePositions.Length; i++) {
                lerpPose.posePositions[i] = Vector3.Lerp(from.posePositions[i], to.posePositions[i], point);
                lerpPose.poseRotations[i] = Quaternion.Lerp(from.poseRotations[i], to.poseRotations[i], point);
            }

            return lerpPose;
        }

        public static void LerpPose(ref HandPoseData lerpPose, HandPoseData from, HandPoseData to, float point) {
            lerpPose.handOffset = Vector3.Lerp(from.handOffset, to.handOffset, point);
            lerpPose.localQuaternionOffset = Quaternion.Lerp(from.localQuaternionOffset, to.localQuaternionOffset, point);
            lerpPose.posePositions = new Vector3[from.posePositions.Length];
            lerpPose.poseRotations = new Quaternion[from.poseRotations.Length];

            for(int i = 0; i < from.posePositions.Length; i++) {
                lerpPose.posePositions[i] = Vector3.Lerp(from.posePositions[i], to.posePositions[i], point);
                lerpPose.poseRotations[i] = Quaternion.Lerp(from.poseRotations[i], to.poseRotations[i], point);
            }
        }
    }
}
