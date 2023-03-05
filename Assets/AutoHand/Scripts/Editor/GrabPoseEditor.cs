using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.SceneManagement;
using UnityEngine;
using UnityEditor.SceneManagement;



namespace Autohand {
    [CustomEditor(typeof(GrabbablePose), true), CanEditMultipleObjects]
    public class GrabPoseEditor : Editor{
        GrabbablePose grabbablePose;

        private void OnEnable() {
            grabbablePose = target as GrabbablePose;
        }

        public override void OnInspectorGUI() {
            DrawDefaultInspector();

            var startBackground = GUI.backgroundColor;

            if(grabbablePose.gameObject.scene.name == null) {
                EditorGUILayout.LabelField("This must be saved in the scene");
                EditorGUILayout.LabelField("-> then use override to prefab to save");
                return;
            }
            else if(Application.isPlaying) {
                EditorGUILayout.LabelField("Cannot edit during runtime");
                return;
            }

            if(grabbablePose.gameObject != null && PrefabStageUtility.GetPrefabStage(grabbablePose.gameObject) == null) {
                grabbablePose.showEditorTools = DrawAutoToggleHeader("Show Editor Tools", grabbablePose.showEditorTools);

                if(grabbablePose.showEditorTools) {

                    ShowScriptableSaveButton();

                    ShowHandEditorHand();

                    ShowSaveButtons();

                    DrawHorizontalLine();

                    ShowDeleteOptions();
                }
            }

            GUI.backgroundColor = startBackground;
        }


        public void ShowScriptableSaveButton() {
            EditorGUILayout.Space();
            EditorGUILayout.Space();

            grabbablePose.poseScriptable = (HandPoseScriptable)EditorGUILayout.ObjectField(new GUIContent("Pose Scriptable", "Allows you to save the pose to a scriptable pose, create scriptable pose by right clicking in project [Create > Auto hand > Custom Pose]"), grabbablePose.poseScriptable, typeof(HandPoseScriptable), true);

            if(grabbablePose.poseScriptable != null) {
                var rect = EditorGUILayout.GetControlRect();

                if(GUI.Button(rect, "Overwrite Scriptable")) {
                    EditorUtility.SetDirty(grabbablePose.poseScriptable);
                    grabbablePose.SaveScriptable();
                }

                EditorGUILayout.Space();
            }
            EditorGUILayout.Space();
        }

        public void ShowDeleteOptions() {
            GUI.backgroundColor = Color.red;

            if(GUILayout.Button("Delete Hand Copy")) {
                if(string.Equals(grabbablePose.editorHand.transform.parent.name, "HAND COPY CONTAINER DELETE"))
                    DestroyImmediate(grabbablePose.editorHand.transform.parent.gameObject);
                else
                    Debug.LogError("Not a copy - Will not delete");
            }
            if(GUILayout.Button("Clear Saved Poses")) {
                EditorUtility.SetDirty(grabbablePose);
                grabbablePose.EditorClearPoses();
            }

        }

        public void ShowHandEditorHand() {
            grabbablePose.editorHand = (Hand)EditorGUILayout.ObjectField(new GUIContent("Editor Hand", "This will be used as a reference to create a hand copy that can be used to model your new pose"), grabbablePose.editorHand, typeof(Hand), true);

            if(GUILayout.Button("Create Hand Copy")) {
                EditorUtility.SetDirty(grabbablePose);
                grabbablePose.EditorCreateCopySetPose(grabbablePose.editorHand, grabbablePose.transform);
            }

            if(GUILayout.Button("Select Hand Copy")) {
                EditorUtility.SetDirty(grabbablePose);
                Selection.activeGameObject = grabbablePose.editorHand.gameObject;
            }
        }

        public void DrawHorizontalLine() {

            var rect = EditorGUILayout.GetControlRect();
            rect.y += rect.height / 2f;
            rect.height /= 10f;

            EditorGUI.DrawRect(rect, Color.grey);
        }

        public bool DrawAutoToggleHeader(string label, bool value) {

            EditorGUILayout.Space();
            EditorGUILayout.Space();


            // draw header background and label
            var headerRect = EditorGUILayout.GetControlRect();

            var biggerRect = new Rect(headerRect);
            biggerRect.width += biggerRect.x * 2;
            biggerRect.x = 0;
            biggerRect.y -= 5f;
            biggerRect.height += 10f;
            EditorGUI.DrawRect(biggerRect, Constants.BackgroundColor);


            var labelStyle = Constants.LabelStyle;

            var oldColor1 = GUI.color;
            if(!value) {
                var newColor = new Color(0.65f, 0.65f, 0.65f, 1f);
                newColor.a = 1;
                GUI.contentColor = newColor;
            }

            EditorGUI.LabelField(headerRect, new GUIContent("   " + label), labelStyle);

            GUI.contentColor = oldColor1;

            var oldColor = GUI.color;
            GUI.color = value ? new Color(0.7f, 1f, 0.7f) : new Color(1f, 0.7f, 0.7f);

            var newRect = new Rect(headerRect);
            newRect.position = new Vector2(newRect.x + newRect.width - 18, newRect.y);
            value = EditorGUI.Toggle(newRect, value);

            GUI.color = oldColor;


            return value;
        }

        public void ShowSaveButtons() {
            EditorGUILayout.Space();
            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();

            if(grabbablePose.leftPoseSet || (grabbablePose.poseScriptable != null && grabbablePose.poseScriptable.leftSaved))
                GUI.backgroundColor = Color.green;
            else
                GUI.backgroundColor = Color.red;


            if(GUILayout.Button("Save Left")) {
                EditorUtility.SetDirty(grabbablePose);
                if(grabbablePose.poseIndex != grabbablePose.editorHand.poseIndex)
                    Debug.LogError("CANNOT SAVE: Your hand's \"Pose Index\" value does not match the local \"Pose Index\" value");
                else
                    grabbablePose.EditorSaveGrabPose(grabbablePose.editorHand, true);
            }


            if(grabbablePose.rightPoseSet || (grabbablePose.poseScriptable != null && grabbablePose.poseScriptable.rightSaved))
                GUI.backgroundColor = Color.green;
            else
                GUI.backgroundColor = Color.red;


            if(GUILayout.Button("Save Right")) {
                EditorUtility.SetDirty(grabbablePose);
                if(grabbablePose.poseIndex != grabbablePose.editorHand.poseIndex)
                    Debug.LogError("CANNOT SAVE: Your hand's \"Pose Index\" value does not match the local \"Pose Index\" value");
                else
                    grabbablePose.EditorSaveGrabPose(grabbablePose.editorHand, false);
            }


            GUILayout.EndHorizontal();
        }

    }
}
