#if UNITY_EDITOR

using SEE.Net;
using System;
using UnityEditor;

namespace SEEEditor
{

    /// <summary>
    /// Custom editor for a <see cref="TransformView"/>.
    /// </summary>
    [CustomEditor(typeof(TransformView))]
    public class TransformViewEditor : Editor
    {
        /// <summary>
        /// Whether infos should be displayed.
        /// </summary>
        public bool showInfos = true;

        /// <summary>
        /// Whether setting should be displayed.
        /// </summary>
        public bool showSettings = true;

        public override void OnInspectorGUI()
        {
            SerializedProperty transformToSynchronize = serializedObject.FindProperty("transformToSynchronize");
            SerializedProperty synchronizePosition = serializedObject.FindProperty("synchronizePosition");
            SerializedProperty synchronizeRotation = serializedObject.FindProperty("synchronizeRotation");
            SerializedProperty synchronizeScale = serializedObject.FindProperty("synchronizeScale");
            SerializedProperty teleportForGreatDistances = serializedObject.FindProperty("teleportForGreatDistances");
            SerializedProperty teleportMinDistance = serializedObject.FindProperty("teleportMinDistance");

            // Infos
            showInfos = EditorGUILayout.BeginFoldoutHeaderGroup(showInfos, "Infos");
            if (showInfos)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    EditorGUILayout.LabelField("Update Start Time Offset", TimeSpan.FromSeconds(TransformView.UpdateTimeStartDelay).ToString(@"ss\.ffff") + " s");
                    EditorGUILayout.LabelField("Update Frequency", TimeSpan.FromSeconds(TransformView.UpdateRepeatRate).ToString(@"ss\.ffff") + " s");
                }
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            // Settings
            showSettings = EditorGUILayout.BeginFoldoutHeaderGroup(showSettings, "Settings");
            if (showSettings)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    EditorGUILayout.PropertyField(transformToSynchronize);
                    EditorGUILayout.Space();
                    EditorGUILayout.PropertyField(synchronizePosition);
                    EditorGUILayout.PropertyField(synchronizeRotation);
                    EditorGUILayout.PropertyField(synchronizeScale);
                    EditorGUILayout.Space();
                    EditorGUILayout.PropertyField(teleportForGreatDistances);
                    EditorGUI.BeginDisabledGroup(!teleportForGreatDistances.boolValue);
                    {
                        EditorGUILayout.PropertyField(teleportMinDistance);
                    }
                    EditorGUI.EndDisabledGroup();
                }
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }
    }

}

#endif
