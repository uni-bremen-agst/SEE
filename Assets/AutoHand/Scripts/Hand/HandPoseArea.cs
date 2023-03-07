using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Autohand{
    [HelpURL("https://app.gitbook.com/s/5zKO0EvOjzUDeT2aiFk3/auto-hand/custom-poses#hand-pose-areas")]
    public class HandPoseArea : MonoBehaviour{
        public string poseName;
        public int poseIndex = 0;


        public float transitionTime = 0.2f;

        [Header("Events")]
        public UnityHandEvent OnHandEnter = new UnityHandEvent();
        public UnityHandEvent OnHandExit = new UnityHandEvent();

        [HideInInspector, Tooltip("Scriptable options NOT REQUIRED (will be saved locally instead if empty) -> Create scriptable throught [Auto Hand/Custom Pose]")]
        public HandPoseScriptable poseScriptable;
#if UNITY_EDITOR
        [HideInInspector]
        public bool showEditorTools = true;
        [HideInInspector, Tooltip("Used to pose for the grabbable")]
        public Hand editorHand;
#endif

        [HideInInspector]
        public HandPoseData rightPose;
        [HideInInspector]
        public bool rightPoseSet = false;
        [HideInInspector]
        public HandPoseData leftPose;
        [HideInInspector]
        public bool leftPoseSet = false;

        internal HandPoseArea[] poseAreas;
        List<Hand> posingHands = new List<Hand>();

        private void Start(){
            poseAreas = GetComponents<HandPoseArea>();
        }

        private void OnEnable() {
            OnHandEnter.AddListener(HandEnter);
            OnHandExit.AddListener(HandExit);
        }

        private void OnDisable() {
            for(int i = posingHands.Count - 1; i >= 0; i--) 
                posingHands[i].TryRemoveHandPoseArea(this);
            OnHandEnter.RemoveListener(HandEnter);
            OnHandExit.RemoveListener(HandExit);
        }

        void HandEnter(Hand hand) {
            posingHands.Add(hand);
        }
        
        void HandExit(Hand hand) {
            posingHands.Remove(hand);
        }


        public virtual HandPoseData GetHandPoseData(bool left) {
            if(poseScriptable != null)
                return (left) ? poseScriptable.leftPose : poseScriptable.rightPose;

            return (left) ? leftPose : rightPose;
        }


        public void SetHandPose(Hand hand) {
            HandPoseData pose;
            if(hand.left){
                if(leftPoseSet) pose = leftPose;
                else return;
            }
            else{
                if(rightPoseSet) pose = rightPose;
                else return;
            }

            pose.SetPose(hand, transform);
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

        public void SaveScriptable(){
            if (poseScriptable != null){
                if (rightPoseSet)
                    poseScriptable.SaveRightPose(rightPose);
                if (leftPoseSet)
                    poseScriptable.SaveLeftPose(leftPose);
            }
        }

        //This is because parenting is used at runtime, but cannot be used on prefabs in editor so a copy is required
        public void EditorCreateCopySetPose(Hand hand, Transform relativeTo) {
            Hand handCopy;
            if(hand.name != "HAND COPY DELETE")
                handCopy = Instantiate(hand, relativeTo.transform.position, hand.transform.rotation);
            else
                handCopy = hand;

            handCopy.name = "HAND COPY DELETE";
            var referenceHand = handCopy.gameObject.AddComponent<EditorHand>();
            referenceHand.grabbablePoseArea = this;
            referenceHand.grabbablePose = null;

            editorHand = handCopy;

            Selection.activeGameObject = editorHand.gameObject;
            SceneView.lastActiveSceneView.FrameSelected();

            if(hand.left && leftPoseSet) {
                leftPose.SetPose(handCopy, transform);
            }
            else if(!hand.left && rightPoseSet) {
                rightPose.SetPose(handCopy, transform);
            }
            else {
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
            
            var posePositionsList = new List<Vector3>();
            var poseRotationsList = new List<Quaternion>();
            
            var handCopy = Instantiate(hand, hand.transform.position, hand.transform.rotation);
            var handParent = handCopy.transform.parent;
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
            rightPoseSet = false;
        }
#endif

        public bool HasPose(bool left) {
            if(poseScriptable != null && ((left) ? poseScriptable.leftSaved : poseScriptable.rightSaved))
                return (left) ? poseScriptable.leftSaved : poseScriptable.rightSaved;
            return left ? leftPoseSet : rightPoseSet;
        }
    }
}
