using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using SEE.Controls;

[CustomEditor(typeof(InteractionBehavior))]
[CanEditMultipleObjects]
public class InteractionBehaviorEditor : Editor
{
    /// <summary>
    /// Reference to the script this editor representation is build for.
    /// </summary>
    private InteractionBehavior targetScript;

    /// <summary>
    /// Serialized list of mappings from original InteractionBehavior script.
    /// </summary>
    SerializedProperty SMappings;

    /// <summary>
    /// Serialized mapping from original InteractionBehavior script which ich currently set active.
    /// </summary>
    SerializedProperty SCurrentMapping;

    /// <summary>
    /// Internal type representation needed for dropdown list in window.
    /// </summary>
    private enum INPUTTYPE
    {
        Empty,
        Vive,
        Leap,
        Keys,
        Touch,
        Con
    };

    /// <summary>
    /// The selected type in dropdown list.
    /// </summary>
    private INPUTTYPE SelectedType;

    /// <summary>
    /// Reference to the new mapping object.
    /// </summary>
    private ActionMapping NewMapping;

    private void OnEnable()
    {
        targetScript = (InteractionBehavior)target;
        SMappings = serializedObject.FindProperty("mappings");
        SCurrentMapping = serializedObject.FindProperty("CurrentMapping");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        GUILayout.Label("Control Sets", EditorStyles.boldLabel);
        GUILayout.Space(5f);

        //EditorGUILayout.PropertyField(SCurrentMapping);

        int size = SMappings.arraySize;

        for(int i = 0; i < size; i++)
        {
            EditorGUILayout.PropertyField(SMappings.GetArrayElementAtIndex(i));

            GUILayout.BeginHorizontal();
            if(GUILayout.Button("Select"))
            {
                SCurrentMapping = SMappings.GetArrayElementAtIndex(i);
            }
            if(GUILayout.Button("Delete"))
            {
                targetScript.RemoveMapping(i);
            }
            GUILayout.EndHorizontal();
        }

        GUILayout.BeginHorizontal();
        SelectedType = (INPUTTYPE)EditorGUILayout.EnumPopup("Type of Set:", SelectedType);
        if (GUILayout.Button("Add new Set"))
        {
            InstantiateNewMapping(SelectedType);
        }
        GUILayout.EndHorizontal();

        serializedObject.ApplyModifiedProperties();
    }

    /// <summary>
    /// Creates new Window for editing.
    /// </summary>
    /// <param name="mapping"></param>
    private void OpenSettingWindow(ActionMapping mapping)
    {
        SetEditingWindow instance = ScriptableObject.CreateInstance<SetEditingWindow>();
        instance.initWindow(mapping);
    }

    /// <summary>
    /// Returns the class internal enum representation of the given ActionMapping type.
    /// </summary>
    /// <param name="mapping"></param>
    /// <returns></returns>
    private void InstantiateNewMapping(INPUTTYPE type)
    {
        switch (type)
        {
            case INPUTTYPE.Empty:
                Debug.Log("Empty type cant be Instatiatet.");
                return;
            case INPUTTYPE.Keys:
                NewMapping = ScriptableObject.CreateInstance<KeyActionMapping>();
                break;
            case INPUTTYPE.Vive:
                NewMapping = ScriptableObject.CreateInstance<ViveActionMapping>();
                break;
            case INPUTTYPE.Leap:
                break;
            case INPUTTYPE.Touch:
                break;
            case INPUTTYPE.Con:
                break;
        }

        targetScript.AddMapping(NewMapping);
    }
}
