using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Autohand{


#if UNITY_EDITOR
    [CanEditMultipleObjects]
#endif
    [HelpURL("https://app.gitbook.com/s/5zKO0EvOjzUDeT2aiFk3/auto-hand/custom-poses")]
    public class GrabbablePose : MonoBehaviour{
        [AutoHeader("Grabbable Pose")]
        public bool ignoreMe;
        public bool poseEnabled = true;
        [Tooltip("Purely for organizational purposes in the editor")]
        public string poseName = "";
        [Tooltip("This value must match the pose index of the a hand in order for the pose to work")]
        public int poseIndex = 0;
        [Tooltip("Whether or not this pose can be used by both hands at once or only one hand at a time")]
        public bool singleHanded = false;


        [AutoSmallHeader("Advanced Settings")]
        public bool showAdvanced = true;
        public float positionWeight = 1;
        public float rotationWeight = 1;
        [Tooltip("These poses will only be enabled when this pose is active. Great for secondary poses like holding the front of a gun with your second hand, only while holding the trigger")]
        public GrabbablePose[] linkedPoses;



        [HideInInspector]
        public bool showEditorTools = true;
        [Tooltip("Scriptable options NOT REQUIRED -> Create scriptable throught [Auto Hand/Custom Pose]")]
        [HideInInspector]
        public HandPoseScriptable poseScriptable;

        [Tooltip("Used to pose for the grabbable")]
        [HideInInspector]
        public Hand editorHand;



        [HideInInspector]
        public HandPoseData rightPose;
        [HideInInspector]
        public bool rightPoseSet = false;
        [HideInInspector]
        public HandPoseData leftPose;
        [HideInInspector]
        public bool leftPoseSet = false;

        public List<Hand> posingHands { get; protected set; }

        protected virtual void Awake() {
            posingHands = new List<Hand>();
            if (poseScriptable != null)
            {
                if (poseScriptable.leftSaved)
                    leftPoseSet = true;
                if (poseScriptable.rightSaved)
                    rightPoseSet = true;
            }

            for (int i = 0; i < linkedPoses.Length; i++)
                linkedPoses[i].poseEnabled = false;
        }


        public bool CanSetPose(Hand hand, Grabbable grab) {
            if(singleHanded && posingHands.Count > 0 && !posingHands.Contains(hand) && !(grab.singleHandOnly && grab.allowHeldSwapping))
                return false;
            if(hand.poseIndex != poseIndex)
                return false;
            if(hand.left && !leftPoseSet)
                return false;
            if(!hand.left && !rightPoseSet)
                return false;

            return poseEnabled;
        }


        public virtual HandPoseData GetHandPoseData(Hand hand) {
            if(poseScriptable != null)
                return (hand.left) ? poseScriptable.leftPose : poseScriptable.rightPose;

            return (hand.left) ? leftPose : rightPose;
        }


        /// <summary>Sets the hand to this pose, make sure to check CanSetPose() flag for proper use</summary>
        /// <param name="isProjection">for pose projections, so they wont fill condition for single handed before grab</param>
        public virtual void SetHandPose(Hand hand, bool isProjection = false) {
            if(!isProjection) {
                if(!posingHands.Contains(hand))
                    posingHands.Add(hand);

                for(int i = 0; i < linkedPoses.Length; i++)
                    linkedPoses[i].poseEnabled = true;
            }

            GetHandPoseData(hand).SetPose(hand, transform);

        }


        public virtual void CancelHandPose(Hand hand) {
            if(posingHands.Contains(hand)) {
                posingHands.Remove(hand);
            }

            for(int i = 0; i < linkedPoses.Length; i++)
                linkedPoses[i].poseEnabled = false;
        }


        public HandPoseData GetNewPoseData(Hand hand) {
            var pose = new HandPoseData();

            var posePositionsList = new List<Vector3>();
            var poseRotationsList = new List<Quaternion>();

            var tempContainer = AutoHandExtensions.transformRuler;
            tempContainer.position = transform.position;
            tempContainer.rotation = transform.rotation;
            tempContainer.localScale = transform.lossyScale;

            var handMatch = AutoHandExtensions.transformRulerChild;
            handMatch.position = hand.transform.position;
            handMatch.rotation = hand.transform.rotation;

            pose.handOffset = handMatch.localPosition;
            pose.localQuaternionOffset = handMatch.localRotation;

            tempContainer.localScale = Vector3.one;

            foreach (var finger in hand.fingers) {
                AssignChildrenPose(finger.transform);
            }

            void AssignChildrenPose(Transform obj) {
                AddPoint(obj.localPosition, obj.localRotation);
                for (int j = 0; j < obj.childCount; j++) {
                    AssignChildrenPose(obj.GetChild(j));
                }
            }

            void AddPoint(Vector3 pos, Quaternion rot) {
                posePositionsList.Add(pos);
                poseRotationsList.Add(rot);
            }

            pose.posePositions = new Vector3[posePositionsList.Count];
            pose.poseRotations = new Quaternion[posePositionsList.Count];
            for (int i = 0; i < posePositionsList.Count; i++) {
                pose.posePositions[i] = posePositionsList[i];
                pose.poseRotations[i] = poseRotationsList[i];
            }

#if UNITY_EDITOR
            if(Application.isEditor && !Application.isPlaying)
                DestroyImmediate(tempContainer.gameObject);
#endif
            return pose;
        }


#if UNITY_EDITOR
        [ContextMenu("SAVE RIGHT")]
        public void EditorSavePoseRight() {
            if(editorHand != null)
                EditorSaveGrabPose(editorHand, false);
            else
                Debug.Log("Editor Hand must be assigned");
        }

        [ContextMenu("SAVE LEFT")]
        public void EditorSavePoseLeft() {
            if(editorHand != null)
                EditorSaveGrabPose(editorHand, true);
            else
                Debug.Log("Editor Hand must be assigned");
        }

        [ContextMenu("OVERWRITE SCRIPTABLE")]
        public void SaveScriptable(){
            if (poseScriptable != null){
                if (rightPoseSet)
                    poseScriptable.SaveRightPose(rightPose);
                if (leftPoseSet)
                    poseScriptable.SaveLeftPose(leftPose);
            }
        }

        //This is because parenting is used at runtime, but cannot be used on prefabs in editor so a copy is required
        public void EditorCreateCopySetPose(Hand hand, Transform relativeTo){
            Hand handCopy;
            if (hand.name != "HAND COPY DELETE")
                handCopy = Instantiate(hand, relativeTo.transform.position, hand.transform.rotation);
            else
                handCopy = hand;

            handCopy.name = "HAND COPY DELETE";
            var referenceHand = handCopy.gameObject.AddComponent<EditorHand>();
            referenceHand.grabbablePoseArea = null;
            referenceHand.grabbablePose = this;

            editorHand = handCopy;

            Selection.activeGameObject = editorHand.gameObject; 
            SceneView.lastActiveSceneView.FrameSelected();

            if(hand.left && leftPoseSet){
                leftPose.SetPose(handCopy, transform);
            }
            else if(!hand.left && rightPoseSet){
                rightPose.SetPose(handCopy, transform);
            }
            else
            {
                handCopy.transform.position = relativeTo.transform.position; 
                editorHand.RelaxHand();
            }

            var contrainer = new GameObject();
            contrainer.name = "HAND COPY CONTAINER DELETE";
            contrainer.transform.position = relativeTo.transform.position;
            contrainer.transform.rotation = relativeTo.transform.rotation;
            handCopy.transform.parent = contrainer.transform;
            EditorGUIUtility.PingObject(handCopy);
            SceneView.lastActiveSceneView.FrameSelected();
        }

        public void EditorSaveGrabPose(Hand hand, bool left){
            var pose = new HandPoseData();
            
            hand.left = left;

            var posePositionsList = new List<Vector3>();
            var poseRotationsList = new List<Quaternion>();
            
            var handCopy = Instantiate(hand, hand.transform.position, hand.transform.rotation);
            handCopy.transform.parent = transform;
            pose.handOffset = handCopy.transform.localPosition;
            pose.localQuaternionOffset = handCopy.transform.localRotation;
            DestroyImmediate(handCopy.gameObject);

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
            
            pose.posePositions = new Vector3[posePositionsList.Count];
            pose.poseRotations = new Quaternion[posePositionsList.Count];
            for(int i = 0; i < posePositionsList.Count; i++) {
                pose.posePositions[i] = posePositionsList[i];
                pose.poseRotations[i] = poseRotationsList[i];
            }

            if(left){
                leftPose = pose;
                leftPoseSet = true;
                Debug.Log("Pose Saved - Left");
                if (poseScriptable != null)
                    if (!poseScriptable.leftSaved)
                        poseScriptable.SaveLeftPose(leftPose);
                }
            else{
                rightPose = pose;
                rightPoseSet = true;
                Debug.Log("Pose Saved - Right");
                if (poseScriptable != null)
                    if (!poseScriptable.rightSaved)
                        poseScriptable.SaveRightPose(rightPose);
            }
        }
        
        public void EditorClearPoses() {
            leftPoseSet = false;
            leftPose = new HandPoseData();
            rightPoseSet = false;
            rightPose = new HandPoseData();
        }
#endif

        public bool HasPose(bool left) {
            if(poseScriptable != null && ((left) ? poseScriptable.leftSaved : poseScriptable.rightSaved))
                return (left) ? poseScriptable.leftSaved : poseScriptable.rightSaved;
            return left ? leftPoseSet : rightPoseSet;
        }
    }
}
