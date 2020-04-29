using UnityEditor;
using SEE.GO;
using UnityEngine;

namespace SEEEditor
{
    /// <summary>
    /// A custom editor for instances of NodeRef.
    /// </summary>
    [CustomEditor(typeof(NodeRef))]
    [CanEditMultipleObjects]
    public class NodeRefEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            NodeRef nodeRef = target as NodeRef;

            GUILayout.Label("Node attributes", EditorStyles.boldLabel);
            if (nodeRef.node != null)
            {
                EditorGUILayout.TextField("ID", nodeRef.node.ID);
                EditorGUILayout.TextField("Type", nodeRef.node.Type);

                GUILayout.Label("String attributes", EditorStyles.boldLabel);
                foreach (var entry in nodeRef.node.StringAttributes)
                {
                    EditorGUILayout.TextField(entry.Key, entry.Value);
                }
                GUILayout.Label("Float attributes", EditorStyles.boldLabel);
                foreach (var entry in nodeRef.node.FloatAttributes)
                {
                    EditorGUILayout.TextField(entry.Key, entry.Value.ToString());
                }
                GUILayout.Label("Integer attributes", EditorStyles.boldLabel);
                foreach (var entry in nodeRef.node.IntAttributes)
                {
                    EditorGUILayout.TextField(entry.Key, entry.Value.ToString());
                }
                GUILayout.Label("Toggle attributes", EditorStyles.boldLabel);
                foreach (var entry in nodeRef.node.ToggleAttributes)
                {
                    EditorGUILayout.LabelField(entry);
                }
            }
            else
            {
                GUILayout.Label("Node: NONE", EditorStyles.boldLabel);
            }
        }
    }
}