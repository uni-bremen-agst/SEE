#if UNITY_EDITOR

using SEE.GO;
using UnityEditor;
using UnityEngine;

namespace SEEEditor
{
    /// <summary>
    /// A custom editor for instances of NodeRef.
    /// </summary>
    [CustomEditor(typeof(NodeRef))]
    [CanEditMultipleObjects]
    public class NodeRefEditor : GraphElementRefEditor
    {
        public override void OnInspectorGUI()
        {
            NodeRef nodeRef = target as NodeRef;

            GUILayout.Label("Node attributes", EditorStyles.boldLabel);
            if (nodeRef.Value != null)
            {
                EditorGUILayout.TextField("Level", nodeRef.Value.Level.ToString());
                ShowTypeAndAttributes(nodeRef.Value);
            }
            else
            {
                GUILayout.Label("Node: NONE", EditorStyles.boldLabel);
            }
        }
    }
}

#endif
