using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;


[CustomEditor(typeof(ControlMode))]
[CanEditMultipleObjects]

public class ControlModeEditor : Editor
{
    SerializedProperty viveSupport;
    SerializedProperty leapMotionSupport;

    private void OnEnable()
    {
        viveSupport = serializedObject.FindProperty("ViveControler");
        leapMotionSupport = serializedObject.FindProperty("LeapMotion");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUILayout.PropertyField(viveSupport);
        EditorGUILayout.PropertyField(leapMotionSupport);

        if (viveSupport.boolValue)
            leapMotionSupport.boolValue = false;
        else if (leapMotionSupport.boolValue)
            viveSupport.boolValue = false;

        serializedObject.ApplyModifiedProperties();
    }
}
