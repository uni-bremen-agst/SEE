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
                nodeRef.node.LinkName = EditorGUILayout.TextField("Linkage name", nodeRef.node.LinkName);
                nodeRef.node.SourceName = EditorGUILayout.TextField("Source name", nodeRef.node.SourceName);
                nodeRef.node.Type = EditorGUILayout.TextField("Type", nodeRef.node.Type);
            }
            else
            {
                GUILayout.Label("Node: NONE", EditorStyles.boldLabel);
            }
        }
    }
}