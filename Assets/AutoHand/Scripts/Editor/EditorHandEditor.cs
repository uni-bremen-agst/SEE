using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Autohand {
    [CustomEditor(typeof(EditorHand))]
    public class EditorHandEditor : Editor {

        float bendFingers = 0;
        bool[] fingerStates = new bool[] { };

        private void OnEnable() {
            var hand = (target as EditorHand).hand;

            if(fingerStates.Length == 0)
                fingerStates = new bool[hand.fingers.Length];
            for(int i = 0; i < fingerStates.Length; i++) {
                fingerStates[i] = true;
            }

            hand.SetLayerRecursive(hand.transform, LayerMask.NameToLayer(hand.left ? Hand.leftHandLayerName : Hand.rightHandLayerName));
        }

        void OnSceneGUI() {
            var hand = (target as EditorHand).hand;

            Handles.BeginGUI();
            GUILayout.BeginArea(new Rect(30, 30, 150, 300));

            var rect1 = EditorGUILayout.BeginVertical();
            GUI.color = Color.grey;
            GUI.Box(rect1, GUIContent.none);
            EditorGUILayout.EndVertical();

            GUILayout.EndArea();
            Handles.EndGUI();


            Handles.BeginGUI();

            GUILayout.BeginArea(new Rect(60, 30, 150, 300));

            var rect = EditorGUILayout.BeginVertical();
            GUI.color = Color.grey;
            GUI.Box(rect, GUIContent.none);
            GUI.Box(rect, GUIContent.none);
            GUI.Box(rect, GUIContent.none);

            GUI.color = Color.white;

            GUILayout.BeginHorizontal();
            GUILayout.Label("Hand Pose Tool", AutoHandExtensions.LabelStyle(TextAnchor.MiddleCenter, FontStyle.Bold, 16));
            GUILayout.EndHorizontal();

            for(int i = 0; i < fingerStates.Length; i++) {
                GUILayout.BeginHorizontal();


                fingerStates[i] = GUILayout.Toggle(fingerStates[i], hand.fingers[i].name);

                GUILayout.EndHorizontal();
            }


            GUILayout.Space(5f);
            GUILayout.BeginHorizontal();
            GUI.backgroundColor = new Color(0.9f, 0.3f, 0.3f, 1f);

            if(GUILayout.Button("Grab")) {
                for(int i = 0; i < hand.fingers.Length; i++) {
                    if(fingerStates[i])
                        hand.fingers[i].BendFingerUntilHit(100, ~LayerMask.GetMask(Hand.rightHandLayerName, Hand.leftHandLayerName));
                }
            }
            GUILayout.EndHorizontal();

            GUI.backgroundColor = new Color(0.9f, 0.3f, 0.3f, 1f);

            if(GUILayout.Button("Invert Hand - X")) {
                var scale = hand.transform.parent.localScale;
                scale.x = -scale.x;
                hand.transform.parent.localScale = scale;
                hand.left = !hand.left;
            }
            if(GUILayout.Button("Invert Hand - Y")) {
                var scale = hand.transform.parent.localScale;
                scale.x = -scale.x;
                hand.transform.parent.Rotate(new Vector3(0, 0, 180));
                hand.transform.parent.localScale = scale;
                hand.left = !hand.left;
            }
            if(GUILayout.Button("Invert Hand - Z")) {
                var scale = hand.transform.parent.localScale;
                hand.transform.parent.Rotate(new Vector3(0, 180, 0));
                scale.x = -scale.x;
                hand.transform.parent.localScale = scale;
                hand.left = !hand.left;
            }

            GUILayout.BeginHorizontal();

            GUI.backgroundColor = Color.white;

            bendFingers = GUILayout.HorizontalSlider(bendFingers, 0, 1);

            GUI.backgroundColor = new Color(0.9f, 0.3f, 0.3f, 1f);
            if(GUILayout.Button("Set Bend")) {
                for(int i = 0; i < hand.fingers.Length; i++) {
                    if(fingerStates[i])
                        hand.fingers[i].SetFingerBend(bendFingers);
                }
            }

            GUILayout.EndHorizontal();

            ShowSaveButtons();

            GUI.backgroundColor = new Color(0.8f, 0.8f, 0.8f, 1f);

            GUILayout.Space(6f);
            if(GUILayout.Button("Select Grabbable")) {
                if((target as EditorHand).grabbablePose != null)
                    Selection.activeGameObject = (target as EditorHand).grabbablePose.gameObject;
                else
                    Selection.activeGameObject = (target as EditorHand).grabbablePoseArea.gameObject;
            }


            GUI.backgroundColor = new Color(1f, 0f, 0f, 1f);

            if(GUILayout.Button("Delete Hand Copy")) {
                if((target as EditorHand).grabbablePose != null)
                    Selection.activeGameObject = (target as EditorHand).grabbablePose.gameObject;
                else
                    Selection.activeGameObject = (target as EditorHand).grabbablePoseArea.gameObject;
                DestroyImmediate((target as EditorHand).hand.transform.parent.gameObject);
            }


            GUILayout.Space(3f);
            EditorGUILayout.EndVertical();


            GUILayout.EndArea();

            Handles.EndGUI();
        }


        public void ShowSaveButtons() {
            if((target as EditorHand).grabbablePose != null) {
                var pose = (target as EditorHand).grabbablePose;
                EditorGUILayout.Space();
                EditorGUILayout.Space();

                EditorGUILayout.BeginHorizontal();

                if(pose.leftPoseSet)
                    GUI.backgroundColor = Color.green;
                else
                    GUI.backgroundColor = Color.red;


                if(GUILayout.Button("Save Left"))
                {
                    if (pose.poseIndex != pose.editorHand.poseIndex)
                    {
                        Debug.Log("Automatically overriding local Pose Index to match hand Pose Index");
                        pose.poseIndex = pose.editorHand.poseIndex;
                    }
                    else
                        pose.EditorSaveGrabPose(pose.editorHand, true);
                }


                if(pose.rightPoseSet)
                    GUI.backgroundColor = Color.green;
                else
                    GUI.backgroundColor = Color.red;


                if(GUILayout.Button("Save Right")) {
                    if (pose.poseIndex != pose.editorHand.poseIndex)
                    {
                        Debug.Log("Automatically overriding local Pose Index to match hand Pose Index");
                        pose.poseIndex = pose.editorHand.poseIndex;
                    }
                    else
                        pose.EditorSaveGrabPose(pose.editorHand, false);
                }


                GUILayout.EndHorizontal();
            }
            else {
                var pose = (target as EditorHand).grabbablePoseArea;
                EditorGUILayout.Space();
                EditorGUILayout.Space();

                EditorGUILayout.BeginHorizontal();

                if(pose.leftPoseSet)
                    GUI.backgroundColor = Color.green;
                else
                    GUI.backgroundColor = Color.red;


                if(GUILayout.Button("Save Left")) {
                    if(pose.poseIndex != pose.editorHand.poseIndex)
                        Debug.LogError("CANNOT SAVE: Your hand's \"Pose Index\" value does not match the local \"Pose Index\" value");
                    else
                        pose.EditorSaveGrabPose(pose.editorHand, true);
                }


                if(pose.rightPoseSet)
                    GUI.backgroundColor = Color.green;
                else
                    GUI.backgroundColor = Color.red;


                if(GUILayout.Button("Save Right")) {
                    if(pose.poseIndex != pose.editorHand.poseIndex)
                        Debug.LogError("CANNOT SAVE: Your hand's \"Pose Index\" value does not match the local \"Pose Index\" value");
                    else
                        pose.EditorSaveGrabPose(pose.editorHand, false);
                }


                GUILayout.EndHorizontal();
            }
        }
    }
}