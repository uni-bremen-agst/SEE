using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using SEE.Controls;
using UnityEngine.Events;

public class SetEditingWindow : EditorWindow
{
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
    /// The internal type representation of the current mapping.
    /// </summary>
    private INPUTTYPE SetType;

    /// <summary>
    /// The mappings name.
    /// </summary>
    private string SetName = "newName";

    /// <summary>
    /// The mapping to work with.
    /// </summary>
    private ActionMapping SelectedMapping;

    /// <summary>
    /// Function for initiating the window.
    /// </summary>
    /// <param name="mapping"></param>
    [MenuItem("Window/Set Configuration")]
    public void initWindow(ActionMapping mapping)
    {

        if(mapping == null)
        {
            SetType = INPUTTYPE.Empty;
        }
        else
        {
            SetName = mapping.Name;
            SetType = GetTypeForClass(mapping);
            SelectedMapping = mapping;
        }
        this.Show();
    }

    private void OnGUI()
    {

            GUILayout.BeginHorizontal();
            GUILayout.Label("Name");
            SetName = GUILayout.TextField(SetName);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Type of Set");
            GUILayout.Label(SetType.ToString());
            GUILayout.EndHorizontal();

        
            //EditorGUILayout.PropertyField(SelectedMapping)

            //foreach (KeyValuePair<string, Action> action in SelectedMapping.mapping)
            //{
            //    GUILayout.BeginHorizontal();
            //    //DropDown Inputs
            //    GUILayout.Label(action.Key);
            //    //UnityEvent
            //    SerializedObject sobj = new SerializedObject((Action)action.Value);
            //    SerializedProperty sprob = sobj.FindProperty("ActionEvent");
            //    EditorGUILayout.PropertyField(sprob);
            //    //GUILayout.Label("placeholder for function");
            //    GUILayout.EndHorizontal();
            //    sobj.ApplyModifiedProperties();
            //}

            GUILayout.Space(10f);
            if (GUILayout.Button("save"))
            {
                this.Close();
            }

    }

    /// <summary>
    /// Returns the class internal enum representation of the given ActionMapping type.
    /// </summary>
    /// <param name="mapping"></param>
    /// <returns></returns>
    private INPUTTYPE GetTypeForClass(ActionMapping mapping)
    {
        if(mapping.GetType() == typeof(KeyActionMapping))
        {
            return INPUTTYPE.Keys;
        }
        else if(mapping.GetType() == typeof(ViveActionMapping))
        {
            return INPUTTYPE.Vive;
        }
        else if(false)
        {
            return INPUTTYPE.Leap;
        }
        else if(false)
        {
            return INPUTTYPE.Con;
        }
        else
        {
            return INPUTTYPE.Empty;
        }
    }
}
