﻿#if UNITY_EDITOR

using SEE.DataModel.DG;
using UnityEditor;
using UnityEngine;

namespace SEEEditor
{
    /// <summary>
    /// Common abstract superclass for NodeRef and EdgeRef editors. Handles the
    /// shared attributes.
    /// </summary>
    public abstract class GraphElementRefEditor : Editor
    {
        /// <summary>
        /// Emits the ID, Type, and all attributes of the given <paramref name="graphElement"/>
        /// in text fields of the editor.
        /// </summary>
        /// <param name="graphElement">graph elements whose attributes are to be shown</param>
        protected static void ShowTypeAndAttributes(GraphElement graphElement)
        {
            EditorGUILayout.TextField("ID", graphElement.ID);
            EditorGUILayout.TextField("Type", graphElement.Type);

            GUILayout.Label("String attributes", EditorStyles.boldLabel);
            foreach (System.Collections.Generic.KeyValuePair<string, string> entry in graphElement.StringAttributes)
            {
                EditorGUILayout.TextField(entry.Key, entry.Value);
            }
            GUILayout.Label("Float attributes", EditorStyles.boldLabel);
            foreach (System.Collections.Generic.KeyValuePair<string, float> entry in graphElement.FloatAttributes)
            {
                EditorGUILayout.TextField(entry.Key, entry.Value.ToString());
            }
            GUILayout.Label("Integer attributes", EditorStyles.boldLabel);
            foreach (System.Collections.Generic.KeyValuePair<string, int> entry in graphElement.IntAttributes)
            {
                EditorGUILayout.TextField(entry.Key, entry.Value.ToString());
            }
            GUILayout.Label("Toggle attributes", EditorStyles.boldLabel);
            foreach (string entry in graphElement.ToggleAttributes)
            {
                EditorGUILayout.LabelField(entry);
            }
        }
    }
}

#endif