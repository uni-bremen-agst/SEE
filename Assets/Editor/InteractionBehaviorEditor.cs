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
        targetScript = (InteractionBehavior)target;
        SMappings = serializedObject.FindProperty("mappings");
        SCurrentMapping = serializedObject.FindProperty("CurrentMapping");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        //shows current mapping in inspector
        GUILayout.Space(10f);
        GUILayout.BeginHorizontal();
        GUILayout.Label("Active mapping:", EditorStyles.boldLabel);
        GUILayout.Label(targetScript.GetActive().GetName());
        GUILayout.Label("Mapping type:", EditorStyles.boldLabel);
        GUILayout.Label(targetScript.GetActive().GetTypeAsString());
        GUILayout.EndHorizontal();
        GUILayout.Space(10f);

        int size = SMappings.arraySize;

        for (int i = 0; i < size; i++)
        {
            ActionMapping actionMapping = targetScript.GetMapping(i);
            if (actionMapping != null)
            {
                string mappingName = actionMapping.GetName();
                if (string.IsNullOrEmpty(mappingName))
                {
                    Debug.LogErrorFormat("Mapping at index {0} does not have a name. Please assign one.\n", i);
                    mappingName = "";
                }
                EditorGUILayout.PropertyField(SMappings.GetArrayElementAtIndex(i),
                                              new GUIContent(mappingName));

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Activate"))
                {
                    Debug.LogFormat("InteractionBehaviorEditor: index of selected mapping: {0}\n", i);
                    targetScript.SetActive(i);
                    Debug.LogFormat("InteractionBehaviorEditor: selected mapping: {0}\n", SCurrentMapping);
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
        if (GUILayout.Button("Add new Set"))
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
                Debug.Log("Empty type cannot be instantiated.");
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
