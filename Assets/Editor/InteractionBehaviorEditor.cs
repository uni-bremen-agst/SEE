using UnityEngine;
using UnityEditor;
using SEE.Controls;

[CustomEditor(typeof(InteractionBehavior))]
[CanEditMultipleObjects]
public class InteractionBehaviorEditor : Editor
{
    /// <summary>
    /// Reference to the script this editor representation is built for.
    /// </summary>
    private InteractionBehavior targetScript;

    /// <summary>
    /// Serialized list of mappings from original InteractionBehavior script.
    /// </summary>
    SerializedProperty SMappings;

    /// <summary>
    /// Serialized mapping from original InteractionBehavior script which is currently set active.
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
        targetScript = target as InteractionBehavior;
        SMappings = serializedObject.FindProperty("mappings");
        SCurrentMapping = serializedObject.FindProperty("CurrentMapping");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // Shows current mapping in inspector.
        GUILayout.Space(10f);
        GUILayout.BeginHorizontal();
        GUILayout.Label("Active mapping:", EditorStyles.boldLabel);         
        ActionMapping activeMapping = targetScript.GetActive();
        GUILayout.Label(activeMapping != null ? activeMapping.Name : "");
        GUILayout.Label("Mapping type:", EditorStyles.boldLabel);
        GUILayout.Label(activeMapping != null ? activeMapping.GetTypeAsString() : "");
        GUILayout.EndHorizontal();
        GUILayout.Space(10f);

        int size = SMappings.arraySize;

        for (int i = 0; i < size; i++)
        {
            ActionMapping actionMapping = targetScript.GetMapping(i);
            if (actionMapping != null)
            {
                // Name and type of mapping
                GUILayout.BeginHorizontal();
                string mappingName = actionMapping.Name;
                string newName = EditorGUILayout.TextField(mappingName);
                if (mappingName != newName)
                {
                    actionMapping.Name = newName;
                }
                EditorGUILayout.PropertyField(SMappings.GetArrayElementAtIndex(i), GUIContent.none);
                GUILayout.EndHorizontal();

                // Activate and Delete buttons
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Activate"))
                {
                    targetScript.SetActive(i);
                }
                if (GUILayout.Button("Delete"))
                {
                    targetScript.RemoveMapping(i);
                }
                GUILayout.EndHorizontal();
            } 
            else
            {
                Debug.LogErrorFormat("Mapping at index {0} (out of {1}) was undefined and has been removed.\n", i, size);
                // We fix the problem by removing the undefined mapping and terminate the loop.
                // The correction will become active in very short time when OnInspectorGUI()
                // is called (note: OnInspectorGUI is called periodically when the inspector has
                // the mouse focus).
                targetScript.RemoveMapping(i);
                break;
            }
        }

        GUILayout.BeginHorizontal();
        SelectedType = (INPUTTYPE)EditorGUILayout.EnumPopup("Type of Set:", SelectedType);
        if (GUILayout.Button("Add New Set"))
        {
            InstantiateNewMapping(SelectedType);
        }
        GUILayout.EndHorizontal();

        serializedObject.ApplyModifiedProperties();
    }

    /// <summary>
    /// Creates new window for editing.
    /// </summary>
    /// <param name="mapping">mapping to be edited</param>
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
                Debug.LogWarning("Empty type cannot be instantiated.\n");
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
            default:
                Debug.LogErrorFormat("Unhandled input type {0}\n", type);
                break;
        }

        targetScript.AddMapping(NewMapping);
    }
}
